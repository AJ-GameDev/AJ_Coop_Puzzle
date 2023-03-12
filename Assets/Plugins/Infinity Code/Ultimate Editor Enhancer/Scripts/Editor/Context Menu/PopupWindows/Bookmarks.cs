/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.PopupWindows
{
    public class Bookmarks : PopupWindowItem
    {
        public override Texture icon => Styles.isProSkin ? Icons.starWhite : Icons.starBlack;

        protected override string label => "Bookmarks";

        public override float order => 80;

        protected override void ShowTab(Vector2 mousePosition)
        {
            Windows.Bookmarks.ShowWindow(mousePosition);
        }

        protected override void ShowUtility(Vector2 mousePosition)
        {
            Windows.Bookmarks.ShowUtilityWindow(mousePosition);
        }

        protected override void ShowPopup(Vector2 mousePosition)
        {
            var wnd = Windows.Bookmarks.ShowDropDownWindow(mousePosition);
            EventManager.AddBinding(EventManager.ClosePopupEvent).OnInvoke += b =>
            {
                wnd.Close();
                b.Remove();
            };
        }
    }
}