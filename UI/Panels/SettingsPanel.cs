using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if IL2CPP
using Il2CppInterop.Runtime;
#endif

namespace CineCam.UI
{
    public class SettingsPanel : BasePanel
    {
        private Slider _moveSpeedSlider;
        private Slider _followSmoothnessSlider;
        private Slider _fovSlider;
        private Slider _zoomSpeedSlider;
        private Toggle _showControlsToggle;
        private Toggle _showGridOverlayToggle;
        private Toggle _disableNPCEyeMovementToggle;
        private Toggle _disableCameraEffectsInFreeCamToggle;

        private Text _moveSpeedValue;
        private Text _followSmoothnessValue;
        private Text _fovValue;
        private Text _zoomSpeedValue;

        private bool _settingsInitialized = false;

        public SettingsPanel(GameObject parent) : base(parent, "Camera Settings", new Vector2(450, 450))
        {
            CreateSettingsContent();
        }

        private void CreateSettingsContent()
        {
            // Find content area
            Transform contentArea = _panelObject.transform.Find("ContentArea");
            if (contentArea == null)
                return;

            // Create scrollable content
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(contentArea, false);

            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(0, 0);
            scrollRect.offsetMax = new Vector2(-20, 0); // Leave space for scrollbar

            ScrollRect scrollComponent = scrollView.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-20, 0); // Leave space for scrollbar

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            scrollComponent.viewport = viewportRect;

            // Create content container
            GameObject contentContainer = new GameObject("Content");
            contentContainer.transform.SetParent(viewport.transform, false);

            RectTransform contentContainerRect = contentContainer.AddComponent<RectTransform>();
            contentContainerRect.anchorMin = new Vector2(0, 1);
            contentContainerRect.anchorMax = new Vector2(1, 1);
            contentContainerRect.pivot = new Vector2(0.5f, 1);
            contentContainerRect.offsetMin = new Vector2(0, 0);
            contentContainerRect.offsetMax = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(20, 20, 20, 20);
            contentLayout.spacing = 20;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = contentContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollComponent.content = contentContainerRect;

            // Create and configure the scrollbar AFTER viewport and content
            GameObject scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(scrollView.transform, false);
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1;

            // Create scrollbar background
            GameObject scrollbarBg = new GameObject("Background");
            scrollbarBg.transform.SetParent(scrollbarObj.transform, false);
            RectTransform bgRect = scrollbarBg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = scrollbarBg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Create scrollbar handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(scrollbarObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(1, 1);
            handleRect.sizeDelta = new Vector2(-4, 0);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.3f, 0.7f, 1f, 0.9f);

            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;

            // Connect scrollbar to ScrollRect
            scrollComponent.verticalScrollbar = scrollbar;

            // Ensure scrollbar is rendered on top by setting it as last sibling
            scrollbarObj.transform.SetAsLastSibling();

            // Create settings items
            CreateMoveSpeedSetting(contentContainer);
            CreateFollowSmoothnessSetting(contentContainer);
            CreateFOVSetting(contentContainer);
            CreateZoomSpeedSetting(contentContainer);
            CreateShowControlsSetting(contentContainer);
            CreateShowGridOverlaySetting(contentContainer);
            CreateDisableNPCEyeMovementSetting(contentContainer);
            CreateDisableCameraEffectsInFreeCamSetting(contentContainer);

            // Add save button
            GameObject saveButtonObj = new GameObject("SaveButton");
            saveButtonObj.transform.SetParent(contentContainer.transform, false);

            RectTransform saveButtonRect = saveButtonObj.AddComponent<RectTransform>();
            saveButtonRect.sizeDelta = new Vector2(200, 40);

            Image saveButtonImage = saveButtonObj.AddComponent<Image>();
            saveButtonImage.color = new Color(0.2f, 0.6f, 0.2f);

            Button saveButton = saveButtonObj.AddComponent<Button>();
            saveButton.targetGraphic = saveButtonImage;

            ColorBlock colors = saveButton.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.1f);
            saveButton.colors = colors;

            GameObject saveTextObj = new GameObject("Text");
            saveTextObj.transform.SetParent(saveButtonObj.transform, false);

            RectTransform saveTextRect = saveTextObj.AddComponent<RectTransform>();
            saveTextRect.anchorMin = Vector2.zero;
            saveTextRect.anchorMax = Vector2.one;
            saveTextRect.offsetMin = Vector2.zero;
            saveTextRect.offsetMax = Vector2.zero;

