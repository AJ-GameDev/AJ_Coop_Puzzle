/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Text.RegularExpressions;

namespace InfinityCode.UltimateEditorEnhancer
{
    public abstract class SearchableItem
    {
        private const int upFactor = -3;

        protected float _accuracy;

        public float accuracy
        {
            get => _accuracy;
            set => _accuracy = value;
        }

        protected static bool Contains(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2) || str2.Length > str1.Length) return false;

            int i, j;

            var textInfo = Culture.textInfo;

            var l2 = str2.Length;

            for (i = 0; i < str1.Length - l2 + 1; i++)
            {
                for (j = 0; j < l2; j++)
                {
                    var c1 = textInfo.ToUpper(str1[i + j]);
                    var c2 = textInfo.ToUpper(str2[j]);
                    if (c1 != c2) break;
                }

                if (j == l2) return true;
            }

            return false;
        }

        public static float GetAccuracy(string pattern, params string[] values)
        {
            if (values == null || values.Length == 0) return 0;

            if (string.IsNullOrEmpty(pattern)) return 1;

            float accuracy = 0;

            for (var i = 0; i < values.Length; i++)
            {
                var s = values[i];

                var r = Match(s, pattern);
                if (r == int.MinValue) continue;

                var v = 1 - r / (float)s.Length;
                if (r == pattern.Length * upFactor)
                {
                    accuracy = v;
                    return accuracy;
                }

                if (accuracy < v) accuracy = v;
            }

            return accuracy;
        }

        public static string GetPattern(string str)
        {
            var search = str;

            var textInfo = Culture.textInfo;
            StaticStringBuilder.Clear();

            var lastWhite = false;

            for (var i = 0; i < search.Length; i++)
            {
                var c = search[i];
                if (c == ' ' || c == '\t' || c == '\n')
                {
                    if (!lastWhite && StaticStringBuilder.Length > 0)
                    {
                        StaticStringBuilder.Append(' ');
                        lastWhite = true;
                    }
                }
                else
                {
                    StaticStringBuilder.Append(textInfo.ToUpper(c));
                    lastWhite = false;
                }
            }

            if (lastWhite) StaticStringBuilder.Length -= 1;

            return StaticStringBuilder.GetString();
        }

        public static string GetPattern(string str, out string assetType)
        {
            assetType = string.Empty;
            var search = str;

            var textInfo = Culture.textInfo;

            var match = Regex.Match(search, @"(:)(\w*)");
            if (match.Success)
            {
                assetType = textInfo.ToUpper(match.Groups[2].Value);
                if (assetType == "PREFAB") assetType = "GAMEOBJECT";
                search = Regex.Replace(search, @"(:)(\w*)", "");
            }

            StaticStringBuilder.Clear();

            var lastWhite = false;

            for (var i = 0; i < search.Length; i++)
            {
                var c = search[i];
                if (c == ' ' || c == '\t' || c == '\n')
                {
                    if (!lastWhite && StaticStringBuilder.Length > 0)
                    {
                        StaticStringBuilder.Append(' ');
                        lastWhite = true;
                    }
                }
                else
                {
                    StaticStringBuilder.Append(textInfo.ToUpper(c));
                    lastWhite = false;
                }
            }

            if (lastWhite) StaticStringBuilder.Length -= 1;

            return StaticStringBuilder.GetString();
        }

        protected abstract int GetSearchCount();

        protected abstract string GetSearchString(int index);

        protected static int Match(string str1, string str2)
        {
            var bestExtra = int.MaxValue;

            var l1 = str1.Length;
            var l2 = str2.Length;

            var textInfo = Culture.textInfo;

            for (var i = 0; i < l1 - l2 + 1; i++)
            {
                var success = true;
                var iOffset = 0;
                var extra = 0;
                var prevNoSkip = true;

                for (var j = 0; j < l2; j++)
                {
                    var c = str2[j];

                    var j2 = i + j;
                    var i1 = j2 + iOffset;
                    if (i1 >= l1)
                    {
                        success = false;
                        break;
                    }

                    var c1 = str1[i1];

                    if (c1 >= 'A' && c1 <= 'Z')
                    {
                        if (prevNoSkip) extra += upFactor;
                    }
                    else if (c1 >= 'a' && c1 <= 'z')
                    {
                        c1 = (char)(c1 - 32);
                    }
                    else
                    {
                        var c2 = textInfo.ToUpper(c1);
                        if (c1 == c2 && prevNoSkip) extra += upFactor;
                        c1 = c2;
                    }

                    if (c == c1)
                    {
                        prevNoSkip = true;
                        continue;
                    }

                    if (j == 0)
                    {
                        success = false;
                        break;
                    }

                    var successSkip = false;
                    iOffset++;
                    var cOffset = 0;

                    while (j2 + iOffset < l1)
                    {
                        var oc = str1[j2 + iOffset];
                        var uc = textInfo.ToUpper(oc);
                        cOffset++;
                        if (uc != c)
                        {
                            iOffset++;
                            continue;
                        }

                        if (oc == uc) extra += upFactor;
                        else extra += cOffset;

                        successSkip = true;
                        break;
                    }

                    if (!successSkip)
                    {
                        success = false;
                        break;
                    }

                    prevNoSkip = false;
                }

                if (success)
                {
                    if (extra == l2 * upFactor) return extra;
                    bestExtra = Math.Min(extra, bestExtra);
                }
            }

            return bestExtra != int.MaxValue ? bestExtra : int.MinValue;
        }

        public virtual float UpdateAccuracy(string pattern)
        {
            _accuracy = 0;

            if (string.IsNullOrEmpty(pattern))
            {
                _accuracy = 1;
                return 1;
            }

            for (var i = 0; i < GetSearchCount(); i++)
            {
                var s = GetSearchString(i);

                var r = Match(s, pattern);
                if (r == int.MinValue) continue;

                var v = 1 - r / (float)s.Length;
                if (r == pattern.Length * upFactor)
                {
                    _accuracy = v;
                    return _accuracy;
                }

                if (_accuracy < v) _accuracy = v;
            }

            return _accuracy;
        }
    }
}