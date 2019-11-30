using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapleCodeSharpTest
{
    class DocumentComparer
    {
        private struct TypePair
        {
            public readonly int A, B;
            public TypePair(int a, int b) { A = a; B = b; }
        }

        private HashSet<TypePair> _equalTypes = new HashSet<TypePair>();

        private List<Node> _compareNodeA = new List<Node>();
        private List<Node> _compareNodeB = new List<Node>();

        private Dictionary<Node, int> _nodeSequence = new Dictionary<Node, int>();
        private int _nodeCount = 0;

        private bool CompareDoc(Document a, Document b)
        {
            var nodesA = a.AllNodes.ToArray();
            var nodesB = b.AllNodes.ToArray();
            if (nodesA.Length != nodesB.Length)
            {
                return false;
            }
            for (int i = 0; i < nodesA.Length; ++i)
            {
                if (!Compare(nodesA[i], nodesB[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < _compareNodeA.Count; ++i)
            {
                if (!_nodeSequence.TryGetValue(_compareNodeA[i], out var ia))
                {
                    return false;
                }
                if (!_nodeSequence.TryGetValue(_compareNodeB[i], out var ib))
                {
                    return false;
                }
                if (ia != ib) return false;
            }

            return true;
        }

        private bool Compare(Node a, Node b)
        {
            var ta = a.NodeType;
            var tb = b.NodeType;
            if (!_equalTypes.Contains(new TypePair(ta.Index, tb.Index)))
            {
                if (!Compare(ta, tb))
                {
                    return false;
                }
                _equalTypes.Add(new TypePair(ta.Index, tb.Index));
            }

            var ga = a.ReadGenericArgs();
            var gb = b.ReadGenericArgs();
            for (int i = 0; i < ga.Length; ++i)
            {
                if (ga[i] != gb[i]) return false;
            }

            var aa = a.ReadArguments();
            var ab = b.ReadArguments();
            for (int i = 0; i < aa.Length; ++i)
            {
                if (!Compare(aa[i], ab[i])) return false;
            }

            var ca = a.Children.ToArray();
            var cb = b.Children.ToArray();
            if (ca.Length != cb.Length) return false;
            for (int i = 0; i < ca.Length; ++i)
            {
                if (!Compare(ca[i], cb[i])) return false;
            }

            _nodeSequence[a] = _nodeCount;
            _nodeSequence[b] = _nodeCount;
            _nodeCount += 1;

            return true;
        }

        private bool Compare(NodeType a, NodeType b)
        {
            if (a.Name != b.Name) return false;
            if (a.GenericArgCount != b.GenericArgCount) return false;
            if (a.HasChildren != b.HasChildren) return false;
            if (a.ArgumentTypes.Count != b.ArgumentTypes.Count) return false;
            for (int i = 0; i < a.ArgumentTypes.Count; ++i)
            {
                if (a.ArgumentTypes[i] != b.ArgumentTypes[i]) return false;
            }
            return true;
        }

        private bool Compare(NodeArgument a, NodeArgument b)
        {
            if (a.TypeClass != b.TypeClass) return false;
            switch (a.TypeClass)
            {
                case NodeArgumentTypeClass.Unsigned:
                    return a.GetUnsigned() == b.GetUnsigned();
                case NodeArgumentTypeClass.Signed:
                    return a.GetSigned() == b.GetSigned();
                case NodeArgumentTypeClass.Float:
                    return a.GetFloat() == b.GetFloat();
                case NodeArgumentTypeClass.String:
                    return a.GetString() == b.GetString();
                case NodeArgumentTypeClass.Data:
                    return Enumerable.SequenceEqual(a.GetData(), b.GetData());
                case NodeArgumentTypeClass.Ref:
                {
                    _compareNodeA.Add(a.GetNode());
                    _compareNodeB.Add(b.GetNode());
                    return true;
                }
                case NodeArgumentTypeClass.RefField:
                {
                    _compareNodeA.Add(a.GetNode());
                    _compareNodeB.Add(b.GetNode());
                    return a.GetField() == b.GetField();
                }
                default: return false;
            }
        }

        public static bool Compare(Document a, Document b)
        {
            return new DocumentComparer().CompareDoc(a, b);
        }
    }
}