            Text saveText = saveTextObj.AddComponent<Text>();
            saveText.text = "Save Settings";
            saveText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            saveText.fontSize = 16;
            saveText.alignment = TextAnchor.MiddleCenter;
            saveText.color = Color.white;

#if IL2CPP
            Action saveAction = () => SaveSettings();
            saveButton.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(saveAction));
#else
            saveButton.onClick.AddListener(SaveSettings);
#endif

            // Don't load settings immediately - wait until the panel becomes visible
            _settingsInitialized = true;
        }

        private void CreateMoveSpeedSetting(GameObject parent)
        {
            // Similar structure as zoom speed but with different values
            GameObject settingObj = new GameObject("MoveSpeedSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 60);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.spacing = 15;
            settingLayout.padding = new RectOffset(0, 0, 10, 0);
            settingLayout.childAlignment = TextAnchor.UpperLeft;
            settingLayout.childControlHeight = false;
            settingLayout.childForceExpandHeight = false;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(0, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Camera Movement Speed";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            // Slider container
            GameObject sliderContainer = new GameObject("SliderContainer");
            sliderContainer.transform.SetParent(settingObj.transform, false);

            RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
            sliderContainerRect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup sliderLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
            sliderLayout.spacing = 10;
            sliderLayout.childAlignment = TextAnchor.MiddleLeft;
            sliderLayout.childControlWidth = false;
            sliderLayout.childForceExpandWidth = false;

            // Slider (simplified - using same structure as zoom)
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 20);

            _moveSpeedSlider = sliderObj.AddComponent<Slider>();
            _moveSpeedSlider.minValue = 0.5f;
            _moveSpeedSlider.maxValue = 25.0f;
            _moveSpeedSlider.value = 10.0f;
#if IL2CPP
            Action<float> moveSpeedAction = (value) => UpdateMoveSpeedValue(value);
            _moveSpeedSlider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(moveSpeedAction));
#else
            _moveSpeedSlider.onValueChanged.AddListener(UpdateMoveSpeedValue);
#endif

            // Handle slider visuals similar to zoom speed slider
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(0, -5);
            bgRect.offsetMax = new Vector2(0, 5);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _moveSpeedSlider.targetGraphic = bgImage;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.offsetMin = new Vector2(0, -5);
            fillRect.offsetMax = new Vector2(0, 5);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);

            _moveSpeedSlider.fillRect = fillRect;

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f);

            _moveSpeedSlider.handleRect = handleRect;

            // Value display
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(50, 20);

            _moveSpeedValue = valueObj.AddComponent<Text>();
            _moveSpeedValue.text = "10.0";
            _moveSpeedValue.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _moveSpeedValue.fontSize = 14;
            _moveSpeedValue.alignment = TextAnchor.MiddleCenter;
            _moveSpeedValue.color = Color.white;
        }

        private void CreateFollowSmoothnessSetting(GameObject parent)
        {
            // Similar to previous settings
            GameObject settingObj = new GameObject("FollowSmoothnessSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 60);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.spacing = 15;
            settingLayout.padding = new RectOffset(0, 0, 10, 0);
            settingLayout.childAlignment = TextAnchor.UpperLeft;
            settingLayout.childControlHeight = false;
            settingLayout.childForceExpandHeight = false;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(0, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Camera Follow Smoothness";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            // Slider and value
            GameObject sliderContainer = new GameObject("SliderContainer");
            sliderContainer.transform.SetParent(settingObj.transform, false);

            RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
            sliderContainerRect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup sliderLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
            sliderLayout.spacing = 10;
            sliderLayout.childAlignment = TextAnchor.MiddleLeft;
            sliderLayout.childControlWidth = false;
            sliderLayout.childForceExpandWidth = false;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 20);

            _followSmoothnessSlider = sliderObj.AddComponent<Slider>();
            _followSmoothnessSlider.minValue = 0.0f;
            _followSmoothnessSlider.maxValue = 1.0f;
            _followSmoothnessSlider.value = 0.5f;
#if IL2CPP
            Action<float> smoothnessAction = (value) => UpdateFollowSmoothnessValue(value);
            _followSmoothnessSlider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(smoothnessAction));
#else
            _followSmoothnessSlider.onValueChanged.AddListener(UpdateFollowSmoothnessValue);
#endif

            // Background, fill, handle similar to previous settings
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(0, -5);
            bgRect.offsetMax = new Vector2(0, 5);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _followSmoothnessSlider.targetGraphic = bgImage;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.offsetMin = new Vector2(0, -5);
            fillRect.offsetMax = new Vector2(0, 5);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);

            _followSmoothnessSlider.fillRect = fillRect;

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f);

            _followSmoothnessSlider.handleRect = handleRect;

            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(50, 20);

            _followSmoothnessValue = valueObj.AddComponent<Text>();
            _followSmoothnessValue.text = "0.5";
            _followSmoothnessValue.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _followSmoothnessValue.fontSize = 14;
            _followSmoothnessValue.alignment = TextAnchor.MiddleCenter;
            _followSmoothnessValue.color = Color.white;
        }

        private void CreateFOVSetting(GameObject parent)
        {
            GameObject settingObj = new GameObject("FOVSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 60);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.spacing = 15;
            settingLayout.padding = new RectOffset(0, 0, 10, 0);
            settingLayout.childAlignment = TextAnchor.UpperLeft;
            settingLayout.childControlHeight = false;
            settingLayout.childForceExpandHeight = false;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(0, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Field of View (FOV)";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            // Slider and value container
            GameObject sliderContainer = new GameObject("SliderContainer");
            sliderContainer.transform.SetParent(settingObj.transform, false);

            RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
            sliderContainerRect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup sliderLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
            sliderLayout.spacing = 10;
            sliderLayout.childAlignment = TextAnchor.MiddleLeft;
            sliderLayout.childControlWidth = false;
            sliderLayout.childForceExpandWidth = false;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 20);

            _fovSlider = sliderObj.AddComponent<Slider>();
            _fovSlider.minValue = 10f;
            _fovSlider.maxValue = 120f;
            _fovSlider.value = 60f;
#if IL2CPP
            Action<float> fovAction = (value) => {
                UpdateFOVValue(value);
                // Apply FOV change in real-time
                if (Core.Instance.CameraManager != null)
                {
                    Core.Instance.CameraManager.SetFOV(value);
                }
            };
            _fovSlider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(fovAction));
#else
            _fovSlider.onValueChanged.AddListener((value) => {
                UpdateFOVValue(value);
                // Apply FOV change in real-time
                if (Core.Instance.CameraManager != null)
                {
                    Core.Instance.CameraManager.SetFOV(value);
                }
            });
#endif

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(0, -5);
            bgRect.offsetMax = new Vector2(0, 5);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _fovSlider.targetGraphic = bgImage;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.offsetMin = new Vector2(0, -5);
            fillRect.offsetMax = new Vector2(0, 5);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);

            _fovSlider.fillRect = fillRect;

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f);

            _fovSlider.handleRect = handleRect;

            // Value display
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(50, 20);

            _fovValue = valueObj.AddComponent<Text>();
            _fovValue.text = "60";
            _fovValue.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _fovValue.fontSize = 14;
            _fovValue.alignment = TextAnchor.MiddleCenter;
            _fovValue.color = Color.white;
        }

        private void CreateZoomSpeedSetting(GameObject parent)
        {
            // Similar structure as zoom speed but with different values
            GameObject settingObj = new GameObject("ZoomSpeedSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 60);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.spacing = 15;
            settingLayout.padding = new RectOffset(0, 0, 10, 0);
            settingLayout.childAlignment = TextAnchor.UpperLeft;
            settingLayout.childControlHeight = false;
            settingLayout.childForceExpandHeight = false;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(settingObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(0, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Camera Zoom Speed";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            // Slider container
            GameObject sliderContainer = new GameObject("SliderContainer");
            sliderContainer.transform.SetParent(settingObj.transform, false);

            RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
            sliderContainerRect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup sliderLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
            sliderLayout.spacing = 10;
            sliderLayout.childAlignment = TextAnchor.MiddleLeft;
            sliderLayout.childControlWidth = false;
            sliderLayout.childForceExpandWidth = false;

            // Slider (simplified - using same structure as zoom)
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 20);

            _zoomSpeedSlider = sliderObj.AddComponent<Slider>();
            _zoomSpeedSlider.minValue = 0.1f;
            _zoomSpeedSlider.maxValue = 5.0f;
            _zoomSpeedSlider.value = 1.0f;
#if IL2CPP
            Action<float> zoomSpeedAction = (value) => UpdateZoomSpeedValue(value);
            _zoomSpeedSlider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(zoomSpeedAction));
#else
            _zoomSpeedSlider.onValueChanged.AddListener(UpdateZoomSpeedValue);
#endif

            // Handle slider visuals similar to zoom speed slider
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(0, -5);
            bgRect.offsetMax = new Vector2(0, 5);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _zoomSpeedSlider.targetGraphic = bgImage;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.offsetMin = new Vector2(0, -5);
            fillRect.offsetMax = new Vector2(0, 5);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);

            _zoomSpeedSlider.fillRect = fillRect;

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(sliderObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f);

            _zoomSpeedSlider.handleRect = handleRect;

            // Value display
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(sliderContainer.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(50, 20);

            _zoomSpeedValue = valueObj.AddComponent<Text>();
            _zoomSpeedValue.text = "1.0";
            _zoomSpeedValue.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _zoomSpeedValue.fontSize = 14;
            _zoomSpeedValue.alignment = TextAnchor.MiddleCenter;
            _zoomSpeedValue.color = Color.white;
        }

        private void CreateShowControlsSetting(GameObject parent)
        {
            GameObject settingObj = new GameObject("ShowControlsSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 40);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.padding = new RectOffset(0, 0, 20, 0);
            settingLayout.spacing = 10;
            settingLayout.childAlignment = TextAnchor.MiddleLeft;
            settingLayout.childControlWidth = false;
            settingLayout.childForceExpandWidth = false;

            // Create a container for the toggle and label
            GameObject toggleContainer = new GameObject("ToggleContainer");
            toggleContainer.transform.SetParent(settingObj.transform, false);

            RectTransform toggleContainerRect = toggleContainer.AddComponent<RectTransform>();
            toggleContainerRect.sizeDelta = new Vector2(0, 20);

            HorizontalLayoutGroup toggleLayout = toggleContainer.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 10;
            toggleLayout.childAlignment = TextAnchor.MiddleLeft;
            toggleLayout.childControlWidth = false;
            toggleLayout.childForceExpandWidth = false;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(20, 20);
            toggleRect.anchorMin = new Vector2(0, 0.5f);
            toggleRect.anchorMax = new Vector2(0, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);

            _showControlsToggle = toggleObj.AddComponent<Toggle>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _showControlsToggle.targetGraphic = bgImage;

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.7f, 1f);

            _showControlsToggle.graphic = checkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Show Controls on Startup";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
        }

        private void CreateShowGridOverlaySetting(GameObject parent)
        {
            GameObject settingObj = new GameObject("ShowGridOverlaySetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 40);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.padding = new RectOffset(0, 0, 20, 0);
            settingLayout.spacing = 10;
            settingLayout.childAlignment = TextAnchor.MiddleLeft;
            settingLayout.childControlWidth = false;
            settingLayout.childForceExpandWidth = false;

            // Create a container for the toggle and label
            GameObject toggleContainer = new GameObject("ToggleContainer");
            toggleContainer.transform.SetParent(settingObj.transform, false);

            RectTransform toggleContainerRect = toggleContainer.AddComponent<RectTransform>();
            toggleContainerRect.sizeDelta = new Vector2(0, 20);

            HorizontalLayoutGroup toggleLayout = toggleContainer.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 10;
            toggleLayout.childAlignment = TextAnchor.MiddleLeft;
            toggleLayout.childControlWidth = false;
            toggleLayout.childForceExpandWidth = false;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(20, 20);
            toggleRect.anchorMin = new Vector2(0, 0.5f);
            toggleRect.anchorMax = new Vector2(0, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);

            _showGridOverlayToggle = toggleObj.AddComponent<Toggle>();

            // Set real-time update
#if IL2CPP
            Action<bool> gridAction = (value) => Core.ShowGridOverlay.Value = value;
            _showGridOverlayToggle.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(gridAction));
#else
            _showGridOverlayToggle.onValueChanged.AddListener((value) => Core.ShowGridOverlay.Value = value);
#endif

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _showGridOverlayToggle.targetGraphic = bgImage;

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.7f, 1f);

            _showGridOverlayToggle.graphic = checkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Show Grid Overlay";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
        }

        private void CreateDisableNPCEyeMovementSetting(GameObject parent)
        {
            GameObject settingObj = new GameObject("DisableNPCEyeMovementSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 40);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.padding = new RectOffset(0, 0, 20, 0);
            settingLayout.spacing = 10;
            settingLayout.childAlignment = TextAnchor.MiddleLeft;
            settingLayout.childControlWidth = false;
            settingLayout.childForceExpandWidth = false;

            // Create a container for the toggle and label
            GameObject toggleContainer = new GameObject("ToggleContainer");
            toggleContainer.transform.SetParent(settingObj.transform, false);

            RectTransform toggleContainerRect = toggleContainer.AddComponent<RectTransform>();
            toggleContainerRect.sizeDelta = new Vector2(0, 20);

            HorizontalLayoutGroup toggleLayout = toggleContainer.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 10;
            toggleLayout.childAlignment = TextAnchor.MiddleLeft;
            toggleLayout.childControlWidth = false;
            toggleLayout.childForceExpandWidth = false;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(20, 20);
            toggleRect.anchorMin = new Vector2(0, 0.5f);
            toggleRect.anchorMax = new Vector2(0, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);

            _disableNPCEyeMovementToggle = toggleObj.AddComponent<Toggle>();

            // Set real-time update
#if IL2CPP
            Action<bool> disableNPCEyeMovementAction = (value) => Core.DisableNPCEyeMovement.Value = value;
            _disableNPCEyeMovementToggle.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(disableNPCEyeMovementAction));
#else
            _disableNPCEyeMovementToggle.onValueChanged.AddListener((value) => Core.DisableNPCEyeMovement.Value = value);
#endif

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _disableNPCEyeMovementToggle.targetGraphic = bgImage;

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.7f, 1f);

            _disableNPCEyeMovementToggle.graphic = checkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Disable NPC Eye Movement";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
        }

        private void CreateDisableCameraEffectsInFreeCamSetting(GameObject parent)
        {
            GameObject settingObj = new GameObject("DisableCameraEffectsInFreeCamSetting");
            settingObj.transform.SetParent(parent.transform, false);

            RectTransform settingRect = settingObj.AddComponent<RectTransform>();
            settingRect.sizeDelta = new Vector2(0, 40);

            VerticalLayoutGroup settingLayout = settingObj.AddComponent<VerticalLayoutGroup>();
            settingLayout.padding = new RectOffset(0, 0, 20, 0);
            settingLayout.spacing = 10;
            settingLayout.childAlignment = TextAnchor.MiddleLeft;
            settingLayout.childControlWidth = false;
            settingLayout.childForceExpandWidth = false;

            // Create a container for the toggle and label
            GameObject toggleContainer = new GameObject("ToggleContainer");
            toggleContainer.transform.SetParent(settingObj.transform, false);

            RectTransform toggleContainerRect = toggleContainer.AddComponent<RectTransform>();
            toggleContainerRect.sizeDelta = new Vector2(0, 20);

            HorizontalLayoutGroup toggleLayout = toggleContainer.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 10;
            toggleLayout.childAlignment = TextAnchor.MiddleLeft;
            toggleLayout.childControlWidth = false;
            toggleLayout.childForceExpandWidth = false;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(20, 20);
            toggleRect.anchorMin = new Vector2(0, 0.5f);
            toggleRect.anchorMax = new Vector2(0, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);

            _disableCameraEffectsInFreeCamToggle = toggleObj.AddComponent<Toggle>();

            // Set real-time update
#if IL2CPP
            Action<bool> disableCameraEffectsInFreeCamAction = (value) => Core.DisableCameraEffectsInFreeCam.Value = value;
            _disableCameraEffectsInFreeCamToggle.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(disableCameraEffectsInFreeCamAction));
