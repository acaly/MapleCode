using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapleCodeSharpTest.TestBuilder
{
    class DocumentBuilder
    {
        private readonly int _sizeMode;
        public DataSectionBuilder Data { get; }
        public StringTableBuilder String { get; }
        public TypeTableBuilder Type { get; }
        public DataSectionBuilder NodeData { get; }
        public NodeBuilder Node { get; }

        public readonly int SizeStr, SizeType, SizeNode, SizeData;

        public DocumentBuilder(int sizeStr, int sizeType, int sizeNode, int sizeData)
        {
            SizeStr = sizeStr;
            SizeType = sizeType;
            SizeNode = sizeNode;
            SizeData = sizeData;

            _sizeMode = sizeStr | sizeType << 2 | sizeNode << 4 | sizeData << 6;

            Data = new DataSectionBuilder();
            String = new StringTableBuilder(Data, sizeData);
            Type = new TypeTableBuilder(sizeStr, sizeData, String.AddString, Data);
            NodeData = new DataSectionBuilder();
            Node = new NodeBuilder(this);
        }

        public byte[] Generate()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            var str = String.Generate();
            var type = Type.Generate();
            var node = NodeData.Generate();
            var data = Data.Generate();

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
