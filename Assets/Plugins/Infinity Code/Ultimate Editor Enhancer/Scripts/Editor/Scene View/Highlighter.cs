﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class Highlighter
    {
        private static GameObject hoveredGO;
        private static readonly Dictionary<SceneView, SceneViewOutline> viewDict;
        private static Renderer[] lastRenderers;
        private static RectTransform lastRectTransform;
        private static Vector3[] rectTransformCorners;
        private static Transform lastNoRendererTransform;

        static Highlighter()
        {
            SceneViewManager.AddListener(OnSceneGUI, SceneViewOrder.normal, true);
            HierarchyItemDrawer.Register("Highlighter", DrawHierarchyItem);
            EditorApplication.modifierKeysChanged += RepaintAllHierarchies;
            EditorApplication.update += EditorUpdate;
            viewDict = new Dictionary<SceneView, SceneViewOutline>();
        }

        public static GameObject lastGameObject { get; private set; }

        private static void DrawHierarchyItem(HierarchyItem item)
        {
            if (!Prefs.highlight) return;
            if (Prefs.highlightOnHierarchy) HighlightOnHierarchy(item);
        }

        private static void DrawRectTransformBounds()
        {
            if (rectTransformCorners == null) rectTransformCorners = new Vector3[4];
            lastRectTransform.GetWorldCorners(rectTransformCorners);

            var color = Handles.color;
            Handles.color = Prefs.highlightUIColor;

#if UNITY_2020_2_OR_NEWER
            var thickness = 2;
            Handles.DrawLine(rectTransformCorners[0], rectTransformCorners[1], thickness);
            Handles.DrawLine(rectTransformCorners[1], rectTransformCorners[2], thickness);
            Handles.DrawLine(rectTransformCorners[2], rectTransformCorners[3], thickness);
            Handles.DrawLine(rectTransformCorners[3], rectTransformCorners[0], thickness);
#else
            Handles.DrawLine(rectTransformCorners[0], rectTransformCorners[1]);
            Handles.DrawLine(rectTransformCorners[1], rectTransformCorners[2]);
            Handles.DrawLine(rectTransformCorners[2], rectTransformCorners[3]);
            Handles.DrawLine(rectTransformCorners[3], rectTransformCorners[0]);
#endif
            Handles.color = color;
        }

        private static void EditorUpdate()
        {
            var wnd = EditorWindow.mouseOverWindow;
            if (wnd != null && wnd.GetType() == SceneHierarchyWindowRef.type && !wnd.wantsMouseMove)
                wnd.wantsMouseMove = true;
        }

        public static bool Highlight(GameObject go)
        {
            var state = false;
            HighlightUI(go, ref state);
            if (GraphicsSettings.renderPipelineAsset == null) HighlightRenderers(go, ref state);
            HighlightWithoutRenderer(go, ref state);

            if (state) SceneView.RepaintAll();

            lastGameObject = go;

            return state;
        }

        private static void HighlightOnHierarchy(HierarchyItem item)
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDrag:
                case EventType.DragUpdated:
                case EventType.DragPerform:
                case EventType.DragExited:
                case EventType.MouseMove:
                    var go = item.gameObject;
                    if (go == null) break;
                    var contains = item.rect.Contains(e.mousePosition);
                    if (contains && e.modifiers == EventModifiers.Control)
                    {
                        if (hoveredGO != go)
                        {
                            hoveredGO = go;
                            Highlight(go);
                        }
                    }
                    else if (hoveredGO == go)
                    {
                        hoveredGO = null;
                        Highlight(null);
                    }

                    break;
            }
        }

        private static void HighlightRenderers(GameObject go, ref bool state)
        {
            if (lastGameObject == go) return;

            Renderer[] renderers = null;
            if (go != null) renderers = go.GetComponentsInChildren<Renderer>();

            if (renderers != null && renderers.Length > 0)
            {
                foreach (SceneView view in SceneView.sceneViews)
                {
                    SceneViewOutline outline;
                    if (!viewDict.TryGetValue(view, out outline))
                    {
                        outline = view.camera.gameObject.AddComponent<SceneViewOutline>();
                        viewDict.Add(view, outline);
                    }

                    outline.SetRenderer(renderers);
                }

                lastRenderers = renderers;
                state = true;
            }
            else if (lastRenderers != null && lastRenderers.Length > 0)
            {
                foreach (SceneView view in SceneView.sceneViews)
                {
                    SceneViewOutline outline;
                    if (!viewDict.TryGetValue(view, out outline))
                    {
                        outline = view.camera.gameObject.AddComponent<SceneViewOutline>();
                        viewDict.Add(view, outline);
                    }

                    outline.SetRenderer(null);
                }

                lastRenderers = null;
                state = true;
            }
        }

        private static void HighlightUI(GameObject go, ref bool state)
        {
            RectTransform rectTransform = null;
            if (go != null) rectTransform = go.GetComponent<RectTransform>();

            if (rectTransform != lastRectTransform)
            {
                lastRectTransform = rectTransform;
                state = true;
            }
        }

        private static void HighlightWithoutRenderer(GameObject go, ref bool state)
        {
            Transform transform = null;
            if (go != null) transform = go.transform;

            if (lastNoRendererTransform != transform)
            {
                if (lastRenderers != null && lastRenderers.Length > 0) lastNoRendererTransform = null;
                else lastNoRendererTransform = transform;
                state = true;
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint) return;

            if (lastRectTransform != null)
            {
                DrawRectTransformBounds();
            }
            else if (Prefs.highlightNoRenderer && lastNoRendererTransform != null &&
                     !(EditorWindow.mouseOverWindow is SceneView))
            {
                var position = lastNoRendererTransform.position;
                var size = HandleUtility.GetHandleSize(position);

                var rotation = Quaternion.Euler(-45, 45, 0);
                var color = Handles.color;
                Handles.color = Prefs.highlightNoRendererColor;
                position -= rotation * new Vector3(0, 0, size * 1.2f);
                Handles.ArrowHandleCap(0, position, rotation, size, EventType.Repaint);
                Handles.color = color;
            }
        }

        public static void RepaintAllHierarchies()
        {
            if (!Prefs.highlightHierarchyRow) return;
            if (!(EditorWindow.mouseOverWindow is SceneView)) return;
            if (Event.current == null) return;

            EditorApplication.RepaintHierarchyWindow();
        }


        [ExecuteInEditMode]
        public class SceneViewOutline : MonoBehaviour
        {
            private CommandBuffer _buffer;
            private Material _childMaterial;
            private Material _material;
            private Shader _shader;
            private bool useRenderImage;

            private CommandBuffer buffer
            {
                get
                {
                    if (_buffer == null) _buffer = new CommandBuffer();

                    return _buffer;
                }
            }

            private Material material
            {
                get
                {
                    if (_material == null) _material = new Material(shader);
                    return _material;
                }
            }

            private Shader shader
            {
                get
                {
                    if (_shader == null)
                        _shader = Shader.Find("Hidden/InfinityCode/UltimateEditorEnhancer/SceneViewHighlight");
                    return _shader;
                }
            }

            public void OnRenderImage(RenderTexture source, RenderTexture destination)
            {
                if (source == null || destination == null) return;
                var b = buffer;

                if (!useRenderImage)
                {
                    b.Clear();
                    Graphics.Blit(source, destination);
                    return;
                }

                var width = source.width;
                var height = source.height;
                var t1 = RenderTexture.GetTemporary(width, height, 32);
                var t2 = RenderTexture.GetTemporary(width, height, 32);

                RenderTexture.active = t1;
                GL.Clear(true, true, Color.clear);

                Graphics.ExecuteCommandBuffer(b);
                RenderTexture.active = null;

                var m = material;

                Graphics.Blit(t1, t2, m, 1);
                Graphics.Blit(t2, t1, m, 2);

                m.SetTexture("_OutlineTex", t1);
                m.SetTexture("_FillTex", t2);
                m.SetColor("_Color", Prefs.highlightColor);
                Graphics.Blit(source, destination, m, 3);

                RenderTexture.ReleaseTemporary(t1);
                RenderTexture.ReleaseTemporary(t2);
            }

            public void SetRenderer(Renderer[] renderers)
            {
                var b = buffer;
                b.Clear();

                useRenderImage = renderers != null && renderers.Length > 0;
                if (!useRenderImage) return;

                var m = material;

                var id = 1;
                foreach (var r in renderers.OrderBy(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID))
                             .ThenBy(r => r.sortingOrder))
                {
                    var count = 1;
                    if (r is MeshRenderer)
                    {
                        var f = r.GetComponent<MeshFilter>();
                        if (f != null && f.sharedMesh != null) count = f.sharedMesh.subMeshCount;
                    }
                    else
                    {
                        var smr = r as SkinnedMeshRenderer;
                        if (smr != null && smr.sharedMesh != null) count = smr.sharedMesh.subMeshCount;
                    }

                    var objectID = new Color32((byte)(id & 0xff), (byte)((id >> 8) & 0xff), 0, 0);
                    b.SetGlobalColor("_ObjectID", objectID);
                    for (var i = 0; i < count; i++) b.DrawRenderer(r, m, i, 0);
                    id++;
                }
            }
        }
    }
}