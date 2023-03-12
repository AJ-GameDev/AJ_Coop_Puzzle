/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class DropToFloor
    {
        public enum CountRays
        {
            five = 0,
            one = 1
        }

        public static Dictionary<Transform, float> movedObjects;
        public static List<Vector3> points;

        static DropToFloor()
        {
            var binding = KeyManager.AddBinding();
            binding.OnPress += OnInvoke;
            binding.OnValidate += OnValidate;
        }

        private static void DropRenderer(Renderer renderer)
        {
            var bounds = renderer.bounds;
            var min = bounds.min;
            var size = bounds.size;

            points.Clear();

            RaycastRendererPoints(min, size);

            if (points.Count == 0)
                points = new List<Vector3>
                {
                    new(min.x, 0, min.z)
                };

            Undo.RecordObject(renderer.transform, "Drop To Floor");

            var shift = points.Average(v => v.y) - min.y;

            renderer.transform.Translate(0, shift, 0, Space.World);
            movedObjects.Add(renderer.transform, shift);
        }

        private static void DropTransform(Transform transform)
        {
            Vector3 p;
            if (RaycastDown(transform.position + new Vector3(0, 0.1f, 0), out p) == -1)
                p = new Vector3(transform.position.x, 0, transform.position.z);

            Undo.RecordObject(transform, "Drop To Floor");

            var shift = p.y - transform.position.y;

            transform.Translate(0, shift, 0, Space.World);
            movedObjects.Add(transform, shift);
        }

        private static void OnInvoke()
        {
            var targets = Selection.gameObjects.Where(g => g.scene.name != null).OrderBy(g => g.transform.position.y)
                .ToArray();

            Undo.SetCurrentGroupName("Drop To Floor");
            var group = Undo.GetCurrentGroup();

            if (movedObjects == null) movedObjects = new Dictionary<Transform, float>();
            if (points == null) points = new List<Vector3>(5);

            for (var i = 0; i < targets.Length; i++)
            {
                var go = targets[i];
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) DropRenderer(renderer);
                else DropTransform(go.transform);
            }

            movedObjects.Clear();
            Undo.CollapseUndoOperations(group);
        }

        private static bool OnValidate()
        {
            var e = Event.current;
            if (!Prefs.dropToFloor || e.keyCode != Prefs.dropToFloorKeyCode ||
                e.modifiers != (Prefs.dropToFloorModifiers | EventModifiers.FunctionKey)) return false;

            if (Selection.gameObjects.Length == 0) return false;
            if (Selection.gameObjects.All(g => g.scene.name == null)) return false;
            return true;
        }

        public static int RaycastDown(Vector3 point, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            RaycastHit hit;
            if (Physics.Raycast(point, Vector3.down, out hit))
            {
                hitPoint = hit.point;
                float shift;
                if (movedObjects.TryGetValue(hit.transform, out shift))
                {
                    hitPoint.y += shift;
                    return 1;
                }

                return 0;
            }

            return -1;
        }

        public static void RaycastRendererPoints(Vector3 min, Vector3 size, CountRays countRays = CountRays.five)
        {
            Vector3 p;
            var y = min.y + 0.1f;
            int r;
            if (countRays == CountRays.five)
            {
                r = RaycastDown(new Vector3(min.x, y, min.z), out p);
                if (r == 0)
                {
                    points.Add(p);
                }
                else if (r == 1)
                {
                    points.Clear();
                    points.Add(p);
                    return;
                }

                r = RaycastDown(new Vector3(min.x + size.x, y, min.z), out p);
                if (r == 0)
                {
                    points.Add(p);
                }
                else if (r == 1)
                {
                    points.Clear();
                    points.Add(p);
                    return;
                }

                r = RaycastDown(new Vector3(min.x + size.x, y, min.z + size.z), out p);
                if (r == 0)
                {
                    points.Add(p);
                }
                else if (r == 1)
                {
                    points.Clear();
                    points.Add(p);
                    return;
                }

                r = RaycastDown(new Vector3(min.x, y, min.z + size.z), out p);
                if (r == 0)
                {
                    points.Add(p);
                }
                else if (r == 1)
                {
                    points.Clear();
                    points.Add(p);
                    return;
                }
            }

            r = RaycastDown(new Vector3(min.x + size.x / 2, y, min.z + size.z / 2), out p);
            if (r == 0)
            {
                points.Add(p);
            }
            else if (r == 1)
            {
                points.Clear();
                points.Add(p);
            }
        }
    }
}