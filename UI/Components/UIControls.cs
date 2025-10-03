using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
#if IL2CPP
using Il2CppInterop.Runtime;
#endif

namespace CineCam.UI.Components // Changed namespace to CineCam.UI.Components
{
    public static class UIControls
    {
        private static Font _defaultFont;

        private static Font GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_defaultFont == null)
                {
                    Debug.LogWarning("UIControls: Arial.ttf not found, UI text may not display correctly.");
                    // Fallback to Unity's default UI font if Arial isn't available for some reason
                    _defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14); // Or some other safe fallback
                }
            }
            return _defaultFont;
        }

        public static Text CreateLabel(GameObject parent, string text, string gameObjectName = "Label")
        {
            GameObject labelObj = new GameObject(gameObjectName);
            labelObj.transform.SetParent(parent.transform, false);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = text;
            labelText.font = GetDefaultFont();
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;

            LayoutElement layoutElement = labelObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 20; // Default min height
            layoutElement.preferredHeight = 20; // Default preferred height

            return labelText;
        }

        public static Button CreateButton(GameObject parent, string buttonText, string gameObjectName, Action onClickAction)
        {
            GameObject buttonObj = new GameObject(gameObjectName);
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Default button color

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.25f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f);
            button.colors = colors;

            Text textComponent = CreateLabel(buttonObj, buttonText, "ButtonText");
            textComponent.alignment = TextAnchor.MiddleCenter;
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            LayoutElement buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
            buttonLayoutElement.minHeight = 30; // Default min height for buttons
            buttonLayoutElement.preferredHeight = 30; // Default preferred height

            if (onClickAction != null)
            {
#if IL2CPP
                button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(onClickAction));
#else
                button.onClick.AddListener(new UnityAction(onClickAction));
#endif
            }

            return button;
        }

        public static InputField CreateInputField(GameObject parent, string placeholderText, string gameObjectName = "InputField")
        {
            GameObject inputObj = new GameObject(gameObjectName);
            inputObj.transform.SetParent(parent.transform, false);

            Image bgImage = inputObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.targetGraphic = bgImage;

            Text textComponent = CreateLabel(inputObj, "", "Text");
            textComponent.color = Color.white;
            textComponent.supportRichText = false;
            inputField.textComponent = textComponent;
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(5, 2); // Padding
            textRect.offsetMax = new Vector2(-5, -2); // Padding


            Text placeholder = CreateLabel(inputObj, placeholderText, "Placeholder");
            placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            placeholder.fontStyle = FontStyle.Italic;
            inputField.placeholder = placeholder;
            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0, 0);
            placeholderRect.anchorMax = new Vector2(1, 1);
            placeholderRect.offsetMin = new Vector2(5, 2);
            placeholderRect.offsetMax = new Vector2(-5, -2);
            
            LayoutElement inputLayoutElement = inputObj.AddComponent<LayoutElement>();
            inputLayoutElement.minHeight = 30; // Default min height for input fields
            inputLayoutElement.preferredHeight = 30; // Default preferred height

            return inputField;
        }
        
        public static GameObject CreateScrollView(GameObject parent, out GameObject contentContainer, string gameObjectName = "ScrollView")
        {
            GameObject scrollViewGO = new GameObject(gameObjectName);
            scrollViewGO.transform.SetParent(parent.transform, false);
            RectTransform scrollRectTransform = scrollViewGO.AddComponent<RectTransform>();

            ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 20;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollViewGO.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Slightly transparent background for viewport
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 5;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false; // Content should determine its height
            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;
            
            // Assign out parameter
            contentContainer = content;

            // Default ScrollView to stretch within its parent by default
            // This can be overridden by adding a LayoutElement to the scrollViewGO outside this method if needed.
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

            // Viewport also stretches within scroll view
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // Content Rect settings
            contentRect.anchorMin = new Vector2(0, 1); // Anchor to top
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1); // Pivot at top-center
            contentRect.sizeDelta = new Vector2(0, 0); // Width determined by parent, height by ContentSizeFitter

            return scrollViewGO;
        }
    }
} 