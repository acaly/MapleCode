using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    internal class StringTable
    {
        private readonly List<int> _data = new List<int>();
        private readonly DataSection _dataSection;
        private readonly Dictionary<string, int> _stringMap = new Dictionary<string, int>();

        public StringTable(DataSection dataSection)
        {
            _dataSection = dataSection;
        }

        public int AddString(string str)
        {
            if (!_stringMap.TryGetValue(str, out var ret))
            {
                var data = Encoding.UTF8.GetBytes(str);
                var dataPos = _dataSection.Position;

                _dataSection.AddInt8(data);
                _dataSection.Add(0, DataSection.SlotType.Int8);

                ret = _data.Count;
                _data.Add(dataPos);
                _stringMap.Add(str, ret);
            }
            return ret;
        }

        public int StringIndexSize => MapleCodeCompiler.GetByteSize(_data.Count);

        public byte[] Generate(int dataSize)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, true);

            foreach (var pos in _data)
            {
                var offset = _dataSection.GetGeneratedOffset(pos);
                bw.Write(offset);
                ms.Seek(dataSize - 4, SeekOrigin.Current);
            }

            ms.SetLength(ms.Position);
            return ms.ToArray();
        }
    }
}
