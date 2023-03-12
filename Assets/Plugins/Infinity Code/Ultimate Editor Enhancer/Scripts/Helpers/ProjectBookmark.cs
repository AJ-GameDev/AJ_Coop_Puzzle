/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer
{
    [Serializable]
    public class ProjectBookmark : BookmarkItem
    {
        [NonSerialized] private string _path;

        public ProjectBookmark()
        {
        }

        public ProjectBookmark(Object obj) : base(obj)
        {
        }

        public override bool isProjectItem => true;

        public string path
        {
            get
            {
#if UNITY_EDITOR
                if (_path == null) _path = AssetDatabase.GetAssetPath(target);
#endif

                return _path;
            }
        }

        public override bool HasLabel(string label)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetLabels(target).Contains(label);
#else
            return false;
#endif
        }
    }
}