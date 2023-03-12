/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.Tools;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class ToolValues
    {
        public const string StyleID = "sv_label_0";

        public static Action<Rect> OnDrawLate;

        public static Vector3 lastScreenPosition;
        public static bool isBelowHandle;
        public static GUIStyle style;

        private static Transform firstTransform;
        private static bool samePositionX;
        private static bool samePositionY;
        private static bool samePositionZ;
        private static bool sameRotationX;
        private static bool sameRotationY;
        private static bool sameRotationZ;
        private static bool sameScaleX;
        private static bool sameScaleY;
        private static bool sameScaleZ;
        private static string label;

        static ToolValues()
        {
            SceneViewManager.AddListener(OnSceneViewGUI, SceneViewOrder.normal, true);
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private static void AppendPosition()
        {
            Vector3 pos;

            var rt = firstTransform as RectTransform;

            if (rt != null)
            {
                StaticStringBuilder.Append("Anchored Position (");
                pos = rt.anchoredPosition3D;
            }
            else
            {
                StaticStringBuilder.Append("Position (");
                pos = firstTransform.localPosition;
            }

            StaticStringBuilder.Append(samePositionX ? pos.x.ToString("F2", Culture.numberFormat) : "---");
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(samePositionY ? pos.y.ToString("F2", Culture.numberFormat) : "---");
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(samePositionZ ? pos.z.ToString("F2", Culture.numberFormat) : "---");
            StaticStringBuilder.Append(")");
        }

        private static void AppendRotation()
        {
            StaticStringBuilder.Append("Rotation (");
            StaticStringBuilder.Append(sameRotationX
                ? firstTransform.eulerAngles.x.ToString("F2", Culture.numberFormat)
                : "---");
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(sameRotationY
                ? firstTransform.eulerAngles.y.ToString("F2", Culture.numberFormat)
                : "---");
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(sameRotationZ
                ? firstTransform.eulerAngles.z.ToString("F2", Culture.numberFormat)
                : "---");
            StaticStringBuilder.Append(")");
        }

        private static void AppendScale()
        {
            StaticStringBuilder.Append("Scale (");
            StaticStringBuilder.Append(sameScaleX
                ? firstTransform.localScale.x.ToString("F2", Culture.numberFormat)
                : "---");
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(sameScaleY
                ? firstTransform.localScale.y.ToString("F2", Culture.numberFormat)
                : "---");
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(sameScaleZ
                ? firstTransform.localScale.z.ToString("F2", Culture.numberFormat)
                : "---");
            StaticStringBuilder.Append(")");
        }

        private static void AppendSize(RectTransform rt)
        {
            StaticStringBuilder.Append("Size (");
            StaticStringBuilder.Append(rt.sizeDelta.x.ToString("F2", Culture.numberFormat));
            StaticStringBuilder.Append(", ");
            StaticStringBuilder.Append(rt.sizeDelta.y.ToString("F2", Culture.numberFormat));
            StaticStringBuilder.Append(")");
        }

        private static void DrawLabel(SceneView sceneView, string text)
        {
            if (style == null)
                style = new GUIStyle(StyleID)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    fixedHeight = 0,
                    border = new RectOffset(8, 8, 8, 8)
                };

            var content = TempContent.Get(text);
            var pixelPerPoint = EditorGUIUtility.pixelsPerPoint;
            var size = style.CalcSize(content);

            Handles.BeginGUI();
            var screenPoint = sceneView.camera.WorldToScreenPoint(UnityEditor.Tools.handlePosition) / pixelPerPoint;
            if (screenPoint.y > size.y + 150 / pixelPerPoint)
            {
                screenPoint.y -= size.y + 50 / pixelPerPoint;
                isBelowHandle = true;
            }
            else
            {
                screenPoint.y += size.y + 150 / pixelPerPoint;
                isBelowHandle = false;
            }

            lastScreenPosition = screenPoint;

            var rect = new Rect(screenPoint.x - size.x / 2, Screen.height / pixelPerPoint - screenPoint.y - size.y / 2,
                size.x, size.y);
            var e = Event.current;

            if (e.type == EventType.Repaint) GUI.Label(rect, content, style);
            else if (e.type == EventType.MouseDown)
                if (rect.Contains(e.mousePosition))
                {
                    var transforms = Selection.gameObjects.Select(g => g.GetComponent<Transform>()).ToArray();
                    TransformEditorWindow.ShowPopup(transforms);

                    e.Use();
                    SceneViewManager.BlockMouseUp();
                }

            if (OnDrawLate != null) OnDrawLate(rect);

            Handles.EndGUI();
        }

        private static void InitLabel()
        {
            var e = Event.current;
            label = null;

            if (!Prefs.showToolValues) return;
            if (firstTransform == null) return;
            if (TransformEditorWindow.instance != null) return;
            if (UnityEditor.Tools.hidden || UnityEditor.Tools.current == Tool.View) return;
            if (e.modifiers != Prefs.toolValuesModifiers) return;

            StaticStringBuilder.Clear();

            var tool = UnityEditor.Tools.current;
#if UNITY_2020_2_OR_NEWER
            if (tool == Tool.Move || ToolManager.activeToolType == typeof(DuplicateTool))
#else
            if (tool == Tool.Move || EditorTools.activeToolType == typeof(DuplicateTool))
#endif
            {
                AppendPosition();
                label = StaticStringBuilder.GetString(true);
            }
            else if (tool == Tool.Rotate)
            {
                AppendRotation();
                label = StaticStringBuilder.GetString(true);
            }
            else if (tool == Tool.Scale)
            {
                AppendScale();
                label = StaticStringBuilder.GetString(true);
            }
            else if (tool == Tool.Rect)
            {
                AppendPosition();
                StaticStringBuilder.Append("\n");

                var rt = firstTransform as RectTransform;
                if (rt != null) AppendSize(rt);
                else AppendScale();

                label = StaticStringBuilder.GetString(true);
            }
            else if (tool == Tool.Transform)
            {
                AppendPosition();
                StaticStringBuilder.Append("\n");
                AppendRotation();
                StaticStringBuilder.Append("\n");
                AppendScale();
                label = StaticStringBuilder.GetString(true);
            }
        }

        private static void OnSceneViewGUI(SceneView sceneView)
        {
            try
            {
                if (Event.current.type == EventType.Layout) InitLabel();
                if (!string.IsNullOrEmpty(label)) DrawLabel(sceneView, label);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void OnSelectionChanged()
        {
            firstTransform = null;
            if (Selection.gameObjects.Length == 0) return;

            samePositionX = samePositionY = samePositionZ =
                sameRotationX = sameRotationY = sameRotationZ = sameScaleX = sameScaleY = sameScaleZ = true;

            var instanceIDs = Selection.instanceIDs;

            for (var i = 0; i < instanceIDs.Length; i++)
            {
                var go = EditorUtility.InstanceIDToObject(instanceIDs[i]) as GameObject;
                if (go == null || go.scene.name == null) continue;

                if (firstTransform == null)
                {
                    firstTransform = go.transform;
                    continue;
                }

                var t = go.transform;
                var p1 = t.localPosition;
                var p2 = firstTransform.localPosition;
                var r1 = t.eulerAngles;
                var r2 = firstTransform.eulerAngles;
                var s1 = t.localScale;
                var s2 = firstTransform.localScale;
                if (samePositionX && Math.Abs(p1.x - p2.x) > float.Epsilon) samePositionX = false;
                if (samePositionY && Math.Abs(p1.y - p2.y) > float.Epsilon) samePositionY = false;
                if (samePositionZ && Math.Abs(p1.z - p2.z) > float.Epsilon) samePositionZ = false;
                if (sameRotationX && Math.Abs(r1.x - r2.x) > float.Epsilon) sameRotationX = false;
                if (sameRotationY && Math.Abs(r1.y - r2.y) > float.Epsilon) sameRotationY = false;
                if (sameRotationZ && Math.Abs(r1.z - r2.z) > float.Epsilon) sameRotationZ = false;
                if (sameScaleX && Math.Abs(s1.x - s2.x) > float.Epsilon) sameScaleX = false;
                if (sameScaleY && Math.Abs(s1.y - s2.y) > float.Epsilon) sameScaleY = false;
                if (sameScaleZ && Math.Abs(s1.z - s2.z) > float.Epsilon) sameScaleZ = false;
            }
        }
    }
}