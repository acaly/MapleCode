using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Reader
{
    public struct NodeArgument : IEquatable<NodeArgument>
    {
        internal readonly Document Document;
        internal readonly int AbsOffset;
        internal readonly NodeArgumentType ArgType;

        internal NodeArgument(Document doc, int offset, NodeArgumentType type)
        {
            Document = doc;
            AbsOffset = offset;
            ArgType = type;
        }

        private NodeArgumentType CheckType() => ArgType ?? throw new InvalidOperationException();

        public NodeArgumentTypeClass TypeClass => CheckType().TypeClass;
        public int GetSigned() => CheckType().GetSigned(this);
        public uint GetUnsigned() => CheckType().GetUnsigned(this);
        public string GetString() => CheckType().GetString(this);
        public byte[] GetData() => CheckType().GetData(this);
        public string GetField() => CheckType().GetField(this);
        public float GetFloat() => CheckType().GetFloat(this);
        public Node GetNode() => CheckType().GetNode(this);

        public override bool Equals(object obj)
        {
            if (obj is NodeArgument a)
            {
                return a.Document == Document && a.AbsOffset == AbsOffset;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(NodeArgument left, NodeArgument right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodeArgument left, NodeArgument right)
        {
            return !(left == right);
        }

        bool IEquatable<NodeArgument>.Equals(NodeArgument other)
        {
            return other.Document == Document && other.AbsOffset == AbsOffset;
        }
    }
}
