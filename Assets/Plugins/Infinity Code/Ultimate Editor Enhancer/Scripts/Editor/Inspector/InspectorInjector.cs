/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine.UIElements;

namespace InfinityCode.UltimateEditorEnhancer.InspectorTools
{
    public abstract class InspectorInjector
    {
        private List<EditorWindow> failedWindows;
        private double initTime;

        protected static VisualElement GetMainContainer(EditorWindow wnd)
        {
            return wnd != null ? GetVisualElement(wnd.rootVisualElement, "unity-inspector-main-container") : null;
        }

        protected static VisualElement GetVisualElement(VisualElement element, string className)
        {
            for (var i = 0; i < element.childCount; i++)
            {
                var el = element[i];
                if (el.ClassListContains(className)) return el;
                el = GetVisualElement(el, className);
                if (el != null) return el;
            }

            return null;
        }

        protected void InitInspector()
        {
            failedWindows = new List<EditorWindow>();

            var windows = UnityEngine.Resources.FindObjectsOfTypeAll(InspectorWindowRef.type);
            foreach (EditorWindow wnd in windows)
            {
                if (wnd == null) continue;
                if (!InjectBar(wnd)) failedWindows.Add(wnd);
            }

            if (failedWindows.Count > 0)
            {
                initTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += TryReinit;
            }
        }

        protected bool InjectBar(EditorWindow wnd)
        {
            var mainContainer = GetMainContainer(wnd);
            if (mainContainer == null) return false;
            if (mainContainer.childCount < 2) return false;
            var editorsList = GetVisualElement(mainContainer, "unity-inspector-editors-list");

            return OnInject(wnd, mainContainer, editorsList);
        }

        protected abstract bool OnInject(EditorWindow wnd, VisualElement mainContainer, VisualElement editorsList);

        protected void OnMaximizedChanged(EditorWindow w)
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll(InspectorWindowRef.type);
            foreach (EditorWindow wnd in windows)
                if (wnd != null)
                    InjectBar(wnd);
        }

        private void TryReinit()
        {
            if (EditorApplication.timeSinceStartup - initTime <= 0.5) return;
            EditorApplication.update -= TryReinit;
            if (failedWindows != null)
            {
                TryReinit(failedWindows);
                failedWindows = null;
            }
        }

        private void TryReinit(List<EditorWindow> windows)
        {
            foreach (var wnd in windows)
            {
                if (wnd == null) continue;
                InjectBar(wnd);
            }
        }
    }
}