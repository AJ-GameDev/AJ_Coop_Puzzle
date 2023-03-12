/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions
{
    public class UpdateAvailable : ActionItem, IValidatableLayoutItem
    {
        public override float order => 950;

        public bool Validate()
        {
            Updater.CheckNewVersionAvailable();
            return Updater.hasNewVersion;
        }

        protected override void Init()
        {
            guiContent = new GUIContent(Icons.updateAvailable, "Update Available");
        }

        public override void Invoke()
        {
            Updater.OpenWindow();
        }
    }
}