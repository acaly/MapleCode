using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    internal class TypeTable
    {
        private struct Entry
        {
            public int Name;
            public int ArgsDataPos;
            public byte GenericCount;
            public bool HasChildren;
        }

        private readonly StringTable _stringTable;
        private readonly DataSection _dataSection;
        private readonly List<Entry> _entries = new List<Entry>();

        public TypeTable(StringTable stringTable, DataSection dataSection)
        {
            _stringTable = stringTable;
            _dataSection = dataSection;
        }

        public int Add(string name, List<byte> args, int genericCount, bool hasChildren)
        {
            var ret = _entries.Count;

            var dataPos = _dataSection.Position;
            _dataSection.Add((uint)args.Count, DataSection.SlotType.Int8);
            _dataSection.AddInt8(args.ToArray());

            _entries.Add(new Entry
            {
                Name = _stringTable.AddString(name),
                ArgsDataPos = dataPos,
                GenericCount = (byte)genericCount,
                HasChildren = hasChildren,
            });
            return ret;
        }

        public int TypeIndexSize => MapleCodeCompiler.GetByteSize(_entries.Count);

        public byte[] Generate(int stringSize, int dataSize)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, true);

            foreach (var e in _entries)
            {
                bw.Write((uint)e.Name);
                ms.Seek(stringSize - 4, SeekOrigin.Current);
                bw.Write(_dataSection.GetGeneratedOffset(e.ArgsDataPos));
                ms.Seek(dataSize - 4, SeekOrigin.Current);
                bw.Write(e.GenericCount);
                bw.Write((byte)(e.HasChildren ? 1 : 0));
            }

            ms.SetLength(ms.Position);
            return ms.ToArray();
        }
    }
}
