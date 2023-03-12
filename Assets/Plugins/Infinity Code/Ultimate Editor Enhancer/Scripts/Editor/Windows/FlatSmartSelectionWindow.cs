/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.HierarchyTools;
using InfinityCode.UltimateEditorEnhancer.SceneTools;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Highlighter = InfinityCode.UltimateEditorEnhancer.SceneTools.Highlighter;

namespace InfinityCode.UltimateEditorEnhancer.Tools
{
    public class FlatSmartSelectionWindow : AutoSizePopupWindow
    {
        private static string filterText;
        private static bool resetSelection;
        private static List<FlatItem> activeItems;
        private static List<FlatItem> flatItems;

        private static TreeViewState treeViewState;
        private static FlatTreeView treeView;
        private GameObject highlightGO;
        private GameObject lastHighlightGO;

        public static FlatSmartSelectionWindow instance { get; private set; }

        private void OnEnable()
        {
            resetSelection = true;
            Highlighter.Highlight(null);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var item in flatItems) item.Dispose();
            flatItems.Clear();

            Waila.mode = 0;
            UnityEditor.Tools.hidden = false;
            filterText = null;
            activeItems = null;
            scrollPosition = Vector2.zero;
            treeView = null;
            treeViewState = null;

            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
        }

        private static bool DrawFilterTextField()
        {
            GUI.SetNextControlName("UEESmartSelectionSearchTextField");
            EditorGUI.BeginChangeCheck();
            filterText = GUILayoutUtils.ToolbarSearchField(filterText);
            var changed = EditorGUI.EndChangeCheck();

            if (resetSelection && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("UEESmartSelectionSearchTextField");
                resetSelection = false;
            }

            return changed;
        }

        protected override void OnContentGUI()
        {
            var e = Event.current;
            var type = e.type;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Select GameObject");
            if (GUILayoutUtils.ToolbarButton("?")) Links.OpenDocumentation("smart-selection");
            EditorGUILayout.EndHorizontal();

            var filterChanged = DrawFilterTextField();

            var needReload = false;

            if (filterChanged || activeItems == null)
            {
                if (string.IsNullOrEmpty(filterText))
                {
                    activeItems = flatItems;
                }
                else
                {
                    var pattern = SearchableItem.GetPattern(filterText);
                    activeItems = flatItems.Where(p => p.UpdateAccuracy(pattern) > 0).OrderByDescending(p => p.accuracy)
                        .ToList();
                }

                needReload = true;
            }

            if (treeView == null)
            {
                treeViewState = new TreeViewState();
                treeView = new FlatTreeView(treeViewState);
                needReload = false;
            }

            if (needReload) treeView.Reload();

            highlightGO = null;
            var lastRect = GUILayoutUtility.GetLastRect();
            var rect = GUILayoutUtility.GetRect(0, 100000, 0,
                Mathf.Min(Prefs.defaultWindowSize.y - lastRect.y, treeView.totalHeight));
            treeView.OnGUI(rect);

            if (highlightGO != lastHighlightGO)
            {
                lastHighlightGO = highlightGO;
                Highlighter.Highlight(highlightGO);
                GUI.changed = true;
            }

            if (type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                }
                else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
                {
                    if (GUI.GetNameOfFocusedControl() == "UEESmartSelectionSearchTextField")
                    {
                        treeView.SetFocusAndEnsureSelectedItem();
                        e.Use();
                    }
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    var selection = treeView.GetSelection();
                    var id = selection.Count > 0 ? selection.First() : 0;
                    SelectByDisplayID(id);
                }
            }
        }

        public GameObject Get(int id)
        {
            if (id < treeView.items.Count) return treeView.items[id];
            return null;
        }

        private void SelectByDisplayID(int id)
        {
            if (id < treeView.items.Count)
            {
                var target = treeView.items[id];
                if (Event.current.control || Event.current.shift) SelectionRef.Add(target);
                else Selection.activeGameObject = target;
            }

            Close();
        }

        public new static FlatSmartSelectionWindow Show()
        {
            if (flatItems == null) flatItems = new List<FlatItem>();
            else flatItems.Clear();

            flatItems.AddRange(Waila.targets.Select(t => new FlatItem(t)));
            if (flatItems.Count == 0) return null;

            Vector2 size = Prefs.defaultWindowSize;
            var position = Event.current.mousePosition - new Vector2(size.x / 2, -30);

            position = GUIUtility.GUIToScreenPoint(position);

            var rect = new Rect(position, size);

            instance = CreateInstance<FlatSmartSelectionWindow>();
            instance.minSize = Vector2.zero;
            instance.position = rect;
            instance.adjustHeight = AutoSize.top;
            instance.ShowPopup();
            instance.Focus();
            instance.wantsMouseMove = true;
            return instance;
        }

        internal class FlatTreeView : TreeView
        {
            public List<GameObject> items;

            public FlatTreeView(TreeViewState state) : base(state)
            {
                Reload();
            }

            public FlatTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state,
                multiColumnHeader)
            {
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
                var allItems = new List<TreeViewItem>();

                var nextID = 0;
                items = new List<GameObject>();

                for (var i = 0; i < activeItems.Count; i++)
                {
                    var flatItem = activeItems[i];
                    var item = new TreeViewItem
                    {
                        id = nextID++, depth = 0, displayName = flatItem.name,
                        icon = BestIconDrawer.GetGameObjectIcon(flatItem.target) as Texture2D
                    };
                    allItems.Add(item);
                    items.Add(flatItem.target);

                    var parent = flatItem.target.transform.parent;
                    while (parent != null)
                    {
                        var go = parent.gameObject;
                        item = new TreeViewItem
                        {
                            id = nextID++, depth = 1, displayName = go.name,
                            icon = BestIconDrawer.GetGameObjectIcon(go) as Texture2D
                        };
                        items.Add(go);
                        allItems.Add(item);
                        parent = parent.parent;
                    }
                }

                SetupParentsAndChildrenFromDepths(root, allItems);

                return root;
            }

            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                return true;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (args.rowRect.Contains(Event.current.mousePosition))
                {
                    instance.highlightGO = instance.Get(args.item.id);
                    GUI.DrawTexture(args.rowRect, Styles.selectedRowTexture, ScaleMode.StretchToFill);
                }

                base.RowGUI(args);
            }

            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                var go = instance.Get(args.draggedItemIDs[0]);
                if (go == null) return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { go };
                DragAndDrop.StartDrag("Drag " + go.name);
                Event.current.Use();
                instance.Close();
            }

            protected override void SingleClickedItem(int id)
            {
                instance.SelectByDisplayID(id);
            }
        }

        internal class FlatItem : SearchableItem
        {
            public string name;
            public GameObject target;

            public FlatItem(GameObject target)
            {
                this.target = target;
                name = target.name;
            }

            public void Dispose()
            {
                target = null;
            }

            protected override int GetSearchCount()
            {
                return 1;
            }

            protected override string GetSearchString(int index)
            {
                return name;
            }
        }
    }
}