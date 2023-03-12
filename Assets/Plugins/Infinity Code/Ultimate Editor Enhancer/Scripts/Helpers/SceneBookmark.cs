/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;

namespace InfinityCode.UltimateEditorEnhancer
{
    [Serializable]
    public class SceneBookmark : BookmarkItem
    {
        public SceneBookmark()
        {
        }

        public SceneBookmark(UnityEngine.Object obj) : base(obj)
        {
        }

        public override bool isProjectItem
        {
            get { return false; }
        }

        public override bool HasLabel(string label)
        {
            return false;
        }
    }
}