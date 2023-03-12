﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    [InitializeOnLoad]
    public static class LogManager
    {
        private const double FREQUENCY = 1;
        private const int ERROR_MODE = 16640;
        private const int ERROR_MODE2 = 8405248;
        private const int EXCEPTION_MODE = 4325632;
        private const int EXCEPTION_MODE2 = 12714240;

        private static readonly Dictionary<int, List<Entry>> entries;
        private static double lastUpdatedTime;
        private static bool isDirty;
        private static int lastCount;

        static LogManager()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            entries = new Dictionary<int, List<Entry>>();

            UpdateEntries();
        }

        public static List<Entry> GetEntries(int id)
        {
            List<Entry> localEntries;
            if (entries.TryGetValue(id, out localEntries)) return localEntries;
            return null;
        }

        private static void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            isDirty = true;
        }

        private static void OnUpdate()
        {
            if (!Prefs.hierarchyErrorIcons) return;

            if (EditorApplication.timeSinceStartup - lastUpdatedTime > FREQUENCY)
                if (!isDirty)
                {
                    var currentCount = LogEntriesRef.GetCount();
                    if (lastCount > currentCount) isDirty = true;
                    lastCount = currentCount;
                }

            if (isDirty) UpdateEntries();
        }

        private static void UpdateEntries()
        {
            entries.Clear();

            try
            {
                var count = LogEntriesRef.StartGettingEntries();
                var nativeEntry = Activator.CreateInstance(LogEntryRef.type);

                var maxRecords = Mathf.Min(count, 999);

                for (var i = 0; i < maxRecords; i++)
                {
                    LogEntriesRef.GetEntryInternal(i, nativeEntry);
                    var mode = LogEntryRef.GetMode(nativeEntry);
                    if (mode != ERROR_MODE && mode != ERROR_MODE2 &&
                        mode != EXCEPTION_MODE && mode != EXCEPTION_MODE2) continue;


                    var instanceID = LogEntryRef.GetInstanceID(nativeEntry);
                    if (instanceID == 0) continue;

                    var entry = new Entry(nativeEntry, i);
                    var reference = EditorUtility.InstanceIDToObject(instanceID);
                    if (reference == null) continue;

                    var target = reference as GameObject;

                    if (target == null)
                    {
                        var component = reference as Component;
                        if (component == null) continue;
                        target = component.gameObject;
                    }

                    List<Entry> localEntries;
                    var id = target.GetInstanceID();
                    if (entries.TryGetValue(id, out localEntries)) localEntries.Add(entry);
                    else entries.Add(id, new List<Entry> { entry });
                }

                EditorApplication.RepaintHierarchyWindow();
            }
            catch (Exception ex)
            {
                Log.Add(ex);
            }

            LogEntriesRef.EndGettingEntries();
            lastUpdatedTime = EditorApplication.timeSinceStartup;
            isDirty = false;
        }

        public class Entry
        {
            private readonly int index;
            public string message;

            public Entry(object nativeEntry, int index)
            {
                this.index = index;
                message = LogEntryRef.GetMessage(nativeEntry);
            }

            public void Open()
            {
                LogEntriesRef.RowGotDoubleClicked(index);
            }
        }
    }
}