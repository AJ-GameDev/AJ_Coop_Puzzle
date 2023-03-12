/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions;
using InfinityCode.UltimateEditorEnhancer.Integration;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.HierarchyTools
{
    [InitializeOnLoad]
    public static class ComponentIconDrawer
    {
        private static GameObject prevTarget;
        private static readonly List<Item> prevItems;
        private static Dictionary<int, List<Item>> cache;
        private static bool ehVisible = true;
        private static int ehRightMargin;
        private static int activeID;

        static ComponentIconDrawer()
        {
            prevItems = new List<Item>();
            HierarchyItemDrawer.Register("ComponentIconDrawer", DrawHierarchyItem);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            if (Prefs.hierarchyIconsDisplayRule != HierarchyIconsDisplayRule.always) return;
            if (mode == PlayModeStateChange.ExitingEditMode || mode == PlayModeStateChange.ExitingPlayMode) return;

            if (cache != null) cache.Clear();
            prevItems.Clear();
        }

        private static void DrawHierarchyItem(HierarchyItem item)
        {
            if (!Prefs.hierarchyIcons) return;

            var e = Event.current;
            if (Prefs.hierarchyIconsDisplayRule == HierarchyIconsDisplayRule.onHoverWithModifiers &&
                e.modifiers != Prefs.hierarchyIconsModifiers)
            {
                if (!ehVisible)
                {
                    EnhancedHierarchy.SetRightMargin(ehRightMargin);
                    ehVisible = true;
                }

                return;
            }

            if (ehVisible)
            {
                ehRightMargin = EnhancedHierarchy.GetRightMargin();
                EnhancedHierarchy.SetRightMargin(-10000);
                ehVisible = false;
            }

            var rect = item.rect;

            if (Prefs.hierarchyIconsDisplayRule != HierarchyIconsDisplayRule.always)
            {
                if (e.type == EventType.Layout)
                {
                    if (!item.hovered)
                    {
                        if (item.id == activeID) activeID = -1;
                    }
                    else
                    {
                        activeID = item.id;
                    }
                }
                else if (activeID != item.id)
                {
                    return;
                }

                if (item.gameObject != prevTarget)
                {
                    prevTarget = item.gameObject;
                    UpdateItems(rect, item.gameObject, prevItems);
                }

                if (prevTarget == null) return;

                var lastRect = new Rect(rect.xMax, rect.y, 0, rect.height);
                if (prevItems.Any(i => i.content.tooltip == "Cinemachine Brain")) lastRect.x -= 20;

                for (var i = prevItems.Count - 1; i >= 0; i--)
                {
                    var iconItem = prevItems[i];
                    var lastX = iconItem.Draw(lastRect);
                    lastRect.x = lastX;
                }
            }
            else
            {
                var lastRect = new Rect(rect.xMax, rect.y, 0, rect.height);

                var items = GetItemsFromCache(item.gameObject, rect);

                if (items.Any(i => i.content.tooltip == "Cinemachine Brain")) lastRect.x -= 20;

                for (var i = items.Count - 1; i >= 0; i--)
                {
                    var iconItem = items[i];
                    var lastX = iconItem.Draw(lastRect);
                    lastRect.x = lastX;
                }
            }
        }

        private static List<Item> GetItemsFromCache(GameObject target, Rect rect)
        {
            if (target == null) return new List<Item>();

            List<Item> cachedItems;
            if (cache == null) cache = new Dictionary<int, List<Item>>();
            else if (cache.TryGetValue(target.GetInstanceID(), out cachedItems)) return cachedItems;
            cachedItems = new List<Item>();
            UpdateItems(rect, target, cachedItems);
            cache.Add(target.GetInstanceID(), cachedItems);
            return cachedItems;
        }

        private static void ShowAddComponent(Rect hierarchyRect)
        {
            var e = Event.current;
            var position = e.mousePosition;
            position.y = hierarchyRect.yMax;
            if (EditorWindow.focusedWindow != null) position += EditorWindow.focusedWindow.position.position;
            else position = HandleUtility.GUIPointToScreenPixelCoordinate(position);

            Vector2 size = Prefs.defaultWindowSize;
            var rect = new Rect(position + new Vector2(-size.x / 2, 36), size);

#if !UNITY_EDITOR_OSX
            if (rect.yMax > Screen.currentResolution.height - 10) rect.y -= rect.height - 50;

            if (rect.x < 5) rect.x = 5;
            else if (rect.xMax > Screen.currentResolution.width - 5) rect.x =
 Screen.currentResolution.width - 5 - rect.width;
#endif

            Selection.activeGameObject = prevTarget;
            AddComponent.ShowAddComponent(rect);
        }

        private static void ShowComponent(Component component, Rect hierarchyRect)
        {
            var e = Event.current;
            var position = e.mousePosition;
            position.y = hierarchyRect.yMax;
            if (EditorWindow.focusedWindow != null) position += EditorWindow.focusedWindow.position.position;
            else position = HandleUtility.GUIPointToScreenPixelCoordinate(position);

            Vector2 size = Prefs.defaultWindowSize;
            var rect = new Rect(position + new Vector2(-size.x / 2, 36), size);

#if !UNITY_EDITOR_OSX
            if (rect.yMax > Screen.currentResolution.height - 10) rect.y -= rect.height - 50;

            int screenWidth = Screen.currentResolution.width;

            if (rect.x < 5) rect.x = 5;
            else if (rect.center.x > screenWidth)
            {
                if (rect.xMin < screenWidth) rect.x = screenWidth;
            }
            else if (rect.xMax > screenWidth - 5)
            {
                rect.x = screenWidth - 5 - rect.width;
            }
#endif

            var wnd = ComponentWindow.Show(component);
            wnd.position = rect;
        }

        private static void ShowMore(IEnumerable<Component> components, Rect rect)
        {
            var menu = GenericMenuEx.Start();
            var useSeparator = false;

            foreach (var c in components)
            {
                menu.Add(c.GetType().Name, () =>
                {
                    SceneViewManager.OnNextGUI += () => ShowComponent(c, rect);
                    SceneView.RepaintAll();
                });
                useSeparator = true;
            }

            if (useSeparator) menu.AddSeparator();

            menu.Add("Add Component", () =>
            {
                SceneViewManager.OnNextGUI += () => ShowAddComponent(rect);
                SceneView.RepaintAll();
            });

            menu.Add("Add To Bookmark", () =>
            {
                Bookmarks.Add(prevTarget);
                SceneView.RepaintAll();
            });
            menu.Show();
        }

        private static void UpdateItems(Rect rect, GameObject target, List<Item> items)
        {
            items.Clear();

            if (target == null) return;

            var components = target.GetComponents<Component>();
            Item item;

            for (var i = 0; i < Mathf.Min(components.Length, Prefs.hierarchyIconsMaxItems); i++)
            {
                var component = components[i];
                if (component == null) continue;
                var thumbnail = AssetPreview.GetMiniThumbnail(component);
                var content = new GUIContent(
                    thumbnail,
                    ObjectNames.NicifyVariableName(component.GetType().Name)
                );
                if (thumbnail.name == "cs Script Icon" || thumbnail.name == "d_cs Script Icon")
                    GameObjectUtils.GetPsIconContent(content);

                item = new Item(content);
                item.OnClick += () => ShowComponent(component, rect);
                item.OnDrag += () =>
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { component };
                    DragAndDrop.StartDrag("Drag Component");
                };
                item.OnRightClick += () => ComponentUtils.ShowContextMenu(component);
                items.Add(item);
            }

            var moreItems = components.Length - Prefs.hierarchyIconsMaxItems;

            item = new Item(new GUIContent(moreItems > 0 ? "+" + moreItems : "...", "More"));
            item.OnClick += () => ShowMore(components.Skip(Prefs.hierarchyIconsMaxItems), rect);
            items.Add(item);
        }

        internal class Item
        {
            public GUIContent content;
            public Action OnClick;
            public Action OnDrag;
            public Action OnRightClick;

            public Item(GUIContent content)
            {
                this.content = content;
            }

            public float Draw(Rect rect)
            {
                var useButton = !string.IsNullOrEmpty(content.text);
                rect.xMin -= useButton ? Styles.hierarchyIcon.CalcSize(content).x + 8 : 18;
                GUI.Box(rect, content, Styles.hierarchyIcon);
                var e = Event.current;
                if (!rect.Contains(e.mousePosition)) return rect.x;
                if (e.type == EventType.MouseUp)
                {
                    if (e.button == 0)
                    {
                        if (OnClick != null) OnClick();
                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        if (OnRightClick != null) OnRightClick();
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDrag)
                {
                    if (OnDrag != null) OnDrag();
                    e.Use();
                }

                return rect.x;
            }
        }
    }
}