#else
            _disableCameraEffectsInFreeCamToggle.onValueChanged.AddListener((value) => Core.DisableCameraEffectsInFreeCam.Value = value);
#endif

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            _disableCameraEffectsInFreeCamToggle.targetGraphic = bgImage;

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.7f, 1f);

            _disableCameraEffectsInFreeCamToggle.graphic = checkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleContainer.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, 20);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Disable Camera Effects in Free Cam";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
        }

        private void UpdateMoveSpeedValue(float value)
        {
            if (_moveSpeedValue != null)
            {
                _moveSpeedValue.text = value.ToString("F1");
            }

            // Apply move speed in real-time
            Core.CameraMovementSpeed.Value = value;

            // Update freecam speed if active
            if (Core.Instance.CameraManager != null && Core.Instance.CameraManager.IsFreeCamEnabled && !Core.Instance.CameraManager.IsCameraLocked)
            {
                Core.Instance.CameraManager.SetFreeCamSpeed(value);
            }
        }

        private void UpdateFollowSmoothnessValue(float value)
        {
            if (_followSmoothnessValue != null)
            {
                _followSmoothnessValue.text = value.ToString("F1");
            }

            // Apply follow smoothness in real-time
            Core.CameraFollowSmoothness.Value = value;
        }

        private void UpdateFOVValue(float value)
        {
            if (_fovValue != null)
            {
                _fovValue.text = value.ToString("F0");
            }
        }

        private void UpdateZoomSpeedValue(float value)
        {
            if (_zoomSpeedValue != null)
            {
                _zoomSpeedValue.text = value.ToString("F1");
            }

            // Apply zoom speed in real-time
            Core.CameraZoomSpeed.Value = value;
        }

        private void LoadCurrentSettings()
        {
            // Check if Core is initialized
            if (Core.Instance == null)
                return;

            // Safety checks for Core static properties
            if (Core.CameraMovementSpeed == null ||
                Core.CameraFollowSmoothness == null || Core.ShowControlsOnStartup == null ||
                Core.ShowGridOverlay == null || Core.DefaultFov == null || Core.CameraZoomSpeed == null ||
                Core.DisableNPCEyeMovement == null || Core.DisableCameraEffectsInFreeCam == null)
            {
                // Settings not yet initialized in Core
                return;
            }

            // Load from Core's preference variables
            if (_moveSpeedSlider != null)
            {
                _moveSpeedSlider.value = Core.CameraMovementSpeed.Value;
                UpdateMoveSpeedValue(Core.CameraMovementSpeed.Value);
            }

            if (_followSmoothnessSlider != null)
            {
                _followSmoothnessSlider.value = Core.CameraFollowSmoothness.Value;
                UpdateFollowSmoothnessValue(Core.CameraFollowSmoothness.Value);
            }

            if (_showControlsToggle != null)
            {
                _showControlsToggle.isOn = Core.ShowControlsOnStartup.Value;
            }

            if (_showGridOverlayToggle != null)
            {
                _showGridOverlayToggle.isOn = Core.ShowGridOverlay.Value;
            }

            if (_disableNPCEyeMovementToggle != null)
            {
                _disableNPCEyeMovementToggle.isOn = Core.DisableNPCEyeMovement.Value;
            }

            if (_disableCameraEffectsInFreeCamToggle != null)
            {
                _disableCameraEffectsInFreeCamToggle.isOn = Core.DisableCameraEffectsInFreeCam.Value;
            }

            if (_fovSlider != null)
            {
                _fovSlider.value = Core.DefaultFov.Value;
                UpdateFOVValue(Core.DefaultFov.Value);
            }

            if (_zoomSpeedSlider != null)
            {
                _zoomSpeedSlider.value = Core.CameraZoomSpeed.Value;
                UpdateZoomSpeedValue(Core.CameraZoomSpeed.Value);
            }
        }

        private void SaveSettings()
        {
            // Safety check for Core static properties
            if (Core.CameraMovementSpeed == null ||
                Core.CameraFollowSmoothness == null || Core.ShowControlsOnStartup == null ||
                Core.ShowGridOverlay == null || Core.DefaultFov == null || Core.CameraZoomSpeed == null ||
                Core.DisableNPCEyeMovement == null || Core.DisableCameraEffectsInFreeCam == null)
            {
                return;
            }

            // Update Core's preference variables
            Core.CameraMovementSpeed.Value = _moveSpeedSlider.value;
            Core.CameraFollowSmoothness.Value = _followSmoothnessSlider.value;
            Core.ShowControlsOnStartup.Value = _showControlsToggle.isOn;
            Core.ShowGridOverlay.Value = _showGridOverlayToggle.isOn;
            Core.DisableNPCEyeMovement.Value = _disableNPCEyeMovementToggle.isOn;
            Core.DisableCameraEffectsInFreeCam.Value = _disableCameraEffectsInFreeCamToggle.isOn;
            Core.DefaultFov.Value = _fovSlider.value;
            Core.CameraZoomSpeed.Value = _zoomSpeedSlider.value;

            // Update camera FOV if camera is active
            if (Core.Instance.CameraManager != null)
            {
                Core.Instance.CameraManager.SetFOV(_fovSlider.value);

                // Update the free cam speed if active
                if (Core.Instance.CameraManager.IsFreeCamEnabled && !Core.Instance.CameraManager.IsCameraLocked)
                {
                    Core.Instance.CameraManager.SetFreeCamSpeed(Core.CameraMovementSpeed.Value);
                }
            }

            // Save preferences
            MelonLoader.MelonPreferences.Save();
        }

        public override void SetVisible(bool visible)
        {
            base.SetVisible(visible);

            // Load settings when the panel becomes visible
            if (visible && _settingsInitialized)
            {
                LoadCurrentSettings();
            }
        }
    }
}