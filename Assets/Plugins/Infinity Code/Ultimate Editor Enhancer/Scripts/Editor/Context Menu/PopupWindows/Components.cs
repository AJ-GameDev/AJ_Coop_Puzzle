/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Linq;
using InfinityCode.UltimateEditorEnhancer.Attributes;
using InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.PopupWindows
{
    [RequireSelected]
    public class Components : PopupWindowItem, IValidatableLayoutItem
    {
        private GUIContent addContent;
        private Component[] components;
        private GUIContent[] contents;
        private GUIContent labelContent;
        private Vector2 labelSize;

        public override float order => 100;

        public bool Validate()
        {
            if (!Prefs.componentsInPopupWindow) return false;
            if (targets == null || targets.Length != 1) return false;

            return true;
        }

        protected override void CalcSize()
        {
            labelSize = EditorStyles.whiteLabel.CalcSize(labelContent);
            _size = labelSize;
            _size.y += GUI.skin.label.margin.bottom;
            _size.x += EditorStyles.whiteLabel.CalcSize(new GUIContent("+")).x;

            var style = Styles.buttonWithToggleAlignLeft;
            var marginBottom = style.margin.bottom;

            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;

                var s = style.CalcSize(contents[i]);
                if (contents[i].image != null) s.x -= contents[i].image.width - 20;
                _size.x = Mathf.Max(_size.x, s.x);
                _size.y += s.y + marginBottom;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            components = null;
            contents = null;
            labelContent = null;
        }

        public override void Draw()
        {
            var e = Event.current;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Components:", EditorStyles.whiteLabel, GUILayout.Width(labelSize.x));

            if (Prefs.componentsInPopupWindowAddComponent &&
                GUILayout.Button(addContent, EditorStyles.whiteLabel, GUILayout.ExpandWidth(false)))
            {
                Vector2 s = Prefs.defaultWindowSize;
                var rect = new Rect(GUIUtility.GUIToScreenPoint(e.mousePosition) - s / 2, s);

                EditorMenu.Close();
                AddComponent.ShowAddComponent(rect);
            }

            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null) continue;

                var content = contents[i];

                var buttonEvent = DrawComponent(component, content);

                if (buttonEvent == ButtonEvent.drag)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { component };
                    DragAndDrop.StartDrag("Drag " + component.name);
                    e.Use();
                    GUI.changed = true;
                }
                else if (buttonEvent == ButtonEvent.click)
                {
                    if (e.button == 0)
                    {
                        if (Prefs.popupWindowTab && Prefs.popupWindowTabModifiers == e.modifiers)
                        {
                            ComponentWindow.Show(component);
                        }
                        else if (Prefs.popupWindowUtility && Prefs.popupWindowUtilityModifiers == e.modifiers)
                        {
                            ComponentWindow.ShowUtility(component);
                        }
                        else if (Prefs.popupWindowPopup && Prefs.popupWindowPopupModifiers == e.modifiers)
                        {
                            EditorWindow wnd = ComponentWindow.ShowPopup(component);
                            EventManager.AddBinding(EventManager.ClosePopupEvent).OnInvoke += b =>
                            {
                                wnd.Close();
                                b.Remove();
                            };
                        }

                        EditorMenu.Close();
                    }
                    else if (e.button == 1)
                    {
                        ComponentUtils.ShowContextMenu(component);
                        SceneViewManager.OnNextGUI += WaitCloseContextMenu;
                    }

                    e.Use();
                }
            }
        }

        public static ButtonEvent DrawComponent(Component component, GUIContent content)
        {
            var e = Event.current;

            var rect = GUILayoutUtility.GetRect(content, Styles.buttonWithToggleAlignLeft);
            var toggleRect = new Rect(rect.x + 4, rect.y + 2, 16, 16);
            var id = GUIUtility.GetControlID(GUILayoutUtils.buttonHash, FocusType.Passive, rect);
            var isHover = rect.Contains(e.mousePosition) && !toggleRect.Contains(e.mousePosition);
            var hasMouseControl = GUIUtility.hotControl == id;

            var state = ButtonEvent.none;

            if (e.type == EventType.Repaint)
            {
                Styles.buttonWithToggleAlignLeft.Draw(rect, content, isHover, hasMouseControl, false, false);
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (hasMouseControl)
                {
                    GUIUtility.hotControl = 0;
                    state = ButtonEvent.drag;
                }
            }
            else if (e.type == EventType.MouseDown)
            {
                if (isHover && GUIUtility.hotControl == 0)
                {
                    GUIUtility.hotControl = id;
                    e.Use();
                    state = ButtonEvent.press;
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (hasMouseControl)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();

                    if (isHover)
                    {
                        GUI.changed = true;
                        state = ButtonEvent.click;
                    }
                }
                else
                {
                    state = ButtonEvent.release;
                }
            }

            if (component.hideFlags == HideFlags.HideAndDontSave || component.hideFlags == HideFlags.HideInInspector)
            {
                if (GUI.Button(toggleRect, EditorIconContents.sceneVisibilityHiddenHover, Styles.transparentButton))
                    component.hideFlags = HideFlags.None;
            }
            else if (ComponentUtils.CanBeDisabled(component))
            {
                EditorGUI.BeginChangeCheck();
                var value = GUI.Toggle(toggleRect, ComponentUtils.GetEnabled(component), GUIContent.none);
                if (EditorGUI.EndChangeCheck()) ComponentUtils.SetEnabled(component, value);
            }

            return state;
        }

        protected override void Init()
        {
            components = targets[0].GetComponents<Component>();
            contents = new GUIContent[components.Length];
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null) continue;

                var assetPreview = AssetPreview.GetMiniThumbnail(component);
                var content = new GUIContent(assetPreview);

                var type = component.GetType();
                var acm = type.GetCustomAttributes(typeof(AddComponentMenu), true);
                if (acm.Length > 0)
                {
                    var componentMenu = (acm[0] as AddComponentMenu).componentMenu;
                    if (!string.IsNullOrEmpty(componentMenu)) content.text = componentMenu.Split('/').Last();
                    else content.text = ObjectNames.NicifyVariableName(type.Name);
                }
                else
                {
                    content.text = ObjectNames.NicifyVariableName(type.Name);
                }

                contents[i] = content;
            }

            labelContent = new GUIContent("Components:");
            addContent = new GUIContent("+", "Add Component");
        }

        private void WaitCloseContextMenu()
        {
            EditorMenu.Close();
        }
    }
}