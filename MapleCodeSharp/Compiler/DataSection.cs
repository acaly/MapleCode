using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    internal class DataSection
    {
        public enum SlotType : byte
        {
            Int8,
            Int16,
            Int32,
            StringIndex,
            TypeIndex,
            NodeIndex,
            DataIndex,

            NodeLengthEnd, //Automatically fixed to the byte size in this data section. Size is NodeOffset.

            Count,
        }

        private readonly List<uint> _data = new List<uint>();
        private readonly List<SlotType> _type = new List<SlotType>();
        private readonly int[] _count = new int[(int)SlotType.Count];
        private readonly List<uint> _offset = new List<uint>();

        public int Position => _data.Count;

        //data must use unsigned extension (don't move sign bit).
        public void Add(uint data, SlotType type)
        {
            _data.Add(data);
            _type.Add(type);
            _count[(int)type] += 1;
        }

        public void AddInt8(byte[] data)
        {
            foreach (var b in data)
            {
                Add(b, SlotType.Int8);
            }
        }

        public int TryCalculateSize(int sizeStr, int sizeType, int sizeNode, int sizeData)
        {
            var invariant = _count[0] + _count[1] * 2 + _count[2] * 4;
            var s = sizeStr * _count[3];
            var t = sizeType * _count[4];
            var n = sizeNode * (_count[5] + _count[7]);
            var d = sizeData * _count[6];
            return MapleCodeCompiler.GetByteSize(invariant + s + t + n + d);
        }

        public void GenerateOffset(int sizeStr, int sizeType, int sizeNode, int sizeData)
        {
            uint[] size = new uint[] { 1, 2, 4,
                (uint)sizeStr, (uint)sizeType, (uint)sizeNode, (uint)sizeData, (uint)sizeNode };
            uint pos = 0;
            _offset.Clear();
            for (int i = 0; i < _data.Count; ++i)
            {
                _offset.Add(pos);
                pos += size[(int)_type[i]];
            }
            for (int i = 0; i < _data.Count; ++i)
            {
                if (_type[i] == SlotType.NodeLengthEnd)
                {
                    _data[i] = _offset[(int)_data[i]] - _offset[i + 1];
                }
            }
        }

        public uint GetGeneratedOffset(int pos)
        {
            return _offset[pos];
        }

        public void FixOffset(SlotType type, DataSection source)
        {
            for (int i = 0; i < _data.Count; ++i)
            {
                if (_type[i] == type)
                {
                    _data[i] = source.GetGeneratedOffset((int)_data[i]);
                }
            }
        }

        public void FixSingleSlot(int pos, uint val)
        {
            _data[pos] = val;
        }

        public byte[] Generate(int sizeStr, int sizeType, int sizeNode, int sizeData)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, true);

            int[] seekBack = new[] { -3, -2, 0,
                sizeStr - 4, sizeType - 4, sizeNode - 4, sizeData - 4, sizeNode - 4 };
            for (int i = 0; i < _data.Count; ++i)
            {
                bw.Write(_data[i]);
                ms.Seek(seekBack[(int)_type[i]], SeekOrigin.Current);
            }
            ms.SetLength(ms.Position);
            return ms.ToArray();
        }
    }
}
