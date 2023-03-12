/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions
{
    public class SelectGameObject : ActionItem
    {
        protected override bool closeOnSelect => false;

        public override float order => -950;

        protected override void Init()
        {
            guiContent = new GUIContent(Icons.hierarchy, "Select GameObject");
        }

        public override void Invoke()
        {
            var items = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            var menu = GenericMenuEx.Start();

            for (var i = 0; i < items.Count; i++) GetChilds(items[i].transform, menu, "");

            menu.Show();
        }

        private void GetChilds(Transform t, GenericMenuEx menu, string prefix)
        {
            var title = prefix + t.name;

            if (t.childCount > 0)
            {
                menu.Add(title + "/Select", Selection.activeGameObject == t.gameObject, () =>
                {
                    Selection.activeGameObject = t.gameObject;
                    SceneViewManager.OnNextGUI += EditorMenu.ShowInLastPosition;
                });
                menu.AddSeparator(title + "/");
                for (var i = 0; i < t.childCount; i++) GetChilds(t.GetChild(i), menu, title + "/");
            }
            else
            {
                menu.Add(title, Selection.activeGameObject == t.gameObject, () =>
                {
                    Selection.activeGameObject = t.gameObject;
                    SceneViewManager.OnNextGUI += EditorMenu.ShowInLastPosition;
                });
            }
        }
    }
}