using CineCam.Managers;
using CineCam.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
#if IL2CPP
using Il2CppInterop.Runtime;
#endif

namespace CineCam.UI
{
    public class HelpPanel : BasePanel
    {
        private Text _cameraStatusText;
        private GameObject _tabContainer;
        private GameObject _contentContainer;
        private readonly Dictionary<string, GameObject> _tabContents = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Button> _tabButtons = new Dictionary<string, Button>();
        private string _activeTab = "Movement";

        // Control categories data
        private Dictionary<string, ControlCategory> _controlCategories;

        public HelpPanel(GameObject parent) : base(parent, "Controls & Help", new Vector2(550, 900))
        {
            try
            {
                InitializeControlCategories();
                CreateHelpContent();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating help panel content: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void InitializeControlCategories()
        {
            _controlCategories = new Dictionary<string, ControlCategory>
            {
                ["Movement"] = new("Camera Movement & Navigation", new Dictionary<string, string>
                {
                    ["F2"] = "Toggle free-cam mode (switch between free camera and normal view)",
                    ["WASD"] = "Move camera forward/back/left/right in free-cam mode",
                    ["Mouse"] = "Look around and rotate camera view in free-cam mode",
                    ["Mouse Scroll"] = "Adjust free-cam movement speed (scroll up to increase, down to decrease)",
                    ["Q / E"] = "Move camera up/down in world space",
                    ["LCtrl / Space"] = "Move camera up/down relative to camera orientation",
                    ["PgUp / PgDn"] = "Zoom camera in/out (adjust field of view)",
                    ["LAlt + Movement"] = "Hold for precise, slower camera movement"
                }),

                ["Modes"] = new("Camera Modes & States", new Dictionary<string, string>
                {
                    ["F5"] = "Lock/unlock camera movement (freeze camera position)",
                    ["F6"] = "Toggle player movement on/off (enable/disable player controls)",
                    ["F7"] = "Switch player follow mode",
                    ["F8"] = "Toggle time freeze (pause/unpause game time)",
                    ["F11"] = "Toggle fullscreen camera preview mode",
                    ["ESC"] = "Exit camera mode and return to normal gameplay"
                }),

                ["Visual"] = new("Visual Aids & Debug Tools", new Dictionary<string, string>
                {
                    ["G"] = "Toggle grid overlay (show/hide reference grid)",
                    ["F1"] = "Toggle camera editor UI on/off",
                    ["Mouse Click"] = "Interact with buttons and UI elements",
                    ["Mouse Drag"] = "Move and reposition UI panels"
                }),
            };
        }

        private void CreateHelpContent()
        {
            GameObject contentArea = GetContentArea();
            if (contentArea == null)
                throw new Exception("ContentArea is null, cannot create help panel content");

            try
            {
                // Main layout with optimized spacing for more content space
                VerticalLayoutGroup mainLayout = contentArea.AddComponent<VerticalLayoutGroup>();
                mainLayout.padding = new RectOffset(5, 5, 5, 5);
                mainLayout.spacing = 5;
                mainLayout.childAlignment = TextAnchor.UpperCenter;
                mainLayout.childControlWidth = true;
                mainLayout.childControlHeight = true;
                mainLayout.childForceExpandWidth = true;
                mainLayout.childForceExpandHeight = true;

                // 1. Create tab container
                CreateTabContainer(contentArea);

                // 2. Create content container
                CreateContentContainer(contentArea);

                // 3. Create all tab content
                CreateAllTabContents();

                // 4. Create status section
                GameObject statusSection = CreateStatusSection();
                if (statusSection != null)
                    statusSection.transform.SetParent(contentArea.transform, false);

                // 5. Show default tab
                ShowTab(_activeTab);

                // Force layout update
                Canvas.ForceUpdateCanvases();
                if (mainLayout != null)
                {
                    mainLayout.enabled = false;
                    mainLayout.enabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in CreateHelpContent: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void CreateTabContainer(GameObject parent)
        {
            _tabContainer = new GameObject("TabContainer");
            _tabContainer.transform.SetParent(parent.transform, false);

            RectTransform tabRect = _tabContainer.AddComponent<RectTransform>();
            LayoutElement tabLayout = _tabContainer.AddComponent<LayoutElement>();
            // Reduce tab height to give more space to content
            tabLayout.minHeight = 35;
            tabLayout.preferredHeight = 35;
            tabLayout.flexibleHeight = 0; // Ensure tab container doesn't expand

            // Background with improved styling
            Image tabBg = _tabContainer.AddComponent<Image>();
            tabBg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            // Horizontal layout for tabs with better spacing
            HorizontalLayoutGroup tabLayoutGroup = _tabContainer.AddComponent<HorizontalLayoutGroup>();
            tabLayoutGroup.padding = new RectOffset(8, 8, 5, 5);
            tabLayoutGroup.spacing = 3;
            tabLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            tabLayoutGroup.childControlWidth = true;
            tabLayoutGroup.childControlHeight = true;
            tabLayoutGroup.childForceExpandWidth = true;
            tabLayoutGroup.childForceExpandHeight = true;

            // Create tab buttons
            foreach (var category in _controlCategories.Keys)
            {
                CreateTabButton(category);
            }
        }

        private void CreateTabButton(string categoryName)
        {
            GameObject buttonObj = new GameObject($"Tab_{categoryName}");
            buttonObj.transform.SetParent(_tabContainer.transform, false);

            Image buttonBg = buttonObj.AddComponent<Image>();
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;

            // Tab button styling with improved colors and transitions
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.22f, 0.22f, 0.9f);
            colors.highlightedColor = new Color(0.32f, 0.32f, 0.32f, 0.95f);
            colors.pressedColor = new Color(0.25f, 0.6f, 0.9f, 1f);
            colors.selectedColor = new Color(0.25f, 0.6f, 0.9f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Button text with improved styling
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6, 3);
            textRect.offsetMax = new Vector2(-6, -3);

            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = categoryName;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 14;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            // Add click handler
#if IL2CPP
            Action clickAction = () => ShowTab(categoryName);
            button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(clickAction));
#else
            button.onClick.AddListener(() => ShowTab(categoryName));
#endif

            _tabButtons[categoryName] = button;
        }

        private void CreateContentContainer(GameObject parent)
        {
            _contentContainer = new GameObject("ContentContainer");
            _contentContainer.transform.SetParent(parent.transform, false);

            RectTransform contentRect = _contentContainer.AddComponent<RectTransform>();
            LayoutElement contentLayout = _contentContainer.AddComponent<LayoutElement>();
            // Maximize content container size to use most of the panel space
            contentLayout.minHeight = 400;
            contentLayout.preferredHeight = 600;
            contentLayout.flexibleHeight = 5; // Much higher flexible height to dominate layout

            // Background with better styling
            Image contentBg = _contentContainer.AddComponent<Image>();
            contentBg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            // Add subtle border effect
            contentBg.type = Image.Type.Sliced;
        }

        private void CreateAllTabContents()
        {
            foreach (var (categoryName, category) in _controlCategories)
            {
                GameObject tabContent = CreateTabContent(categoryName, category);
                if (tabContent == null) continue;
                _tabContents[categoryName] = tabContent;
                tabContent.SetActive(false); // Initially hidden
            }
        }

        private GameObject CreateTabContent(string categoryName, ControlCategory category)
        {
            try
            {
                GameObject tabContent = new GameObject($"Content_{categoryName}");
                tabContent.transform.SetParent(_contentContainer.transform, false);

                RectTransform contentRect = tabContent.AddComponent<RectTransform>();
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;

                // Create scroll view for content that fills the entire tab content area
                GameObject scrollView = UIControls.CreateScrollView(tabContent, out GameObject scrollContent, "ScrollView");

                RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
                scrollRect.anchorMin = Vector2.zero;
                scrollRect.anchorMax = Vector2.one;
                // Reduce margins to maximize space for controls
                scrollRect.offsetMin = new Vector2(5, 5);
                scrollRect.offsetMax = new Vector2(-25, -5); // Leave more space for scrollbar and padding

                // Configure the ScrollRect component for visible scrollbar
                ScrollRect scrollRectComponent = scrollView.GetComponent<ScrollRect>();
                if (scrollRectComponent != null)
                {
                    scrollRectComponent.horizontal = false;
                    scrollRectComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                }

                // Configure the existing layout components created by UIControls.CreateScrollView
                VerticalLayoutGroup scrollLayout = scrollContent.GetComponent<VerticalLayoutGroup>();
                if (scrollLayout != null)
                {
                    scrollLayout.padding = new RectOffset(8, 8, 8, 8);
                    scrollLayout.spacing = 4;
                    scrollLayout.childAlignment = TextAnchor.UpperCenter;
                    scrollLayout.childControlWidth = true;
                    scrollLayout.childControlHeight = false;
                    scrollLayout.childForceExpandWidth = true;
                    scrollLayout.childForceExpandHeight = false;
                }

                // Ensure ContentSizeFitter is properly configured
                ContentSizeFitter contentSizeFitter = scrollContent.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    contentSizeFitter.enabled = false;
                    contentSizeFitter.enabled = true; // Force refresh
                }

                // Create and configure the scrollbar AFTER content setup
                GameObject scrollbarObj = new GameObject("Scrollbar");
                scrollbarObj.transform.SetParent(scrollView.transform, false);
                RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.pivot = new Vector2(1, 0.5f);
                scrollbarRect.sizeDelta = new Vector2(18, -10); // Slightly smaller width and height padding
                scrollbarRect.anchoredPosition = new Vector2(-2, 0); // Small offset from edge

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
                if (scrollRectComponent != null)
                {
                    scrollRectComponent.verticalScrollbar = scrollbar;
                }

                // Ensure scrollbar is rendered on top by setting it as last sibling
                scrollbarObj.transform.SetAsLastSibling();

                // Add title with improved styling
                GameObject titleObj = new GameObject("CategoryTitle");
                titleObj.transform.SetParent(scrollContent.transform, false);

                // Create title background
                Image titleBg = titleObj.AddComponent<Image>();
                titleBg.color = new Color(0.2f, 0.35f, 0.5f, 0.9f);

                LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
                titleLayout.minHeight = 45;
                titleLayout.preferredHeight = 45;
                titleLayout.flexibleHeight = 0;

                Text titleText = UIControls.CreateLabel(titleObj, category.Title, "TitleText");
                titleText.fontSize = 20;
                titleText.fontStyle = FontStyle.Bold;
                titleText.color = new Color(1f, 1f, 1f, 1f);
                titleText.alignment = TextAnchor.MiddleCenter;

                // Position title text to fill the title background with padding
                RectTransform titleTextRect = titleText.GetComponent<RectTransform>();
                titleTextRect.anchorMin = Vector2.zero;
                titleTextRect.anchorMax = Vector2.one;
                titleTextRect.offsetMin = new Vector2(10, 5);
                titleTextRect.offsetMax = new Vector2(-15, -5);

                // Add controls with improved spacing
                for (int i = 0; i < category.Controls.Count; i++)
                {
                    var control = category.Controls.ElementAt(i);
                    CreateControlItem(scrollContent, control.Key, control.Value, i % 2 == 0);
                }

                // Add category-specific notes if any
                if (!string.IsNullOrEmpty(category.Notes))
                {
                    // Add spacer before notes
                    GameObject notesSpacer = new GameObject("NotesSpacer");
                    notesSpacer.transform.SetParent(scrollContent.transform, false);
                    LayoutElement notesSpacerLayout = notesSpacer.AddComponent<LayoutElement>();
                    notesSpacerLayout.minHeight = 12;
                    notesSpacerLayout.preferredHeight = 12;
                    notesSpacerLayout.flexibleHeight = 0;

                    GameObject noteObj = new GameObject("CategoryNote");
                    noteObj.transform.SetParent(scrollContent.transform, false);

                    Image noteBg = noteObj.AddComponent<Image>();
                    noteBg.color = new Color(0.18f, 0.15f, 0.1f, 0.9f);

                    LayoutElement noteLayout = noteObj.AddComponent<LayoutElement>();
                    noteLayout.minHeight = 50;
                    noteLayout.preferredHeight = 50;
                    noteLayout.flexibleHeight = 0;

                    Text noteText = UIControls.CreateLabel(noteObj, category.Notes, "NoteText");
                    noteText.fontSize = 14;
                    noteText.fontStyle = FontStyle.Italic;
                    noteText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
                    noteText.alignment = TextAnchor.MiddleCenter;

                    RectTransform noteTextRect = noteText.GetComponent<RectTransform>();
                    noteTextRect.anchorMin = Vector2.zero;
                    noteTextRect.anchorMax = Vector2.one;
                    noteTextRect.offsetMin = new Vector2(10, 5);
                    noteTextRect.offsetMax = new Vector2(-10, -5);
                }

                // Force immediate layout rebuild for the scroll content
                if (scrollContent == null) return tabContent;
                // Force layout rebuild on scroll content first
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent.GetComponent<RectTransform>());

                // Also force rebuild on the scroll view itself
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.GetComponent<RectTransform>());

                // Force the ScrollRect to update its content bounds
                ScrollRect scrollRectToRebuild = scrollView.GetComponent<ScrollRect>();
                if (scrollRectToRebuild == null) return tabContent;
                Canvas.ForceUpdateCanvases();
                scrollRectToRebuild.Rebuild(CanvasUpdate.PostLayout);

                return tabContent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating tab content for {categoryName}: {ex.Message}");
                return null;
            }
        }

