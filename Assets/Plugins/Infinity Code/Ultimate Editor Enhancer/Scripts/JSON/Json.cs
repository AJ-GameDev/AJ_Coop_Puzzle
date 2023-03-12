/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.JSON
{
    /// <summary>
    ///     Class for working with JSON. It is used for parsing of string, serialization and deserialization of object.
    /// </summary>
    public class Json
    {
        private int index;
        private readonly string json;
        private readonly int length;
        private Token lookAheadToken = Token.None;
        private readonly StringBuilder s;

        protected Json(string json)
        {
            s = new StringBuilder();
            this.json = json;
            length = json.Length;
        }

        /// <summary>
        ///     Deserialize string into object.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>Deserialized object</returns>
        public static T Deserialize<T>(string json)
        {
            var obj = ParseDirect(json);
            if (obj is IDictionary) return (T)DeserializeObject(typeof(T), obj as Dictionary<string, object>);
            if (obj is IList) return (T)DeserializeArray(typeof(T), obj as List<object>);
            return (T)DeserializeValue(typeof(T), obj);
        }

        private static object DeserializeValue(Type type, object obj)
        {
            if (obj == null) return null;
            try
            {
                return Convert.ChangeType(obj, type);
            }
            catch (Exception exception)
            {
                Debug.Log(exception.Message + "\n" + exception.StackTrace);
            }

            return null;
        }

        private static object DeserializeArray(Type type, List<object> list)
        {
            if (list == null || list.Count == 0) return null;
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var v = Array.CreateInstance(elementType, list.Count);
                for (var i = 0; i < list.Count; i++)
                {
                    var child = list[i];
                    object item;
                    if (child is IDictionary)
                        item = DeserializeObject(elementType, child as Dictionary<string, object>);
                    else if (child is IList) item = DeserializeArray(elementType, child as List<object>);
                    else item = DeserializeValue(elementType, child);
                    v.SetValue(item, i);
                }

                return v;
            }

            if (Reflection.IsGenericType(type))
            {
                var listType = Reflection.GetGenericArguments(type)[0];
                var v = Activator.CreateInstance(type);

                for (var i = 0; i < list.Count; i++)
                {
                    var child = list[i];
                    object item;
                    if (child is IDictionary) item = DeserializeObject(listType, child as Dictionary<string, object>);
                    else if (child is IList) item = DeserializeArray(listType, child as List<object>);
                    else item = DeserializeValue(listType, child);
                    try
                    {
                        var methodInfo = Reflection.GetMethod(type, "Add");
                        if (methodInfo != null) methodInfo.Invoke(v, new[] { item });
                    }
                    catch
                    {
                    }
                }

                return v;
            }


            return null;
        }

        private static object DeserializeObject(Type type, Dictionary<string, object> table)
        {
            var members = Reflection.GetMembers(type, BindingFlags.Instance | BindingFlags.Public);

            var v = Activator.CreateInstance(type);

            foreach (var member in members)
            {
#if !NETFX_CORE
                var memberType = member.MemberType;
                if (memberType != MemberTypes.Field && memberType != MemberTypes.Property) continue;
#else
                MemberTypes memberType;
                if (member is PropertyInfo) memberType = MemberTypes.Property;
                else if (member is FieldInfo) memberType = MemberTypes.Field;
                else continue;
#endif

                if (memberType == MemberTypes.Property && !((PropertyInfo)member).CanWrite) continue;
                object item;

#if !NETFX_CORE
                var attributes = member.GetCustomAttributes(typeof(AliasAttribute), true);
                var alias = attributes.Length > 0 ? attributes[0] as AliasAttribute : null;
#else
                IEnumerable<Attribute> attributes = member.GetCustomAttributes(typeof(AliasAttribute), true);
                AliasAttribute alias = null;
                foreach (Attribute a in attributes)
                {
                    alias = a as AliasAttribute;
                    break;
                }
#endif
                if (alias == null || !alias.ignoreFieldName)
                    if (table.TryGetValue(member.Name, out item))
                    {
                        DeserializeValue(memberType, member, item, v);
                        continue;
                    }

                if (alias != null)
                    for (var j = 0; j < alias.aliases.Length; j++)
                        if (table.TryGetValue(alias.aliases[j], out item))
                        {
                            DeserializeValue(memberType, member, item, v);
                            break;
                        }
            }

            return v;
        }

        public static void DeserializeTo(string json, Object target)
        {
            if (target == null) return;

            var obj = ParseDirect(json);
            var table = obj as Dictionary<string, object>;
            var type = target.GetType();

            var members = Reflection.GetMembers(type, BindingFlags.Instance | BindingFlags.Public);

            foreach (var member in members)
            {
#if !NETFX_CORE
                var memberType = member.MemberType;
                if (memberType != MemberTypes.Field && memberType != MemberTypes.Property) continue;
#else
                MemberTypes memberType;
                if (member is PropertyInfo) memberType = MemberTypes.Property;
                else if (member is FieldInfo) memberType = MemberTypes.Field;
                else continue;
#endif

                if (memberType == MemberTypes.Property && !((PropertyInfo)member).CanWrite) continue;
                object item;

#if !NETFX_CORE
                var attributes = member.GetCustomAttributes(typeof(AliasAttribute), true);
                var alias = attributes.Length > 0 ? attributes[0] as AliasAttribute : null;
#else
                IEnumerable<Attribute> attributes = member.GetCustomAttributes(typeof(AliasAttribute), true);
                AliasAttribute alias = null;
                foreach (Attribute a in attributes)
                {
                    alias = a as AliasAttribute;
                    break;
                }
#endif
                if (alias == null || !alias.ignoreFieldName)
                    if (table.TryGetValue(member.Name, out item))
                    {
                        DeserializeValue(memberType, member, item, target);
                        continue;
                    }

                if (alias != null)
                    for (var j = 0; j < alias.aliases.Length; j++)
                        if (table.TryGetValue(alias.aliases[j], out item))
                        {
                            DeserializeValue(memberType, member, item, target);
                            break;
                        }
            }
        }

        private static void DeserializeValue(MemberTypes memberType, MemberInfo member, object item, object v)
        {
            object cv;
            var t = memberType == MemberTypes.Field
                ? ((FieldInfo)member).FieldType
                : ((PropertyInfo)member).PropertyType;
            if (t == typeof(object))
            {
                cv = item;
            }
#if UNITY_EDITOR
            else if (t.IsSubclassOf(typeof(Object)))
            {
                var id = (int)(long)item;
                if (id != 0) cv = EditorUtility.InstanceIDToObject(id);
                else cv = null;
            }
#endif
            else if (t.IsEnum)
            {
                cv = Enum.Parse(t, item as string);
            }
            else if (item is IDictionary)
            {
                cv = DeserializeObject(t, item as Dictionary<string, object>);
            }
            else if (item is IList)
            {
                cv = DeserializeArray(t, item as List<object>);
            }
            else
            {
                cv = DeserializeValue(t, item);
            }

            if (memberType == MemberTypes.Field) ((FieldInfo)member).SetValue(v, cv);
            else ((PropertyInfo)member).SetValue(v, cv, null);
        }

        private Token LookAhead()
        {
            if (lookAheadToken != Token.None) return lookAheadToken;
            return lookAheadToken = NextTokenCore();
        }

        private Token NextToken()
        {
            var result = lookAheadToken != Token.None ? lookAheadToken : NextTokenCore();
            lookAheadToken = Token.None;
            return result;
        }

        private Token NextTokenCore()
        {
            char c;

            do
            {
                c = json[index];

                if (c == '/' && json[index + 1] == '/')
                {
                    index += 2;
                    do
                    {
                        c = json[index];
                        if (c == '\r' || c == '\n') break;
                    } while (++index < length);
                }

                if (c > ' ') break;
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;
            } while (++index < length);

            if (index == length) throw new Exception("Reached end of string unexpectedly");

            c = json[index];

            index++;

            switch (c)
            {
                case '{':
                    return Token.Curly_Open;

                case '}':
                    return Token.Curly_Close;

                case '[':
                    return Token.Squared_Open;

                case ']':
                    return Token.Squared_Close;

                case ',':
                    return Token.Comma;

                case '"':
                    return Token.String;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                case '+':
                case '.':
                    return Token.Number;

                case ':':
                    return Token.Colon;

                case 'f':
                    if (length - index >= 4 &&
                        json[index + 0] == 'a' &&
                        json[index + 1] == 'l' &&
                        json[index + 2] == 's' &&
                        json[index + 3] == 'e')
                    {
                        index += 4;
                        return Token.False;
                    }

                    break;

                case 't':
                    if (length - index >= 3 &&
                        json[index + 0] == 'r' &&
                        json[index + 1] == 'u' &&
                        json[index + 2] == 'e')
                    {
                        index += 3;
                        return Token.True;
                    }

                    break;

                case 'n':
                    if (length - index >= 3 &&
                        json[index + 0] == 'u' &&
                        json[index + 1] == 'l' &&
                        json[index + 2] == 'l')
                    {
                        index += 3;
                        return Token.Null;
                    }

                    break;
            }

            throw new Exception("Could not find token at index " + --index);
        }

        /// <summary>
        ///     Parse JSON string into JsonItem
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Root object</returns>
        public static JsonItem Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return new JsonValue(null, JsonValue.ValueType.NULL);

            var instance = new Json(json);
            return instance.ParseValue();
        }

        /// <summary>
        ///     Parse JSON string into Dictonary, List and Object
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Root object</returns>
        public static object ParseDirect(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var instance = new Json(json);
            return instance.ParseValueDirect();
        }

        private JsonArray ParseArray()
        {
            var array = new JsonArray();
            lookAheadToken = Token.None;

            while (true)
                switch (LookAhead())
                {
                    case Token.Comma:
                        lookAheadToken = Token.None;
                        break;

                    case Token.Squared_Close:
                        lookAheadToken = Token.None;
                        return array;

                    default:
                        array.Add(ParseValue());
                        break;
                }
        }

        private List<object> ParseArrayDirect()
        {
            var array = new List<object>();
            lookAheadToken = Token.None;

            while (true)
                switch (LookAhead())
                {
                    case Token.Comma:
                        lookAheadToken = Token.None;
                        break;

                    case Token.Squared_Close:
                        lookAheadToken = Token.None;
                        return array;

                    default:
                        array.Add(ParseValueDirect());
                        break;
                }
        }

        private object ParseNumber()
        {
            lookAheadToken = Token.None;

            index--;

            long n = 0;
            var neg = false;
            long decimalV = 0;
            long exp = 0;
            var negExp = false;

            while (index < length)
            {
                var c = json[index];

                if (c >= '0' && c <= '9')
                {
                    n = n * 10 + (c - '0');
                    decimalV *= 10;
                }
                else if (c == '.')
                {
                    decimalV = 1;
                }
                else if (c == '-')
                {
                    neg = true;
                }
                else if (c == '+')
                {
                    neg = false;
                }
                else if (c == 'e' || c == 'E')
                {
                    if (decimalV == 0) decimalV = 1;
                    index++;
                    exp = 0;
                    while (index < length)
                    {
                        c = json[index];
                        if (c >= '0' && c <= '9') exp = exp * 10 + (c - '0');
                        else if (c == '-') negExp = true;
                        else if (c == '+') negExp = false;
                        else break;
                        index++;
                    }

                    break;
                }
                else
                {
                    break;
                }

                index++;
            }

            if (neg) n = -n;
            if (decimalV != 0)
            {
                var v = n / (double)decimalV;
                if (exp > 0)
                {
                    if (negExp) v /= Math.Pow(10, exp);
                    else v *= Math.Pow(10, exp);
                }

                return v;
            }

            return n;
        }

        private JsonObject ParseObject()
        {
            var obj = new JsonObject();

            lookAheadToken = Token.None;

            while (true)
                switch (LookAhead())
                {
                    case Token.Comma:
                        lookAheadToken = Token.None;
                        break;

                    case Token.Curly_Close:
                        lookAheadToken = Token.None;
                        return obj;

                    default:
                    {
                        var name = ParseString();
                        if (NextToken() != Token.Colon) throw new Exception("Expected colon at index " + index);
                        obj.Add(name, ParseValue());
                    }
                        break;
                }
        }

        private Dictionary<string, object> ParseObjectDirect()
        {
            var obj = new Dictionary<string, object>();

            lookAheadToken = Token.None;

            while (true)
                switch (LookAhead())
                {
                    case Token.Comma:
                        lookAheadToken = Token.None;
                        break;

                    case Token.Curly_Close:
                        lookAheadToken = Token.None;
                        return obj;

                    default:
                    {
                        var name = ParseString();
                        if (NextToken() != Token.Colon) throw new Exception("Expected colon at index " + index);
                        obj.Add(name, ParseValueDirect());
                    }
                        break;
                }
        }

        private uint ParseSingleChar(char c1, uint multipliyer)
        {
            uint p1 = 0;
            if (c1 >= '0' && c1 <= '9') p1 = (uint)(c1 - '0') * multipliyer;
            else if (c1 >= 'A' && c1 <= 'F') p1 = (uint)(c1 - 'A' + 10) * multipliyer;
            else if (c1 >= 'a' && c1 <= 'f') p1 = (uint)(c1 - 'a' + 10) * multipliyer;
            return p1;
        }

        private string ParseString()
        {
            lookAheadToken = Token.None;

            s.Length = 0;

            var runIndex = -1;
            var l = length;
            var p = json;
            {
                while (index < l)
                {
                    var c = p[index++];

                    if (c == '"')
                    {
                        if (runIndex != -1)
                        {
                            if (s.Length == 0) return p.Substring(runIndex, index - runIndex - 1);
                            s.Append(p, runIndex, index - runIndex - 1);
                        }

                        return s.ToString();
                    }

                    if (c != '\\')
                    {
                        if (runIndex == -1) runIndex = index - 1;
                        continue;
                    }

                    if (index == l) break;

                    if (runIndex != -1)
                    {
                        s.Append(p, runIndex, index - runIndex - 1);
                        runIndex = -1;
                    }

                    switch (p[index++])
                    {
                        case '"':
                            s.Append('"');
                            break;

                        case '\\':
                            s.Append('\\');
                            break;

                        case '/':
                            s.Append('/');
                            break;

                        case 'b':
                            s.Append('\b');
                            break;

                        case 'f':
                            s.Append('\f');
                            break;

                        case 'n':
                            s.Append('\n');
                            break;

                        case 'r':
                            s.Append('\r');
                            break;

                        case 't':
                            s.Append('\t');
                            break;

                        case 'u':
                        {
                            var remainingLength = l - index;
                            if (remainingLength < 4) break;

                            var codePoint = ParseUnicode(p[index], p[index + 1], p[index + 2], p[index + 3]);
                            s.Append((char)codePoint);

                            index += 4;
                        }
                            break;
                    }
                }
            }

            throw new Exception("Unexpectedly reached end of string");
        }

        private uint ParseUnicode(char c1, char c2, char c3, char c4)
        {
            var p1 = ParseSingleChar(c1, 0x1000);
            var p2 = ParseSingleChar(c2, 0x100);
            var p3 = ParseSingleChar(c3, 0x10);
            var p4 = ParseSingleChar(c4, 1);

            return p1 + p2 + p3 + p4;
        }

        private JsonItem ParseValue()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    var number = ParseNumber();
                    return new JsonValue(number,
                        number is double ? JsonValue.ValueType.DOUBLE : JsonValue.ValueType.LONG);

                case Token.String:
                    return new JsonValue(ParseString(), JsonValue.ValueType.STRING);

                case Token.Curly_Open:
                    return ParseObject();

                case Token.Squared_Open:
                    return ParseArray();

                case Token.True:
                    lookAheadToken = Token.None;
                    return new JsonValue(true, JsonValue.ValueType.BOOLEAN);

                case Token.False:
                    lookAheadToken = Token.None;
                    return new JsonValue(false, JsonValue.ValueType.BOOLEAN);

                case Token.Null:
                    lookAheadToken = Token.None;
                    return new JsonValue(null, JsonValue.ValueType.NULL);
            }

            throw new Exception("Unrecognized token at index" + index);
        }

        private object ParseValueDirect()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    return ParseNumber();

                case Token.String:
                    return ParseString();

                case Token.Curly_Open:
                    return ParseObjectDirect();

                case Token.Squared_Open:
                    return ParseArrayDirect();

                case Token.True:
                    lookAheadToken = Token.None;
                    return true;

                case Token.False:
                    lookAheadToken = Token.None;
                    return false;

                case Token.Null:
                    lookAheadToken = Token.None;
                    return null;
            }

            throw new Exception("Unrecognized token at index" + index);
        }

        public static JsonItem Serialize(object obj,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            return Serialize(obj, false, bindingFlags);
        }

        /// <summary>
        ///     Serializes an object to JSON.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="includeChildren"></param>
        /// <param name="bindingFlags">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.</param>
        /// <returns>JSON</returns>
        public static JsonItem Serialize(object obj, bool includeChildren,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
#if !UNITY_WP_8_1 || UNITY_EDITOR
            if (obj == null || obj is DBNull) return new JsonValue(obj, JsonValue.ValueType.NULL);
#else
            if (obj == null) return new JsonValue(obj, JsonValue.ValueType.NULL);
#endif
            if (obj is string || obj is bool || obj is int || obj is long || obj is short || obj is float ||
                obj is double) return new JsonValue(obj);
            if (obj.GetType().IsEnum) return new JsonValue(Enum.GetName(obj.GetType(), obj));
            if (obj is Object)
                if (!includeChildren || !(obj is Component || obj is ScriptableObject))
                    return new JsonValue((obj as Object).GetInstanceID());

            if (obj is IDictionary)
            {
                var d = obj as IDictionary;
                var dv = new JsonObject();
                var keys = d.Keys;
                var values = d.Values;
                var keysEnum = keys.GetEnumerator();
                var valuesEnum = values.GetEnumerator();
                while (keysEnum.MoveNext() && valuesEnum.MoveNext())
                {
                    var k = keysEnum.Current;
                    var v = valuesEnum.Current;

                    dv.Add(k as string, Serialize(v, bindingFlags));
                }

                return dv;
            }

            if (obj is IEnumerable)
            {
                var v = (IEnumerable)obj;
                var array = new JsonArray();
                foreach (var item in v) array.Add(Serialize(item, bindingFlags));
                return array;
            }

            var o = new JsonObject();
            var type = obj.GetType();

            if (Reflection.CheckIfAnonymousType(type)) bindingFlags |= BindingFlags.NonPublic;
            var fields = Reflection.GetFields(type, bindingFlags);
            foreach (var field in fields)
            {
                var fieldName = field.Name;
                if (field.Attributes == (FieldAttributes.Private | FieldAttributes.InitOnly))
                {
                    var startIndex = fieldName.IndexOf('<') + 1;
                    var endIndex = fieldName.IndexOf('>', startIndex);
                    if (endIndex != -1 && startIndex != -1)
                        fieldName = fieldName.Substring(startIndex, endIndex - startIndex);
                    else fieldName = fieldName.Trim('<', '>');
                }

                o.Add(fieldName, Serialize(field.GetValue(obj)));
            }

            return o;
        }

        private enum Token
        {
            None = -1,
            Curly_Open,
            Curly_Close,
            Squared_Open,
            Squared_Close,
            Colon,
            Comma,
            String,
            Number,
            True,
            False,
            Null
        }

        /// <summary>
        ///     Alias of field used during deserialization.
        /// </summary>
        public class AliasAttribute : Attribute
        {
            /// <summary>
            ///     Aliases
            /// </summary>
            public readonly string[] aliases;

            /// <summary>
            ///     If true, the original field name will be ignored.
            /// </summary>
            public readonly bool ignoreFieldName;

            /// <summary>
            ///     Constructor
            /// </summary>
            /// <param name="ignoreFieldName">If true, the original field name will be ignored.</param>
            /// <param name="aliases">Aliases</param>
            public AliasAttribute(bool ignoreFieldName, params string[] aliases)
            {
                if (aliases == null || aliases.Length == 0) throw new Exception("You must use at least one alias.");

                this.ignoreFieldName = ignoreFieldName;
                this.aliases = aliases;
            }

            /// <summary>
            ///     Constructor
            /// </summary>
            /// <param name="aliases">Aliases</param>
            public AliasAttribute(params string[] aliases) : this(false, aliases)
            {
            }
        }
    }
}