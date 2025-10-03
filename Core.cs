using CineCam.Managers;
using CineCam.UI;
using MelonLoader;
using System;
using System.Reflection;
using CineCam;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.GameTime;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.GameTime;
#endif
using UnityEngine;
using HarmonyLib;

[assembly: MelonInfo(typeof(CineCam.Core), "CineCam", VersionInfo.VERSION_STRING, "Bars", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace CineCam
{
    [HarmonyPatch]
    public class PlayerCameraPatches
    {
        // Patch for player camera initialization
        [HarmonyPatch(typeof(PlayerCamera), "Awake")]
        [HarmonyPostfix]
        public static void OnPlayerCameraAwake(PlayerCamera __instance)
        {
            try
            {
                if (Core.Instance != null)
                {
                    Core.Instance.LoggerInstance.Msg("PlayerCamera Awake detected via Harmony patch!");
                    Core.Instance.OnPlayerCameraInitialized(__instance);
                    
                    // Create a helper GameObject to ensure update calls
#if MONO
                    Core.Instance.LoggerInstance.Msg("Creating UpdateHelper GameObject for Mono");
                    var updateHelper = new GameObject("CineCamUpdateHelper");
                    var helper = updateHelper.AddComponent<UpdateHelper>();
                    helper.CoreInstance = Core.Instance;
                    UnityEngine.Object.DontDestroyOnLoad(updateHelper);
                    Core.Instance.LoggerInstance.Msg("UpdateHelper created and attached");
#endif
                }
                else
                {
                    MelonLogger.Msg("PlayerCamera Awake detected but Core instance is null");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in PlayerCamera Awake patch: {ex.Message}");
            }
        }
    }

    // Helper class to ensure Update is called in Mono
    public class UpdateHelper : MonoBehaviour
    {
        public Core CoreInstance;
        
        void Update()
        {
            if (CoreInstance != null)
            {
                CoreInstance.MonoUpdate();
            }
        }
    }

    public class Core : MelonMod
    {
        public static Core Instance { get; private set; }
        public CinematicCameraManager CameraManager { get; private set; }
        public static MelonPreferences_Category CameraSettings { get; private set; }
        public static MelonPreferences_Entry<float> CameraMovementSpeed { get; private set; }
        public static MelonPreferences_Entry<bool> ShowControlsOnStartup { get; private set; }
        public static MelonPreferences_Entry<float> CameraFollowSmoothness { get; private set; }
        public static MelonPreferences_Entry<bool> ShowGridOverlay { get; private set; }
        public static MelonPreferences_Entry<float> DefaultFov { get; private set; }
        public static MelonPreferences_Entry<float> CameraZoomSpeed { get; private set; }
        public static MelonPreferences_Entry<bool> DisableNPCEyeMovement { get; private set; }
        public static MelonPreferences_Entry<bool> DisableCameraEffectsInFreeCam { get; private set; }
        private HarmonyLib.Harmony _harmony;
        private bool isTimeFrozen = false;
        private float originalTimeMultiplier;
        private bool _isWaitingForPlayer;
        private float _playerCheckTimer;
        private const float PlayerCheckInterval = 0.5f; // How often to check for player existence
        private const float PlayerCheckTimeout = 60f;
        private float _playerCheckElapsed;
        private bool _hasLoggedCameraControls;
        private EditorUI _editorUi;
        private bool _editorUiInitialized;
        private static Texture2D _gridTexture;
        private static bool _gridInitialized;
        private bool _hasLoggedMonoUpdate = false;

        public override void OnInitializeMelon()
        {
            try
            {
                Instance = this;
                InitializePreferences();

                _harmony = new HarmonyLib.Harmony("com.bars.cinecam");

                // Apply Harmony patches to disable camera effects in free-cam
                ApplyHarmonyPatches();

                _editorUi = new EditorUI(ShowControlsOnStartup.Value);
                _editorUiInitialized = true;

                LoggerInstance.Msg($"CineCam v{VersionInfo.Version} initialized.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize CineCam: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                // Apply all patches from the current assembly
                _harmony.PatchAll(typeof(Core).Assembly);
                LoggerInstance.Msg("Successfully applied all camera patches");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to apply Harmony patches: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
#if MONO
            LoggerInstance.Msg($"OnSceneWasLoaded called in Mono runtime - Scene: {sceneName}, buildIndex: {buildIndex}");
#endif
            _playerCheckTimer = 0f;
            _playerCheckElapsed = 0f;
            _editorUi.UpdateCameraStatus(CameraManager, LoggerInstance);
            LoggerInstance.Msg($"Loaded scene: {sceneName}");

            if (sceneName != "Main" && sceneName != "Tutorial") return;
            _isWaitingForPlayer = true;
            LoggerInstance.Msg("Starting to wait for player to spawn...");
#if MONO
            LoggerInstance.Msg($"_isWaitingForPlayer set to: {_isWaitingForPlayer}");
#endif
        }

        public override void OnUpdate()
        {
            try
            {
                // In IL2CPP, we use the normal OnUpdate from MelonLoader
                Services.InputManager.UpdateKeyStates();

                if (_isWaitingForPlayer)
                {
                    _playerCheckTimer += Time.deltaTime;
                    _playerCheckElapsed += Time.deltaTime;

                    if (_playerCheckTimer >= PlayerCheckInterval)
                    {
                        _playerCheckTimer = 0f;
                        CheckForPlayerSpawn();

                        if (_playerCheckElapsed > PlayerCheckTimeout)
                        {
                            LoggerInstance.Error($"Timed out waiting for player after {PlayerCheckTimeout} seconds");
                            _isWaitingForPlayer = false;
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (CameraManager is { IsActive: true })
                    {
                        CameraManager.HandleEscapeInput();
                    }
                }

                if (Input.GetKeyDown(KeyCode.F1))
                {
                    if (!_editorUiInitialized)
                    {
                        LoggerInstance.Error("EditorUI not initialized yet");
                        return;
                    }

                    _editorUi.ToggleOverallUI();

                    if (_editorUi.ShowUI)
                    {
                        if (!_editorUi.IsCreated)
                        {
                            _editorUi.CreateUI(LoggerInstance);
                        }

                        _editorUi.UpdateCameraStatus(CameraManager, LoggerInstance);
                    }
                }

                if (CameraManager == null) return;
                HandleCameraControls();
                CameraManager.OnUpdate();

                if (_editorUiInitialized && _editorUi.ShowUI && _editorUi.IsCreated)
                {
                    _editorUi.UpdateCameraStatus(CameraManager, LoggerInstance);
                }

                if (_hasLoggedCameraControls) return;
                LoggerInstance.Msg("Camera controls are now active:");
                LoggerInstance.Msg("F1 - Open camera editor UI");
                _hasLoggedCameraControls = true;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in OnUpdate: {ex.Message}");
            }
        }

        public override void OnGUI()
        {
            // Draw grid overlay if enabled
            if (ShowGridOverlay.Value && CameraManager != null && CameraManager.IsActive)
            {
                DrawGridOverlay();
            }
        }
        
#if MONO
        // Critical key handlers for Mono
        private float _lastKeyCheck = 0f;
        private void HandleKeyboardInputInGUI()
        {
            // Only check every 0.1 seconds to avoid excessive checks
            if (Time.realtimeSinceStartup - _lastKeyCheck < 0.1f)
                return;
                
            _lastKeyCheck = Time.realtimeSinceStartup;
            
            // F1 - Toggle UI
            if (Input.GetKey(KeyCode.F1))
            {
                if (_editorUiInitialized)
                {
                    LoggerInstance.Msg("F1 detected in OnGUI");
                    _editorUi.ToggleOverallUI();
                    
                    if (_editorUi.ShowUI && !_editorUi.IsCreated)
                    {
                        _editorUi.CreateUI(LoggerInstance);
                    }
                    
                    if (_editorUi.ShowUI)
                    {
                        _editorUi.UpdateCameraStatus(CameraManager, LoggerInstance);
                    }
                }
            }
            
            // F2 - Toggle free camera
            if (Input.GetKey(KeyCode.F2) && CameraManager != null)
            {
                LoggerInstance.Msg("F2 detected in OnGUI");
                if (CameraManager.IsPlayerControlMode) CameraManager.TogglePlayerControl();
                CameraManager.SetCineCamFreeCam(!CameraManager.IsActive);
            }
            
            // Other critical keys
            if (CameraManager != null && CameraManager.IsActive)
            {
                // F5 - Toggle camera lock
                if (Input.GetKey(KeyCode.F5) && !CameraManager.IsPlayerControlMode)
                {
                    LoggerInstance.Msg("F5 detected in OnGUI");
                    CameraManager.ToggleCameraLock();
                }
                
                // F6 - Toggle player control
                if (Input.GetKey(KeyCode.F6))
                {
                    LoggerInstance.Msg("F6 detected in OnGUI");
                    CameraManager.TogglePlayerControl();
                }
            }
        }
#endif

        private void DrawGridOverlay()
        {
            if (!_gridInitialized)
            {
                InitializeGridTexture();
            }

            if (_gridTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _gridTexture, ScaleMode.StretchToFill);
            }
        }

        private void InitializeGridTexture()
        {
            try
            {
                int size = 512;
                _gridTexture = new Texture2D(size, size);
                _gridTexture.filterMode = FilterMode.Bilinear;

                Color[] colors = new Color[size * size];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(1, 1, 1, 0);
                }

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        if (y == size / 3 || y == 2 * size / 3)
                        {
                            colors[y * size + x] = new Color(1, 1, 1, 0.5f);
                        }
                        else if (x == size / 3 || x == 2 * size / 3)
                        {
                            colors[y * size + x] = new Color(1, 1, 1, 0.5f);
                        }
                    }
                }

                _gridTexture.SetPixels(colors);
                _gridTexture.Apply();
                _gridInitialized = true;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize grid texture: {ex.Message}");
            }
        }

        private void CheckForPlayerSpawn()
        {
#if MONO
            LoggerInstance.Msg($"CheckForPlayerSpawn - PlayerSingleton<PlayerCamera>.InstanceExists: {PlayerSingleton<PlayerCamera>.InstanceExists}");
#endif
            if (!PlayerSingleton<PlayerCamera>.InstanceExists) return;
            
            PlayerCamera playerCamera = PlayerSingleton<PlayerCamera>.Instance;
#if MONO
            LoggerInstance.Msg($"CheckForPlayerSpawn - playerCamera: {(playerCamera != null ? "not null" : "null")}");
            if (playerCamera != null)
            {
                LoggerInstance.Msg($"CheckForPlayerSpawn - playerCamera.transform: {(playerCamera.transform != null ? "not null" : "null")}");
                LoggerInstance.Msg($"CheckForPlayerSpawn - playerCamera.Camera: {(playerCamera.Camera != null ? "not null" : "null")}");
            }
#endif
            if (playerCamera == null || playerCamera.transform == null || playerCamera.Camera == null) return;
            try
            {
                if (DefaultFov.Value <= 0)
                {
                    DefaultFov.Value = playerCamera.Camera.fieldOfView;
                    MelonPreferences.Save();
                }

                float currentFov = playerCamera.Camera.fieldOfView;
                if (currentFov > 0 && (DefaultFov.Value <= 0 || DefaultFov.Value > 120))
                {
                    DefaultFov.Value = currentFov;
                    MelonPreferences.Save();
                }

                CameraManager = new CinematicCameraManager(playerCamera);
                LoggerInstance.Msg("Cinematic camera system ready");

                if (_editorUiInitialized && _editorUi.ShowUI && !_editorUi.IsCreated)
                {
                    _editorUi.CreateUI(LoggerInstance);
                }

                _isWaitingForPlayer = false;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to create CameraManager: {ex.Message}");
                _isWaitingForPlayer = false;
            }
        }

        void ToggleTimeFreeze()
        {
            if (!NetworkSingleton<TimeManager>.InstanceExists)
                return;

            TimeManager timeManager = NetworkSingleton<TimeManager>.Instance;

            if (!isTimeFrozen)
            {
                // Freeze time
                originalTimeMultiplier = timeManager.TimeProgressionMultiplier;
                timeManager.TimeProgressionMultiplier = 0f;
                isTimeFrozen = true;
                LoggerInstance.Msg("Time frozen");
            }
            else
            {
                // Unfreeze time
                timeManager.TimeProgressionMultiplier = originalTimeMultiplier;
                isTimeFrozen = false;
                LoggerInstance.Msg("Time unfrozen");
            }
        }

        private void HandleCameraControls()
        {
            if (CameraManager == null)
                return;


            if (CameraManager.IsPlayerControlMode && Input.GetKeyDown(KeyCode.E))
            {
                CameraManager.HandlePlayerInteraction();
            }


            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (CameraManager.IsPlayerControlMode) CameraManager.TogglePlayerControl();
                CameraManager.SetCineCamFreeCam(!CameraManager.IsActive);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleTimeFreeze();
            }

            if (CameraManager.IsActive)
            {
                if (Input.GetKeyDown(KeyCode.F5) && !CameraManager.IsPlayerControlMode)
                {
                    CameraManager.ToggleCameraLock();
                }

                if (Input.GetKeyDown(KeyCode.F6))
                {
                    CameraManager.TogglePlayerControl();
                }

                if (Input.GetKeyDown(KeyCode.F7))
                {
                    CameraManager.ToggleCameraFollow();
                }

                if (Input.GetKeyDown(KeyCode.G))
                {
                    ShowGridOverlay.Value = !ShowGridOverlay.Value;
                }

                if (Input.GetKey(KeyCode.PageUp))
                {
                    CameraManager.ZoomCamera(CameraZoomSpeed.Value);
                }
                else if (Input.GetKey(KeyCode.PageDown))
                {
                    CameraManager.ZoomCamera(-CameraZoomSpeed.Value);
                }

                if (CameraManager.IsFreeCamEnabled && !CameraManager.IsCameraLocked)
                {
                    CameraManager.SetPrecisionMode(Input.GetKey(KeyCode.LeftAlt));
                }
            }

            if (!CameraManager.IsFreeCamEnabled || CameraManager.IsCameraLocked) return;
            if (Input.GetKey(KeyCode.Q))
            {
                CameraManager.MoveCamera(Vector3.down / 4);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                CameraManager.MoveCamera(Vector3.up / 4);
            }
        }

        private void InitializePreferences()
        {
            CameraSettings = MelonPreferences.CreateCategory("CineCam");
            CameraMovementSpeed = CameraSettings.CreateEntry("CameraMovementSpeed", 10.0f, "Camera Movement Speed", "Controls how fast the camera moves in free cam mode");
            ShowControlsOnStartup = CameraSettings.CreateEntry("ShowControlsOnStartup", true, "Show Controls on Startup", "Show the controls help window when the game starts");
            ShowGridOverlay = CameraSettings.CreateEntry("ShowGridOverlay", false, "Show Grid Overlay", "Show a grid overlay on the screen for composition");
            DefaultFov = CameraSettings.CreateEntry("DefaultFOV", 60.0f, "Default FOV", "The default field of view for the camera");
            CameraZoomSpeed = CameraSettings.CreateEntry("CameraZoomSpeed", 1.0f, "Camera Zoom Speed", "Controls how fast the camera zooms in and out");
            DisableNPCEyeMovement = CameraSettings.CreateEntry("DisableNPCEyeMovement", true, "Disable NPC Eye Movement", "When enabled, NPCs won't look at the player when free cam is active, preventing NPCs from breaking the fourth wall in your shots");
            CameraFollowSmoothness = CameraSettings.CreateEntry("CameraFollowSmoothness", 0.1f, "Camera Follow Smoothness", "Controls how smoothly the camera follows the player in camera follow mode (lower = faster)");
            DisableCameraEffectsInFreeCam = CameraSettings.CreateEntry("DisableCameraEffectsInFreeCam", true, "Disable Camera Effects in Free-Cam", "When enabled, disables camera bob and sprint FOV changes while in free-cam mode");
        }

        // New method to handle player camera initialization detected by Harmony patch
        public void OnPlayerCameraInitialized(PlayerCamera playerCamera)
        {
            try
            {
                LoggerInstance.Msg("PlayerCamera initialized event received!");
                
                if (playerCamera == null)
                {
                    LoggerInstance.Error("PlayerCamera instance is null");
                    return;
                }

                LoggerInstance.Msg($"PlayerCamera transform: {(playerCamera.transform != null ? "not null" : "null")}");
                LoggerInstance.Msg($"PlayerCamera Camera: {(playerCamera.Camera != null ? "not null" : "null")}");

                if (playerCamera.transform == null || playerCamera.Camera == null) return;

                // Initialize camera settings
                if (DefaultFov.Value <= 0)
                {
                    DefaultFov.Value = playerCamera.Camera.fieldOfView;
                    MelonPreferences.Save();
                }

                float currentFov = playerCamera.Camera.fieldOfView;
                if (currentFov > 0 && (DefaultFov.Value <= 0 || DefaultFov.Value > 120))
                {
                    DefaultFov.Value = currentFov;
                    MelonPreferences.Save();
                }

                // Create camera manager
                CameraManager = new CinematicCameraManager(playerCamera);
                LoggerInstance.Msg("Cinematic camera system ready via event hook");

                if (_editorUiInitialized && _editorUi.ShowUI && !_editorUi.IsCreated)
                {
                    _editorUi.CreateUI(LoggerInstance);
                }

                _isWaitingForPlayer = false;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize camera from event: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        // New method for Mono to call from UpdateHelper
        public void MonoUpdate()
        {
            try
            {
#if MONO
                // Only log this once to avoid spam
                if (!_hasLoggedMonoUpdate)
                {
                    LoggerInstance.Msg("MonoUpdate called via UpdateHelper");
                    _hasLoggedMonoUpdate = true;
                }
#endif
                
                // Handle input
                if (Input.GetKeyDown(KeyCode.Escape) && CameraManager != null && CameraManager.IsActive)
                {
                    CameraManager.HandleEscapeInput();
                }

                if (Input.GetKeyDown(KeyCode.F1))
                {
                    if (!_editorUiInitialized)
                    {
                        LoggerInstance.Error("EditorUI not initialized yet");
                        return;
                    }

                    _editorUi.ToggleOverallUI();
                    LoggerInstance.Msg($"UI toggled: {_editorUi.ShowUI}");

                    if (_editorUi.ShowUI)
                    {
                        if (!_editorUi.IsCreated)
                        {
                            _editorUi.CreateUI(LoggerInstance);
                        }

                        _editorUi.UpdateCameraStatus(CameraManager, LoggerInstance);
                    }
                }

                if (CameraManager == null) return;
                HandleCameraControls();
                CameraManager.OnUpdate();

                if (_editorUiInitialized && _editorUi.ShowUI && _editorUi.IsCreated)
                {
                    _editorUi.UpdateCameraStatus(CameraManager, LoggerInstance);
                }

                if (!_hasLoggedCameraControls) 
                {
                    LoggerInstance.Msg("Camera controls are now active:");
                    LoggerInstance.Msg("F1 - Open camera editor UI");
                    LoggerInstance.Msg("F2 - Toggle free camera mode");
                    LoggerInstance.Msg("F5 - Toggle camera lock");
                    LoggerInstance.Msg("F6 - Toggle player control");
                    LoggerInstance.Msg("F7 - Toggle camera follow");
                    LoggerInstance.Msg("F8 - Toggle time freeze");
                    LoggerInstance.Msg("G - Toggle grid overlay");
                    LoggerInstance.Msg("PageUp/PageDown - Zoom camera");
                    _hasLoggedCameraControls = true;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in MonoUpdate: {ex.Message}");
            }
        }
    }
}