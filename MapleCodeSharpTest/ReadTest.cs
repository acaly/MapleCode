using MapleCodeSharp.Builder;
using MapleCodeSharp.Reader;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MapleCodeSharpTest
{
    public class ReadTest
    {
        [Fact]
        public void ReadEmpty()
        {
            Document.ReadFromData(new DocumentBuilder(1, 1, 1, 1).Generate());
        }

        [Fact]
        public void ReadTypeTable()
        {
            Document doc;
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                var func1 = builder.TypeTable.AddType("func1", 0, false, NodeArgumentTypeValues.U8);
                var func2 = builder.TypeTable.AddType("func2", 0, true, NodeArgumentTypeValues.U8, NodeArgumentTypeValues.U8);
                var func3 = builder.TypeTable.AddType("func3", 2, false, NodeArgumentTypeValues.STR);
                doc = Document.ReadFromData(builder.Generate());
            }
            {
                var func1 = doc.GetNodeType(0);
                Assert.Equal("func1", func1.Name);
                Assert.Equal(0, func1.GenericArgCount);
                Assert.False(func1.HasChildren);
                Assert.Equal("U8", Assert.Single(func1.ArgumentTypes).Name);

                var func2 = doc.GetNodeType(1);
                Assert.Equal("func2", func2.Name);
                Assert.Equal(0, func2.GenericArgCount);
                Assert.True(func2.HasChildren);
                Assert.Equal(new[] { "U8", "U8" }, func2.ArgumentTypes.Select(a => a.Name));

                var func3 = doc.GetNodeType(2);
                Assert.Equal("func3", func3.Name);
                Assert.Equal(2, func3.GenericArgCount);
                Assert.False(func3.HasChildren);
                Assert.Equal("STR", Assert.Single(func3.ArgumentTypes).Name);
            }
        }

        [Fact]
        public void ReadSimpleNodes()
        {
            var doc = Document.ReadFromStream(typeof(ReadTest).Assembly
                .GetManifestResourceStream("MapleCodeSharpTest.TestFiles.SimpleNodes.dat"));

            var root = doc.AllNodes.ToArray();
            Assert.Equal(3, root.Length);

            var node1 = root[0];
            Assert.Equal("node_a", node1.NodeType.Name);
            Assert.Equal(10u, Assert.Single(node1.ReadArguments()).GetUnsigned());

            var node2 = root[1];
            Assert.Equal("node_b", node2.NodeType.Name);
            Assert.Equal(3, node2.ReadArguments().Length);
            Assert.Equal(-1, node2.ReadArguments()[0].GetSigned());
            Assert.Equal("string", node2.ReadArguments()[1].GetString());
            Assert.Equal(0.1f, node2.ReadArguments()[2].GetFloat());

            var node3 = root[2];
            Assert.Equal("node_c", node3.NodeType.Name);
            Assert.Equal(new[] { "t1", "t2" }, node3.ReadGenericArgs());
            Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, Assert.Single(node3.ReadArguments()).GetData());
        }

        [Fact]
        public void ReadChildren()
        {
            var doc = Document.ReadFromStream(typeof(ReadTest).Assembly
                .GetManifestResourceStream("MapleCodeSharpTest.TestFiles.Children.dat"));

            var root = doc.AllNodes.ToArray();
            var n1 = Assert.Single(root);
            var endNode = doc.AllNodes.End;

            Assert.Equal("node_a", n1.NodeType.Name);
            var n1c = n1.Children.ToArray();
            Assert.Equal(2, n1c.Length);
            Assert.Equal(endNode, n1.Next);
            Assert.True(n1.FindParentNode().IsNull);

            var n11 = n1c[0];
            Assert.Equal("node_b", n11.NodeType.Name);
            Assert.Empty(n11.Children);
            Assert.Equal(n1c[1], n11.Next);
            Assert.Equal(n1, n11.FindParentNode());

            var n12 = n1c[1];
            Assert.Equal("node_a", n12.NodeType.Name);
            var n12c = n12.Children.ToArray();
            Assert.Equal(2, n12c.Length);
            Assert.Equal(endNode, n12.Next);
            Assert.Equal(n1, n12.FindParentNode());

            var n121 = n12c[0];
            Assert.Equal("node_a", n121.NodeType.Name);
            var n1211 = Assert.Single(n121.Children);
            Assert.Equal(n12c[1], n121.Next);
            Assert.Equal(n12, n121.FindParentNode());

            Assert.Equal("node_b", n1211.NodeType.Name);
            Assert.Empty(n1211.Children);
            Assert.Equal(n12c[1], n1211.Next);
            Assert.Equal(n121, n1211.FindParentNode());

            var n122 = n12c[1];
            Assert.Equal("node_b", n122.NodeType.Name);
            Assert.Empty(n122.Children);
            Assert.Equal(endNode, n122.Next);
            Assert.Equal(n12, n122.FindParentNode());
        }

        [Fact]
        public void ReadReference()
        {
            var doc = Document.ReadFromStream(typeof(ReadTest).Assembly
                .GetManifestResourceStream("MapleCodeSharpTest.TestFiles.Reference.dat"));

            var nodes = doc.AllNodes.ToArray();
            Assert.Equal(2, nodes.Length);

            var n1 = nodes[0];
            var n2 = nodes[1];
            var n1c = n1.ReadArguments();
            var n2c = n2.ReadArguments();

            Assert.Equal(n1, n1c[0].GetNode());
            Assert.Equal(n2, n1c[1].GetNode());
            Assert.Equal("x", n1c[1].GetField());

            Assert.Equal(n1, n2c[0].GetNode());
            Assert.Equal(n2, n2c[1].GetNode());
            Assert.Equal("y", n2c[1].GetField());
        }
    }
}
