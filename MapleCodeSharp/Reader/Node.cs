using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Reader
{
    public struct Node : IEquatable<Node>
    {
        public Document Document { get; }
        private readonly int _offset;

        internal Node(Document doc, int sectionOffset)
        {
            Document = doc;
            _offset = sectionOffset;
        }

        public bool IsNull => Document == null;

        public NodeRange Children
        {
            get
            {
                if (Document == null)
                {
                    throw new InvalidOperationException();
                }
                Document.GetChildNodeRange(_offset, out var s, out var e);
                if (s < 0)
                {
                    return new NodeRange();
                }
                return new NodeRange(Document, s, e);
            }
        }

        public Node Next
        {
            get
            {
                if (Document == null)
                {
                    throw new InvalidOperationException();
                }
                return new Node(Document, Document.GetNextNode(_offset));
            }
        }

        public Node FindParentNode()
        {
            if (Document == null)
            {
                throw new InvalidOperationException();
            }
            var parent = Document.GetParentNode(_offset);
            if (parent == -1)
            {
                return new Node();
            }
            return new Node(Document, parent);
        }

        public NodeType NodeType
        {
            get
            {
                if (Document == null)
                {
                    throw new InvalidOperationException();
                }
                int pos = Document.NodeSectionOffset + _offset;
                return Document.GetNodeType(Document._readTypeIndex(ref pos));
            }
        }

        public string[] ReadGenericArgs()
        {
            int pos = Document.NodeSectionOffset + _offset;
            var t = Document.GetNodeType(Document._readTypeIndex(ref pos));
            var count = t.GenericArgCount;
            var ret = new string[count];
            for (int i = 0; i < count; ++i)
            {
                var str = Document.RetrieveString(Document._readStrIndex(ref pos));
                ret[i] = str;
            }
            return ret;
        }

        public NodeArgument[] ReadArguments()
        {
            int pos = Document.NodeSectionOffset + _offset;
            var t = Document.GetNodeType(Document._readTypeIndex(ref pos));
            pos += t.GenericArgCount * Document.StringIndexSize;
            var types = t.ArgumentTypes;
            var ret = new NodeArgument[types.Count];
            for (int i = 0; i < types.Count; ++i)
            {
                ret[i] = new NodeArgument(Document, pos, types[i]);
                pos += types[i].GetByteSize(Document);
            }
            return ret;
        }

        public override bool Equals(object obj)
        {
            if (obj is Node node)
            {
                return node.Document == Document && node._offset == _offset;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _offset.GetHashCode();
        }

        public static bool operator ==(Node left, Node right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Node left, Node right)
        {
            return !(left == right);
        }

        bool IEquatable<Node>.Equals(Node other)
        {
            return other.Document == Document && other._offset == _offset;
        }

        public override string ToString()
        {
            if (Document == null)
            {
                return "<null>";
            }
            return $"{NodeType.Name} @ {_offset}";
        }
    }
}
