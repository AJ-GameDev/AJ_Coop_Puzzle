/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.EditorMenus.Layouts;
using InfinityCode.UltimateEditorEnhancer.SceneTools;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using PopupWindow = InfinityCode.UltimateEditorEnhancer.Windows.PopupWindow;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus
{
    [InitializeOnLoad]
    public static class EditorMenu
    {
        public static bool allowCloseWindow = true;

        private static List<MainLayoutItem> items;
        private static Vector2 _lastPosition;

        static EditorMenu()
        {
            AssetPreview.SetPreviewTextureCacheSize(32000);

            var binding = KeyManager.AddBinding();
            binding.OnValidate += () =>
            {
                if (!Prefs.contextMenuOnHotKey) return false;
                if (EditorApplication.isPlaying && Prefs.contextMenuDisableInPlayMode &&
                    EditorWindow.focusedWindow.GetType() == GameViewRef.type) return false;
                if (Event.current.keyCode != Prefs.contextMenuHotKey) return false;
                if (Event.current.modifiers != Prefs.contextMenuHotKeyModifiers) return false;
                return true;
            };

            binding.OnPress += () =>
            {
                if (!Prefs.contextMenuOnHotKey) return;

                if (EditorWindow.focusedWindow != null)
                {
                    var rect = EditorWindow.focusedWindow.position;
                    rect.position += Event.current.mousePosition;
                    Show(rect.position);
                }
                else
                {
                    Show(Event.current.mousePosition);
                }
            };

            EventManager.AddBinding(EventManager.ClosePopupEvent).OnInvoke += b => Close();

            EditorApplication.quitting += CloseAll;
            EditorApplication.update += OnEditorApplicationUpdate;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            CloseAllFloatingWindows();
        }

        public static EditorWindow lastWindow { get; private set; }

        public static Vector3 lastWorldPosition { get; private set; }

        public static bool isOpened { get; private set; }

        private static void CloseAllFloatingWindows()
        {
            try
            {
                var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PopupWindow>();
                foreach (var wnd in windows)
                {
                    if (wnd == null) continue;
                    if (wnd is AutoSizePopupWindow pw && !pw.closeOnLossFocus && !pw.closeOnCompileOrPlay) continue;
                    try
                    {
                        wnd.Close();
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Log.Add(e);
            }

            ObjectToolbar.CloseActiveWindow();
        }

        private static void CheckOpened()
        {
            if (!isOpened || items == null) return;

            var lastOpened = isOpened;

            isOpened = false;
            foreach (var item in items)
                if (EditorWindow.focusedWindow == item.window)
                {
                    isOpened = true;
                    return;
                }

            if (lastOpened && !isOpened) EventManager.BroadcastClosePopup();
        }

        public static void Close()
        {
            isOpened = false;
            lastWindow = null;
            if (items != null)
                foreach (var wnd in items)
                    wnd.Close();
        }

        private static void CloseAll()
        {
            Close();
            CloseAllFloatingWindows();
            EventManager.BroadcastClosePopup();
        }

        private static void GetWindows()
        {
            if (items != null) return;

            items = new List<MainLayoutItem>();
            var types = typeof(EditorMenu).Assembly.GetTypes();
            foreach (var type in types)
                if (!type.IsAbstract && type.IsSubclassOf(typeof(MainLayoutItem)))
                    items.Add(Activator.CreateInstance(type, true) as MainLayoutItem);

            items = items.OrderBy(w => w.order).ToList();
        }

        private static void OnCompilationStarted(object obj)
        {
            CloseAll();
        }

        private static void OnEditorApplicationUpdate()
        {
            if (EditorApplication.isCompiling) CloseAll();
            CheckOpened();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) allowCloseWindow = false;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                allowCloseWindow = true;
                EditorApplication.delayCall += CloseAllFloatingWindows;
            }
        }

        private static void Prepare(Vector2 position)
        {
            var offset = Vector2.zero;
            var flipHorizontal = false;
            var flipVertical = false;
            var targets = Selection.gameObjects;

            foreach (var item in items)
                try
                {
                    item.Prepare(targets, position, ref offset, ref flipHorizontal, ref flipVertical);
                }
                catch (Exception e)
                {
                    Log.Add(e);
                }

            foreach (var item in items)
                try
                {
                    if (item.isActive) item.SetPosition(position, offset, flipHorizontal, flipVertical);
                }
                catch (Exception e)
                {
                    Log.Add(e);
                }
        }

        public static void Show(Vector2 position)
        {
#if !UNITY_2021_1_OR_NEWER || UNITY_2021_2_OR_NEWER
            var focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null) position -= focusedWindow.position.position;
#endif
            _lastPosition = position = GUIUtility.GUIToScreenPoint(position);
            lastWorldPosition = SceneViewManager.lastWorldPosition;

            GetWindows();
            Prepare(position);
            Show();
        }

        private static void Show()
        {
            EventManager.BroadcastClosePopup();

            if (Prefs.contextMenuPauseInPlayMode && EditorApplication.isPlaying) EditorApplication.isPaused = true;

            lastWindow = EditorWindow.focusedWindow;

            foreach (var item in items)
                if (item.isActive)
                    item.Show();

            isOpened = true;
        }

        public static void ShowInLastPosition()
        {
            EventManager.BroadcastClosePopup();

            GetWindows();
            Prepare(_lastPosition);
            Show();
        }
    }
}