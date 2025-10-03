using CineCam.Managers;
using CineCam.UI.Panels;
using CineCam.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CineCam.UI
{
    public class EditorUI
    {
        private GameObject _editorUIObject;
        private bool _uiCreated = false;
        private bool _showUI = false;
        private readonly Dictionary<string, BasePanel> _panels = new Dictionary<string, BasePanel>();
        private Toolbar _toolbar;
        private bool _firstOpen = true;
        private MelonLoader.MelonLogger.Instance _loggerInstance;

        // Default cursor state for game (cursor hidden and locked)
        private bool _gameCursorVisible = false;
        private CursorLockMode _gameCursorLockState = CursorLockMode.Locked;

        public bool ShowUI
        {
            get => _showUI;
            set
            {
                bool wasVisible = _showUI;
                _showUI = value;
                if (_editorUIObject != null)
                {
                    _editorUIObject.SetActive(_showUI);
                }

                // Control cursor visibility based on UI state
                if (_showUI)
                {
                    // Show cursor and unlock it for UI interaction
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    // If UI was visible and is now being hidden, save state
                    if (wasVisible && !_showUI && _uiCreated)
                    {
                        SaveUIStateInternal();
                    }

                    // Restore game cursor state when UI is closed
                    Cursor.visible = _gameCursorVisible;
                    Cursor.lockState = _gameCursorLockState;
                }

                // If UI is being shown and it's the first time, or no panels are visible, show Help by default.
                if (!_showUI || !_firstOpen || !_panels.ContainsKey("Help")) return;
                TogglePanelVisibility("Help", true); // Ensure help is visible
                _firstOpen = false;
            }
        }

        public EditorUI(bool showOnStartup)
        {
            _showUI = showOnStartup;

            // If we're showing UI on startup, make cursor visible
            if (_showUI)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        // Method to update the game's expected cursor state (should be called by game code)
        public void SetGameCursorState(bool visible, CursorLockMode lockState)
        {
            _gameCursorVisible = visible;
            _gameCursorLockState = lockState;

            // Only apply these settings if UI is currently hidden
            if (!_showUI)
            {
                Cursor.visible = visible;
                Cursor.lockState = lockState;
            }
        }

        public void CreateUI(MelonLoader.MelonLogger.Instance loggerInstance)
        {
            if (_uiCreated)
                return;

            try
            {
                _loggerInstance = loggerInstance;
                loggerInstance.Msg("Creating CineCam Editor UI...");

                _editorUIObject = new GameObject("CineCamEditorUI");
                if (_editorUIObject == null) { loggerInstance.Error("Failed to create EditorUI GameObject"); return; }
                GameObject.DontDestroyOnLoad(_editorUIObject);

                Canvas canvas = _editorUIObject.AddComponent<Canvas>();
                if (canvas == null) { loggerInstance.Error("Failed to add Canvas component"); return; }
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32767; // Ensure it's on top

                _editorUIObject.AddComponent<GraphicRaycaster>();

                CanvasScaler scaler = _editorUIObject.AddComponent<CanvasScaler>();
                if (scaler == null) { loggerInstance.Error("Failed to add CanvasScaler component"); return; }
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                GameObject mainContainer = new GameObject("MainContainer");
                if (mainContainer == null) { loggerInstance.Error("Failed to create MainContainer GameObject"); return; }
                mainContainer.transform.SetParent(_editorUIObject.transform, false);
                RectTransform mainRect = mainContainer.AddComponent<RectTransform>();
                mainRect.anchorMin = Vector2.zero;
                mainRect.anchorMax = Vector2.one;
                mainRect.offsetMin = Vector2.zero;
                mainRect.offsetMax = Vector2.zero;

                GameObject panelContainer = new GameObject("PanelContainer");
                if (panelContainer == null) { loggerInstance.Error("Failed to create PanelContainer GameObject"); return; }
                panelContainer.transform.SetParent(mainContainer.transform, false);
                RectTransform panelContainerRect = panelContainer.AddComponent<RectTransform>();
                panelContainerRect.anchorMin = new Vector2(0, 0);
                panelContainerRect.anchorMax = new Vector2(1, 1);
                panelContainerRect.offsetMin = new Vector2(0, 0);
                panelContainerRect.offsetMax = new Vector2(0, -40);

                InitializePanels(panelContainer, loggerInstance);

                _toolbar = new Toolbar(mainContainer, this, loggerInstance);

                foreach (var panelName in _panels.Keys)
                {
                    _toolbar.AddPanelButton(panelName, panelName);
                }

                LoadUIState();

                _editorUIObject.SetActive(_showUI);
                _uiCreated = true;

                if (_showUI)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                loggerInstance.Msg("CineCam Editor UI created successfully");
            }
            catch (Exception ex)
            {
                loggerInstance.Error($"Failed to create CineCam Editor UI: {ex.Message}\nStack trace: {ex.StackTrace}");
                _uiCreated = false;
            }
        }

        private void InitializePanels(GameObject container, MelonLoader.MelonLogger.Instance loggerInstance)
        {
            // Create Help Panel
            try
            {
                HelpPanel helpPanel = new HelpPanel(container);
                if (!_panels.ContainsKey("Help"))
                {
                    _panels.Add("Help", helpPanel);
                }
                else
                {
                    loggerInstance.Warning("Help panel already exists, skipping duplicate addition");
                }
            }
            catch (Exception ex) { loggerInstance.Error($"Failed to create Help panel: {ex.Message}\nStack trace: {ex.StackTrace}"); }

            // Create Settings Panel
            try
            {
                SettingsPanel settingsPanel = new SettingsPanel(container);
                if (!_panels.ContainsKey("Settings"))
                {
                    _panels.Add("Settings", settingsPanel);
                }
                else
                {
                    loggerInstance.Warning("Settings panel already exists, skipping duplicate addition");
                }
            }
            catch (Exception ex) { loggerInstance.Error($"Failed to create Settings panel: {ex.Message}\nStack trace: {ex.StackTrace}"); }

            // Create Shot Panel
            try
            {
                ShotPanel shotPanel = new ShotPanel(container);
                if (!_panels.ContainsKey("Shots"))
                {
                    _panels.Add("Shots", shotPanel);
                }
                else
                {
                    loggerInstance.Warning("Shots panel already exists, skipping duplicate addition");
                }
            }
            catch (Exception ex) { loggerInstance.Error($"Failed to create Shots panel: {ex.Message}\nStack trace: {ex.StackTrace}"); }

            // Create About Panel
            try
            {
                AboutPanel aboutPanel = new AboutPanel(container);
                if (!_panels.ContainsKey("About"))
                {
                    _panels.Add("About", aboutPanel);
                }
                else
                {
                    loggerInstance.Warning("About panel already exists, skipping duplicate addition");
                }
            }
            catch (Exception ex) { loggerInstance.Error($"Failed to create About panel: {ex.Message}\nStack trace: {ex.StackTrace}"); }

            // Initially hide all panels (or load saved state in the future)
            foreach (var panel in _panels.Values)
            {
                panel.SetVisible(false);
            }
        }

        // Method to toggle panel visibility, or set it to a specific state
        public void TogglePanelVisibility(string panelName, bool? forceState = null)
        {
            if (!_panels.ContainsKey(panelName))
            {
                // Optionally log an error if panelName is invalid
                // loggerInstance.Warning($"Attempted to toggle unknown panel: {panelName}");
                return;
            }

            BasePanel panel = _panels[panelName];
            bool targetVisibility = forceState ?? !panel.IsVisible;

            panel.SetVisible(targetVisibility);

            if (targetVisibility)
            {
                panel.BringToFront(); // Ensure visible panel is on top

                // Ensure cursor is visible when panels are shown
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Update toolbar button state
            _toolbar?.UpdateButtonState(panelName, targetVisibility);
        }

        public void ToggleOverallUI()
        {
            ShowUI = !_showUI;
            // The ShowUI setter handles the firstOpen logic for the Help panel.
        }

        public void UpdateCameraStatus(CinematicCameraManager cameraManager, MelonLoader.MelonLogger.Instance loggerInstance)
        {
            if (_panels == null || _panels.Count == 0) return;

            foreach (var panel in _panels.Values.Where(panel => panel != null))
            {
                try { panel.UpdateCameraStatus(cameraManager); }
                catch (Exception ex) { loggerInstance.Error($"Error updating panel camera status: {ex.Message}"); }
            }
        }

        private void SaveUIStateInternal()
        {
            if (_loggerInstance != null)
            {
                UISerializer.SaveUIState(this, _loggerInstance);
            }
        }

        // Public method to allow external classes to trigger UI state saving
        public void SaveUIState()
        {
            if (_uiCreated)
            {
                SaveUIStateInternal();
            }
        }

        private void LoadUIState()
        {
            if (_loggerInstance == null) return;

            UIState state = UISerializer.LoadUIState(_loggerInstance);

            // Apply UI visibility
            _showUI = state.IsUIVisible;

            // Apply panel states
            foreach (var panelState in state.PanelStates)
            {
                if (_panels.TryGetValue(panelState.PanelName, out BasePanel panel))
                {
                    // Set panel position - convert from SerializableVector2 to Vector2
                    panel.SetPosition(panelState.Position.ToVector2());

                    // Set panel visibility
                    panel.SetVisible(panelState.IsVisible);

                    // Update toolbar button state
                    _toolbar?.UpdateButtonState(panelState.PanelName, panelState.IsVisible);
                }
            }
        }

        public IEnumerable<string> GetPanelNames()
        {
            return _panels.Keys;
        }

        public bool IsCreated => _uiCreated;
        public BasePanel GetPanel(string panelName) => _panels.TryGetValue(panelName, out var panel) ? panel : null;
    }
}