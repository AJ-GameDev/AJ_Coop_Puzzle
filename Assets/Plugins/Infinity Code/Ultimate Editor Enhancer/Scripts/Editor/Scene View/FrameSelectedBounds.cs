/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class FrameSelectedBounds
    {
        static FrameSelectedBounds()
        {
            var binding = KeyManager.AddBinding();
            binding.OnValidate += OnValidate;
            binding.OnPress += OnInvoke;
        }

        private static void OnInvoke()
        {
            var gameObjects = Selection.gameObjects;

            var isFirst = true;
            var is2D = false;
            var bounds = new Bounds();

            for (var i = 0; i < gameObjects.Length; i++)
            {
                var go = gameObjects[i];
                if (go.scene.name == null) continue;

                var renderers = go.GetComponentsInChildren<Renderer>();
                for (var j = 0; j < renderers.Length; j++)
                {
                    var renderer = renderers[j];
                    if (renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;

                    if (isFirst)
                    {
                        bounds = renderer.bounds;
                        isFirst = false;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                var fourCorners = new Vector3[4];
                var rectTransforms = go.GetComponentsInChildren<RectTransform>();
                for (var j = 0; j < rectTransforms.Length; j++)
                {
                    var rt = rectTransforms[j];
                    rt.GetWorldCorners(fourCorners);

                    if (isFirst)
                    {
                        is2D = true;
                        bounds.center = fourCorners[0];
                        for (var k = 1; k < 4; k++) bounds.Encapsulate(fourCorners[k]);
                        isFirst = false;
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++) bounds.Encapsulate(fourCorners[k]);
                    }
                }

                var colliders = go.GetComponentsInChildren<Collider>();

                for (var j = 0; j < colliders.Length; j++)
                {
                    var collider = colliders[j];

                    if (isFirst)
                    {
                        bounds = collider.bounds;
                        isFirst = false;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }
            }

            if (isFirst) return;

            SceneView.lastActiveSceneView.in2DMode = is2D;

            SceneView.lastActiveSceneView.Frame(bounds, false);

            Event.current.Use();
        }

        private static bool OnValidate()
        {
            if (!Prefs.frameSelectedBounds) return false;

            var e = Event.current;
            if (e.type != EventType.KeyDown || e.keyCode != KeyCode.F || e.modifiers != EventModifiers.Shift)
                return false;
            if (SceneView.lastActiveSceneView == null) return false;
            return true;
        }
    }
}