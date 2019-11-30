using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Builder
{
    public struct NodePosition : IEquatable<NodePosition>
    {
        internal readonly bool IsEmpty;
        internal readonly int Position;

        internal NodePosition(int pos)
        {
            IsEmpty = false;
            Position = pos;
        }

        public override bool Equals(object obj)
        {
            if (obj is NodePosition other)
            {
                return IsEmpty == other.IsEmpty && Position == other.Position;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(NodePosition left, NodePosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodePosition left, NodePosition right)
        {
            return !(left == right);
        }

        public bool Equals(NodePosition other)
        {
            return IsEmpty == other.IsEmpty && Position == other.Position;
        }
    }
}
