/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace InfinityCode.UltimateEditorEnhancer.JSON
{
    /// <summary>
    ///     The wrapper for an array of JSON elements.
    /// </summary>
    public class JsonArray : JsonItem
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public JsonArray()
        {
            items = new List<JsonItem>();
        }

        public List<JsonItem> items { get; }

        /// <summary>
        ///     Count elements
        /// </summary>
        public int count { get; private set; }

        public override JsonItem this[int index]
        {
            get
            {
                if (index < 0 || index >= count) return null;
                return items[index];
            }
        }


        public override JsonItem this[string key] => Get(key);

        /// <summary>
        ///     Adds an element to the array.
        /// </summary>
        /// <param name="item">Element</param>
        public void Add(JsonItem item)
        {
            items.Add(item);
            count++;
        }

        /// <summary>
        ///     Adds an elements to the array.
        /// </summary>
        /// <param name="collection">Array of elements</param>
        public void AddRange(JsonArray collection)
        {
            if (collection == null) return;
            items.AddRange(collection.items);
            count += collection.count;
        }

        public void AddRange(JsonItem collection)
        {
            AddRange(collection as JsonArray);
        }

        public JsonObject CreateObject()
        {
            var obj = new JsonObject();
            Add(obj);
            return obj;
        }

        public override object Deserialize(Type type,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (count == 0) return null;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var v = Array.CreateInstance(elementType, count);
                if (items[0] is JsonObject)
                {
                    var members = Reflection.GetMembers(elementType, bindingFlags);
                    for (var i = 0; i < count; i++)
                    {
                        var child = items[i];
                        var item = (child as JsonObject).Deserialize(elementType, members, bindingFlags);
                        v.SetValue(item, i);
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var child = items[i];
                        var item = child.Deserialize(elementType, bindingFlags);
                        v.SetValue(item, i);
                    }
                }

                return v;
            }

            if (Reflection.IsGenericType(type))
            {
                var listType = Reflection.GetGenericArguments(type)[0];
                var v = Activator.CreateInstance(type);

                if (items[0] is JsonObject)
                {
                    var members = Reflection.GetMembers(listType, BindingFlags.Instance | BindingFlags.Public);
                    for (var i = 0; i < count; i++)
                    {
                        var child = items[i];
                        var item = (child as JsonObject).Deserialize(listType, members);
                        try
                        {
                            var methodInfo = Reflection.GetMethod(type, "Add");
                            if (methodInfo != null) methodInfo.Invoke(v, new[] { item });
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var child = items[i];
                        var item = child.Deserialize(listType);
                        try
                        {
                            var methodInfo = Reflection.GetMethod(type, "Add");
                            if (methodInfo != null) methodInfo.Invoke(v, new[] { item });
                        }
                        catch
                        {
                        }
                    }
                }

                return v;
            }


            return null;
        }

        private JsonItem Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (key.StartsWith("//"))
            {
                var k = key.Substring(2);
                if (string.IsNullOrEmpty(k) || k.StartsWith("//")) return null;
                return GetAll(k);
            }

            return GetThis(key);
        }

        private JsonItem GetThis(string key)
        {
            int kindex;

            if (key.Contains("/"))
            {
                var index = key.IndexOf("/");
                var k = key.Substring(0, index);
                var nextPart = key.Substring(index + 1);

                if (k == "*")
                {
                    var arr = new JsonArray();
                    for (var i = 0; i < count; i++)
                    {
                        var item = items[i][nextPart];
                        if (item != null) arr.Add(item);
                    }

                    return arr;
                }

                if (int.TryParse(k, out kindex))
                {
                    if (kindex < 0 || kindex >= count) return null;
                    var item = items[kindex];
                    return item[nextPart];
                }
            }

            if (key == "*") return this;
            if (int.TryParse(key, out kindex)) return this[kindex];
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

            for (var i = 0; i < count; i++)
            {
                item = items[i];
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
            return items.GetEnumerator();
        }

        /// <summary>
        ///     Parse a string that contains an array
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Instance</returns>
        public static JsonArray ParseArray(string json)
        {
            return Json.Parse(json) as JsonArray;
        }

        public override void ToJSON(StringBuilder b)
        {
            b.Append("[");
            for (var i = 0; i < count; i++)
            {
                if (i != 0) b.Append(",");
                items[i].ToJSON(b);
            }

            b.Append("]");
        }

        public override object Value(Type type)
        {
            if (Reflection.IsValueType(type)) return Activator.CreateInstance(type);
            return null;
        }
    }
}