        private void CreateControlItem(GameObject parent, string key, string description, bool isEven)
        {
            GameObject controlItem = new GameObject($"Control_{key}");
            controlItem.transform.SetParent(parent.transform, false);

            LayoutElement controlLayout = controlItem.AddComponent<LayoutElement>();
            controlLayout.minHeight = 33;
            controlLayout.preferredHeight = 33;
            controlLayout.flexibleHeight = 0;

            // Background with alternating colors for better readability
            Image itemBg = controlItem.AddComponent<Image>();
            itemBg.color = isEven ? new Color(0.12f, 0.12f, 0.12f, 0.9f) : new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // Use horizontal layout for key and description with improved spacing
            HorizontalLayoutGroup itemLayoutGroup = controlItem.AddComponent<HorizontalLayoutGroup>();
            itemLayoutGroup.padding = new RectOffset(8, 8, 6, 6);
            itemLayoutGroup.spacing = 12;
            itemLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            itemLayoutGroup.childControlWidth = true;
            itemLayoutGroup.childControlHeight = true;
            itemLayoutGroup.childForceExpandWidth = true;
            itemLayoutGroup.childForceExpandHeight = true;

            // Key part with improved styling
            GameObject keyObj = new GameObject("Key");
            keyObj.transform.SetParent(controlItem.transform, false);

            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = new Color(0.25f, 0.6f, 0.9f, 0.9f);

            LayoutElement keyLayout = keyObj.AddComponent<LayoutElement>();
            keyLayout.minWidth = 80;
            keyLayout.preferredWidth = 80;
            keyLayout.flexibleWidth = 0;

            // Key text with better styling
            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyObj.transform, false);

            RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.offsetMin = new Vector2(4, 2);
            keyTextRect.offsetMax = new Vector2(-4, -2);

            Text keyText = keyTextObj.AddComponent<Text>();
            keyText.text = key;
            keyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            keyText.fontSize = 20;
            keyText.fontStyle = FontStyle.Bold;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.color = Color.white;

            // Description part with improved layout
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(controlItem.transform, false);

            LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
            descLayout.flexibleWidth = 1;
            descLayout.minWidth = 225;

            // Description text with better positioning
            GameObject descTextObj = new GameObject("DescText");
            descTextObj.transform.SetParent(descObj.transform, false);

            RectTransform descTextRect = descTextObj.AddComponent<RectTransform>();
            descTextRect.anchorMin = Vector2.zero;
            descTextRect.anchorMax = Vector2.one;
            descTextRect.offsetMin = new Vector2(8, 2);
            descTextRect.offsetMax = new Vector2(-8, -2);

            Text descText = descTextObj.AddComponent<Text>();
            descText.text = description;
            descText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            descText.fontSize = 14;
            descText.alignment = TextAnchor.MiddleLeft;
            descText.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            // Enable text wrapping for longer descriptions
            descText.resizeTextForBestFit = false;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void ShowTab(string tabName)
        {
            if (!_controlCategories.ContainsKey(tabName))
                return;

            // Hide all tab contents
            foreach (var content in _tabContents.Values.Where(content => content != null))
            {
                content.SetActive(false);
            }

            // Show selected tab content
            if (_tabContents.ContainsKey(tabName) && _tabContents[tabName] != null)
            {
                _tabContents[tabName].SetActive(true);
            }

            // Update tab button states with improved visual feedback
            foreach (var kvp in _tabButtons)
            {
                string buttonTabName = kvp.Key;
                Button button = kvp.Value;

                if (button == null || button.targetGraphic is not Image image) continue;
                if (buttonTabName == tabName)
                {
                    // Active tab styling
                    image.color = new Color(0.25f, 0.6f, 0.9f, 1f);

                    // Update text color for active tab
                    Text buttonText = button.GetComponentInChildren<Text>();
                    if (buttonText != null)
                        buttonText.color = Color.white;
                }
                else
                {
                    // Inactive tab styling
                    image.color = new Color(0.22f, 0.22f, 0.22f, 0.9f);

                    // Update text color for inactive tab
                    Text buttonText = button.GetComponentInChildren<Text>();
                    if (buttonText != null)
                        buttonText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }
            }

            _activeTab = tabName;
        }

