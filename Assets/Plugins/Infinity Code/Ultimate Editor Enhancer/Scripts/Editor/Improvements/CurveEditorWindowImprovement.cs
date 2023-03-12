/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.Interceptors;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Improvements
{
    [InitializeOnLoad]
    public static class CurveEditorWindowImprovement
    {
        private static bool enabled;
        private static Rect frameButtonRect;
        private static Rect pointsButtonRect;
        private static Vector2 scrollPosition;

        static CurveEditorWindowImprovement()
        {
            CurveEditorWindowGetCurveEditorInterceptor.OnGetCurveEditorRect += OnGetCurveEditorRect;
            CurveEditorWindowOnGUIInterceptor.OnGUIAfter += OnGUIAfter;
            CurveEditorWindowOnGUIInterceptor.OnGUIBefore += OnGUIBefore;
        }

        private static void DrawList(EditorWindow window)
        {
            GUILayout.BeginArea(new Rect(window.position.width - 200, 0, 200, window.position.height));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(TempContent.Get("<", "Close Points"), EditorStyles.toolbarButton,
                    GUILayout.ExpandWidth(false))) enabled = false;
            GUILayout.Label("Points");

            GUILayout.EndHorizontal();

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            EditorGUI.BeginChangeCheck();

            var curve = CurveEditorWindowRef.GetCurve();
            var keys = curve.keys;
            var addTime = float.MinValue;
            var removeIndex = -1;

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];

                EditorGUILayout.BeginHorizontal();
                key.time = EditorGUILayout.DelayedFloatField("Time", key.time);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false))) removeIndex = i;
                EditorGUILayout.EndHorizontal();
                key.value = EditorGUILayout.DelayedFloatField("Value", key.value);

                keys[i] = key;

                if (i < keys.Length - 1 &&
                    GUILayout.Button(TempContent.Get("+", "Insert Key"), EditorStyles.miniButton))
                    addTime = (keys[i].time + keys[i + 1].time) / 2;
            }

            if (EditorGUI.EndChangeCheck())
            {
                curve.keys = keys;
                CurveEditorWindowRef.RefreshShownCurves(window);
                CurveEditorWindowRef.SendEvent(window, "CurveChanged", false);
            }

            if (removeIndex != -1)
            {
                ArrayUtility.RemoveAt(ref keys, removeIndex);
                curve.keys = keys;
                CurveEditorWindowRef.RefreshShownCurves(window);
                CurveEditorWindowRef.SendEvent(window, "CurveChanged", false);
            }
            else if (addTime != float.MinValue)
            {
                var index = AnimationUtilityRef.AddInbetweenKey(curve, addTime);
                var type = Reflection.GetEditorType("CurveUtility");
                Reflection.InvokeStaticMethod(type, "SetKeyModeFromContext",
                    new[] { typeof(AnimationCurve), typeof(int) }, new object[] { curve, index });
                AnimationUtilityRef.UpdateTangentsFromModeSurrounding(curve, index);
                CurveEditorWindowRef.RefreshShownCurves(window);
                CurveEditorWindowRef.SendEvent(window, "CurveChanged", false);
            }

            EditorGUIUtility.labelWidth = labelWidth;

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static void OnGUIAfter(EditorWindow window)
        {
            if (!Prefs.improveCurveEditor) return;
            if (Event.current.type != EventType.Repaint) return;

            GUI.skin.button.Draw(frameButtonRect,
                TempContent.Get(EditorIconContents.rectTransformBlueprint.image, "Frame All Points"), -1);

            if (!enabled) GUI.skin.button.Draw(pointsButtonRect, TempContent.Get(Icons.hierarchy, "Show Points"), -1);
        }

        private static void OnGUIBefore(EditorWindow window)
        {
            if (!Prefs.improveCurveEditor) return;

            frameButtonRect = new Rect(window.position.width - 40, 10, 25, 25);

            if (enabled) frameButtonRect.x -= 200;

            pointsButtonRect = new Rect(window.position.width - 40, 40, 25, 25);
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (frameButtonRect.Contains(e.mousePosition))
                {
                    CurveEditorWindowRef.FrameClip(window);
                    e.Use();
                }
                else if (!enabled && pointsButtonRect.Contains(e.mousePosition))
                {
                    enabled = true;
                    e.Use();
                }
            }

            if (enabled) DrawList(window);
        }

        private static Rect OnGetCurveEditorRect(EditorWindow window)
        {
            if (!enabled) return default;

            var position = window.position;
            var width = position.width - 200;
            var height = position.height - 46f;
            return new Rect(0, 0, width, height);
        }
    }
}