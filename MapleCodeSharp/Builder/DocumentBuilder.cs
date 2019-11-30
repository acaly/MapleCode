using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapleCodeSharp.Builder
{
    public sealed class DocumentBuilder
    {
        private readonly int _sizeMode;
        public DataSectionBuilder DataSection { get; }
        public StringTableBuilder StringTable { get; }
        public TypeTableBuilder TypeTable { get; }
        internal DataSectionBuilder NodeData { get; }
        public NodeBuilder NodeSection { get; }

        internal readonly int SizeStr, SizeType, SizeNode, SizeData;
        private static readonly int[] SizeModes = new[] { 0, 1, 2, 0, 3 };

        private static bool CheckSize(int s)
        {
            if (s < 1 || s > 4 || s == 3) return false;
            return true;
        }

        public DocumentBuilder(int sizeStr, int sizeType, int sizeNode, int sizeData)
        {
            if (!CheckSize(sizeStr))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeStr));
            }
            if (!CheckSize(sizeType))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeType));
            }
            if (!CheckSize(sizeNode))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeNode));
            }
            if (!CheckSize(sizeData))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeData));
            }

            SizeStr = sizeStr;
            SizeType = sizeType;
            SizeNode = sizeNode;
            SizeData = sizeData;

            _sizeMode = SizeModes[sizeStr] | SizeModes[sizeType] << 2 |
                SizeModes[sizeNode] << 4 | SizeModes[sizeData] << 6;

            //Assume MemoryStream is safe to be left not disposed.
            DataSection = new DataSectionBuilder(new MemoryStream());
            NodeData = new DataSectionBuilder(new MemoryStream());

            StringTable = new StringTableBuilder(DataSection, sizeData);
            TypeTable = new TypeTableBuilder(sizeStr, sizeData, StringTable.AddString, DataSection);
            NodeSection = new NodeBuilder(this);
        }

        public byte[] Generate()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            var str = StringTable.Generate();
            var type = TypeTable.Generate();
            var node = NodeData.Generate();
            var data = DataSection.Generate();

            bw.Write((byte)_sizeMode);
            bw.Write(str.Length);
            ms.Seek(SizeStr - 4, SeekOrigin.Current);
            bw.Write(type.Length);
            ms.Seek(SizeType - 4, SeekOrigin.Current);
            bw.Write(node.Length);
            ms.Seek(SizeNode - 4, SeekOrigin.Current);
            bw.Write(data.Length);
            ms.Seek(SizeData - 4, SeekOrigin.Current);
            ms.SetLength(ms.Position);

            bw.Write(str);
            bw.Write(type);
            bw.Write(node);
            bw.Write(data);

            return ms.ToArray();
        }
    }
}
