using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapleCodeSharp.Reader
{
    public sealed class Document
    {
        internal delegate int ReadNumberDelegate(ref int pos);

        private struct TableRange
        {
            public int Start, End;
            public static readonly TableRange Empty = new TableRange { Start = -1, End = -1 };
        }

        internal byte[] _data;

        internal ReadNumberDelegate _readStrIndex, _readTypeIndex, _readNodeOffset, _readDataOffset;

        private readonly List<string> _strList = new List<string>();
        private readonly Dictionary<string, int> _strTable = new Dictionary<string, int>();
        private readonly List<NodeType> _types = new List<NodeType>();

        private TableRange _strTableRange, _typeTableRange, _nodeSectionRange, _dataSectionRange;
        internal int NodeSectionOffset => _nodeSectionRange.Start;
        internal int NodeSectionLength => _nodeSectionRange.End - _nodeSectionRange.Start;

        private int _typeTableEntrySize;

        public int StringIndexSize { get; }
        public int DataOffsetSize { get; }
        public int TypeIndexSize { get; }
        public int NodeOffsetSize { get; }

        private Document(int str, int type, int node, int data)
        {
            StringIndexSize = str;
            TypeIndexSize = type;
            NodeOffsetSize = node;
            DataOffsetSize = data;
        }

        private int ReadInt8(ref int pos)
        {
            return _data[pos++];
        }

        private int ReadInt16(ref int pos)
        {
            var ret = BitConverter.ToInt16(_data, pos);
            pos += 2;
            return ret;
        }

        private int ReadInt32(ref int pos)
        {
            var ret = BitConverter.ToInt32(_data, pos);
            pos += 4;
            return ret;
        }

        public static Document ReadFromData(byte[] data)
        {
            using var s = new MemoryStream(data);
            return ReadFromStream(s);
        }

        public static Document ReadFromStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Cannot read from stream", nameof(stream));

            byte[] headerBuffer = new byte[4];
            stream.Read(headerBuffer, 0, 1);
            var sizeMode = (uint)headerBuffer[0];

            int[] sizeArray = new int[] { 0, 1, 2, 4 };
            var strSize = sizeArray[(sizeMode >> 0) & 3];
            var typeSize = sizeArray[(sizeMode >> 2) & 3];
            var nodeSize = sizeArray[(sizeMode >> 4) & 3];
            var dataSize = sizeArray[(sizeMode >> 6) & 3];

            if (strSize == 0 || typeSize == 0 || nodeSize == 0 || dataSize == 0)
            {
                throw new ReaderException("Invalid size mode");
            }

            Array.Clear(headerBuffer, 0, 4);
            stream.Read(headerBuffer, 0, strSize);
            var strLength = BitConverter.ToInt32(headerBuffer, 0);
            Array.Clear(headerBuffer, 0, 4);
            stream.Read(headerBuffer, 0, typeSize);
            var typeLength = BitConverter.ToInt32(headerBuffer, 0);
            Array.Clear(headerBuffer, 0, 4);
            stream.Read(headerBuffer, 0, nodeSize);
            var nodeLength = BitConverter.ToInt32(headerBuffer, 0);
            Array.Clear(headerBuffer, 0, 4);
            stream.Read(headerBuffer, 0, dataSize);
            var dataLength = BitConverter.ToInt32(headerBuffer, 0);

            var totalLength = strLength + typeLength + nodeLength + dataLength;

            var docData = new byte[totalLength];
            var readCount = stream.Read(docData, 0, totalLength);
            if (readCount != totalLength)
            {
                throw new ReaderException("Cannot read to the end of document");
            }

            var ret = new Document(strSize, typeSize, nodeSize, dataSize)
            {
                _data = docData,
            };

            ret.SetupReadFunctions(sizeMode);
            ret.SetupSectionRange(strLength, typeLength, nodeLength, dataLength);
            ret.ReadStringTable();
            ret.ReadNodeTypeEntries();

            return ret;
        }

        private ReadNumberDelegate GetReadNumberDelegate(uint channelMode)
        {
            return channelMode switch
            {
                1 => ReadInt8,
                2 => ReadInt16,
                3 => ReadInt32,
                _ => throw new ReaderException("Invalid channel size"),
            };
        }

        private void SetupSectionRange(int strLen, int typeLen, int nodeLen, int dataLen)
        {
            _strTableRange = new TableRange
            {
                Start = 0,
                End = strLen,
            };
            _typeTableRange = new TableRange
            {
                Start = _strTableRange.End,
                End = _strTableRange.End + typeLen,
            };
            _nodeSectionRange = new TableRange
            {
                Start = _typeTableRange.End,
                End = _typeTableRange.End + nodeLen,
            };
            _dataSectionRange = new TableRange
            {
                Start = _nodeSectionRange.End,
                End = _nodeSectionRange.End + dataLen,
            };
            if (strLen == 0) _strTableRange = TableRange.Empty;
            if (typeLen == 0) _typeTableRange = TableRange.Empty;
            if (nodeLen == 0) _nodeSectionRange = TableRange.Empty;
            if (dataLen == 0) _dataSectionRange = TableRange.Empty;
            _typeTableEntrySize = StringIndexSize + DataOffsetSize + 2;
        }

        private void SetupReadFunctions(uint mode)
        {
            _readStrIndex = GetReadNumberDelegate(mode & 0x3);
            _readTypeIndex = GetReadNumberDelegate((mode >> 2) & 0x3);
            _readNodeOffset = GetReadNumberDelegate((mode >> 4) & 0x3);
            _readDataOffset = GetReadNumberDelegate((mode >> 6) & 0x3);
        }

        private void ReadStringTable()
        {
            int pos = _strTableRange.Start;
            if (pos == -1)
            {
                //No string table.
                return;
            }
            int i = 0;
            while (pos < _strTableRange.End)
            {
                int dataOffset = _readDataOffset(ref pos) + _dataSectionRange.Start;
                int dataEnd = Array.IndexOf<byte>(_data, 0, dataOffset);
                if (dataEnd == -1)
                {
                    throw new ReaderException("Invalid string");
                }
                var str = Encoding.UTF8.GetString(_data, dataOffset, dataEnd - dataOffset);
                if (_strTable.ContainsKey(str))
                {
                    throw new ReaderException("Invalid string");
                }
                _strList.Add(str);
                _strTable.Add(str, i++);
            }
        }

        private void ReadNodeTypeEntries()
        {
            if (_typeTableRange.Start == -1)
            {
                //No type table.
                return;
            }
            var count = (_typeTableRange.End - _typeTableRange.Start) / _typeTableEntrySize;
            for (int i = 0; i < count; ++i)
            {
                var pos = _typeTableRange.Start + i * _typeTableEntrySize;
                var str = _readStrIndex(ref pos);
                var args = _readDataOffset(ref pos);
                var generics = ReadInt8(ref pos);
                var hasChildren = ReadInt8(ref pos);
                if (hasChildren != 0 && hasChildren != 1)
                {
                    throw new ReaderException("Invalid node type entry");
                }
                var type = new NodeType(this, i, str, args, generics, hasChildren != 0);
                _types.Add(type);
            }
        }

        public int LookupStringTable(string str)
        {
            if (str == null || !_strTable.TryGetValue(str, out var ret))
            {
                return -1;
            }
            return ret;
        }

        public string RetrieveString(int index)
        {
            if (index < 0 || index >= _strList.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _strList[index];
        }

        public int NodeTypeCount => _types.Count;

        public NodeType GetNodeType(int index)
        {
            if (index < 0 || index >= _types.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _types[index];
        }

        internal void CopyData(int sourceOffset, byte[] buffer, int start, int len)
        {
            if (sourceOffset < 0 || len < 0 || sourceOffset + len > _dataSectionRange.End - _dataSectionRange.Start)
            {
                throw new ReaderException("Invalid data offset");
            }
            if (start < 0 || start + len > buffer.Length)
            {
                throw new ReaderException("Invalid data offset");
            }
            Buffer.BlockCopy(_data, _dataSectionRange.Start + sourceOffset, buffer, start, len);
        }

        public NodeRange AllNodes => new NodeRange(this, 0, NodeSectionLength);

        internal void GetChildNodeRange(int node, out int start, out int end)
        {
            int pos = NodeSectionOffset + node;
            var type = GetNodeType(_readTypeIndex(ref pos));
            if (!type.HasChildren)
            {
                start = end = -1;
            }
            else
            {
                pos = NodeSectionOffset + node + type.TotalSize;
                start = node + type.TotalSize + NodeOffsetSize;
                end = start + _readNodeOffset(ref pos);
            }
        }

        internal int GetNextNode(int node)
        {
            int pos = NodeSectionOffset + node;
            var type = GetNodeType(_readTypeIndex(ref pos));
            if (type.HasChildren)
            {
                pos = NodeSectionOffset + node + type.TotalSize;
                var childrenLen = _readNodeOffset(ref pos);
                return node + type.TotalSize + NodeOffsetSize + childrenLen;
            }
            return node + type.TotalSize;
        }

        internal int GetParentNode(int node)
        {
            if (node < 0 || node >= NodeSectionLength)
            {
                throw new ArgumentOutOfRangeException(nameof(node));
            }
            var start = 0;
            var end = GetNextNode(start);
            while (true)
            {
                if (node == start) return -1;
                if (start < node && node < end)
                {
                    return GetParentNode(node, start);
                }
                start = end;
                end = GetNextNode(start);
            }
        }

        private int GetParentNode(int node, int start)
        {
            int pos = NodeSectionOffset + start;
            var type = GetNodeType(_readTypeIndex(ref pos));
            if (!type.HasChildren)
            {
                throw new ReaderException("Invalid node hierarchy");
            }
            var child = start + type.TotalSize + NodeOffsetSize;
            if (node < child) throw new ReaderException("Invalid node hierarchy");
            var childEnd = GetNextNode(child);
            while (true)
            {
                if (child == node) return start;
                if (child < node && node < childEnd)
                {
                    return GetParentNode(node, child);
                }
                child = childEnd;
                childEnd = GetNextNode(child);
            }
        }

        //TODO remove
        internal int GetParentNode(int node, out int siblingEnd)
        {
            if (node == 0)
            {
                siblingEnd = -1;
                return -1;
            }

            int start = 0, end = NodeSectionLength, parent = -1;
            while (start < node && end > node)
            {
                var scan = start;
                var scanLast = start;
                while (scan < node)
                {
                    scanLast = scan;
                    scan = GetNextNode(scan);
                }
                if (scan == node)
                {
                    siblingEnd = end;
                    return parent;
                }
                start = scan;
                end = scanLast;
                GetChildNodeRange(start, out start, out _);
                if (start == -2)
                {
                    siblingEnd = end;
                    return scan;
                }
            }
            siblingEnd = end;
            return start;
        }
    }
}
