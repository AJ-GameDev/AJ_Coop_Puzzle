/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Behaviors
{
    [InitializeOnLoad]
    public static class SelectionHistory
    {
        private const int MAX_RECORDS = 30;

        private static bool ignoreNextAdd;

        static SelectionHistory()
        {
            Selection.selectionChanged += SelectionChanged;

            var prevBinding = KeyManager.AddBinding();
            prevBinding.OnValidate += ValidatePrev;
            prevBinding.OnPress += Prev;

            var nextBinding = KeyManager.AddBinding();
            nextBinding.OnValidate += ValidateNext;
            nextBinding.OnPress += Next;

            records = new List<SelectionRecord>();
            if (Selection.instanceIDs.Length > 0) Add(Selection.instanceIDs);
        }

        public static List<SelectionRecord> records { get; }

        public static int activeIndex { get; private set; } = -1;

        public static void Add(params int[] ids)
        {
            if (ignoreNextAdd)
            {
                ignoreNextAdd = false;
                return;
            }

            while (records.Count > activeIndex + 1) records.RemoveAt(records.Count - 1);

            while (records.Count > MAX_RECORDS - 1) records.RemoveAt(records.Count - 1);

            var r = new SelectionRecord();
            r.ids = ids;
            r.names = ids.Select(id =>
            {
                var obj = EditorUtility.InstanceIDToObject(id);
                return obj != null ? obj.name : null;
            }).ToArray();
            records.Add(r);

            activeIndex = records.Count - 1;
        }

        public static void Clear()
        {
            records.Clear();
        }

        public static void Next()
        {
            if (records.Count == 0 || activeIndex >= records.Count - 1) return;

            activeIndex++;
            ignoreNextAdd = true;

            Selection.instanceIDs = records[activeIndex].ids;

            Event.current.Use();
        }

        public static void Prev()
        {
            if (records.Count == 0 || activeIndex <= 0) return;

            activeIndex--;
            ignoreNextAdd = true;

            Selection.instanceIDs = records[activeIndex].ids;

            Event.current.Use();
        }

        private static void SelectionChanged()
        {
            if (Prefs.selectionHistory) Add(Selection.instanceIDs);
        }

        public static void SetIndex(int newIndex)
        {
            if (newIndex < 0 || newIndex >= records.Count) return;

            ignoreNextAdd = true;

            activeIndex = newIndex;
            Selection.instanceIDs = records[activeIndex].ids;
        }

        private static bool ValidateNext()
        {
            if (!Prefs.selectionHistory) return false;
            if (Event.current.modifiers != Prefs.selectionHistoryModifiers) return false;
            return Event.current.keyCode == Prefs.selectionHistoryNextKeyCode;
        }

        private static bool ValidatePrev()
        {
            if (!Prefs.selectionHistory) return false;
            if (Event.current.modifiers != Prefs.selectionHistoryModifiers) return false;
            return Event.current.keyCode == Prefs.selectionHistoryPrevKeyCode;
        }

        public class SelectionRecord
        {
            public int[] ids;
            public string[] names;

            public string GetShortNames()
            {
                if (names == null || names.Length == 0) return string.Empty;
                if (names.Length == 1) return names[0];
                if (names.Length == 2) return names[0] + " + " + names[1];
                return names[0] + " + (" + (names.Length - 1) + " GameObjects)";
            }
        }
    }
}