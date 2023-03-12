/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class HierarchyItem
    {
        public GameObject gameObject;
        public bool hovered;
        public int id;
        public Rect rect;
        public Object target;

        public void Set(int id, Rect rect)
        {
            this.id = id;
            this.rect = rect;

            target = EditorUtility.InstanceIDToObject(id);
            gameObject = target as GameObject;

            Vector2 p = Event.current.mousePosition;
            hovered = p.x >= 0 && p.x <= rect.xMax + 16 && p.y >= rect.y && p.y < rect.yMax;
        }
    }
}