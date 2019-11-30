using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MapleCodeSharp.Builder
{
    public sealed class StringTableBuilder
    {
        private readonly DataSectionBuilder _dataSection;
        private readonly List<uint> _data = new List<uint>();
        private readonly Dictionary<string, int> _stringMap = new Dictionary<string, int>();
        private readonly int _dataSize;

        public StringTableBuilder(DataSectionBuilder dataSection, int dataSize)
        {
            _dataSection = dataSection;
            _dataSize = dataSize;
        }

        public int AddString(string str)
        {
            if (!_stringMap.TryGetValue(str, out var ret))
            {
                ret = _data.Count;
                _stringMap.Add(str, ret);
                _data.Add((uint)_dataSection.AppendRaw(Encoding.UTF8.GetBytes(str + "\0")));
            }
            return ret;
        }

        public byte[] Generate()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            foreach (var i in _data)
            {
                bw.Write(i);
                ms.Seek(_dataSize - 4, SeekOrigin.Current);
            }
            ms.SetLength(ms.Position);
            return ms.ToArray();
        }
    }
}
