using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Beholder.Models.PathTree
{
    public class Node<T> : IEqualityComparer, IEnumerable<T>, IEnumerable<Node<T>>
    {
        private readonly List<Node<T>> children = new List<Node<T>>();

        public Node(T value)
        {
            Value = value;
        }

        public Node<T>? Parent { get; private set; }

        public T Value { get; set; }

        public Node<T> this[int index] => children[index];

        public IEnumerable<Node<T>> Ancestors =>
            IsRoot ? Enumerable.Empty<Node<T>>() : Parent!.ToIEnumerable().Concat(Parent!.Ancestors);

        public IEnumerable<Node<T>> Descendants => SelfAndDescendants.Skip(1);

        public IEnumerable<Node<T>> Children => children;

        public IEnumerable<Node<T>> Siblings => SelfAndSiblings.Where(Other);

        public IEnumerable<Node<T>> SelfAndChildren => this.ToIEnumerable().Concat(Children);

        public IEnumerable<Node<T>> SelfAndAncestors => this.ToIEnumerable().Concat(Ancestors);

        public IEnumerable<Node<T>> SelfAndDescendants =>
            this.ToIEnumerable().Concat(Children.SelectMany(c => c.SelfAndDescendants));

        public IEnumerable<Node<T>> SelfAndSiblings => IsRoot ? this.ToIEnumerable() : Parent!.Children;

        public IEnumerable<Node<T>> All => Root.SelfAndDescendants;

        public IEnumerable<Node<T>> SameLevel => SelfAndSameLevel.Where(Other);

        public int Level => Ancestors.Count();

        public IEnumerable<Node<T>> SelfAndSameLevel => GetNodesAtLevel(Level);

        public Node<T> Root => SelfAndAncestors.Last();

        public bool IsRoot => Parent is null;

        public IEnumerator<Node<T>> GetEnumerator()
        {
            return children.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return children.Values().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return children.GetEnumerator();
        }

        public Node<T> Add(T value, int index = -1)
        {
            var childNode = new Node<T>(value);
            Add(childNode, index);
            return childNode;
        }

        public void Add(Node<T> childNode, int index = -1)
        {
            if (index < -1)
            {
                throw new ArgumentException("The index can not be lower then -1");
            }

            if (index > Children.Count() - 1)
            {
                throw new ArgumentException(
                    $"The index ({index}) can not be higher then index of the last item. Use the AddChild() method without an index to add at the end");
            }

            if (!childNode.IsRoot)
            {
                throw new ArgumentException(
                    $"The child node with value [{childNode.Value}] can not be added because it is not a root node.");
            }

            if (Root == childNode)
            {
                throw new ArgumentException(
                    $"The child node with value [{childNode.Value}] is the root node of the parent.");
            }

            if (childNode.SelfAndDescendants.Any(n => this == n))
            {
                throw new ArgumentException(
                    $"The child node with value [{childNode.Value}] can not be added to itself or its descendants.");
            }

            childNode.Parent = this;

            if (index == -1)
            {
                children.Add(childNode);
            }
            else
            {
                children.Insert(index, childNode);
            }
        }

        public Node<T> AddFirstChild(T value)
        {
            var childNode = new Node<T>(value);
            AddFirstChild(childNode);
            return childNode;
        }

        public void AddFirstChild(Node<T> childNode)
        {
            Add(childNode, 0);
        }

        public Node<T> AddFirstSibling(T value)
        {
            var childNode = new Node<T>(value);
            AddFirstSibling(childNode);
            return childNode;
        }

        public void AddFirstSibling(Node<T> childNode)
        {
            Parent!.AddFirstChild(childNode);
        }

        public Node<T> AddLastSibling(T value)
        {
            var childNode = new Node<T>(value);
            AddLastSibling(childNode);
            return childNode;
        }

        public void AddLastSibling(Node<T> childNode)
        {
            Parent!.Add(childNode);
        }

        public Node<T> AddParent(T value)
        {
            var newNode = new Node<T>(value);
            AddParent(newNode);
            return newNode;
        }

        public void AddParent(Node<T> parentNode)
        {
            if (!IsRoot)
            {
                throw new ArgumentException($"This node [{Value}] already has a parent", nameof(parentNode));
            }

            parentNode.Add(this);
        }

        private bool Other(Node<T> node)
        {
            return !ReferenceEquals(node, this);
        }

        public IEnumerable<Node<T>> GetNodesAtLevel(int level)
        {
            return Root.GetNodesAtLevelInternal(level);
        }

        private IEnumerable<Node<T>> GetNodesAtLevelInternal(int level)
        {
            if (level == Level)
            {
                return this.ToIEnumerable();
            }

            return Children.SelectMany(c => c.GetNodesAtLevelInternal(level));
        }

        public void Disconnect()
        {
            if (IsRoot)
            {
                throw new InvalidOperationException($"The root node [{Value}] can not get disconnected from a parent.");
            }

            Parent!.children.Remove(this);
            Parent = null;
        }

        public override string? ToString()
        {
            return Value!.ToString();
        }

        public static IEnumerable<Node<T>> CreateTree(IEnumerable<T> values,
            Func<T, string> idSelector,
            Func<T, string?> parentIdSelector)

        {
            var valuesCache = values.ToList();

            if (!valuesCache.Any())
            {
                return Enumerable.Empty<Node<T>>();
            }

            var itemWithIdAndParentIdIsTheSame =
                valuesCache.FirstOrDefault(v => IsSameId(idSelector(v), parentIdSelector(v)));

            if (itemWithIdAndParentIdIsTheSame != null)
            {
                throw new ArgumentException(
                    $"At least one value has the same Id and parentId [{itemWithIdAndParentIdIsTheSame}]");
            }

            var nodes = valuesCache.Select(v => new Node<T>(v));
            return CreateTree(nodes, idSelector, parentIdSelector);
        }

        public static IEnumerable<Node<T>> CreateTree(IEnumerable<Node<T>> rootNodes,
            Func<T, string> idSelector,
            Func<T, string?> parentIdSelector)
        {
            var rootNodesCache = rootNodes.ToList();
            var duplicates = rootNodesCache.Duplicates(n => n).ToList();
            if (duplicates.Any())
            {
                throw new ArgumentException(
                    $"One or more values contains {duplicates.Count} duplicate keys. The first duplicate is: [{duplicates[0]}]");
            }

            foreach (var rootNode in rootNodesCache)
            {
                var parentId = parentIdSelector(rootNode.Value);
                var parent = rootNodesCache.FirstOrDefault(n => IsSameId(idSelector(n.Value), parentId));

                if (parent != null)
                {
                    parent.Add(rootNode);
                }
                else if (parentId != null)
                {
                    throw new ArgumentException(
                        $"A value has the parent ID [{parentId}] but no other nodes has this ID");
                }
            }

            var result = rootNodesCache.Where(n => n.IsRoot);
            return result;
        }


        private static bool IsSameId(string id, string? parentId)
        {
            return parentId != null && id.Equals(parentId);
        }

        #region Equals en ==

        public static bool operator ==(Node<T>? value1, Node<T>? value2)
        {
            return value1 is null && value2 is null || ReferenceEquals(value1, value2);
        }

        public static bool operator !=(Node<T>? value1, Node<T>? value2)
        {
            return !(value1 == value2);
        }

        public override bool Equals(object? obj)
        {
            var valueThisType = obj as Node<T>;
            return this == valueThisType;
        }

        public bool Equals(Node<T>? value)
        {
            return this == value;
        }

        public static bool Equals(Node<T>? value1, Node<T>? value2)
        {
            return value1 == value2;
        }

        bool IEqualityComparer.Equals(object? value1, object? value2)
        {
            return Equals(value1 as Node<T>, value2 as Node<T>);
        }

        public int GetHashCode(object? obj)
        {
            return obj is Node<T> node ? HashCode.Combine(node.Value!.GetHashCode(), 42) : -1;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        #endregion
    }
}
