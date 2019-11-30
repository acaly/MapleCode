using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Reader
{
    public struct NodeRange : IEquatable<NodeRange>, IEnumerable<Node>
    {
        public Document Document { get; }
        private readonly int _start, _end;

        public Node Begin => new Node(Document, Document == null ? 0 : _start);
        public Node End => new Node(Document, Document == null ? 0 : _end);

        internal NodeRange(Document document, int start, int end)
        {
            Document = document;
            _start = start;
            _end = end;
        }

        public override bool Equals(object obj)
        {
            if (obj is NodeRange other)
            {
                return Document == other.Document &&
                    _start == other._start && _end == other._end;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(NodeRange left, NodeRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodeRange left, NodeRange right)
        {
            return !(left == right);
        }

        public bool Equals(NodeRange other)
        {
            return Document == other.Document &&
                _start == other._start && _end == other._end;
        }

        public IEnumerator<Node> GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            if (Document == null) return "<null>";
            return $" {_start}, {_end} ";
        }

        private class NodeEnumerator : IEnumerator<Node>
        {
            private readonly NodeRange _range;
            private int _pos = -1;

            public Node Current => _pos < 0 ? throw new InvalidOperationException() : new Node(_range.Document, _pos);
            object IEnumerator.Current => Current;

            public NodeEnumerator(NodeRange range)
            {
                _range = range;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_pos == -2)
                {
                    throw new InvalidOperationException();
                }
                if (_pos == -1)
                {
                    _pos = _range._start;
                }
                else
                {
                    _pos = _range.Document.GetNextNode(_pos);
                }
                if (_pos >= _range._end)
                {
                    _pos = -2;
                    return false;
                }
                return true;
            }

            public void Reset()
            {
                _pos = -1;
            }
        }
    }
}
