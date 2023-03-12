/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class GenericMenuItem
    {
        public GenericMenu.MenuFunction action;
        public GenericMenu.MenuFunction2 action2;
        public GUIContent content;
        public bool disabled;
        public bool on;
        public float order;
        public string path;
        public bool separator;
        public object userdata;

        public void Dispose()
        {
            content = null;
            action = null;
            action2 = null;
            userdata = null;
        }
    }
}