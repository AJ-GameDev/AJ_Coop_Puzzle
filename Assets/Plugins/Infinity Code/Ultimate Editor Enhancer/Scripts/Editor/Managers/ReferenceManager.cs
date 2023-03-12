/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.IO;
using InfinityCode.UltimateEditorEnhancer.HierarchyTools;
using InfinityCode.UltimateEditorEnhancer.SceneTools;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    [Serializable]
    public class ReferenceManager : ScriptableObject
    {
        private static ReferenceManager _instance;

        [SerializeField] private List<ProjectBookmark> _bookmarks = new();

        [SerializeField] private List<FavoriteWindowItem> _favoriteWindows = new();

        [SerializeField] private List<HeaderRule> _headerRules = new();

        [SerializeField] private List<QuickAccessItem> _quickAccessItems = new();

        [SerializeField] private List<SceneHistoryItem> _sceneHistory = new();

        public static ReferenceManager instance
        {
            get
            {
                if (_instance == null) Load();
                return _instance;
            }
        }

        public static List<ProjectBookmark> bookmarks => instance._bookmarks;

        public static List<FavoriteWindowItem> favoriteWindows => instance._favoriteWindows;

        public static List<HeaderRule> headerRules => instance._headerRules;

        public static List<QuickAccessItem> quickAccessItems => instance._quickAccessItems;

        public static List<SceneHistoryItem> sceneHistory
        {
            get => instance._sceneHistory;
            set => instance._sceneHistory = value;
        }

        private static void Load()
        {
            var path = Resources.settingsFolder + "References.asset";
            try
            {
                if (File.Exists(path))
                    try
                    {
                        _instance = AssetDatabase.LoadAssetAtPath<ReferenceManager>(path);
                    }
                    catch (Exception e)
                    {
                        Log.Add(e);
                    }

                if (_instance == null)
                {
                    _instance = CreateInstance<ReferenceManager>();

#if !UEE_IGNORE_SETTINGS
                    var info = new FileInfo(path);
                    if (!info.Directory.Exists) info.Directory.Create();

                    AssetDatabase.CreateAsset(_instance, path);
                    AssetDatabase.SaveAssets();
#endif
                }
            }
            catch (Exception e)
            {
                Log.Add(e);
            }
        }

        public static void ResetContent()
        {
            bookmarks.Clear();
            favoriteWindows.Clear();
            headerRules.Clear();
            quickAccessItems.Clear();
            Save();
        }

        public static void Save()
        {
            try
            {
                EditorUtility.SetDirty(_instance);
            }
            catch
            {
            }
        }
    }
}