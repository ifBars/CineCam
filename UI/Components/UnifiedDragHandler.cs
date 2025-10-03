using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
#if IL2CPP
using Il2CppInterop.Runtime;
#endif

namespace CineCam.UI.Components
{
    // Class containing shared/common drag handler functionality 
    public static class UnifiedDragHandler
    {
#if IL2CPP
        // This method initializes drag handling for IL2CPP builds
        public static void InitializeDragging(GameObject titleBarObject, RectTransform panelRect, BasePanel basePanel, Canvas canvas)
        {
            if (titleBarObject == null || panelRect == null || canvas == null)
            {
                MelonLogger.Warning($"Cannot initialize drag handler - null parameters: titleBar={titleBarObject != null}, panelRect={panelRect != null}, canvas={canvas != null}");
                return;
            }

            // IL2CPP implementation uses EventTrigger with static methods
            EventTrigger trigger = titleBarObject.AddComponent<EventTrigger>();
            if (trigger == null)
            {
                MelonLogger.Error("Failed to add EventTrigger component to title bar");
                return;
            }
            
            // Add event entry for pointer down
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            
            // Store references for drag operations
            RectTransform canvasRectTransform = null;
            try
            {
                canvasRectTransform = canvas.transform.Cast<RectTransform>();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to get canvas RectTransform: {ex.Message}");
            }
            
            // Convert lambda to delegate using DelegateSupport
            pointerDownEntry.callback.AddListener(
                DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<BaseEventData>>(
                    (BaseEventData data) => {
                        try
                        {
                            if (data != null)
                            {
                                var pointerData = data.Cast<PointerEventData>();
                                IL2CPP_OnPointerDown(panelRect, basePanel, canvas, canvasRectTransform, pointerData);
                            }
                            else
                            {
                                MelonLogger.Warning("PointerDown received null event data");
                            }
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Error($"Error in drag handler PointerDown: {ex.Message}\nStack trace: {ex.StackTrace}");
                        }
                    }
                )
            );
            trigger.triggers.Add(pointerDownEntry);
            
            // Add event entry for pointer drag
            EventTrigger.Entry dragEntry = new EventTrigger.Entry();
            dragEntry.eventID = EventTriggerType.Drag;
            
            dragEntry.callback.AddListener(
                DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<BaseEventData>>(
                    (BaseEventData data) => {
                        try
                        {
                            if (data != null)
                            {
                                var pointerData = data.Cast<PointerEventData>();
                                IL2CPP_OnDrag(panelRect, pointerData);
                            }
                            else
                            {
                                MelonLogger.Warning("Drag received null event data");
                            }
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Error($"Error in drag handler Drag: {ex.Message}\nStack trace: {ex.StackTrace}");
                        }
                    }
                )
            );
            trigger.triggers.Add(dragEntry);
            
            // Add event entry for pointer up
            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            
            pointerUpEntry.callback.AddListener(
                DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<BaseEventData>>(
                    (BaseEventData data) => {
                        try
                        {
                            IL2CPP_OnPointerUp();
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Error($"Error in drag handler PointerUp: {ex.Message}\nStack trace: {ex.StackTrace}");
                        }
                    }
                )
            );
            trigger.triggers.Add(pointerUpEntry);
        }

        // IL2CPP implementation state variables
        private static RectTransform _currentDragPanel;
        private static Vector2 _pointerOffset;
        private static Canvas _canvas;
        private static RectTransform _canvasRectTransform;
        private static BasePanel _basePanel;
        private static bool _isDragging = false;
        private static Camera _uiCamera;

