using MelonLoader;
using UnityEngine;

namespace CineCam.Services
{
    /// <summary>
    /// Manages keyboard shortcuts and input handling for the camera system
    /// </summary>
    public static class InputManager
    {
        // Default key bindings
        private static Dictionary<string, KeyCode> _keyBindings = new Dictionary<string, KeyCode>
        {
            { "ToggleHelp", KeyCode.F1 },
            { "LockCamera", KeyCode.F5 },
            { "TogglePlayerControl", KeyCode.F6 },
            { "ZoomCamera", KeyCode.F7 },
            { "SaveShot", KeyCode.F8 },
            { "LoadShot", KeyCode.F9 },
            { "ToggleSequence", KeyCode.F10 },
            { "QuickSave", KeyCode.F11 },
            { "MoveUp", KeyCode.E },
            { "MoveDown", KeyCode.Q }
        };

        // MelonPreferences entries
        private static Dictionary<string, MelonPreferences_Entry<int>> _keyBindingPrefs = new Dictionary<string, MelonPreferences_Entry<int>>();

        // Cached key states - updated every frame in OnUpdate
        private static Dictionary<string, bool> _keyDownState = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _keyHeldState = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _keyUpState = new Dictionary<string, bool>();

        // Flag to indicate if we've initialized the input cache
        private static bool _inputCacheInitialized = false;

        /// <summary>
        /// Initialize the input manager and load key bindings from preferences
        /// </summary>
        public static void Initialize()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("CineCamKeyBindings");

            foreach (var binding in _keyBindings)
            {
                var entry = category.CreateEntry(binding.Key, (int)binding.Value, binding.Key,
                    $"The key for {binding.Key}. Values are based on Unity's KeyCode enum.");
                _keyBindingPrefs.Add(binding.Key, entry);

                _keyDownState[binding.Key] = false;
                _keyHeldState[binding.Key] = false;
                _keyUpState[binding.Key] = false;
            }

            LoadKeyBindings();
            _inputCacheInitialized = true;
        }

        /// <summary>
        /// Update all key states - call this only from OnUpdate
        /// </summary>
        public static void UpdateKeyStates()
        {
            if (!_inputCacheInitialized)
                return;

            foreach (var binding in _keyBindings)
            {
                KeyCode key = binding.Value;
                string action = binding.Key;

                _keyDownState[action] = Input.GetKeyDown(key);
                _keyHeldState[action] = Input.GetKey(key);
                _keyUpState[action] = Input.GetKeyUp(key);
            }
        }

        /// <summary>
        /// Load key bindings from preferences
        /// </summary>
        public static void LoadKeyBindings()
        {
            foreach (var entry in _keyBindingPrefs)
            {
                _keyBindings[entry.Key] = (KeyCode)entry.Value.Value;
            }
        }

        /// <summary>
        /// Save key bindings to preferences
        /// </summary>
        public static void SaveKeyBindings()
        {
            foreach (var binding in _keyBindings)
            {
                _keyBindingPrefs[binding.Key].Value = (int)binding.Value;
            }

            MelonPreferences.Save();
        }

        /// <summary>
        /// Set a key binding
        /// </summary>
        public static void SetKeyBinding(string action, KeyCode key)
        {
            if (!_keyBindings.ContainsKey(action))
            {
                Core.Instance.LoggerInstance.Error($"Unknown action: {action}");
                return;
            }

            _keyBindings[action] = key;
            _keyBindingPrefs[action].Value = (int)key;
        }

        /// <summary>
        /// Get a key binding
        /// </summary>
        public static KeyCode GetKeyBinding(string action)
        {
            if (!_keyBindings.ContainsKey(action))
            {
                Core.Instance.LoggerInstance.Error($"Unknown action: {action}");
                return KeyCode.None;
            }

            return _keyBindings[action];
        }

        /// <summary>
        /// Check if a key for a specific action is pressed down this frame
        /// </summary>
        public static bool GetKeyDown(string action)
        {
            if (!_inputCacheInitialized || !_keyDownState.ContainsKey(action))
                return false;

            return _keyDownState[action];
        }

        /// <summary>
        /// Check if a key for a specific action is currently held down
        /// </summary>
        public static bool GetKey(string action)
        {
            if (!_inputCacheInitialized || !_keyHeldState.ContainsKey(action))
                return false;

            return _keyHeldState[action];
        }

        /// <summary>
        /// Check if a key for a specific action is released this frame
        /// </summary>
        public static bool GetKeyUp(string action)
        {
            if (!_inputCacheInitialized || !_keyUpState.ContainsKey(action))
                return false;

            return _keyUpState[action];
        }
    }
}