using System.Collections.Generic;
using UnityEngine;

namespace Pegasus
{
    /// <summary>
    ///     A region quadtree implementation used for fast lookup in a two dimensional world.
    /// </summary>
    /// <typeparam name="T">
    ///     The type to store inside the tree.
    /// </typeparam>
    /// <remarks>
    ///     This implementation is not thread-safe.
    /// </remarks>
    public class Quadtree<T>
    {
        /// <summary>
        ///     The maximum number of nodes per tree.
        /// </summary>
        private readonly int nodeCapacity = 32;

        /// <summary>
        ///     The nodes inside this region.
        /// </summary>
        private readonly List<QuadtreeNode> nodes;

        /// <summary>
        ///     The boundaries of this region.
        /// </summary>
        private Rect boundaries;

        /// <summary>
        ///     The child trees inside this region.
        /// </summary>
        private Quadtree<T>[] children;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Quadtree`1" /> class.
        /// </summary>
        /// <param name="boundaries">
        ///     The boundaries of the region.
        /// </param>
        /// <param name="nodeCapacity">
        ///     The maximum number of nodes per tree.
        ///     If the amount of nodes exceeds the tree will be subdivided into 4 sub trees.
        ///     A value of 32 seems fine in terms of insert and remove speed.
        ///     A value greater than 32 improves insert speed but slows down remove speed.
        /// </param>
        public Quadtree(Rect boundaries, int nodeCapacity = 32)
        {
            this.boundaries = boundaries;
            this.nodeCapacity = nodeCapacity;

            nodes = new List<QuadtreeNode>(nodeCapacity);
        }

        /// <summary>
        ///     Gets the number of values inside this tree.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        ///     Inserts a value into the region.
        /// </summary>
        /// <param name="x">
        ///     The X component of the value's position.
        /// </param>
        /// <param name="y">
        ///     The y component of the value's position.
        /// </param>
        /// <param name="value">
        ///     The value to insert.
        /// </param>
        /// <returns>
        ///     true if the value was inserted into the region;
        ///     false if the value's position was outside the region.
        /// </returns>
        public bool Insert(float x, float y, T value)
        {
            var position = new Vector2(x, y);
            var node = new QuadtreeNode(position, value);
            return Insert(node);
        }

        /// <summary>
        ///     Inserts a value into the region.
        /// </summary>
        /// <param name="position">
        ///     The position of the value.
        /// </param>
        /// <param name="value">
        ///     The value to insert.
        /// </param>
        /// <returns>
        ///     true if the value was inserted into the region;
        ///     false if the value's position was outside the region.
        /// </returns>
        public bool Insert(Vector2 position, T value)
        {
            var node = new QuadtreeNode(position, value);
            return Insert(node);
        }

        /// <summary>
        ///     Inserts a node into the region.
        /// </summary>
        /// <param name="node">
        ///     The node to insert.
        /// </param>
        /// <returns>
        ///     true if the node was inserted into the region;
        ///     false if the position of the node was outside the region.
        /// </returns>
        private bool Insert(QuadtreeNode node)
        {
            if (!boundaries.Contains(node.Position)) return false;

            if (children != null)
            {
                Quadtree<T> child;
                if (node.Position.y < children[2].boundaries.yMin)
                {
                    if (node.Position.x < children[1].boundaries.xMin)
                        child = children[0];
                    else
                        child = children[1];
                }
                else
                {
                    if (node.Position.x < children[1].boundaries.xMin)
                        child = children[2];
                    else
                        child = children[3];
                }

                if (child.Insert(node))
                {
                    Count++;
                    return true;
                }
            }

            if (nodes.Count < nodeCapacity)
            {
                nodes.Add(node);
                Count++;
                return true;
            }

            Subdivide();
            return Insert(node);
        }

        /// <summary>
        ///     Returns the values that are within the specified <paramref name="range" />.
        /// </summary>
        /// <param name="range">
        ///     A rectangle representing the region to query.
        /// </param>
        /// <returns>
        ///     Any value found inside the specified <paramref name="range" />.
        /// </returns>
        public IEnumerable<T> Find(Rect range)
        {
            if (Count == 0) yield break;

            if (!boundaries.Overlaps(range, false)) yield break;

            if (children == null)
                for (var index = 0; index < nodes.Count; index++)
                {
                    var node = nodes[index];
                    if (range.Contains(node.Position)) yield return node.Value;
                }
            else
                for (var index = 0; index < children.Length; index++)
                {
                    var child = children[index];

                    foreach (var value in child.Find(range)) yield return value;
                }
        }

        /// <summary>
        ///     Removes a value from the region.
        /// </summary>
        /// <param name="x">
        ///     The X component of the value's position.
        /// </param>
        /// <param name="z">
        ///     The Z component of the value's position.
        /// </param>
        /// <param name="value">
        ///     The value to remove.
        /// </param>
        /// <returns>
        ///     true if the value was removed from the region;
        ///     false if the value's position was outside the region.
        /// </returns>
        public bool Remove(float x, float z, T value)
        {
            return Remove(new Vector2(x, z), value);
        }

        /// <summary>
        ///     Removes a value from the region.
        /// </summary>
        /// <param name="position">
        ///     The position of the value.
        /// </param>
        /// <param name="value">
        ///     The value to remove.
        /// </param>
        /// <returns>
        ///     true if the value was removed from the region;
        ///     false if the value's position was outside the region.
        /// </returns>
        public bool Remove(Vector2 position, T value)
        {
            if (Count == 0) return false;

            if (!boundaries.Contains(position)) return false;

            if (children != null)
            {
                var isRemoved = false;

                Quadtree<T> child;
                if (position.y < children[2].boundaries.yMin)
                {
                    if (position.x < children[1].boundaries.xMin)
                        child = children[0];
                    else
                        child = children[1];
                }
                else
                {
                    if (position.x < children[1].boundaries.xMin)
                        child = children[2];
                    else
                        child = children[3];
                }

                if (child.Remove(position, value))
                {
                    isRemoved = true;
                    Count--;
                }

                if (Count <= nodeCapacity) Combine();

                return isRemoved;
            }

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node.Position.Equals(position))
                {
                    nodes.RemoveAt(index);
                    Count--;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Splits the region into 4 new subregions and moves the existing values into the new subregions.
        /// </summary>
        private void Subdivide()
        {
            children = new Quadtree<T>[4];

            var width = this.boundaries.width * 0.5f;
            var height = this.boundaries.height * 0.5f;

            for (var index = 0; index < children.Length; index++)
            {
                var boundaries = new Rect(
                    this.boundaries.xMin + width * (index % 2),
                    this.boundaries.yMin + height * (index / 2),
                    width,
                    height
                );

                children[index] = new Quadtree<T>(boundaries);
            }

            Count = 0;

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                Insert(node);
            }

            nodes.Clear();
        }

        /// <summary>
        ///     Joins the contents of the children into this region and remove the child regions.
        /// </summary>
        private void Combine()
        {
            for (var index = 0; index < children.Length; index++)
            {
                var child = children[index];
                nodes.AddRange(child.nodes);
            }

            children = null;
        }

        /// <summary>
        ///     A single node inside a quadtree used for keeping values and their position.
        /// </summary>
        private class QuadtreeNode
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="T:QuadtreeNode" /> class.
            /// </summary>
            /// <param name="position">
            ///     The position of the value.
            /// </param>
            /// <param name="value">
            ///     The value.
            /// </param>
            public QuadtreeNode(Vector2 position, T value)
            {
                Position = position;
                Value = value;
            }

            /// <summary>
            ///     Gets the position of the value.
            /// </summary>
            public Vector2 Position { get; }

            /// <summary>
            ///     Gets the value.
            /// </summary>
            public T Value { get; }
        }
    }
}