/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.JSON
{
    /// <summary>
    ///     The wrapper for JSON dictonary.
    /// </summary>
    public class JsonObject : JsonItem
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public JsonObject()
        {
            table = new Dictionary<string, JsonItem>();
        }

        /// <summary>
        ///     Dictionary of items
        /// </summary>
        public Dictionary<string, JsonItem> table { get; }

        public override JsonItem this[string key] => Get(key);

        public override JsonItem this[int index]
        {
            get
            {
                if (index < 0) return null;

                var i = 0;
                foreach (var pair in table)
                {
                    if (i == index) return pair.Value;
                    i++;
                }

                return null;
            }
        }

        /// <summary>
        ///     Adds element to the dictionary
        /// </summary>
        /// <param name="name">Key</param>
        /// <param name="value">Value</param>
        public void Add(string name, JsonItem value)
        {
            table[name] = value;
        }

        public void Add(string name, object value)
        {
            if (value is string || value is bool || value is int || value is long || value is short || value is float ||
                value is double) table[name] = new JsonValue(value);
            else if (value is Object)
                table[name] = new JsonValue((value as Object).GetInstanceID());
            else table[name] = Json.Serialize(value);
        }

        public void Add(string name, object value, JsonValue.ValueType valueType)
        {
            table[name] = new JsonValue(value, valueType);
        }

        public override JsonItem AppendObject(object obj)
        {
            Combine(Json.Serialize(obj));
            return this;
        }

        /// <summary>
        ///     Combines two JSON Object.
        /// </summary>
        /// <param name="other">Other JSON Object</param>
        /// <param name="overwriteExistingValues">Overwrite the existing values?</param>
        public void Combine(JsonItem other, bool overwriteExistingValues = false)
        {
            var otherObj = other as JsonObject;
            if (otherObj == null) throw new Exception("Only JsonObject is allowed to be combined.");
            var otherDict = otherObj.table;
            foreach (var pair in otherDict)
                if (overwriteExistingValues || !table.ContainsKey(pair.Key))
                    table[pair.Key] = pair.Value;
        }

        public bool Contains(string key)
        {
            return table.ContainsKey(key);
        }

        public JsonArray CreateArray(string name)
        {
            var array = new JsonArray();
            Add(name, array);
            return array;
        }

        public JsonObject CreateObject(string name)
        {
            var obj = new JsonObject();
            Add(name, obj);
            return obj;
        }

        public override object Deserialize(Type type,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            var members = Reflection.GetMembers(type, bindingFlags);
            return Deserialize(type, members, bindingFlags);
        }

        /// <summary>
        ///     Deserializes current element
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="members">Members of variable</param>
        /// <returns>Object</returns>
        public object Deserialize(Type type, IEnumerable<MemberInfo> members,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            var v = Activator.CreateInstance(type);
            DeserializeObject(v, members, bindingFlags);
            return v;
        }

        public void DeserializeObject(object obj,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            var members = Reflection.GetMembers(obj.GetType(), bindingFlags);
            DeserializeObject(obj, members);
        }

        public void DeserializeObject(object obj, IEnumerable<MemberInfo> members,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
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
                JsonItem item;

#if !NETFX_CORE
                var attributes = member.GetCustomAttributes(typeof(Json.AliasAttribute), true);
                var alias = attributes.Length > 0 ? attributes[0] as Json.AliasAttribute : null;
#else
                IEnumerable<Attribute> attributes = member.GetCustomAttributes(typeof(Json.AliasAttribute), true);
                Json.AliasAttribute alias = null;
                foreach (Attribute a in attributes)
                {
                    alias = a as Json.AliasAttribute;
                    break;
                }
#endif
                if (alias == null || !alias.ignoreFieldName)
                    if (table.TryGetValue(member.Name, out item))
                    {
                        var t = memberType == MemberTypes.Field
                            ? ((FieldInfo)member).FieldType
                            : ((PropertyInfo)member).PropertyType;
                        if (memberType == MemberTypes.Field)
                            ((FieldInfo)member).SetValue(obj, item.Deserialize(t, bindingFlags));
                        else ((PropertyInfo)member).SetValue(obj, item.Deserialize(t, bindingFlags), null);
                        continue;
                    }

                if (alias != null)
                    for (var j = 0; j < alias.aliases.Length; j++)
                        if (table.TryGetValue(alias.aliases[j], out item))
                        {
                            var t = memberType == MemberTypes.Field
                                ? ((FieldInfo)member).FieldType
                                : ((PropertyInfo)member).PropertyType;
                            if (memberType == MemberTypes.Field)
                                ((FieldInfo)member).SetValue(obj, item.Deserialize(t, bindingFlags));
                            else ((PropertyInfo)member).SetValue(obj, item.Deserialize(t, bindingFlags), null);
                            break;
                        }
            }
        }

        private JsonItem Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (key.Length > 2 && key[0] == '/' && key[1] == '/')
            {
                var k = key.Substring(2);
                if (string.IsNullOrEmpty(k) || k.StartsWith("//")) return null;
                return GetAll(k);
            }

            return GetThis(key);
        }

        private JsonItem GetThis(string key)
        {
            JsonItem item;
            var index = -1;
            for (var i = 0; i < key.Length; i++)
                if (key[i] == '/')
                {
                    index = i;
                    break;
                }

            if (index != -1)
            {
                var k = key.Substring(0, index);
                if (!string.IsNullOrEmpty(k))
                    if (table.TryGetValue(k, out item))
                    {
                        var nextPart = key.Substring(index + 1);
                        return item[nextPart];
                    }

                return null;
            }

            if (table.TryGetValue(key, out item)) return item;
            return null;
        }

        public override JsonItem GetAll(string k)
        {
            var item = GetThis(k);
            JsonArray arr = null;
            if (item != null)
            {
                arr = new JsonArray();
                arr.Add(item);
            }

            var enumerator = table.GetEnumerator();
            while (enumerator.MoveNext())
            {
                item = enumerator.Current.Value;
                var subArr = item.GetAll(k) as JsonArray;
                if (subArr != null)
                {
                    if (arr == null) arr = new JsonArray();
                    arr.AddRange(subArr);
                }
            }

            return arr;
        }

        public override IEnumerator<JsonItem> GetEnumerator()
        {
            return table.Values.GetEnumerator();
        }

        /// <summary>
        ///     Parse a string that contains JSON dictonary
        /// </summary>
        /// <param name="json">String that contains JSON dictonary</param>
        /// <returns>Instance</returns>
        public static JsonObject ParseObject(string json)
        {
            return Json.Parse(json) as JsonObject;
        }

        public JsonItem Remove(string key)
        {
            JsonItem item;
            if (table.TryGetValue(key, out item))
            {
                table.Remove(key);
                return item;
            }

            return null;
        }

        public override void ToJSON(StringBuilder b)
        {
            b.Append("{");
            var hasChilds = false;
            foreach (var pair in table)
            {
                b.Append("\"").Append(pair.Key).Append("\"").Append(":");
                pair.Value.ToJSON(b);
                b.Append(",");
                hasChilds = true;
            }

            if (hasChilds) b.Remove(b.Length - 1, 1);
            b.Append("}");
        }

        public override object Value(Type type)
        {
            if (Reflection.IsValueType(type))
            {
                var obj = Activator.CreateInstance(type);
                DeserializeObject(obj);
                return obj;
            }

            return Deserialize(type);
        }
    }
}