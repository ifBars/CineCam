using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using UnityEngine.Events;
using Il2CppInterop.Runtime;
#endif

namespace CineCam.UI
{
    public class Toolbar
    {
        private GameObject _toolbarObject;
        private RectTransform _toolbarRect;
        private GameObject _buttonContainer;
        private Dictionary<string, Button> _panelButtons =
            new Dictionary<string, Button>();
        private Color _activeButtonColor = new Color(0.3f, 0.7f, 1f);
        private Color _inactiveButtonColor = new Color(0.2f, 0.2f, 0.2f);
        private EditorUI _editorUI;
        private Text _titleText;
        private MelonLoader.MelonLogger.Instance _loggerInstance;

        public Toolbar(GameObject parent, EditorUI editorUI, MelonLoader.MelonLogger.Instance loggerInstance)
        {
            _editorUI = editorUI;
            _loggerInstance = loggerInstance;
            CreateToolbar(parent);
        }

        private void CreateToolbar(GameObject parent)
        {
            // Create toolbar gameobject
            _toolbarObject = new GameObject("Toolbar");
            _toolbarObject.transform.SetParent(parent.transform, false);

            // Set toolbar at the top of the screen
            _toolbarRect = _toolbarObject.AddComponent<RectTransform>();
            _toolbarRect.anchorMin = new Vector2(0, 1);
            _toolbarRect.anchorMax = new Vector2(1, 1);
            _toolbarRect.pivot = new Vector2(0.5f, 1);
            _toolbarRect.sizeDelta = new Vector2(0, 40);

            // Add background image
            Image bgImage = _toolbarObject.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Create button container
            _buttonContainer = new GameObject("ButtonContainer");
            _buttonContainer.transform.SetParent(_toolbarObject.transform, false);

            RectTransform buttonContainerRect =
                _buttonContainer.AddComponent<RectTransform>();
            buttonContainerRect.anchorMin = new Vector2(0, 0);
            buttonContainerRect.anchorMax = new Vector2(1, 1);
            buttonContainerRect.offsetMin = new Vector2(5, 5);
            buttonContainerRect.offsetMax = new Vector2(-205, -5);

            // Create layout for buttons
            HorizontalLayoutGroup layoutGroup =
                _buttonContainer.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(5, 5, 2, 2);
            layoutGroup.childAlignment = TextAnchor.MiddleRight;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            // Add content size fitter
            ContentSizeFitter contentFitter =
                _buttonContainer.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add title text
            GameObject titleObj = new GameObject("ToolbarTitle");
            titleObj.transform.SetParent(_toolbarObject.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.5f);
            titleRect.anchorMax = new Vector2(0, 0.5f);
            titleRect.pivot = new Vector2(0, 0.5f);
            titleRect.sizeDelta = new Vector2(200, 30);
            titleRect.anchoredPosition = new Vector2(10, 0);

            _titleText = titleObj.AddComponent<Text>();
            _titleText.text = "CineCam Editor";
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.fontSize = 18;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.color = Color.white;
        }

        public void AddPanelButton(string panelId, string displayName)
        {
            if (_buttonContainer == null) return;

            // Create button gameobject
            GameObject buttonObj = new GameObject($"Button_{panelId}");
            buttonObj.transform.SetParent(_buttonContainer.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100, 30);

            // Add button image
            Image buttonImage = buttonObj.AddComponent<Image>();
            // Initial color will be set by UpdateButtonState

            // Add button component
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Normal color
            ColorBlock colors = button.colors;
            colors.normalColor = _inactiveButtonColor;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
            colors.pressedColor = new Color(0.2f, 0.5f, 0.8f);
            colors.selectedColor = _activeButtonColor;
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = displayName;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 14;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            // Add click handler
#if IL2CPP
            Action clickAction = () => {
                _editorUI.TogglePanelVisibility(panelId);
                // After toggling, update this button's state
                BasePanel panel = _editorUI.GetPanel(panelId);
                if (panel != null)
                {
                    UpdateButtonState(panelId, panel.IsVisible);
                }
            };
            button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(clickAction));
#else
            button.onClick.AddListener(() =>
            {
                _editorUI.TogglePanelVisibility(panelId);
                // After toggling, update this button's state
                BasePanel panel = _editorUI.GetPanel(panelId);
                if (panel != null)
                {
                    UpdateButtonState(panelId, panel.IsVisible);
                }
            });
#endif

            // Store reference to button
            _panelButtons[panelId] = button;
            // Set initial state
            BasePanel initialPanel = _editorUI.GetPanel(panelId);
            UpdateButtonState(panelId, initialPanel != null && initialPanel.IsVisible);
            // Default to inactive if panel not found (should not happen)
        }

        public void UpdateButtonState(string panelId, bool isPanelVisible)
        {
            if (_panelButtons.TryGetValue(panelId, out Button button) && button != null && button.targetGraphic is Image image)
            {
                image.color = isPanelVisible ? _activeButtonColor : _inactiveButtonColor;
            }
            else
            {
                // _loggerInstance?.Warning($"Toolbar: Could not find button or image for panelId {panelId} to update state.");
            }
        }
    }
}
