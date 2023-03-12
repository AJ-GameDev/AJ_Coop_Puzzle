/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public static bool jumpToPoint = true;
        public static bool highJumpToPoint = true;
        public static bool alternativeJumpShortcut;

        public class JumpToPointManager : StandalonePrefManager<JumpToPointManager>, IHasShortcutPref
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return new[]
                    {
                        "Jump To Point", "High"
                    };
                }
            }

            public override float order => -34;

            public IEnumerable<Shortcut> GetShortcuts()
            {
                var shortcuts = new List<Shortcut>();

                if (jumpToPoint)
                {
                    shortcuts.Add(new Shortcut("Jump To Point", "Scene View", "SHIFT + MMB"));
                    if (alternativeJumpShortcut)
                        shortcuts.Add(new Shortcut("Jump To Point", "Scene View", "SHIFT + SHIFT"));
                }

                if (highJumpToPoint)
                {
#if UNITY_EDITOR_OSX
                    var shortcut = "CMD + SHIFT + MMB";
                    var shortcut2 = "CMD + SHIFT + SHIFT";
#else
                    string shortcut = "CTRL + SHIFT + MMB";
                    string shortcut2 = "CTRL + SHIFT + SHIFT";
#endif
                    shortcuts.Add(new Shortcut("High Jump To Point", "Scene View", shortcut));
                    if (alternativeJumpShortcut)
                        shortcuts.Add(new Shortcut("High Jump To Point", "Scene View", shortcut2));
                }

                return shortcuts;
            }

            public override void Draw()
            {
                jumpToPoint = EditorGUILayout.ToggleLeft("Jump To Point", jumpToPoint);
                highJumpToPoint = EditorGUILayout.ToggleLeft("High Jump To Point", highJumpToPoint);

#if UNITY_EDITOR_OSX
                var alternativeLabel = "Alternative Jump Shortcuts (SHIFT + SHIFT, CMD + SHIFT + SHIFT)";
#else
                string alternativeLabel = "Alternative Jump Shortcuts (SHIFT + SHIFT, CTRL + SHIFT + SHIFT)";
#endif
                alternativeJumpShortcut = EditorGUILayout.ToggleLeft(alternativeLabel, alternativeJumpShortcut);
            }
        }
    }
}