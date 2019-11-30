using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Builder
{
    public struct ChildrenListPosition : IEquatable<ChildrenListPosition>
    {
        internal readonly bool IsEmpty;
        internal readonly int Position;

        internal ChildrenListPosition(int pos)
        {
            IsEmpty = false;
            Position = pos;
        }

        public override bool Equals(object obj)
        {
            if (obj is ChildrenListPosition other)
            {
                return IsEmpty == other.IsEmpty && Position == other.Position;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ChildrenListPosition left, ChildrenListPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChildrenListPosition left, ChildrenListPosition right)
        {
            return !(left == right);
        }

        public bool Equals(ChildrenListPosition other)
        {
            return IsEmpty == other.IsEmpty && Position == other.Position;
        }
    }
}
