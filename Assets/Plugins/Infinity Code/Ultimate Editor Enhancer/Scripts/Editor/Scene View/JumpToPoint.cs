/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class JumpToPoint
    {
        public const double MAX_SHIFT_DELAY = 0.3;
        public static double lastShiftPressed;

        static JumpToPoint()
        {
            SceneViewManager.AddListener(OnSceneGUI);
        }

        public static bool GetTargetPoint(Event e, out Vector3 targetPosition)
        {
            var targets = new List<GameObject>();
            var mousePosition = e.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            targetPosition = Vector3.zero;

            var count = 0;
            while (count < 20)
            {
                var go = HandleUtility.PickGameObject(mousePosition, false, targets.ToArray());
                if (go == null) break;

                targets.Add(go);
                var collider = go.GetComponentInParent<Collider>();

                if (collider != null)
                {
                    RaycastHit hit;
                    if (collider.Raycast(ray, out hit, float.MaxValue))
                    {
                        targetPosition = hit.point;
                        return true;
                    }
                }

                count++;
            }

            return false;
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (!Prefs.jumpToPoint || view.orthographic) return;

            var e = Event.current;
            var isJump = e.type == EventType.MouseUp && e.button == 2 && e.modifiers == EventModifiers.Shift;

            if (!isJump && Prefs.alternativeJumpShortcut && EditorWindow.mouseOverWindow is SceneView)
            {
                var isAlternativeShortcut = e.type == EventType.KeyUp && !e.control && !e.command &&
                                            (e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift);
                if (isAlternativeShortcut)
                {
                    var timeDelta = EditorApplication.timeSinceStartup - lastShiftPressed;
                    isJump = timeDelta < MAX_SHIFT_DELAY;
                    lastShiftPressed = isJump ? 0 : EditorApplication.timeSinceStartup;
                }
            }

            if (!isJump) return;
            if (!GetTargetPoint(e, out var targetPosition)) return;

            view.LookAt(targetPosition + new Vector3(0, 1.5f, 0), view.rotation, 1.5f);

            UnityEditor.Tools.viewTool = ViewTool.None;
            GUIUtility.hotControl = 0;

            e.Use();
        }
    }
}