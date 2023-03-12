/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class FlattenCollection : MonoBehaviour
    {
        public bool flatten;
        public bool removeInPlayMode;

        private void Awake()
        {
            if (flatten)
            {
                var parent = transform.parent;
                var si = transform.GetSiblingIndex();

                while (transform.childCount > 0)
                {
                    var child = transform.GetChild(transform.childCount - 1);
                    child.SetParent(parent, true);
                    child.SetSiblingIndex(si);
                }
            }

            if (removeInPlayMode) Destroy(gameObject);
        }
    }
}