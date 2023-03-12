/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions
{
    public class CloseComponentWindows : ActionItem, IValidatableLayoutItem
    {
        public override float order => 850;

        public bool Validate()
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<ComponentWindow>();
            return windows.Length > 0;
        }

        protected override void Init()
        {
            guiContent = new GUIContent(Icons.closeWindows, "Close All Component Windows");
        }

        public override void Invoke()
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<ComponentWindow>();
            for (var i = 0; i < windows.Length; i++)
                try
                {
                    windows[i].Close();
                }
                catch
                {
                    Object.DestroyImmediate(windows[i]);
                }
        }
    }
}