/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class RotateByShortcut
    {
        static RotateByShortcut()
        {
            var binding = KeyManager.AddBinding();
            binding.OnPress += OnInvoke;
            binding.OnValidate += OnValidate;
        }

        private static Vector3 GetAxis()
        {
            var k = Event.current.keyCode;

            var ey = SceneView.lastActiveSceneView.rotation.eulerAngles.y;
            var axis = Vector3.zero;

            if (k == KeyCode.UpArrow)
            {
                if (ey >= 45 && ey <= 135) axis.z = -1;
                else if (ey >= 225 && ey <= 315) axis.z = 1;
                else if (ey >= 135 && ey <= 225) axis.x = -1;
                else axis.x = 1;
            }
            else if (k == KeyCode.DownArrow)
            {
                if (ey >= 45 && ey <= 135) axis.z = 1;
                else if (ey >= 225 && ey <= 315) axis.z = -1;
                else if (ey >= 135 && ey <= 225) axis.x = 1;
                else axis.x = -1;
            }
            else if (k == KeyCode.LeftArrow)
            {
                axis.y = -1;
            }
            else if (k == KeyCode.RightArrow)
            {
                axis.y = 1;
            }
            else if (k == KeyCode.PageDown)
            {
                if (ey >= 45 && ey <= 135) axis.x = -1;
                else if (ey >= 225 && ey <= 315) axis.x = 1;
                else if (ey >= 135 && ey <= 225) axis.z = 1;
                else axis.z = -1;
            }
            else if (k == KeyCode.PageUp)
            {
                if (ey >= 45 && ey <= 135) axis.x = 1;
                else if (ey >= 225 && ey <= 315) axis.x = -1;
                else if (ey >= 135 && ey <= 225) axis.z = -1;
                else axis.z = 1;
            }

            return axis;
        }

        private static void OnInvoke()
        {
            var axis = GetAxis();
            if (axis == Vector3.zero) return;

            var transforms = Selection.transforms.Where(t => t.gameObject.scene.name != null).ToArray();
            if (transforms.Length == 0) return;

            Undo.RecordObjects(transforms, "Rotate");

            var handlePosition = UnityEditor.Tools.handlePosition;
            var handleRotation = UnityEditor.Tools.handleRotation;

            if (UnityEditor.Tools.pivotMode == PivotMode.Center)
                for (var i = 0; i < transforms.Length; i++)
                    transforms[i].RotateAround(handlePosition, handleRotation * axis, 90);
            else if (UnityEditor.Tools.pivotRotation == PivotRotation.Local)
                for (var i = 0; i < transforms.Length; i++)
                {
                    var t = transforms[i];
                    t.Rotate(t.rotation * axis, 90, Space.World);
                }
            else
                for (var i = 0; i < transforms.Length; i++)
                    transforms[i].Rotate(handleRotation * axis, 90, Space.World);

            Event.current.Use();
        }

        private static bool OnValidate()
        {
            var e = Event.current;
            var k = e.keyCode;

            if (EditorWindow.mouseOverWindow is SceneView &&
                (e.control || e.command) && e.shift &&
                (
                    k == KeyCode.UpArrow || k == KeyCode.DownArrow ||
                    k == KeyCode.LeftArrow || k == KeyCode.RightArrow ||
                    k == KeyCode.PageDown || k == KeyCode.PageUp
                ))
                return true;

            return false;
        }
    }
}