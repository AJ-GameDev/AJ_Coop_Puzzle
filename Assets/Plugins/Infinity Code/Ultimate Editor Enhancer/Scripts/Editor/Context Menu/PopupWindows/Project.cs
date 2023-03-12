/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.PopupWindows
{
    public class Project : PopupWindowItem
    {
        public override Texture icon => EditorIconContents.project.image;

        protected override string label => "Project";

        public override float order => -70;

        private static void FinalizeWindow(EditorWindow window)
        {
            var windowType = window.GetType();
            try
            {
                Reflection.InvokeMethod(windowType, "SetOneColumn", window);
            }
            catch
            {
            }
        }

        protected override void ShowTab(Vector2 mousePosition)
        {
            var windowType = ProjectBrowserRef.type;
            Vector2 windowSize = Prefs.defaultWindowSize;
            var rect = new Rect(GUIUtility.GUIToScreenPoint(mousePosition) - windowSize / 2, Vector2.zero);

            var window = ScriptableObject.CreateInstance(windowType) as EditorWindow;
            window.Show();
            window.Focus();
            window.position = new Rect(rect.position, windowSize);

            FinalizeWindow(window);
        }

        protected override void ShowUtility(Vector2 mousePosition)
        {
            var windowType = ProjectBrowserRef.type;
            Vector2 windowSize = Prefs.defaultWindowSize;
            var rect = new Rect(GUIUtility.GUIToScreenPoint(mousePosition) - windowSize / 2, Vector2.zero);

            var window = ScriptableObject.CreateInstance(windowType) as EditorWindow;
            window.ShowUtility();
            window.Focus();
            window.position = new Rect(rect.position, windowSize);

            FinalizeWindow(window);
        }

        protected override void ShowPopup(Vector2 mousePosition)
        {
            var windowType = ProjectBrowserRef.type;
            Vector2 windowSize = Prefs.defaultWindowSize;
            var rect = new Rect(GUIUtility.GUIToScreenPoint(mousePosition) - windowSize / 2, Vector2.zero);

            var windowRect = new Rect(rect.position, windowSize);
            if (windowRect.y < 40) windowRect.y = 40;

            var window = ScriptableObject.CreateInstance(windowType) as EditorWindow;
            window.position = windowRect;
            window.ShowPopup();
            window.Focus();
            EventManager.AddBinding(EventManager.ClosePopupEvent).OnInvoke += b =>
            {
                window.Close();
                b.Remove();
            };
            PinAndClose.Show(window, windowRect, window.Close, () =>
            {
                var wnd = Object.Instantiate(window);
                wnd.Show();
                var wRect = window.position;
                wRect.yMin -= PinAndClose.HEIGHT;
                wnd.position = wRect;
                wnd.maxSize = new Vector2(4000f, 4000f);
                wnd.minSize = new Vector2(100f, 100f);
                window.Close();
            }, "Project");

            FinalizeWindow(window);
        }
    }
}