        // IL2CPP implementation event handlers
        private static void IL2CPP_OnPointerDown(RectTransform panelRect, BasePanel basePanel, Canvas canvas, RectTransform canvasRectTransform, PointerEventData eventData)
        {
            if (panelRect == null || canvas == null || eventData == null)
            {
                MelonLogger.Warning($"OnPointerDown has null references: panelRect={panelRect != null}, canvas={canvas != null}, eventData={eventData != null}");
                return;
            }

            _currentDragPanel = panelRect;
            _basePanel = basePanel;
            _canvas = canvas;
            _canvasRectTransform = canvasRectTransform;
            _uiCamera = canvas.worldCamera;
            _isDragging = true;
            
            // Bring panel to front when dragging starts
            _basePanel?.BringToFront();
            
            if (_canvasRectTransform == null)
            {
                MelonLogger.Error("Canvas RectTransform is null");
                _isDragging = false;
                return;
            }
            
            try
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRectTransform,
                    eventData.position,
                    _uiCamera,
                    out Vector2 pointerPositionInCanvas))
                {
                    Vector2 panelPositionInCanvas = _currentDragPanel.anchoredPosition;
                    _pointerOffset = pointerPositionInCanvas - panelPositionInCanvas;
                }
                else
                {
                    MelonLogger.Warning("ScreenPointToLocalPointInRectangle returned false");
                    _isDragging = false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in OnPointerDown ScreenPointToLocalPointInRectangle: {ex.Message}\nStack trace: {ex.StackTrace}");
                _isDragging = false;
            }
        }
        
        private static void IL2CPP_OnDrag(RectTransform panelRect, PointerEventData eventData)
        {
            if (panelRect == null || _canvas == null || !_isDragging || _currentDragPanel != panelRect || eventData == null)
            {
                return;
            }
                
            if (_canvasRectTransform == null)
            {
                try
                {
                    _canvasRectTransform = _canvas.transform.Cast<RectTransform>();
                    if (_canvasRectTransform == null)
                    {
                        _isDragging = false;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error getting canvas RectTransform during drag: {ex.Message}");
                    _isDragging = false;
                    return;
                }
            }
                
            try
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRectTransform,
                    eventData.position,
                    _uiCamera,
                    out Vector2 pointerPositionInCanvas))
                {
                    _currentDragPanel.anchoredPosition = pointerPositionInCanvas - _pointerOffset;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in OnDrag ScreenPointToLocalPointInRectangle: {ex.Message}");
                _isDragging = false;
            }
        }
        
        private static void IL2CPP_OnPointerUp()
        {
            _isDragging = false;
            _currentDragPanel = null;
            _basePanel = null;
            _canvas = null;
            _canvasRectTransform = null;
            _uiCamera = null;
        }

#else // MONO implementation
        
        public static void InitializeDragging(GameObject titleBarObject, RectTransform panelRect, BasePanel basePanel, Canvas canvas)
        {
            if (titleBarObject == null || panelRect == null || canvas == null)
                return;
                
            // In Mono, we can directly add a MonoBehaviour component that implements the drag interfaces
            PanelDragHandler dragHandler = titleBarObject.AddComponent<PanelDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.Initialize(panelRect, basePanel, canvas);
            }
            else
            {
                Debug.LogError("Failed to add PanelDragHandler component");
            }
        }
        
#endif
    }
    
#if !IL2CPP
    // Traditional MonoBehaviour implementation for Mono
    public class PanelDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _panelRectTransform;
        private Vector2 _pointerOffset;
        private Canvas _canvas;
        private BasePanel _basePanel;

        public void Initialize(RectTransform panelRectTransform, BasePanel basePanel, Canvas canvas)
        {
            _panelRectTransform = panelRectTransform;
            _basePanel = basePanel;
            _canvas = canvas;
            if (_canvas == null)
            {
                Debug.LogError("PanelDragHandler: Canvas not found!");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_panelRectTransform == null || _canvas == null) return;

            // Bring panel to front when dragging starts
            _basePanel?.BringToFront();

            // Get the offset between the pointer position and the panel's anchor position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPositionInCanvas);

            // Convert from canvas local position to panel position
            Vector2 panelPositionInCanvas = _panelRectTransform.anchoredPosition;

            // Calculate offset (this is what we'll maintain during drag)
            _pointerOffset = pointerPositionInCanvas - panelPositionInCanvas;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_panelRectTransform == null || _canvas == null) return;

            // Convert the current pointer position to canvas space
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPositionInCanvas))
            {
                // Update the panel's position by maintaining the original offset
                _panelRectTransform.anchoredPosition = pointerPositionInCanvas - _pointerOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Optional: Add any logic needed when dragging ends, e.g., snapping.
        }
    }
#endif
} 