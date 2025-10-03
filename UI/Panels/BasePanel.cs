using CineCam.Managers;
using CineCam.UI.Components; // Added for drag handler
using UnityEngine;
using UnityEngine.UI;
using System;

namespace CineCam.UI
{
    public abstract class BasePanel
    {
        protected GameObject _panelObject;
        protected RectTransform _panelRect;
        protected bool _isVisible = false;
        protected string _panelTitle;
        protected GameObject _contentArea;
        protected Canvas _canvas; // Canvas reference for coordinate calculations

        // Default panel size if none is specified
        private static readonly Vector2 DEFAULT_PANEL_SIZE = new Vector2(100, 50);

        // Panel position and size
        protected Vector2 _panelPosition = Vector2.zero;
        protected Vector2 _panelSize;

        public BasePanel(GameObject parent, string title) : this(parent, title, DEFAULT_PANEL_SIZE)
        {
            // This constructor calls the main constructor with the default size
        }

        public BasePanel(GameObject parent, string title, Vector2 panelSize)
        {
            if (parent == null)
                throw new ArgumentNullException("parent", "Parent GameObject cannot be null when creating a panel");

            _panelTitle = title ?? "Untitled Panel";
            _panelSize = panelSize;

            // Attempt to find the root canvas.
            Transform canvasTransform = parent.transform;
            while (canvasTransform.parent != null && canvasTransform.GetComponent<Canvas>() == null)
            {
                canvasTransform = canvasTransform.parent;
            }
            _canvas = canvasTransform.GetComponent<Canvas>();
            if (_canvas == null)
            {
                // Fallback if not found immediately, search upwards in hierarchy
                _canvas = parent.GetComponentInParent<Canvas>();
            }
            if (_canvas == null)
            {
                // If still not found, this is an issue for ScreenSpaceOverlay dragging.
                // Consider throwing an error or logging a warning, as dragging might not work as expected.
                // For now, we'll proceed, but dragging logic in PanelDragHandler might fail.
                UnityEngine.Debug.LogError($"Could not find a Canvas in the parent hierarchy of {_panelTitle}. Dragging might not work correctly.");
            }

            CreateBasePanel(parent);
        }

        protected virtual void CreateBasePanel(GameObject parent)
        {
            // Create panel gameobject
            _panelObject = new GameObject($"Panel_{_panelTitle}");
            if (_panelObject == null)
                throw new Exception($"Failed to create panel GameObject for {_panelTitle}");

            _panelObject.transform.SetParent(parent.transform, false);

            // Setup panel rectangle
            _panelRect = _panelObject.AddComponent<RectTransform>();
            if (_panelRect == null)
                throw new Exception($"Failed to add RectTransform to panel {_panelTitle}");

            // Set anchor to center of screen by default
            _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRect.pivot = new Vector2(0.5f, 0.5f);

            // Apply the panel size
            _panelRect.sizeDelta = _panelSize;
            _panelRect.anchoredPosition = _panelPosition;

            // Add panel background
            Image bgImage = _panelObject.AddComponent<Image>();
            if (bgImage == null)
                throw new Exception($"Failed to add background Image to panel {_panelTitle}");

            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // Add panel title bar
            CreateTitleBar();

            // Add content area
            _contentArea = new GameObject("ContentArea");
            if (_contentArea == null)
                throw new Exception($"Failed to create ContentArea GameObject for panel {_panelTitle}");

            _contentArea.transform.SetParent(_panelObject.transform, false);

            RectTransform contentRect = _contentArea.AddComponent<RectTransform>();
            if (contentRect == null)
                throw new Exception($"Failed to add RectTransform to ContentArea in panel {_panelTitle}");

            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -40); // Leave space for title bar

            // Initialize with hidden state
            _panelObject.SetActive(false);
        }

        protected virtual void CreateTitleBar()
        {
            GameObject titleBar = new GameObject("TitleBar");
            if (titleBar == null)
                throw new Exception($"Failed to create TitleBar GameObject for panel {_panelTitle}");

            titleBar.transform.SetParent(_panelObject.transform, false);

            RectTransform titleRect = titleBar.AddComponent<RectTransform>();
            if (titleRect == null)
                throw new Exception($"Failed to add RectTransform to TitleBar in panel {_panelTitle}");

            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1f); // Pivot at top-center for title bar
            titleRect.anchoredPosition = new Vector2(0, 0);
            titleRect.sizeDelta = new Vector2(0, 30); // Width stretches, height is 30

            Image titleBg = titleBar.AddComponent<Image>();
            if (titleBg == null)
                throw new Exception($"Failed to add background Image to TitleBar in panel {_panelTitle}");

            titleBg.color = new Color(0.1f, 0.1f, 0.1f, 1);

            // Title text
            GameObject titleTextObj = new GameObject("TitleText");
            if (titleTextObj == null)
                throw new Exception($"Failed to create TitleText GameObject for panel {_panelTitle}");

            titleTextObj.transform.SetParent(titleBar.transform, false);

            RectTransform textRect = titleTextObj.AddComponent<RectTransform>();
            if (textRect == null)
                throw new Exception($"Failed to add RectTransform to TitleText in panel {_panelTitle}");

            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            Text titleText = titleTextObj.AddComponent<Text>();
            if (titleText == null)
                throw new Exception($"Failed to add Text component to TitleText in panel {_panelTitle}");

            titleText.text = _panelTitle;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;

            // Use the unified drag handler which automatically selects IL2CPP or Mono implementation
            if (_panelRect != null && _canvas != null)
            {
                // Initialize dragging with UnifiedDragHandler that works for both IL2CPP and Mono
                UnifiedDragHandler.InitializeDragging(titleBar, _panelRect, this, _canvas);
            }
            else
            {
                UnityEngine.Debug.LogError(_canvas == null
                    ? $"Drag handling for {_panelTitle} could not be initialized because Canvas is null."
                    : $"Failed to initialize drag handling for {_panelTitle}");
            }
        }

        public virtual void SetVisible(bool visible)
        {
            if (_panelObject == null)
                return;

            _isVisible = visible;
            _panelObject.SetActive(visible);

            if (visible)
            {
                BringToFront();
            }
        }

        public void BringToFront()
        {
            if (_panelObject != null)
            {
                _panelObject.transform.SetAsLastSibling();
            }
        }

        public virtual void UpdateCameraStatus(CinematicCameraManager cameraManager)
        {
            // Base implementation does nothing, override in subclasses if needed
        }

        public bool IsVisible => _isVisible;
        public Vector2 PanelSize => _panelSize;
        public Vector2 PanelPosition => _panelPosition;

        public Vector2 GetPosition()
        {
            return _panelRect != null ? _panelRect.anchoredPosition : _panelPosition;
        }

        public GameObject GetContentArea()
        {
            return _contentArea;
        }

        // Method to set a new size for the panel
        public virtual void ResizePanel(Vector2 newSize)
        {
            if (_panelRect == null) return;
            _panelSize = newSize;
            _panelRect.sizeDelta = newSize;
        }

        // Method to set a new position for the panel
        public virtual void SetPosition(Vector2 newPosition)
        {
            if (_panelRect == null) return;
            _panelPosition = newPosition;
            _panelRect.anchoredPosition = newPosition;
        }
    }
}