/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer
{
    [Serializable]
    public class SceneBookmark : BookmarkItem
    {
        public SceneBookmark()
        {
        }

        public SceneBookmark(Object obj) : base(obj)
        {
        }

        public override bool isProjectItem => false;

        public override bool HasLabel(string label)
        {
            return false;
        }
    }
}