/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public partial class Bookmarks
    {
        private const string GridSizePref = Prefs.Prefix + "Bookmarks.GridSize";
        private const int gridMargin = 10;
        private static int gridSize = 47;
        private static GUIStyle selectedStyle;
        private readonly int maxGridSize = 128;

        private readonly int minGridSize = 47;

        private bool DrawCell(BookmarkItem item, Rect rect)
        {
            var selected = item.target != null && Selection.activeObject == item.target;

            if (item.preview == null || !item.previewLoaded) InitPreview(item);

            ProcessCellEvents(item, rect);

            if (selected) GUI.Box(new RectOffset(2, 2, 2, 2).Add(rect), GUIContent.none, selectedStyle);

            GUI.DrawTexture(new Rect(rect.x + gridMargin, rect.y, gridSize, gridSize), item.preview);
            var content = new GUIContent(item.title);
            var style = new GUIStyle(EditorStyles.miniLabel);
            var size = style.CalcSize(content);
            if (size.x < rect.width) style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(rect.x, rect.y + gridSize, rect.width, 20), content, style);

            return true;
        }

        private void DrawGridItems(IEnumerable<BookmarkItem> gridItems, string label = null)
        {
            if (!string.IsNullOrEmpty(label)) GUILayout.Label(label);

            var countItems = gridItems.Count();

            var countCols = Mathf.FloorToInt((position.width - 30) / (gridSize + gridMargin * 2));
            var countRows = Mathf.CeilToInt(countItems / (float)countCols);
            var rowHeight = gridSize + 20;
            var width = Mathf.Min(countCols, countItems) * (gridSize + gridMargin * 2);
            var height = countRows * rowHeight;

            var marginLeft = (position.width - width) / 2;

            GUILayout.Box(GUIContent.none, GUIStyle.none, GUILayout.Width(width), GUILayout.Height(height));
            var rect = GUILayoutUtility.GetLastRect();

            var i = 0;
            foreach (var item in gridItems)
            {
                var row = i / countCols;
                var col = i % countCols;
                var r = new Rect(col * (gridSize + gridMargin * 2) + marginLeft, row * rowHeight + rect.y,
                    gridSize + gridMargin * 2, rowHeight);
                if (!DrawCell(item, r)) removeItem = item;
                i++;
            }
        }

        private void ProcessCellEvents(BookmarkItem item, Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type == EventType.MouseUp)
            {
                if (e.button == 0)
                {
                    if (Selection.activeObject == item.target)
                    {
                        ProcessDoubleClick(item);
                    }
                    else
                    {
                        lastClickTime = EditorApplication.timeSinceStartup;
                        Selection.activeObject = item.target;
                        EditorGUIUtility.PingObject(item.target);
                    }

                    e.Use();
                }
                else if (e.button == 1)
                {
                    ShowContextMenu(item);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (GUIUtility.hotControl == 0)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { item.target };

                    DragAndDrop.StartDrag("Drag " + item.target);
                    e.Use();
                }
            }
        }
    }
}