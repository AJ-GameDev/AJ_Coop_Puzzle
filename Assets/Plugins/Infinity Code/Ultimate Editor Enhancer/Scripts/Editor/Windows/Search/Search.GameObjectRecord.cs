/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public partial class Search
    {
        internal class GameObjectRecord : Record
        {
            public GameObjectRecord(GameObject gameObject)
            {
                this.gameObject = gameObject;

                search = new[]
                {
                    gameObject.name
                };
            }

            public GameObject gameObject { get; private set; }

            public override Object target => gameObject;

            public override string tooltip
            {
                get
                {
                    if (_tooltip == null) _tooltip = GameObjectUtils.GetGameObjectPath(gameObject).ToString();
                    return _tooltip;
                }
            }

            public override string type => "gameobject";

            public override void Dispose()
            {
                base.Dispose();
                gameObject = null;
            }

            public override void Select(int state)
            {
                if (state == 2)
                    WindowsHelper.ShowInspector();
                else if (state == 1)
                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.Focus();

                Selection.activeGameObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);

                if (state == 3) SceneView.FrameLastActiveSceneView();
            }

            protected override void ShowContextMenu(int index)
            {
                GameObjectUtils.ShowContextMenu(false, gameObject);
            }

            public override void UpdateGameObjectName(GameObject go)
            {
                if (gameObject == go) return;

                search[0] = go.name;
            }
        }
    }
}