using System;
using System.Collections;

namespace Beholder.Models.PathTree
{
    public class PathNode : IEqualityComparer
    {
        public PathNode(string path, string? parentPath, bool isDir = false, bool forceUpdate = false)
        {
            Path = path;
            ParentPath = parentPath;
            IsDirectory = isDir;
            WillForceUpdate = forceUpdate;
        }

        /// <summary>
        ///     Full path, unique.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     Can be Null (root node).
        /// </summary>
        public string? ParentPath { get; set; }

        public bool IsDirectory { get; set; }

        public bool WillForceUpdate { get; set; }

        public bool JustCreated { get; set; }

        bool IEqualityComparer.Equals(object? value1, object? value2)
        {
            return Equals(value1 as PathNode, value2 as PathNode);
        }

        public int GetHashCode(object? obj)
        {
            return obj is PathNode node
                ? HashCode.Combine(node.Path.GetHashCode(), node.IsDirectory.GetHashCode())
                : -1;
        }

        public override string ToString()
        {
            return Path;
        }

        public static bool operator ==(PathNode? value1, PathNode? value2)
        {
            return value1 is null && value2 is null ||
                   value1 is not null && value2 is not null && value1.Path == value2.Path;
        }

        public static bool operator !=(PathNode? value1, PathNode? value2)
        {
            return !(value1 == value2);
        }

        public override bool Equals(object? obj)
        {
            return obj is PathNode value && this == value;
        }

        public bool Equals(PathNode? value)
        {
            return this == value;
        }

        public static bool Equals(PathNode? value1, PathNode? value2)
        {
            return value1 == value2;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }
}
