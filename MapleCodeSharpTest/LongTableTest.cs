using MapleCodeSharp.Builder;
using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MapleCodeSharpTest
{
    public class LongTableTest
    {
        private static Document ReadDocument(params byte[] data)
        {
            using var s = new MemoryStream(data);
            return Document.ReadFromStream(s);
        }

        private Document CreateElementSizeDocument(int str, int type, int node, int data)
        {
            var builder = new DocumentBuilder(str, type, node, data);

            var t1 = builder.TypeTable.AddType("t1", 1, true, NodeArgumentTypeValues.U16, NodeArgumentTypeValues.REF);
            var t2 = builder.TypeTable.AddType("t2", 0, false, NodeArgumentTypeValues.STR, NodeArgumentTypeValues.DAT);

            var r = ArgumentBuilder.NewRefArg();
            builder.NodeSection.WriteNode(t1, new[] { "t" }, 0u, r);
            var c1 = builder.NodeSection.StartChildrenList();
            var n2 = builder.NodeSection.WriteNode(t2, "string", new byte[] { 1, 2, 3 });
            builder.NodeSection.FixReference(r, n2);
            builder.NodeSection.EndChildrenList(c1);

            return ReadDocument(builder.Generate());
        }

        private void CheckElementSizeDocument(Document doc)
        {
            var nodes = doc.AllNodes;
            var n1 = Assert.Single(nodes);

            Assert.Equal("t1", n1.NodeType.Name);
            Assert.Equal(new[] { "t" }, n1.ReadGenericArgs());
            Assert.True(n1.NodeType.HasChildren);
            var n2 = Assert.Single(n1.Children);
            var n1a = n1.ReadArguments();
            Assert.Equal(0u, n1a[0].GetUnsigned());
            Assert.Equal(n2, n1a[1].GetNode());

            Assert.Equal("t2", n2.NodeType.Name);
            Assert.Empty(n2.ReadGenericArgs());
            Assert.False(n2.NodeType.HasChildren);
            var n2a = n2.ReadArguments();
            Assert.Equal("string", n2a[0].GetString());
            Assert.Equal(new byte[] { 1, 2, 3 }, n2a[1].GetData());
        }

        private void TestElementSize(int str, int type, int node, int data)
        {
            var doc = CreateElementSizeDocument(str, type, node, data);
            CheckElementSizeDocument(doc);
        }

        [Fact]
        public void ReadLongTable()
        {
            TestElementSize(2, 1, 1, 1);
            TestElementSize(4, 1, 1, 1);
            TestElementSize(1, 2, 1, 1);
            TestElementSize(1, 4, 1, 1);
            TestElementSize(1, 1, 2, 1);
            TestElementSize(1, 1, 4, 1);
            TestElementSize(1, 1, 1, 2);
            TestElementSize(1, 1, 1, 4);
            TestElementSize(2, 2, 2, 2);
            TestElementSize(4, 4, 4, 4);
        }

        [Fact]
        public void CompareLongTable()
        {
            var doc1111 = CreateElementSizeDocument(1, 1, 1, 1);
            var doc2222 = CreateElementSizeDocument(2, 2, 2, 2);
            var doc1124 = CreateElementSizeDocument(1, 1, 2, 4);
            Assert.True(DocumentComparer.Compare(doc1111, doc2222));
            Assert.True(DocumentComparer.Compare(doc1111, doc1124));
            Assert.True(DocumentComparer.Compare(doc2222, doc1124));
        }
    }
}
