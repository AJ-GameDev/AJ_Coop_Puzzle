﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public class PinAndClose : PopupWindow
    {
        public const int HEIGHT = 20;

        private static GUIContent _closeContent;
        private static GUIContent _tabContent;
        public bool closeOnLossFocus = true;

        private bool isDragging;
        private GUIContent labelContent;
        private Vector2 lastMousePosition;
        private Action OnClose;

        private Action OnPin;
        private Rect targetRect;
        private bool waitDragAndDropEnds;
        private bool waitRestoreAfterPicker;

        public static GUIContent closeContent
        {
            get
            {
                if (_closeContent == null) _closeContent = new GUIContent(Icons.closeWhite, "Close");
                return _closeContent;
            }
        }

        public static GUIContent tabContent
        {
            get
            {
                if (_tabContent == null) _tabContent = new GUIContent(Icons.pin, "To Tab Window");
                return _tabContent;
            }
        }

        public EditorWindow targetWindow { get; private set; }

        protected void OnDestroy()
        {
            OnPin = null;
            OnClose = null;

            if (targetWindow != null) targetWindow.Close();
            targetWindow = null;
        }

        protected override void OnGUI()
        {
            try
            {
                if (targetWindow == null)
                {
                    Close();
                    return;
                }

                if (closeOnLossFocus && focusedWindow != this && focusedWindow != targetWindow)
                {
                    if (waitRestoreAfterPicker &&
                        (focusedWindow == null || focusedWindow.GetType() != ObjectSelectorRef.type))
                    {
                        targetWindow.Focus();
                        waitRestoreAfterPicker = false;
                    }
                    else
                    {
                        var w = targetWindow as AutoSizePopupWindow;
                        if (w != null && !w.closeOnLossFocus)
                        {
                        }
                        else if (TryToClose())
                        {
                            return;
                        }
                    }
                }

                if (!isDragging && targetRect != targetWindow.position &&
                    targetWindow.position.position != Vector2.zero) SetRect(targetWindow.position);

                base.OnGUI();

                EditorGUILayout.BeginHorizontal();

                DrawLabel();

                if (OnPin != null && GUILayout.Button(tabContent, Styles.transparentButton, GUILayout.Width(12),
                        GUILayout.Height(12))) InvokePin();
                if (GUILayout.Button(closeContent, Styles.transparentButton, GUILayout.Width(12), GUILayout.Height(12)))
                    InvokeClose();

                EditorGUILayout.EndHorizontal();
            }
            catch (ExitGUIException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                Log.Add(e);
            }

            Repaint();
        }

        private void DrawLabel()
        {
            if (labelContent != null)
            {
                var maxWidth = position.width - 35;
                if (OnPin != null) maxWidth -= 20;

                GUILayout.Label(labelContent, EditorStyles.whiteLabel, GUILayout.MaxWidth(maxWidth));
            }

            EditorGUILayout.Space();

            ProcessLabelEvents(GUILayoutUtility.GetLastRect());
        }

        private bool InvokeClose()
        {
            if (OnClose != null) OnClose();
            Close();
            return true;
        }

        private void InvokePin()
        {
            OnPin();
            Close();
        }

        private void OnTargetRectChanged(Rect rect)
        {
            SetRect(rect);
        }

        private void ProcessLabelEvents(Rect labelRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                var r = new Rect(0, 0, labelRect.xMax, 20);
                if (e.button == 0 && r.Contains(e.mousePosition) && GUIUtility.hotControl == 0)
                {
                    isDragging = true;
                    targetWindow.Focus();
                    Focus();
                    lastMousePosition = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    e.Use();
                    GUIUtility.ExitGUI();
                }
            }
            else if (e.rawType == EventType.MouseUp)
            {
                if (isDragging && e.button == 0)
                {
                    isDragging = false;

                    var cw = targetWindow as AutoSizePopupWindow;
                    if (cw != null) cw.wasMoved = true;

                    e.Use();
                    GUIUtility.hotControl = 0;
                    GUIUtility.ExitGUI();
                }
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (isDragging)
                {
                    var mousePosition = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    var delta = mousePosition - lastMousePosition;

                    var rect = position;
                    rect.position += delta;
                    position = rect;

                    rect = targetWindow.position;
                    rect.position += delta;
                    targetWindow.position = rect;

                    lastMousePosition = mousePosition;

                    e.Use();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void SetRect(Rect rect)
        {
            targetRect = rect;
            var size = new Vector2(rect.width, HEIGHT);
            var pos = rect.position - new Vector2(0, size.y);
            position = new Rect(pos, size);
        }

        public static PinAndClose Show(EditorWindow window, Rect inspectorRect, Action OnClose, string label)
        {
            return Show(window, inspectorRect, OnClose, null, label);
        }

        public static PinAndClose Show(EditorWindow window, Rect inspectorRect, Action OnClose, Action OnLock = null,
            string label = null)
        {
            var wnd = CreateInstance<PinAndClose>();
            wnd.minSize = new Vector2(10, 10);
            wnd.targetWindow = window;
            wnd.OnClose = OnClose;
            wnd.OnPin = OnLock;
            wnd.SetRect(inspectorRect);
            if (!string.IsNullOrEmpty(label)) wnd.labelContent = new GUIContent(label);
            wnd.ShowPopup();
            wnd.Focus();
            window.Focus();

            var cw = window as ComponentWindow;
            if (cw != null) cw.OnPositionChanged += wnd.OnTargetRectChanged;

            return wnd;
        }

        private bool TryToClose()
        {
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                waitDragAndDropEnds = true;
                return false;
            }

            if (waitDragAndDropEnds)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    waitDragAndDropEnds = false;
                    targetWindow.Focus();
                }

                return false;
            }

            if (focusedWindow != null && focusedWindow.GetType() == ObjectSelectorRef.type)
            {
                waitRestoreAfterPicker = true;
                return false;
            }

            return InvokeClose();
        }

        public void UpdatePosition(Rect rect)
        {
            var r = position;
            var size = r.size;
            var pos = rect.position + new Vector2(rect.width, 0) - size;
            r.position = pos;
            r.size = size;
            position = r;
        }
    }
}