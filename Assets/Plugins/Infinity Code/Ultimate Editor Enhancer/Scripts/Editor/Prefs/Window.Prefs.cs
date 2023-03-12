/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public partial class Prefs
    {
        public static Vector2Int defaultWindowSize = new(400, 300);

        public class WindowManager : PrefManager
        {
            public override float order => Order.window;

            public override void Draw()
            {
                defaultWindowSize = EditorGUILayout.Vector2IntField("Window Size", defaultWindowSize);
            }
        }
    }
}