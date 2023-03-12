/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.Behaviors;
using InfinityCode.UltimateEditorEnhancer.Tools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions
{
    public class History : ActionItem
    {
        protected override bool closeOnSelect => false;

        public override float order => 800;

        protected override void Init()
        {
            guiContent = new GUIContent(Icons.history, "History");
        }

        public override void Invoke()
        {
            var selectionItems = SelectionHistory.records;

            var menu = GenericMenuEx.Start();

            var sceneRecords = ReferenceManager.sceneHistory;
            var activeScene = SceneManager.GetActiveScene();
            for (var i = 0; i < sceneRecords.Count; i++)
            {
                var r = sceneRecords[i];
                if (r.path == activeScene.path) continue;

                menu.Add("Scenes/" + r.name, () =>
                {
                    EditorSceneManager.OpenScene(r.path);
                    EditorMenu.Close();
                });
            }

            for (var i = 0; i < selectionItems.Count; i++)
            {
                var ci = i;
                var names = selectionItems[i].GetShortNames();
                if (string.IsNullOrEmpty(names)) continue;
                menu.Add("Selection/" + names, SelectionHistory.activeIndex == i, () =>
                {
                    SelectionHistory.SetIndex(ci);
                    EditorMenu.Close();
                });
            }

            var recentWindows = ToolbarWindows.recent;
            for (var i = 0; i < recentWindows.Count; i++)
            {
                var r = recentWindows[i];
                menu.Add("Windows/" + r.title, () =>
                {
                    ToolbarWindows.RestoreRecentWindow(r);
                    EditorMenu.Close();
                });
            }

            menu.Show();
        }
    }
}