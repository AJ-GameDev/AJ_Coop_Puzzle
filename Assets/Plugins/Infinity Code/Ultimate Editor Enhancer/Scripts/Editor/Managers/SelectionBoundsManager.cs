/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    [InitializeOnLoad]
    public static class SelectionBoundsManager
    {
        public static Action OnChanged;
        private static Bounds _bounds;
        private static readonly Vector3[] fourCorners = new Vector3[4];

        static SelectionBoundsManager()
        {
            renderers = new List<Renderer>();
            rectTransforms = new List<RectTransform>();
            Selection.selectionChanged += OnSelectionChanged;

            SceneViewManager.AddListener(OnSceneView);

            OnSelectionChanged();
        }

        public static Bounds bounds => _bounds;

        public static GameObject firstGameObject
        {
            get
            {
                if (renderers.Count > 0)
                {
                    var first = renderers.FirstOrDefault(r => r != null);
                    if (first != null) return first.gameObject;
                }

                if (rectTransforms.Count > 0)
                {
                    var first = rectTransforms.FirstOrDefault(r => r != null);
                    if (first != null) return first.gameObject;
                }

                return null;
            }
        }

        public static bool hasBounds { get; private set; }

        public static bool isRectTransform => renderers.Count == 0 && rectTransforms.Count > 0;

        private static List<RectTransform> rectTransforms { get; }
        private static List<Renderer> renderers { get; }

        private static void OnSceneView(SceneView scene)
        {
            if (!hasBounds) return;
            if (Event.current.type != EventType.Layout) return;

            var b = new Bounds();

            var isFirst = true;

            if (renderers.Count > 0)
                for (var i = 0; i < renderers.Count; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;

                    if (isFirst)
                    {
                        b = r.bounds;
                        isFirst = false;
                    }
                    else
                    {
                        b.Encapsulate(r.bounds);
                    }
                }

            if (rectTransforms.Count > 0)
                for (var i = 0; i < rectTransforms.Count; i++)
                {
                    var rt = rectTransforms[i];
                    if (rt == null) continue;

                    rt.GetWorldCorners(fourCorners);

                    if (isFirst)
                    {
                        b = new Bounds(fourCorners[0], Vector3.zero);
                        isFirst = false;
                    }

                    for (var j = 0; j < 4; j++) b.Encapsulate(fourCorners[j]);
                }

            if (b != _bounds)
            {
                _bounds = b;
                if (OnChanged != null) OnChanged();
            }
        }

        private static void OnSelectionChanged()
        {
            renderers.Clear();
            rectTransforms.Clear();
            hasBounds = false;

            var instanceIDs = Selection.instanceIDs;

            var isFirst = true;

            foreach (var instanceID in instanceIDs)
            {
                var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                if (gameObject == null || gameObject.scene.name == null) continue;

                var rs = gameObject.GetComponentsInChildren<Renderer>();
                if (rs != null && rs.Length != 0)
                {
                    renderers.AddRange(rs);

                    foreach (var renderer in rs)
                        if (isFirst)
                        {
                            _bounds = renderer.bounds;
                            isFirst = false;
                        }
                        else
                        {
                            _bounds.Encapsulate(renderer.bounds);
                        }
                }

                var rts = gameObject.GetComponentsInChildren<RectTransform>();
                if (rts != null && rts.Length > 0)
                {
                    rectTransforms.AddRange(rts);

                    foreach (var rt in rts)
                    {
                        rt.GetWorldCorners(fourCorners);

                        if (isFirst)
                        {
                            _bounds = new Bounds(fourCorners[0], Vector3.zero);
                            isFirst = false;
                        }

                        for (var i = 0; i < 4; i++) _bounds.Encapsulate(fourCorners[i]);
                    }
                }
            }

            if (!isFirst) hasBounds = true;
            if (OnChanged != null) OnChanged();
        }
    }
}