        private GameObject CreateStatusSection()
        {
            try
            {
                GameObject statusSection = new GameObject("StatusSection");
                if (statusSection == null)
                    throw new Exception("Failed to create StatusSection GameObject");

                RectTransform statusRect = statusSection.AddComponent<RectTransform>();
                if (statusRect == null)
                    throw new Exception("Failed to add RectTransform to StatusSection");

                LayoutElement statusLayout = statusSection.AddComponent<LayoutElement>();
                if (statusLayout == null)
                    throw new Exception("Failed to add LayoutElement to StatusSection");

                // Reduce status section height to give more space to content
                statusLayout.minHeight = 30;
                statusLayout.preferredHeight = 30;
                statusLayout.flexibleHeight = 0;

                // Background with improved styling to match the design
                Image statusBg = statusSection.AddComponent<Image>();
                if (statusBg == null)
                    throw new Exception("Failed to add Image to StatusSection");

                statusBg.color = new Color(0.05f, 0.05f, 0.05f, 0.98f);

                // Container for status text with padding
                GameObject textContainer = new GameObject("TextContainer");
                textContainer.transform.SetParent(statusSection.transform, false);

                RectTransform textContainerRect = textContainer.AddComponent<RectTransform>();
                textContainerRect.anchorMin = Vector2.zero;
                textContainerRect.anchorMax = Vector2.one;
                textContainerRect.offsetMin = new Vector2(10, 4);
                textContainerRect.offsetMax = new Vector2(-10, -4);

                // Get font before creating text component
                Font arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (arialFont == null)
                    Debug.LogWarning("Could not load Arial font, text may not display correctly");

                // Status text with improved styling
                _cameraStatusText = textContainer.AddComponent<Text>();
                if (_cameraStatusText == null)
                    throw new Exception("Failed to add Text to StatusSection");

                _cameraStatusText.text = "Camera system: Waiting for player...";
                _cameraStatusText.font = arialFont;
                _cameraStatusText.fontSize = 14;
                _cameraStatusText.fontStyle = FontStyle.Bold;
                _cameraStatusText.alignment = TextAnchor.MiddleCenter;
                _cameraStatusText.color = new Color(1f, 0.85f, 0.3f, 1f);

                return statusSection;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating status section: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public override void UpdateCameraStatus(CinematicCameraManager cameraManager)
        {
            if (_cameraStatusText == null)
            {
                Debug.LogWarning("Cannot update camera status: _cameraStatusText is null");
                return;
            }

            try
            {
                _cameraStatusText.text = cameraManager?.playerCamera != null ? "Camera system: Active" : "Camera system: Waiting for player...";
                _cameraStatusText.color = cameraManager?.playerCamera != null ? new Color(0.3f, 0.8f, 0.4f, 1f) : new Color(1f, 0.85f, 0.3f, 1f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating camera status: {ex.Message}");
            }
        }

        // Method to add or update a control category
        public void AddControlCategory(string categoryName, ControlCategory category)
        {
            _controlCategories[categoryName] = category;

            // If UI is already created, recreate the tabs
            if (_tabContainer != null)
            {
                // TODO: Implement dynamic tab addition
                Debug.Log($"Dynamic category addition not yet implemented: {categoryName}");
            }
        }

        // Method to switch to a specific tab programmatically
        public void SwitchToTab(string tabName)
        {
            ShowTab(tabName);
        }
    }

    // Helper class to organize control categories
    [Serializable]
    public class ControlCategory(string title, Dictionary<string, string> controls, string notes = "")
    {
        public string Title { get; set; } = title;
        public Dictionary<string, string> Controls { get; set; } = controls ?? new Dictionary<string, string>();
        public string Notes { get; set; } = notes;
    }
}