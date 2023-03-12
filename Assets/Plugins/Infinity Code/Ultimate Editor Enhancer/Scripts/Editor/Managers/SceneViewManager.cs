/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.EditorMenus;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InfinityCode.UltimateEditorEnhancer
{
    [InitializeOnLoad]
    public static class SceneViewManager
    {
        public static Action OnNextGUI;
        public static Func<bool> OnValidateOpenContextMenu;

        private static Vector3 _lastWorldPosition;
        private static Ray _screenRay;
        private static bool beforeInvoked;
        private static double lastUpdateLate;
        private static List<Listener> lateListeners;
        private static List<Listener> listeners;
        private static Vector2 pressPoint;
        private static readonly Dictionary<int, VisualElement> rectElements;

        private static bool waitOpenMenu;
        private static readonly Plane plane2D;
        private static readonly Plane plane3D;

        static SceneViewManager()
        {
            rectElements = new Dictionary<int, VisualElement>();

            SceneView.beforeSceneGui += SceneGUI;

            plane3D = new Plane(Vector3.up, Vector3.zero);
            plane2D = new Plane(Vector3.back, Vector3.zero);
        }

        public static GameObject lastGameObjectUnderCursor { get; private set; }

        public static Vector2 lastMousePosition { get; private set; }

        public static Ray lastScreenRay => _screenRay;

        public static Vector3 lastWorldPosition => _lastWorldPosition;

        public static Vector3 lastNormal { get; private set; }

        public static void AddListener(Action<SceneView> invoke, float weight = 0, bool late = false)
        {
            if (!late)
            {
                if (listeners == null) listeners = new List<Listener>();
                listeners.Add(new Listener(invoke, weight));
                listeners = listeners.OrderByDescending(l => l.weight).ToList();
            }
            else
            {
                if (lateListeners == null) lateListeners = new List<Listener>();
                lateListeners.Add(new Listener(invoke, weight));
                lateListeners = lateListeners.OrderByDescending(l => l.weight).ToList();
            }
        }

        public static void BlockMouseUp()
        {
            AddListener(BlockMouseUpMethod);
            GUIUtility.hotControl = 1000;
        }

        private static void BlockMouseUpMethod(SceneView view)
        {
            var e = Event.current;
            if (e.type != EventType.MouseUp) return;

            RemoveListener(BlockMouseUpMethod);
            GUIUtility.hotControl = 0;
        }

        public static Rect GetRect(SceneView view)
        {
#if !UNITY_2021_2_OR_NEWER
            Rect rect = view.position;
            rect.yMin += 20;
            if (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) rect.yMax
 -= 25;
            return rect;
#else

            var id = view.GetInstanceID();
            VisualElement el;
            if (rectElements.TryGetValue(id, out el)) return el.contentRect;

            el = view.rootVisualElement.Q("unity-scene-view-camera-rect");
            rectElements[id] = el;
            return el.contentRect;
#endif
        }

        private static void InvokeSceneGUI(SceneView sceneview)
        {
            if (listeners == null) return;

            for (var i = listeners.Count - 1; i >= 0; i--)
                try
                {
                    if (i < listeners.Count) listeners[i].Invoke(sceneview);
                }
                catch (Exception exception)
                {
                    Log.Add(exception);
                }
        }

        private static void InvokeSceneGUILate(SceneView sceneview)
        {
            if (lateListeners == null) return;

            for (var i = lateListeners.Count - 1; i >= 0; i--)
                try
                {
                    lateListeners[i].Invoke(sceneview);
                }
                catch (Exception exception)
                {
                    Log.Add(exception);
                }
        }

        private static void OnMouseDown(Event e)
        {
            if (e.button != 1) return;

            waitOpenMenu = true;
            pressPoint = e.mousePosition;
        }

        private static void OnMouseDrag(Event e)
        {
            if (!waitOpenMenu) return;

            if ((e.mousePosition - pressPoint).sqrMagnitude > 100) waitOpenMenu = false;
        }

        private static void OnMouseUp(Event e)
        {
            if (e.button != 1 || !waitOpenMenu) return;

            waitOpenMenu = false;

            if (OnValidateOpenContextMenu != null)
            {
                var invocationList = OnValidateOpenContextMenu.GetInvocationList();
                if (invocationList.Any(d => !(bool)d.DynamicInvoke())) return;
            }

            if (Prefs.pickGameObject && e.modifiers == Prefs.pickGameObjectModifiers)
                Selection.activeGameObject = HandleUtility.PickGameObject(e.mousePosition, false);

            if (Prefs.contextMenuOnRightClick && (e.modifiers == Prefs.rightClickModifiers ||
                                                  e.modifiers == Prefs.pickGameObjectModifiers))
            {
#if !UNITY_2021_1
                var position = e.mousePosition;
                if (EditorWindow.focusedWindow != null) position += EditorWindow.focusedWindow.position.position;
                EditorMenu.Show(position);
#else
                EditorMenu.Show(e.mousePosition);
#endif
            }
        }

        public static void RemoveListener(Action<SceneView> invoke)
        {
            if (listeners != null)
                for (var i = listeners.Count - 1; i >= 0; i--)
                    if (listeners[i].Invoke == invoke)
                        listeners.RemoveAt(i);
            if (lateListeners != null)
                for (var i = lateListeners.Count - 1; i >= 0; i--)
                    if (lateListeners[i].Invoke == invoke)
                        lateListeners.RemoveAt(i);
        }

        private static void SceneGUI(SceneView view)
        {
            beforeInvoked = true;
            if (OnNextGUI != null)
            {
                try
                {
                    OnNextGUI();
                }
                catch (Exception exception)
                {
                    Log.Add(exception);
                }

                OnNextGUI = null;
            }

            var e = Event.current;

            if (EditorApplication.timeSinceStartup - lastUpdateLate > 1) UpdateSceneGUILate();

            if (e.type == EventType.MouseMove || e.type == EventType.DragUpdated) UpdateLastItems(view);

            InvokeSceneGUI(view);

            if (e.type == EventType.MouseDown)
            {
                if (GUILayoutUtils.hoveredButtonID != 0) GUIUtility.hotControl = GUILayoutUtils.hoveredButtonID;
                OnMouseDown(e);
            }
            else if (e.type == EventType.MouseUp)
            {
                OnMouseUp(e);
            }
            else if (e.type == EventType.MouseDrag)
            {
                OnMouseDrag(e);
            }
            else if (e.type == EventType.MouseMove)
            {
                GUILayoutUtils.hoveredButtonID = 0;
            }
        }

        private static void SceneGUILate(SceneView view)
        {
            if (!beforeInvoked) SceneGUI(view);
            InvokeSceneGUILate(view);
            beforeInvoked = false;
        }

        public static void UpdateLastItems(SceneView view)
        {
            var camera = SceneView.lastActiveSceneView.camera;
            if (camera == null || camera.pixelWidth == 0 || camera.pixelHeight == 0) return;

            lastMousePosition = Event.current.mousePosition;
            var pixelCoordinate = HandleUtility.GUIPointToScreenPixelCoordinate(lastMousePosition);

            _screenRay = camera.ScreenPointToRay(pixelCoordinate);
            lastGameObjectUnderCursor = HandleUtility.PickGameObject(lastMousePosition, false);

            if (lastGameObjectUnderCursor != null && !view.in2DMode)
            {
                var meshFilter = lastGameObjectUnderCursor.GetComponent<MeshFilter>();
                RaycastHit hit;

                if (meshFilter != null && meshFilter.sharedMesh != null && HandleUtilityRef.IntersectRayMesh(_screenRay,
                        meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix, out hit))
                {
                    _lastWorldPosition = hit.point;
                    lastNormal = hit.normal;
                }
                else
                {
                    var collider = lastGameObjectUnderCursor.GetComponentInParent<Collider>();
                    if (collider != null)
                    {
                        if (collider.Raycast(_screenRay, out hit, float.MaxValue))
                        {
                            _lastWorldPosition = hit.point;
                            lastNormal = hit.normal;
                        }
                    }
                    else
                    {
                        var rectTransform = lastGameObjectUnderCursor.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, pixelCoordinate,
                                view.camera, out _lastWorldPosition);
                            lastNormal = Vector3.forward;
                        }
                    }
                }
            }
            else
            {
                float distance;
                var plane = view.in2DMode ? plane2D : plane3D;
                if (plane.Raycast(_screenRay, out distance)) _lastWorldPosition = _screenRay.GetPoint(distance);
                else _lastWorldPosition = Vector3.zero;
                lastNormal = Vector3.up;
            }
        }

        private static void UpdateSceneGUILate()
        {
            SceneView.duringSceneGui -= SceneGUILate;
            SceneView.duringSceneGui += SceneGUILate;
            lastUpdateLate = EditorApplication.timeSinceStartup;
        }

        internal class Listener
        {
            public Action<SceneView> Invoke;
            public float weight;

            public Listener(Action<SceneView> invoke, float weight)
            {
                Invoke = invoke;
                this.weight = weight;
            }
        }
    }
}