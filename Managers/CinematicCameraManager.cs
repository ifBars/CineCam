using CineCam.Models;
using CineCam.Services;
using CineCam.UI;
using CineCam.Patches; // Add this for CinematicCameraIntegration
using MelonLoader;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.Skating;
using ScheduleOne.Interaction;
using ScheduleOne.Vehicles;
using ScheduleOne.Doors;
using ScheduleOne.AvatarFramework.Animation;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Skating;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.Doors;
using Il2CppScheduleOne.AvatarFramework.Animation;
#endif
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace CineCam.Managers
{
    /// <summary>
    /// Defines the different camera follow modes
    /// </summary>
    [Flags]
    public enum CameraFollowMode
    {
        None = 0,
        Position = 1,  // Camera follows relative position to target
        Rotation = 2   // Camera rotates to look at target
    }

    public class CinematicCameraManager
    {
        // Constants
        private const float MIN_FOV = 10f;
        private const float PRECISION_MODE_MULTIPLIER = 0.3f; // 30% of normal speed for precision mode

        // References
        public readonly PlayerCamera playerCamera;

        // Camera state
        private Vector3 lockedCameraPosition;
        private Quaternion lockedCameraRotation;
        private bool cameraLocked = false;
        private bool playerControlMode = false;
        private float initialFov;
        private object sequenceCoroutine;
        private bool inSequence = false;
        private CameraFollowMode currentFollowMode = CameraFollowMode.None;
        
        // Enhanced follow mode tracking
        private Transform followTarget; // The transform we're following
        private Vector3 localPositionOffset; // Position offset in target's local space
        private Vector3 worldPositionOffset; // Fallback world space offset
        private bool useLocalSpaceOffset = true; // Whether to use local or world space offset
        
        // Camera follow mode preferences (local to manager, not saved)
        private bool cameraFollowPositionEnabled = true;
        private bool cameraFollowRotationEnabled = true;
        
        private bool isPrecisionModeActive = false;
        private float normalCameraSpeed = 10.0f;
        // Store the original camera mode to restore it later
        private PlayerCamera.ECameraMode originalCameraMode;
        // References to vehicle/skateboard camera components that need to be disabled
#if MONO
        private VehicleCamera vehicleCameraComponent;
        private SkateboardCamera skateboardCameraComponent;
#else
        private Il2CppScheduleOne.Vehicles.VehicleCamera vehicleCameraComponent;
        private Il2CppScheduleOne.Skating.SkateboardCamera skateboardCameraComponent;
#endif
        private bool originalVehicleCameraState;
        private bool originalSkateboardCameraState;
        // References to vehicle/skateboard components to control input
#if MONO
        private LandVehicle activeVehicle;
        private Skateboard activeSkateboard;
#else
        private Il2CppScheduleOne.Vehicles.LandVehicle activeVehicle;
        private Il2CppScheduleOne.Skating.Skateboard activeSkateboard;
#endif
        // Store original input state
        private bool originalVehicleDriverState;
        private bool originalSkateboardCanMove;
        // Track if we've found and disabled vehicle/skateboard components
        private bool vehicleComponentsFound = false;
        private bool skateboardComponentsFound = false;
        // Vehicle HUD reference
        private GameObject vehicleHUD;
        private GameObject vehicleCanvas;

        // Shot management
        private List<CameraShot> shotSequence = new List<CameraShot>();
        private int currentShotIndex = 0;

        // NPC eye movement control
        private Dictionary<AvatarLookController, bool> avatarLookControllers = new Dictionary<AvatarLookController, bool>();
        private bool avatarLookControllersDisabled = false;
        private float npcCheckInterval = 5.0f; // Check for new NPCs every 5 seconds
        private float lastNpcCheckTime = 0f;

        // Properties
        public bool IsActive => cameraLocked || playerCamera.FreeCamEnabled || playerControlMode;
        public bool IsCameraLocked => cameraLocked;
        public bool IsPlayerControlMode => playerControlMode;
        public bool IsFreeCamEnabled => playerCamera.FreeCamEnabled && !playerControlMode;
        public bool InSequence => inSequence;
        public bool IsCameraFollowModeActive => currentFollowMode != CameraFollowMode.None;
        public bool IsPositionFollowActive => (currentFollowMode & CameraFollowMode.Position) == CameraFollowMode.Position;
        public bool IsRotationFollowActive => (currentFollowMode & CameraFollowMode.Rotation) == CameraFollowMode.Rotation;
        public CameraFollowMode CurrentFollowMode => currentFollowMode;
        public bool CameraFollowPositionEnabled 
        { 
            get => cameraFollowPositionEnabled; 
            set => cameraFollowPositionEnabled = value; 
        }
        public bool CameraFollowRotationEnabled 
        { 
            get => cameraFollowRotationEnabled; 
            set => cameraFollowRotationEnabled = value; 
        }
        public float CurrentFOV => playerCamera.Camera.fieldOfView;
        public float FreeCamSpeed
        {
            get => playerCamera.FreeCamSpeed;
            set
            {
                if (playerCamera != null && IsFreeCamEnabled && !cameraLocked)
                {
                    playerCamera.FreeCamSpeed = value;
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the cinematic camera controller
        /// 
        /// Features:
        /// - Camera bob and sprint FOV changes are automatically disabled when in free-cam mode
        /// - Viewmodel sway effects are suppressed for cleaner cinematic shots
        /// - All effects can be controlled via the "Disable Camera Effects in Free-Cam" preference
        /// </summary>
        public CinematicCameraManager(PlayerCamera playerCamera)
        {
            this.playerCamera = playerCamera;
            initialFov = playerCamera.Camera.fieldOfView;
            originalCameraMode = playerCamera.CameraMode;

            // Initialize the camera shot manager
            CameraShotManager.Initialize();

            // Initialize the input manager
            // InputManager.Initialize();

            Core.Instance.LoggerInstance.Msg("Cinematic camera manager initialized.");
        }

        /// <summary>
        /// Process input and update the camera state
        /// </summary>
        public void OnUpdate()
        {
            // Only process input if we're in the game and UI isn't active
            // Check for focused text input fields to determine if we're typing
            bool isTyping = false;
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
            {
                var inputField = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>();
                isTyping = inputField != null && inputField.isFocused;
            }

            if (isTyping)
                return;

            // Check for new NPCs if free cam is enabled
            if (playerCamera.FreeCamEnabled && avatarLookControllersDisabled)
            {
                if (Time.time - lastNpcCheckTime >= npcCheckInterval)
                {
                    CheckForNewNpcs();
                    lastNpcCheckTime = Time.time;
                }
            }

            // New camera follow logic - handles both position and rotation follow modes
            if (currentFollowMode != CameraFollowMode.None && playerControlMode && playerCamera != null && playerCamera.transform != null)
            {
                if (followTarget != null)
                {
                    try
                    {
                        float smoothness = (Core.Instance != null && Core.CameraFollowSmoothness != null && Core.CameraFollowSmoothness.Value > 0.001f)
                                         ? Core.CameraFollowSmoothness.Value
                                         : 0.1f; // Default smoothness

                        // Handle position following
                        if ((currentFollowMode & CameraFollowMode.Position) == CameraFollowMode.Position)
                        {
                            Vector3 targetCameraPosition;
                            
                            if (useLocalSpaceOffset && followTarget != null)
                            {
                                // Transform the local offset to world space using the target's current transform
                                targetCameraPosition = followTarget.TransformPoint(localPositionOffset);
                            }
                            else
                            {
                                // Fallback to world space offset
                                targetCameraPosition = followTarget.position + worldPositionOffset;
                            }
                            
                            playerCamera.transform.position = Vector3.Lerp(
                                playerCamera.transform.position,
                                targetCameraPosition,
                                Time.deltaTime / smoothness
                            );

                            // Update locked position for when position following is disabled
                            lockedCameraPosition = playerCamera.transform.position;
                        }
                        else if (cameraLocked)
                        {
                            // If position following is disabled but camera is locked, maintain locked position
                            playerCamera.transform.position = lockedCameraPosition;
                        }

                        // Handle rotation following
                        if ((currentFollowMode & CameraFollowMode.Rotation) == CameraFollowMode.Rotation)
                        {
                            Vector3 directionToTarget = followTarget.position - playerCamera.transform.position;
                            if (directionToTarget != Vector3.zero)
                            {
                                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                                
                                playerCamera.transform.rotation = Quaternion.Slerp(
                                    playerCamera.transform.rotation,
                                    targetRotation,
                                    Time.deltaTime / smoothness
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        currentFollowMode = CameraFollowMode.None; // Disable on error
                        Core.Instance.LoggerInstance.Error($"Error in camera follow mode: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else if (Player.Local != null)
                {
                    // Fallback to old behavior if followTarget is null but Player.Local exists
                    try
                    {
                        Vector3 targetPosition;
                        if (Player.Local.CurrentVehicle != null)
                        {
                            targetPosition = Player.Local.CurrentVehicle.transform.position;
                            followTarget = Player.Local.CurrentVehicle.transform; // Update follow target
                        }
                        else
                        {
                            targetPosition = Player.Local.transform.position;
                            followTarget = Player.Local.transform; // Update follow target
                        }

                        float smoothness = (Core.Instance != null && Core.CameraFollowSmoothness != null && Core.CameraFollowSmoothness.Value > 0.001f)
                                         ? Core.CameraFollowSmoothness.Value
                                         : 0.1f;

                        if ((currentFollowMode & CameraFollowMode.Position) == CameraFollowMode.Position)
                        {
                            Vector3 targetCameraPosition = targetPosition + worldPositionOffset;
                            playerCamera.transform.position = Vector3.Lerp(
                                playerCamera.transform.position,
                                targetCameraPosition,
                                Time.deltaTime / smoothness
                            );
                            lockedCameraPosition = playerCamera.transform.position;
                        }

                        if ((currentFollowMode & CameraFollowMode.Rotation) == CameraFollowMode.Rotation)
                        {
                            if (cameraLocked && (currentFollowMode & CameraFollowMode.Position) != CameraFollowMode.Position)
                            {
                                playerCamera.transform.position = lockedCameraPosition;
                            }

                            Vector3 directionToTarget = targetPosition - playerCamera.transform.position;
                            if (directionToTarget != Vector3.zero)
                            {
                                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                                playerCamera.transform.rotation = Quaternion.Slerp(
                                    playerCamera.transform.rotation,
                                    targetRotation,
                                    Time.deltaTime / smoothness
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        currentFollowMode = CameraFollowMode.None;
                        Core.Instance.LoggerInstance.Error($"Error in camera follow mode fallback: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    // If both followTarget and Player.Local are null, disable follow mode
                    if (currentFollowMode != CameraFollowMode.None)
                    {
                        Core.Instance.LoggerInstance.Warning("Camera follow mode: Target references are null. Disabling follow.");
                        currentFollowMode = CameraFollowMode.None;
                        followTarget = null;
                        if (playerControlMode && playerCamera != null && cameraLocked)
                        {
                            playerCamera.transform.rotation = lockedCameraRotation;
                        }
                    }
                }
            }

            // Check for CinematicCameraIntegration changes
            CinematicCameraIntegration.CheckCinematicStateChange();
        }

        /// <summary>
        /// Find and store references to vehicle/skateboard components
        /// </summary>
        private void FindCameraComponents()
        {
            // Reset state tracking
            vehicleComponentsFound = false;
            skateboardComponentsFound = false;

            // Clear previous references
            activeVehicle = null;
            activeSkateboard = null;
            vehicleCameraComponent = null;
            skateboardCameraComponent = null;
            vehicleHUD = null;
            vehicleCanvas = null;

            // Check if player is null
            if (Player.Local == null)
            {
                Core.Instance.LoggerInstance.Warning("FindCameraComponents: Player.Local is null");
                return;
            }

            // Check if player is in a vehicle
            if (Player.Local.CurrentVehicle != null)
            {
                // Convert NetworkObject to LandVehicle
                activeVehicle = Player.Local.CurrentVehicle.GetComponent<LandVehicle>();
                if (activeVehicle == null)
                {
                    Core.Instance.LoggerInstance.Error("Could not get LandVehicle component from CurrentVehicle");
                    return;
                }

                // Find the vehicle camera component
                vehicleCameraComponent = activeVehicle.GetComponent<VehicleCamera>();
                if (vehicleCameraComponent != null)
                {
                    originalVehicleCameraState = vehicleCameraComponent.enabled;
                    vehicleComponentsFound = true;
                }

                // Store original driver state
                originalVehicleDriverState = activeVehicle.localPlayerIsDriver;

                // Find vehicle HUD/canvas if they exist
                if (activeVehicle.transform.Find("Canvas") != null)
                {
                    vehicleCanvas = activeVehicle.transform.Find("Canvas").gameObject;
                }

                // Look for VehicleCanvas in the scene
                var vehicleCanvasObj = UnityEngine.Object.FindObjectOfType<VehicleCanvas>();
                if (vehicleCanvasObj != null)
                {
                    vehicleHUD = vehicleCanvasObj.gameObject;
                }
            }

            // Check if player is on a skateboard
            if (Player.Local.IsSkating)
            {
                // Try to find the skateboard being used by the player
                Skateboard[] skateboards = UnityEngine.Object.FindObjectsOfType<Skateboard>();
                foreach (var skateboard in skateboards)
                {
                    if (skateboard.Rider == Player.Local)
                    {
                        // Found the active skateboard
                        activeSkateboard = skateboard;
                        skateboardCameraComponent = skateboard.GetComponent<SkateboardCamera>();
                        if (skateboardCameraComponent != null)
                        {
                            originalSkateboardCameraState = skateboardCameraComponent.enabled;
                            skateboardComponentsFound = true;
                        }
                        // Store original state of movement input
                        originalSkateboardCanMove = skateboard.enabled;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Disable vehicle/skateboard camera components
        /// </summary>
        private void DisableCameraComponents()
        {
            if (vehicleCameraComponent != null)
            {
                vehicleCameraComponent.enabled = false;
            }

            if (skateboardCameraComponent != null)
            {
                skateboardCameraComponent.enabled = false;
            }
        }

        /// <summary>
        /// Restore vehicle/skateboard camera components to their original state
        /// </summary>
        private void RestoreCameraComponents()
        {
            if (vehicleCameraComponent != null && vehicleComponentsFound)
            {
                vehicleCameraComponent.enabled = originalVehicleCameraState;
            }

            if (skateboardCameraComponent != null && skateboardComponentsFound)
            {
                skateboardCameraComponent.enabled = originalSkateboardCameraState;
            }
        }

        public void HandlePlayerInteraction()
        {
            if (!playerControlMode)
            {
                return;
            }

            if (Player.Local == null)
            {
                return;
            }

            // Cast a ray from player position (at eye level) in the direction they're facing
            Vector3 eyePosition = Player.Local.transform.position + new Vector3(0, 0, 0);
            Vector3 forward = Player.Local.transform.forward;

            float interactDistance = 3.0f;

            RaycastHit hit;
            if (Physics.Raycast(eyePosition, forward, out hit, interactDistance))
            {
                // APPROACH 1: Try to find a DoorController directly
                DoorController doorController = hit.collider.GetComponent<DoorController>();
                if (doorController == null)
                {
                    doorController = hit.collider.GetComponentInParent<DoorController>();
                }
                if (doorController == null)
                {
                    doorController = hit.collider.GetComponentInChildren<DoorController>();
                }

                if (doorController != null)
                {
                    Debug.Log($"[CinematicCameraManager] Found DoorController on {doorController.gameObject.name}, door is {(doorController.IsOpen ? "open" : "closed")}");
                    if (!doorController.IsOpen)
                    {
                        doorController.InteriorHandleInteracted();
                    }
                    else
                    {
                        doorController.ExteriorHandleInteracted();
                    }
                    return;
                }

                // APPROACH 2: Find the door through a DoorSensor
                DoorSensor doorSensor = hit.collider.GetComponent<DoorSensor>();
                if (doorSensor == null)
                {
                    doorSensor = hit.collider.GetComponentInParent<DoorSensor>();
                }
                if (doorSensor == null)
                {
                    doorSensor = hit.collider.GetComponentInChildren<DoorSensor>();
                }

                if (doorSensor != null && doorSensor.Door != null)
                {
                    Debug.Log($"[CinematicCameraManager] Found Door through DoorSensor on {doorSensor.gameObject.name}, door is {(doorSensor.Door.IsOpen ? "open" : "closed")}");
                    if (!doorSensor.Door.IsOpen)
                    {
                        doorSensor.Door.InteriorHandleInteracted();
                    }
                    else
                    {
                        doorSensor.Door.ExteriorHandleInteracted();
                    }
                    return;
                }

                // APPROACH 3: Look for InteractableObjects like before as a fallback
                InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInParent<InteractableObject>();
                }
                if (interactable == null)
                {
                    InteractableObject[] childInteractables = hit.collider.GetComponentsInChildren<InteractableObject>();
                    if (childInteractables != null && childInteractables.Length > 0)
                    {
                        interactable = childInteractables[0];
                    }
                }

                if (interactable != null)
                {
                    // First hover to show the UI
                    // interactable.Hovered();
                    // Trigger the interaction
                    interactable.StartInteract();
                }
            }
        }

        private string GetObjectPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// Helper method to fully stop a vehicle by zeroing velocity and resetting speed values
        /// </summary>
        private void StopVehicle(LandVehicle vehicle)
        {
            if (vehicle == null || vehicle.Rb == null)
                return;

            try
            {
                // Set velocity to zero to fully stop the vehicle
                vehicle.Rb.velocity = Vector3.zero;
                vehicle.Rb.angularVelocity = Vector3.zero;

                // Force the vehicle to apply handbrake to prevent it from moving
                vehicle.ApplyHandbrake();

                // Try to force reset the speed properties through reflection
                FieldInfo previousSpeedsField = typeof(LandVehicle).GetField("previousSpeeds",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (previousSpeedsField != null)
                {
                    List<float> zeroSpeedList = new List<float>();
                    for (int i = 0; i < 20; i++) // Fill with enough zeros to affect the average
                    {
                        zeroSpeedList.Add(0f);
                    }
                    previousSpeedsField.SetValue(vehicle, zeroSpeedList);
                }
            }
            catch (Exception ex)
            {
                Core.Instance.LoggerInstance.Error($"Error stopping vehicle: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable vehicle/skateboard input
        /// </summary>
        private void DisableVehicleSkateboardInput()
        {
            // Only attempt to disable vehicle input if we found a vehicle
            if (activeVehicle != null && vehicleComponentsFound)
            {
                // Use reflection to access the property (don't try direct access)
                PropertyInfo driverProperty = typeof(LandVehicle).GetProperty("localPlayerIsDriver",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public);

                PropertyInfo isInVehicleProperty = typeof(LandVehicle).GetProperty("localPlayerIsInVehicle",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (driverProperty != null && isInVehicleProperty != null)
                {
                    try
                    {
                        // Set via property's SetMethod directly
                        var setMethod = driverProperty.GetSetMethod(true);
                        var setMethod2 = isInVehicleProperty.GetSetMethod(true);
                        if (setMethod != null)
                        {
                            setMethod.Invoke(activeVehicle, new object[] { false });
                        }
                        if (setMethod2 != null)
                        {
                            setMethod2.Invoke(activeVehicle, new object[] { false });
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Instance.LoggerInstance.Error($"Error setting property: {ex.Message}");
                    }
                }

                // Additional approach: Disable the vehicle camera completely
                if (vehicleCameraComponent != null)
                {
                    vehicleCameraComponent.enabled = false;
                }

                // Handle throttle and steering directly to prevent movement
                try
                {
                    // Block throttle
                    PropertyInfo throttleProperty = typeof(LandVehicle).GetProperty("currentThrottle");
                    if (throttleProperty != null && throttleProperty.CanWrite)
                    {
                        throttleProperty.SetValue(activeVehicle, 0f);
                    }
                    else
                    {
                        // Try throttle as a field
                        FieldInfo throttleField = typeof(LandVehicle).GetField("currentThrottle",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public);

                        if (throttleField != null)
                        {
                            throttleField.SetValue(activeVehicle, 0f);
                        }
                    }

                    // Set brakes applied
                    PropertyInfo brakesProperty = typeof(LandVehicle).GetProperty("brakesApplied");
                    if (brakesProperty != null && brakesProperty.CanWrite)
                    {
                        brakesProperty.SetValue(activeVehicle, true);
                    }

                    // Fully stop the vehicle
                    StopVehicle(activeVehicle);
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    Core.Instance.LoggerInstance.Error($"Failed to block vehicle input: {ex.Message}");
                }
            }

            // Only attempt to disable skateboard input if we found a skateboard
            if (activeSkateboard != null && skateboardComponentsFound)
            {
                activeSkateboard.enabled = false;

                // Additional approach: Disable skateboard camera if it exists
                if (skateboardCameraComponent != null)
                {
                    skateboardCameraComponent.enabled = false;
                }
            }
        }

        /// <summary>
        /// Restore vehicle/skateboard input to original state
        /// </summary>
        private void RestoreVehicleSkateboardInput()
        {
            // Only attempt to restore vehicle input if we found and disabled a vehicle
            if (activeVehicle != null && vehicleComponentsFound)
            {
                // Use reflection to access the property (don't try direct access)
                PropertyInfo driverProperty = typeof(LandVehicle).GetProperty("localPlayerIsDriver",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public);

                PropertyInfo isInVehicleProperty = typeof(LandVehicle).GetProperty("localPlayerIsInVehicle",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (driverProperty != null && isInVehicleProperty != null)
                {
                    try
                    {
                        // Set via property's SetMethod directly
                        var setMethod = driverProperty.GetSetMethod(true);
                        var setMethod2 = isInVehicleProperty.GetSetMethod(true);
                        if (setMethod != null)
                        {
                            setMethod.Invoke(activeVehicle, new object[] { originalVehicleDriverState });
                        }
                        if (setMethod2 != null)
                        {
                            setMethod2.Invoke(activeVehicle, new object[] { originalVehicleDriverState });
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Instance.LoggerInstance.Error($"Error setting property: {ex.Message}");
                    }
                }
            }

            // Only attempt to restore skateboard input if we found and disabled a skateboard
            if (activeSkateboard != null && skateboardComponentsFound)
            {
                activeSkateboard.enabled = originalSkateboardCanMove;
            }
        }

        /// <summary>
        /// Disable vehicle HUD components
        /// </summary>
        private void DisableVehicleHUD()
        {
            // Hide vehicle HUD if it exists
            if (vehicleCanvas != null)
            {
                vehicleCanvas.SetActive(false);
            }

            if (vehicleHUD != null)
            {
                vehicleHUD.SetActive(false);
            }
        }

        /// <summary>
        /// Restore vehicle HUD components
        /// </summary>
        private void RestoreVehicleHUD()
        {
            // Only restore if we have valid references
            if (vehicleCanvas != null)
            {
                vehicleCanvas.SetActive(true);
            }

            if (vehicleHUD != null)
            {
                vehicleHUD.SetActive(true);
            }
        }

        /// <summary>
        /// Set free camera mode with enhanced control over player visibility
        /// </summary>
        public void SetCineCamFreeCam(bool enable, bool showPlayer = true)
        {
            if (enable)
            {
                // Find vehicle/skateboard camera components
                FindCameraComponents();

                // Store current camera mode and FOV before entering free cam mode
                originalCameraMode = playerCamera.CameraMode;
                initialFov = playerCamera.Camera.fieldOfView;

                // Disable vehicle/skateboard camera components
                DisableCameraComponents();

                // Disable vehicle/skateboard input when in free cam mode
                DisableVehicleSkateboardInput();

                // Always disable vehicle HUD in free cam mode
                DisableVehicleHUD();

                // Stop vehicle completely when entering cinematic camera mode
                if (activeVehicle != null)
                {
                    StopVehicle(activeVehicle);
                }

                // Special handling for vehicle and skateboard modes
                if (originalCameraMode == PlayerCamera.ECameraMode.Vehicle ||
                    originalCameraMode == PlayerCamera.ECameraMode.Skateboard)
                {
                    // Ensure we don't break the vehicle/skateboard camera setup
                    playerCamera.blockNextStopTransformOverride = true;

                    // Store the current position/rotation before enabling freecam
                    // This helps maintain the vehicle/skateboard camera position
                    lockedCameraPosition = playerCamera.transform.position;
                    lockedCameraRotation = playerCamera.transform.rotation;
                }

                // Disable NPC eye movement when free cam is enabled
                DisableNpcEyeMovement();

                playerCamera.SetFreeCam(enable);

                // For vehicle/skateboard modes, restore the position after enabling freecam
                if (originalCameraMode == PlayerCamera.ECameraMode.Vehicle ||
                    originalCameraMode == PlayerCamera.ECameraMode.Skateboard)
                {
                    playerCamera.transform.position = lockedCameraPosition;
                    playerCamera.transform.rotation = lockedCameraRotation;
                }

                if (showPlayer)
                {
                    Player.Local.SetVisibleToLocalPlayer(true);
                }
                else
                {
                    Player.Local.SetVisibleToLocalPlayer(false);
                }
                Singleton<HUD>.Instance.canvas.enabled = false;
            }
            else
            {
                playerCamera.SetFreeCam(enable);

                // Restore original camera mode when exiting
                playerCamera.SetCameraMode(originalCameraMode);

                // Restore vehicle/skateboard camera components only if we found them
                RestoreCameraComponents();

                // Restore vehicle/skateboard input only if we found them
                RestoreVehicleSkateboardInput();

                // Only restore vehicle HUD when completely exiting free cam mode
                RestoreVehicleHUD();

                // Restore original FOV when exiting free cam mode
                SetFOV(initialFov);

                // Re-enable NPC eye movement when free cam is disabled
                RestoreNpcEyeMovement();

                Player.Local.SetVisibleToLocalPlayer(false);
                Singleton<HUD>.Instance.canvas.enabled = true;

                // Special case for skateboard/vehicle to ensure everything still works
                if (originalCameraMode == PlayerCamera.ECameraMode.Skateboard && Player.Local.IsSkating)
                {
                    // Make sure player visibility is correct for skateboard
                    Player.Local.SetVisibleToLocalPlayer(true);
                }
                else if (originalCameraMode == PlayerCamera.ECameraMode.Vehicle && Player.Local.CurrentVehicle != null)
                {
                    // Make sure vehicle camera settings are restored
                    Player.Local.SetVisibleToLocalPlayer(false);
                }

                // Reset state tracking variables
                vehicleComponentsFound = false;
                skateboardComponentsFound = false;
                activeVehicle = null;
                activeSkateboard = null;
                vehicleCameraComponent = null;
                skateboardCameraComponent = null;
            }
        }

        /// <summary>
        /// Toggle between locked and unlocked camera position
        /// </summary>
        public void ToggleCameraLock()
        {
            cameraLocked = !cameraLocked;
            Type playerCameraType = typeof(PlayerCamera);
            PropertyInfo freeCamEnabled = playerCameraType.GetProperty("FreeCamEnabled", BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);

            if (cameraLocked)
            {
                // Save current camera position and rotation
                lockedCameraPosition = playerCamera.transform.position;
                lockedCameraRotation = playerCamera.transform.rotation;

                // Make sure player is visible if we're in free cam (don't toggle free cam)
                if (playerCamera.FreeCamEnabled)
                {
                    Player.Local.SetVisibleToLocalPlayer(true);

                    // Set freeCamSpeed to 0 to prevent camera movement
                    playerCamera.FreeCamSpeed = 0f;
                    if (freeCamEnabled != null) freeCamEnabled.SetValue(playerCamera, false);
                }
            }
            else
            {
                if (freeCamEnabled != null) freeCamEnabled.SetValue(playerCamera, true);
                // Use the preference value when unlocking
                playerCamera.FreeCamSpeed = Core.CameraMovementSpeed.Value;
            }
        }

        /// <summary>
        /// Toggle player movement on/off while maintaining free cam view
        /// </summary>
        public void TogglePlayerControl()
        {
            Type playerCameraType = typeof(PlayerCamera);
            PropertyInfo freeCamEnabled = playerCameraType.GetProperty("FreeCamEnabled", BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);

            // Store current FOV before toggling modes
            float currentFOV = playerCamera.Camera.fieldOfView;

            playerControlMode = !playerControlMode;

            if (playerControlMode)
            {
                // Special handling for vehicle/skateboard modes
                if (originalCameraMode == PlayerCamera.ECameraMode.Vehicle ||
                    originalCameraMode == PlayerCamera.ECameraMode.Skateboard)
                {
                    // Don't re-enable vehicle/skateboard camera components
                    // We want to keep using freecam view

                    // Re-enable vehicle/skateboard input
                    RestoreVehicleSkateboardInput();

                    // Keep vehicle HUD disabled in player control mode
                    DisableVehicleHUD();

                    // Save current camera position and rotation
                    lockedCameraPosition = playerCamera.transform.position;
                    lockedCameraRotation = playerCamera.transform.rotation;
                    cameraLocked = true;

                    // Freeze the camera by setting speed to 0
                    playerCamera.FreeCamSpeed = 0f;

                    // Keep freecam mode enabled - DO NOT switch back to vehicle camera
                    // Just make sure freecam is properly enabled
                    if (!playerCamera.FreeCamEnabled)
                    {
                        playerCamera.SetFreeCam(true);
                    }

                    // Allow player movement while keeping camera fixed
                    PlayerSingleton<PlayerMovement>.Instance.canMove = true;

                    // Make sure the camera stays in position
                    playerCamera.transform.position = lockedCameraPosition;
                    playerCamera.transform.rotation = lockedCameraRotation;
                    playerCamera.SetCanLook(false); // Don't allow looking in vehicle/skateboard modes

                    // Keep player model visible for skateboard (but not for vehicle)
                    if (originalCameraMode == PlayerCamera.ECameraMode.Skateboard)
                    {
                        Player.Local.SetVisibleToLocalPlayer(true);
                    }

                    // Hide HUD in cinematic mode
                    Singleton<HUD>.Instance.canvas.enabled = false;

                    // When enabling player control mode for a vehicle, make sure it's at a complete stop initially
                    if (originalCameraMode == PlayerCamera.ECameraMode.Vehicle && activeVehicle != null)
                    {
                        // Reset vehicle speed and physics state initially
                        StopVehicle(activeVehicle);
                    }
                }
                else
                {
                    // Original behavior for default mode
                    lockedCameraPosition = playerCamera.transform.position;
                    lockedCameraRotation = playerCamera.transform.rotation;
                    cameraLocked = true;

                    if (playerCamera.FreeCamEnabled)
                    {
                        playerCamera.FreeCamSpeed = 0f;
                    }
                    else
                    {
                        playerCamera.SetFreeCam(true);
                        playerCamera.FreeCamSpeed = 0f;
                    }

                    PlayerSingleton<PlayerMovement>.Instance.canMove = true;
                    playerCamera.transform.position = lockedCameraPosition;
                    playerCamera.transform.rotation = lockedCameraRotation;
                    playerCamera.SetCanLook(true);
                    if (freeCamEnabled != null) freeCamEnabled.SetValue(playerCamera, false);
                    Player.Local.SetVisibleToLocalPlayer(true);
                    Singleton<HUD>.Instance.canvas.enabled = false;
                }
            }
            else
            {
                // When disabling player control mode
                PlayerSingleton<PlayerMovement>.Instance.canMove = false;

                // Different handling for vehicle/skateboard modes
                if (originalCameraMode == PlayerCamera.ECameraMode.Vehicle ||
                    originalCameraMode == PlayerCamera.ECameraMode.Skateboard)
                {
                    // Disable vehicle/skateboard camera components to allow free camera movement
                    DisableCameraComponents();

                    // Disable vehicle/skateboard input when in camera mode
                    DisableVehicleSkateboardInput();

                    // Keep vehicle HUD disabled while still in free cam
                    DisableVehicleHUD();

                    // Make sure freecam is properly enabled and not locked
                    if (!playerCamera.FreeCamEnabled)
                    {
                        playerCamera.SetFreeCam(true);
                    }
                    playerCamera.SetCanLook(true);
                }
                else
                {
                    playerCamera.SetCanLook(false);
                    if (freeCamEnabled != null) freeCamEnabled.SetValue(playerCamera, true);
                }

                // Disable camera follow mode when exiting player control
                if (currentFollowMode != CameraFollowMode.None)
                {
                    currentFollowMode = CameraFollowMode.None;
                }

                // Ensure we're at the locked position
                playerCamera.transform.position = lockedCameraPosition;
                playerCamera.transform.rotation = lockedCameraRotation;

                // Restore FOV to what it was before toggling modes
                SetFOV(currentFOV);

                // Keep player model visible
                Player.Local.SetVisibleToLocalPlayer(true);

                // Keep free cam enabled but allow movement again
                if (playerCamera.FreeCamEnabled)
                {
                    playerCamera.FreeCamSpeed = Core.CameraMovementSpeed.Value;
                }

                // Hide HUD in cinematic mode
                Singleton<HUD>.Instance.canvas.enabled = false;
                cameraLocked = false;
            }
        }

        /// <summary>
        /// Disables all NPC eye movement by finding and disabling all AvatarLookController instances
        /// </summary>
        private void DisableNpcEyeMovement()
        {
            // Check if the feature is enabled in preferences
            if (!Core.DisableNPCEyeMovement.Value)
            {
                return;
            }

            if (avatarLookControllersDisabled)
                return;

            // Find all AvatarLookController instances in the scene
            AvatarLookController[] controllers = UnityEngine.Object.FindObjectsOfType<AvatarLookController>();

            if (controllers.Length == 0)
            {
                return;
            }

            // Store their current state and disable them
            avatarLookControllers.Clear();
            foreach (AvatarLookController controller in controllers)
            {
                DisableAvatarLookController(controller);
            }

            avatarLookControllersDisabled = true;
            lastNpcCheckTime = Time.time;
        }

        /// <summary>
        /// Disables a single AvatarLookController and stores its state
        /// </summary>
        private void DisableAvatarLookController(AvatarLookController controller)
        {
            if (controller != null && !avatarLookControllers.ContainsKey(controller))
            {
                // Store the current AutoLookAtPlayer state
                avatarLookControllers[controller] = controller.AutoLookAtPlayer;

                // Disable looking at the player
                controller.AutoLookAtPlayer = false;

                // If the controller has an Aim component, disable it too
                if (controller.Aim != null)
                {
                    controller.Aim.enabled = false;
                }
            }
        }

        /// <summary>
        /// Checks for newly loaded NPCs and disables their eye movement
        /// </summary>
        private void CheckForNewNpcs()
        {
            if (!avatarLookControllersDisabled || !Core.DisableNPCEyeMovement.Value)
                return;

            // Find all current AvatarLookController instances
            AvatarLookController[] currentControllers = UnityEngine.Object.FindObjectsOfType<AvatarLookController>();
            int newControllersFound = 0;

            // Check for new controllers that we haven't processed yet
            foreach (AvatarLookController controller in currentControllers)
            {
                if (controller != null && !avatarLookControllers.ContainsKey(controller))
                {
                    DisableAvatarLookController(controller);
                    newControllersFound++;
                }
            }
        }

        /// <summary>
        /// Restores all NPC eye movement by re-enabling all AvatarLookController instances
        /// </summary>
        private void RestoreNpcEyeMovement()
        {
            if (!avatarLookControllersDisabled || avatarLookControllers.Count == 0)
                return;

            // Restore the state of each controller
            foreach (KeyValuePair<AvatarLookController, bool> kvp in avatarLookControllers)
            {
                AvatarLookController controller = kvp.Key;
                bool originalState = kvp.Value;

                if (controller != null)
                {
                    // Restore original AutoLookAtPlayer state
                    controller.AutoLookAtPlayer = originalState;
                }
            }

            avatarLookControllers.Clear();
            avatarLookControllersDisabled = false;
        }

        /// <summary>
        /// Move the camera in the specified direction
        /// </summary>
        public void MoveCamera(Vector3 direction)
        {
            if (playerCamera.FreeCamEnabled && !cameraLocked && !playerControlMode)
            {
                float moveSpeed = Core.CameraMovementSpeed.Value * Time.deltaTime;
                playerCamera.transform.position += direction * moveSpeed;
            }
        }

        /// <summary>
        /// Adjust the camera FOV in real-time
        /// </summary>
        public void SetFOV(float newFOV)
        {
            if (newFOV < MIN_FOV)
                newFOV = MIN_FOV;

            playerCamera.OverrideFOV(newFOV, 0.1f);
            Core.DefaultFov.Value = newFOV;
        }

        /// <summary>
        /// Reset the camera FOV to the default value
        /// </summary>
        public void ResetFOV()
        {
            SetFOV(initialFov);
        }

        /// <summary>
        /// Zoom the camera view using the zoom speed setting
        /// </summary>
        public void ZoomCamera(float zoomAmount)
        {
            // Only allow zooming in free cam mode or when camera is locked
            if (!playerCamera.FreeCamEnabled && !cameraLocked)
                return;

            float newFOV = playerCamera.Camera.fieldOfView;
            // Apply zoom directly
            newFOV -= zoomAmount;

            if (newFOV < MIN_FOV)
                newFOV = MIN_FOV;

            SetFOV(newFOV);
        }

        /// <summary>
        /// Set the FreeCam speed directly 
        /// </summary>
        public void SetFreeCamSpeed(float speed)
        {
            if (playerCamera != null && IsFreeCamEnabled && !cameraLocked)
            {
                playerCamera.FreeCamSpeed = speed;
            }
        }

        /// <summary>
        /// Set specific camera follow mode
        /// </summary>
        public void SetCameraFollowMode(CameraFollowMode mode)
        {
            // Only allow setting follow mode when player movement is enabled (except for turning off)
            if (mode != CameraFollowMode.None && !playerControlMode)
            {
                Core.Instance.LoggerInstance.Msg("Camera follow mode requires player control mode (F6) to be active.");
                return;
            }

            // Check for null player before enabling follow mode
            if (mode != CameraFollowMode.None && (Player.Local == null || Player.Local.transform == null))
            {
                Core.Instance.LoggerInstance.Warning("Cannot enable camera follow mode: Player reference is null.");
                return;
            }

            // Check camera references
            if (mode != CameraFollowMode.None && (playerCamera == null || playerCamera.transform == null))
            {
                Core.Instance.LoggerInstance.Warning("Cannot enable camera follow mode: Camera reference is null.");
                return;
            }

            CameraFollowMode previousMode = currentFollowMode;
            currentFollowMode = mode;

            // Determine the target to follow
            Transform targetTransform = null;
            if (mode != CameraFollowMode.None)
            {
                if (Player.Local.CurrentVehicle != null)
                {
                    targetTransform = Player.Local.CurrentVehicle.transform;
                }
                else
                {
                    targetTransform = Player.Local.transform;
                }
                followTarget = targetTransform;
            }
            else
            {
                followTarget = null;
            }

            // Handle position following setup
            if ((mode & CameraFollowMode.Position) == CameraFollowMode.Position && 
                (previousMode & CameraFollowMode.Position) != CameraFollowMode.Position)
            {
                // Enabling position following
                if (targetTransform != null)
                {
                    // Calculate offset in the target's local space for more robust following
                    Vector3 worldOffset = playerCamera.transform.position - targetTransform.position;
                    
                    try
                    {
                        // Convert world offset to local space
                        localPositionOffset = targetTransform.InverseTransformPoint(playerCamera.transform.position);
                        worldPositionOffset = worldOffset; // Keep as fallback
                        useLocalSpaceOffset = true;
                        
                        Core.Instance.LoggerInstance.Msg("Camera follow mode: Position following enabled (local space)");
                    }
                    catch (Exception)
                    {
                        // Fallback to world space if local space calculation fails
                        localPositionOffset = Vector3.zero;
                        worldPositionOffset = worldOffset;
                        useLocalSpaceOffset = false;
                        
                        Core.Instance.LoggerInstance.Msg("Camera follow mode: Position following enabled (world space fallback)");
                    }
                }
            }
            else if ((mode & CameraFollowMode.Position) != CameraFollowMode.Position && 
                     (previousMode & CameraFollowMode.Position) == CameraFollowMode.Position)
            {
                // Disabling position following
                Core.Instance.LoggerInstance.Msg("Camera follow mode: Position following disabled");
            }

            // Handle rotation following setup
            if ((mode & CameraFollowMode.Rotation) == CameraFollowMode.Rotation && 
                (previousMode & CameraFollowMode.Rotation) != CameraFollowMode.Rotation)
            {
                // Enabling rotation following
                lockedCameraPosition = playerCamera.transform.position;
                lockedCameraRotation = playerCamera.transform.rotation;
                Core.Instance.LoggerInstance.Msg("Camera follow mode: Rotation following enabled");
            }
            else if ((mode & CameraFollowMode.Rotation) != CameraFollowMode.Rotation && 
                     (previousMode & CameraFollowMode.Rotation) == CameraFollowMode.Rotation)
            {
                // Disabling rotation following
                if (playerControlMode && cameraLocked && (mode & CameraFollowMode.Position) != CameraFollowMode.Position)
                {
                    // Restore locked rotation if not using position following
                    playerCamera.transform.rotation = lockedCameraRotation;
                }
                Core.Instance.LoggerInstance.Msg("Camera follow mode: Rotation following disabled");
            }

            // Overall mode status
            if (mode == CameraFollowMode.None)
            {
                Core.Instance.LoggerInstance.Msg("Camera follow mode: Disabled");
            }
            else if (mode == (CameraFollowMode.Position | CameraFollowMode.Rotation))
            {
                Core.Instance.LoggerInstance.Msg("Camera follow mode: Position and Rotation following enabled");
            }
        }

        /// <summary>
        /// Toggle camera follow mode for tracking player movement
        /// </summary>
        public void ToggleCameraFollow()
        {
            // Get preference settings for which modes are enabled
            bool positionEnabled = cameraFollowPositionEnabled;
            bool rotationEnabled = cameraFollowRotationEnabled;
            
            // If neither is enabled in preferences, default to both
            if (!positionEnabled && !rotationEnabled)
            {
                positionEnabled = true;
                rotationEnabled = true;
            }
            
            CameraFollowMode nextMode = CameraFollowMode.None;
            
            if (currentFollowMode == CameraFollowMode.None)
            {
                // Starting from none - enable first available mode
                if (positionEnabled && rotationEnabled)
                {
                    nextMode = CameraFollowMode.Position; // Start with position only
                }
                else if (positionEnabled)
                {
                    nextMode = CameraFollowMode.Position;
                }
                else if (rotationEnabled)
                {
                    nextMode = CameraFollowMode.Rotation;
                }
            }
            else if (currentFollowMode == CameraFollowMode.Position)
            {
                // From position only
                if (rotationEnabled)
                {
                    nextMode = CameraFollowMode.Rotation; // Go to rotation only
                }
                else
                {
                    nextMode = CameraFollowMode.None; // No rotation available, go to none
                }
            }
            else if (currentFollowMode == CameraFollowMode.Rotation)
            {
                // From rotation only
                if (positionEnabled && rotationEnabled)
                {
                    nextMode = CameraFollowMode.Position | CameraFollowMode.Rotation; // Go to both
                }
                else
                {
                    nextMode = CameraFollowMode.None; // Go to none
                }
            }
            else if (currentFollowMode == (CameraFollowMode.Position | CameraFollowMode.Rotation))
            {
                // From both modes - go to none
                nextMode = CameraFollowMode.None;
            }
            else
            {
                // Fallback for any other combination
                nextMode = CameraFollowMode.None;
            }
            
            SetCameraFollowMode(nextMode);
        }

        /// <summary>
        /// Handles the escape key input when the camera is active - only called from OnUpdate
        /// </summary>
        public void HandleEscapeInput()
        {
            // If in a sequence, stop it
            if (inSequence)
            {
                StopCameraSequence();
                return;
            }

            // If camera follow mode is enabled, disable it first
            if (currentFollowMode != CameraFollowMode.None)
            {
                ToggleCameraFollow();
                return;
            }

            // If in player control mode, exit that mode
            if (playerControlMode)
            {
                TogglePlayerControl();
                return;
            }

            // If camera is locked, unlock it
            if (cameraLocked)
            {
                ToggleCameraLock();
                return;
            }

            // If in free cam mode, disable it (exit to normal game mode)
            if (playerCamera.FreeCamEnabled)
            {
                SetCineCamFreeCam(false);
            }
        }

        /// <summary>
        /// Save a camera shot with the given name
        /// </summary>
        public void SaveShotWithName(string name)
        {
            CameraShot shot = new CameraShot(
                name,
                playerCamera.transform.position,
                playerCamera.transform.rotation,
                playerCamera.Camera.fieldOfView
            );

            CameraShotManager.SaveShot(shot);
            Core.Instance.LoggerInstance.Msg($"Saved camera shot: {name}");
        }

        /// <summary>
        /// Saves a camera shot with the given name and an optional screenshot path.
        /// This is intended to be called from UI panels like ShotPanel.
        /// </summary>
        public void SaveShotWithNameAndOptionalScreenshot(string name, string screenshotPath)
        {
            if (playerCamera == null || playerCamera.transform == null || playerCamera.Camera == null)
            {
                Core.Instance.LoggerInstance.Error("Cannot save shot, player camera references are not valid.");
                return;
            }

            CameraShot shot = new CameraShot(
                name,
                playerCamera.transform.position,
                playerCamera.transform.rotation,
                playerCamera.Camera.fieldOfView,
                screenshotPath // Pass the screenshot path to the model
            );

            CameraShotManager.SaveShot(shot);
            // CameraShotManager already logs the save, so an additional log here might be redundant unless more specific info is needed.
            // Core.Instance.LoggerInstance.Msg($"Saved camera shot: {name} with Screenshot: {(screenshotPath ?? "N/A")}"); 
        }

        /// <summary>
        /// Quick save the current camera position without a prompt
        /// </summary>
        public void QuickSaveCameraShot()
        {
            string shotName = "QuickShot";
            CameraShot shot = new CameraShot(
                shotName,
                playerCamera.transform.position,
                playerCamera.transform.rotation,
                playerCamera.Camera.fieldOfView
            );

            CameraShotManager.SaveShot(shot);
            Core.Instance.LoggerInstance.Msg("Quick saved camera position");
        }

        /// <summary>
        /// Load a camera shot
        /// </summary>
        public void LoadShot(CameraShot shot)
        {
            if (shot != null)
            {
                ApplyCameraShot(shot);
                Core.Instance.LoggerInstance.Msg($"Loaded camera shot: {shot.Name}");
            }
        }

        /// <summary>
        /// Apply a camera shot to the camera
        /// </summary>
        private void ApplyCameraShot(CameraShot shot)
        {
            // Make sure we're in free cam mode
            if (!playerCamera.FreeCamEnabled)
            {
                SetCineCamFreeCam(true);
            }

            // Apply the shot
            playerCamera.transform.position = shot.Position.ToVector3();
            playerCamera.transform.rotation = shot.Rotation.ToQuaternion();
            playerCamera.OverrideFOV(shot.FieldOfView, 0.1f);

            // Update locked position if we're currently locked
            if (cameraLocked)
            {
                lockedCameraPosition = shot.Position.ToVector3();
                lockedCameraRotation = shot.Rotation.ToQuaternion();
            }
        }

        /// <summary>
        /// Toggle camera sequence playback
        /// </summary>
        public void ToggleCameraSequence()
        {
            if (inSequence)
            {
                StopCameraSequence();
            }
            else
            {
                StartCameraSequence();
            }
        }

        /// <summary>
        /// Start playing a camera sequence
        /// </summary>
        private void StartCameraSequence()
        {
            List<CameraShot> shots = CameraShotManager.GetAllShots();

            if (shots.Count == 0)
            {
                Core.Instance.LoggerInstance.Msg("No camera shots available for sequence");
                return;
            }

            shotSequence = shots.OrderBy(s => s.Name).ToList();
            currentShotIndex = 0;
            inSequence = true;
            sequenceCoroutine = MelonCoroutines.Start(PlayCameraSequence());
            Core.Instance.LoggerInstance.Msg($"Started camera sequence with {shotSequence.Count} shots");
        }

        /// <summary>
        /// Stop the current camera sequence
        /// </summary>
        private void StopCameraSequence()
        {
            if (!inSequence)
                return;

            inSequence = false;

            if (sequenceCoroutine != null)
            {
                MelonCoroutines.Stop(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            Core.Instance.LoggerInstance.Msg("Stopped camera sequence");
        }

        /// <summary>
        /// Coroutine for playing a camera sequence
        /// </summary>
        private IEnumerator PlayCameraSequence()
        {
            if (!playerCamera.FreeCamEnabled)
            {
                SetCineCamFreeCam(true);
            }

            bool wasLocked = cameraLocked;
            float originalFreeCamSpeed = playerCamera.FreeCamSpeed;

            if (!cameraLocked)
            {
                cameraLocked = true;
            }

            playerCamera.FreeCamSpeed = 0f;
            float transitionDuration = 3.0f;

            while (inSequence)
            {
                CameraShot currentShot = shotSequence[currentShotIndex];
                int nextIndex = (currentShotIndex + 1) % shotSequence.Count;
                CameraShot nextShot = shotSequence[nextIndex];

                yield return MelonCoroutines.Start(TransitionToShot(currentShot, nextShot, transitionDuration));
                currentShotIndex = nextIndex;
                yield return new WaitForSeconds(1.0f);
            }

            if (!wasLocked)
            {
                cameraLocked = false;
                playerCamera.FreeCamSpeed = originalFreeCamSpeed;
            }
            else
            {
                playerCamera.FreeCamSpeed = 0f;
            }
        }

        /// <summary>
        /// Smoothly transition between two camera shots
        /// </summary>
        private IEnumerator TransitionToShot(CameraShot fromShot, CameraShot toShot, float duration)
        {
            Vector3 startPos = fromShot.Position.ToVector3();
            Quaternion startRot = fromShot.Rotation.ToQuaternion();
            float startFov = fromShot.FieldOfView;

            Vector3 endPos = toShot.Position.ToVector3();
            Quaternion endRot = toShot.Rotation.ToQuaternion();
            float endFov = toShot.FieldOfView;

            float elapsed = 0;

            while (elapsed < duration && inSequence)
            {
                float t = elapsed / duration;

                // Use smooth step for easing
                float smoothT = Mathf.SmoothStep(0, 1, t);

                // Interpolate position, rotation, and FOV
                playerCamera.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
                playerCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);
                playerCamera.OverrideFOV(Mathf.Lerp(startFov, endFov, smoothT), 0.1f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Make sure we end exactly at the target
            if (inSequence)
            {
                playerCamera.transform.position = endPos;
                playerCamera.transform.rotation = endRot;
                playerCamera.OverrideFOV(endFov, 0.1f);

                // Update locked position
                lockedCameraPosition = endPos;
                lockedCameraRotation = endRot;
            }
        }

        /// <summary>
        /// Enables or disables precision mode for fine camera movements
        /// </summary>
        public void SetPrecisionMode(bool enabled)
        {
            if (isPrecisionModeActive == enabled)
                return;

            isPrecisionModeActive = enabled;

            if (enabled)
            {
                if (normalCameraSpeed <= 0f)
                {
                    normalCameraSpeed = playerCamera.FreeCamSpeed > 0f
                        ? playerCamera.FreeCamSpeed
                        : Core.CameraMovementSpeed.Value;
                }

                playerCamera.FreeCamSpeed = normalCameraSpeed * PRECISION_MODE_MULTIPLIER;
            }
            else
            {
                playerCamera.FreeCamSpeed = normalCameraSpeed;
            }
        }

        /// <summary>
        /// Manually set the follow target for advanced cinematic shots
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            if (target == null)
            {
                Core.Instance.LoggerInstance.Warning("Cannot set null follow target");
                return;
            }

            followTarget = target;

            // Recalculate offsets if position following is active
            if ((currentFollowMode & CameraFollowMode.Position) == CameraFollowMode.Position && playerCamera?.transform != null)
            {
                Vector3 worldOffset = playerCamera.transform.position - target.position;
                
                try
                {
                    // Convert world offset to local space
                    localPositionOffset = target.InverseTransformPoint(playerCamera.transform.position);
                    worldPositionOffset = worldOffset;
                    useLocalSpaceOffset = true;
                    
                    Core.Instance.LoggerInstance.Msg($"Follow target set to: {target.name} (local space offset calculated)");
                }
                catch (Exception)
                {
                    // Fallback to world space if local space calculation fails
                    localPositionOffset = Vector3.zero;
                    worldPositionOffset = worldOffset;
                    useLocalSpaceOffset = false;
                    
                    Core.Instance.LoggerInstance.Msg($"Follow target set to: {target.name} (world space fallback)");
                }
            }
            else
            {
                Core.Instance.LoggerInstance.Msg($"Follow target set to: {target.name}");
            }
        }

        /// <summary>
        /// Get the current follow target
        /// </summary>
        public Transform GetFollowTarget()
        {
            return followTarget;
        }

        /// <summary>
        /// Manually adjust the position offset for fine-tuning camera position relative to target
        /// </summary>
        public void AdjustPositionOffset(Vector3 offsetAdjustment)
        {
            if (followTarget == null)
            {
                Core.Instance.LoggerInstance.Warning("No follow target set");
                return;
            }

            if (useLocalSpaceOffset)
            {
                localPositionOffset += offsetAdjustment;
                Core.Instance.LoggerInstance.Msg($"Local position offset adjusted by: {offsetAdjustment}");
            }
            else
            {
                worldPositionOffset += offsetAdjustment;
                Core.Instance.LoggerInstance.Msg($"World position offset adjusted by: {offsetAdjustment}");
            }
        }
    }
}