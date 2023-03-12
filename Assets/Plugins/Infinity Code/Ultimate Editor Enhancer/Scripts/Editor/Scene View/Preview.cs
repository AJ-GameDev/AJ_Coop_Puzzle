/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class Preview
    {
        //public delegate void OnPostSceneGUIDelegate(float width, GUIStyle style, ref float lastY);

        //public static OnPostSceneGUIDelegate OnPostSceneGUI;
        public static Texture2D texture;

        private static PreviewItem[] items;
        private static int activeIndex;
        private static GUIStyle style;

        static Preview()
        {
            var binding = KeyManager.AddBinding();
            binding.OnValidate += () =>
            {
                if (!Prefs.preview) return false;
                if (SceneView.lastActiveSceneView == null) return false;
                if (Event.current.keyCode != Prefs.previewKeyCode) return false;
                if (Event.current.modifiers != Prefs.previewModifiers) return false;
                return true;
            };
            binding.OnPress += StartPreview;

            SceneViewManager.AddListener(OnSceneGUI, SceneViewOrder.quickPreview, true);
        }

        public static bool isActive => texture != null;

        public static PreviewItem activeItem
        {
            get
            {
                if (items == null || items.Length <= activeIndex) return null;
                return items[activeIndex];
            }
        }

        private static void Dispose()
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
                texture = null;
            }

            if (items != null)
            {
                foreach (var item in items) item.Dispose();
                items = null;
            }

            UnityEditor.Tools.hidden = false;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (activeItem == null) return;

            var rect = SceneView.lastActiveSceneView.position;
            var e = Event.current;

            if (e.type == EventType.KeyUp)
            {
                OnKeyUp(e);
            }
            else if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.F)
                {
                    activeItem.Focus();

                    e.Use();
                    SceneView.RepaintAll();
                }
            }
            else if (e.type == EventType.ScrollWheel)
            {
                OnScrollWheel(e);
            }

            if (texture == null) return;
            if (e.modifiers != Prefs.previewModifiers)
            {
                Dispose();
                SceneView.RepaintAll();
                return;
            }

            Handles.BeginGUI();
            GUI.DrawTexture(new Rect(0, 0, rect.width, rect.height), texture);
            if (items == null)
            {
                Handles.EndGUI();
                return;
            }

            if (style == null)
                style = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal =
                    {
                        textColor = Styles.isProSkin ? Color.black : Color.white
                    }
                };

            var content = new GUIContent(activeIndex + 1 + " / " + items.Length + " " + items[activeIndex].name);
            var contentSize = style.CalcSize(content);
            var scrollContent = new GUIContent("Mouse Scroll - Change Camera / View");
            var scrollContentSize = style.CalcSize(scrollContent);
            var width = Mathf.Max(scrollContentSize.x, contentSize.x) + 20;

            GUI.Label(new Rect(5, 5, width, 20), content, style);
            GUI.Label(new Rect(5, 30, width, 20), scrollContent, style);
            GUI.Label(new Rect(5, 55, width, 20), "F - Set view", style);

            Handles.EndGUI();
        }

        private static void OnScrollWheel(Event e)
        {
            if (items == null) return;

            if (e.delta.y > 0)
            {
                activeIndex++;
                if (activeIndex == items.Length) activeIndex = 0;
            }
            else
            {
                activeIndex--;
                if (activeIndex < 0) activeIndex = items.Length - 1;
            }

            items[activeIndex].Render(SceneView.lastActiveSceneView.position);

            e.Use();
            SceneView.RepaintAll();
        }

        private static void OnKeyUp(Event e)
        {
            if (texture == null) return;

            Dispose();
            e.Use();
            SceneView.RepaintAll();
        }

        private static void StartPreview()
        {
            if (texture != null) return;
            var view = SceneView.lastActiveSceneView;

            if (view == null) return;

            if (EditorWindow.focusedWindow != view) view.Focus();

            var tempItems = new List<PreviewItem>();

            var mainCamera = Camera.main;
            var cameras = Object.FindObjectsOfType<Camera>();
            activeIndex = 0;
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera.GetComponent<HideInPreview>() != null) continue;

                if (camera == mainCamera) activeIndex = tempItems.Count;

                tempItems.Add(new CameraItem(camera));
            }

            var states = Object.FindObjectsOfType<ViewState>();
            for (var i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (!state.useInPreview) continue;

                tempItems.Add(new ViewStateItem(state));
            }

            items = tempItems.ToArray();
            items[activeIndex].Render(view.position);

            Event.current.Use();
            SceneView.RepaintAll();
        }

        public abstract class PreviewItem
        {
            public abstract string name { get; }
            public abstract void Dispose();
            public abstract void Focus();
            public abstract void Render(Rect position);
        }

        public class CameraItem : PreviewItem
        {
            public Camera camera;

            public CameraItem(Camera camera)
            {
                this.camera = camera;
            }

            public override string name => camera.gameObject.name;

            public override void Dispose()
            {
                camera = null;
            }

            public override void Focus()
            {
                SceneViewHelper.AlignViewToCamera(camera);
            }

            public override void Render(Rect rect)
            {
                if (camera == null) return;

                var canvases = Object.FindObjectsOfType<Canvas>();
                var modifiedCanvases = new List<Canvas>();

                try
                {
                    foreach (var canvas in canvases)
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                            canvas.targetDisplay == camera.targetDisplay)
                        {
                            modifiedCanvases.Add(canvas);
                            canvas.renderMode = RenderMode.ScreenSpaceCamera;
                            canvas.worldCamera = camera;
                            canvas.planeDistance = camera.nearClipPlane * 1.1f;
                        }

                    var clearFlags = camera.clearFlags;
                    if (clearFlags == CameraClearFlags.Depth || clearFlags == CameraClearFlags.Nothing)
                        camera.clearFlags = CameraClearFlags.Skybox;
                    var lastAT = camera.targetTexture;
                    var rt = camera.targetTexture = new RenderTexture((int)rect.width, (int)rect.height, 16,
                        RenderTextureFormat.ARGB32);
                    camera.Render();

                    texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
                    RenderTexture.active = camera.targetTexture;
                    texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    texture.Apply();

                    RenderTexture.active = null;
                    camera.targetTexture.Release();

                    camera.targetTexture = lastAT;
                    camera.clearFlags = clearFlags;

                    Object.DestroyImmediate(rt);

                    UnityEditor.Tools.hidden = true;
                }
                catch (Exception e)
                {
                    Log.Add(e);
                }

                foreach (var canvas in modifiedCanvases)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = null;
                }
            }
        }

        public class ViewStateItem : PreviewItem
        {
            public ViewState state;

            public ViewStateItem(ViewState state)
            {
                this.state = state;
            }

            public override string name => state.title;

            public override void Dispose()
            {
                state = null;
            }

            public override void Focus()
            {
                var view = SceneView.lastActiveSceneView;

                view.orthographic = state.is2D;
                view.pivot = state.pivot;
                view.size = state.size;
                view.rotation = state.rotation;
            }

            public override void Render(Rect rect)
            {
                var renderTexture =
                    new RenderTexture((int)rect.width, (int)rect.height, 16, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;

                var sceneView = SceneView.lastActiveSceneView;
                var sceneCamera = sceneView.camera;
                var lastAT = sceneCamera.activeTexture;
                var clearFlags = sceneCamera.clearFlags;
                if (clearFlags == CameraClearFlags.Depth || clearFlags == CameraClearFlags.Nothing)
                    sceneCamera.clearFlags = CameraClearFlags.Skybox;
                sceneCamera.targetTexture = renderTexture;

                var pivot = sceneView.pivot;
                var size = sceneView.size;
                var rotation = sceneView.rotation;
                var is2D = sceneView.in2DMode;

                state.SetView(sceneCamera);
                sceneCamera.Render();

                var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                texture.Apply();

                if (Preview.texture != null) Object.DestroyImmediate(Preview.texture);
                Preview.texture = texture;

                sceneView.camera.orthographic = is2D;
                var t = sceneView.camera.transform;
                if (is2D)
                {
                    sceneView.camera.orthographicSize = size;
                    t.position = pivot - Vector3.forward * size;
                    t.rotation = Quaternion.identity;
                }
                else
                {
                    sceneView.camera.fieldOfView = 60;
                    t.position = pivot - rotation * Vector3.forward * size;
                    t.rotation = rotation;
                }

                sceneCamera.targetTexture = lastAT;
                sceneCamera.clearFlags = clearFlags;

                RenderTexture.active = null;
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}