/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Integration
{
    [InitializeOnLoad]
    public static class FullscreenEditor
    {
        private static readonly MethodInfo getFullscreenFromViewMethod;
        private static readonly MethodInfo toggleFullscreenMethod;
        private static readonly MethodInfo makeFullscreenMethod;
        private static readonly MethodInfo openSceneViewMethod;
        private static readonly MethodInfo openGameViewMethod;

        static FullscreenEditor()
        {
            var assembly = Reflection.GetAssembly("FullscreenEditor");
            if (assembly == null) return;

            var feType = assembly.GetType("FullscreenEditor.Fullscreen");
            if (feType == null) return;

            getFullscreenFromViewMethod = Reflection.GetMethod(feType, "GetFullscreenFromView",
                new[] { typeof(ScriptableObject), typeof(bool) }, BindingFlags.Static | BindingFlags.Public);
            toggleFullscreenMethod = Reflection.GetMethod(feType, "ToggleFullscreen",
                new[] { typeof(ScriptableObject) }, BindingFlags.Static | BindingFlags.Public);
            makeFullscreenMethod = Reflection.GetMethod(feType, "MakeFullscreen",
                new[] { typeof(Type), typeof(EditorWindow), typeof(bool) }, BindingFlags.Static | BindingFlags.Public);

            var menuItemsType = assembly.GetType("FullscreenEditor.MenuItems");

            openSceneViewMethod = Reflection.GetMethod(menuItemsType, "SVMenuItem", Reflection.StaticLookup);
            openGameViewMethod = Reflection.GetMethod(menuItemsType, "GVMenuItem", Reflection.StaticLookup);

            if (getFullscreenFromViewMethod == null || toggleFullscreenMethod == null ||
                makeFullscreenMethod == null) return;

            isPresent = true;
        }

        public static bool isPresent { get; }

        public static object GetFullscreenFromView(ScriptableObject viewOrWindow, bool rootView = true)
        {
            if (!isPresent) return null;
            return getFullscreenFromViewMethod.Invoke(null, new object[] { viewOrWindow, rootView });
        }

        public static bool IsFullscreen(ScriptableObject viewOrWindow)
        {
            return GetFullscreenFromView(viewOrWindow) != null;
        }

        public static object MakeFullscreen(EditorWindow window)
        {
            return MakeFullscreen(typeof(EditorWindow), window);
        }

        public static object MakeFullscreen(Type type, EditorWindow window = null, bool disposableWindow = false)
        {
            if (!isPresent) return null;
            return makeFullscreenMethod.Invoke(null, new object[] { type, window, disposableWindow });
        }

        public static void OpenFullscreenGameView()
        {
            openGameViewMethod.Invoke(null, null);
        }

        public static void OpenFullscreenSceneView()
        {
            openSceneViewMethod.Invoke(null, null);
        }

        public static void ToggleFullscreen(ScriptableObject viewOrWindow)
        {
            if (!isPresent) return;
            toggleFullscreenMethod.Invoke(null, new[] { viewOrWindow });
        }
    }
}