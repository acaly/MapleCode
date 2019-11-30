using MapleCodeSharp.Reader;
using MapleCodeSharpTest.TestBuilder;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MapleCodeSharpTest
{
    public class DocumentReadTest
    {
        private static Document ReadDocument(params byte[] data)
        {
            using var s = new MemoryStream(data);
            return Document.ReadFromStream(s);
        }

        [Fact]
        public void ReadEmpty()
        {
            ReadDocument(new DocumentBuilder(1, 1, 1, 1).Generate());
        }

        [Fact]
        public void ReadStringTable()
        {
            Document doc;
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                builder.String.AddString("Hello");
                builder.String.AddString("World");
                builder.String.AddString("!");
                builder.String.AddString("  ");
                builder.String.AddString("");
                doc = ReadDocument(builder.Generate());
            }
            {
                Assert.Equal(0, doc.LookupStringTable("Hello"));
                Assert.Equal(1, doc.LookupStringTable("World"));
                Assert.Equal(2, doc.LookupStringTable("!"));
                Assert.Equal(3, doc.LookupStringTable("  "));
                Assert.Equal(4, doc.LookupStringTable(""));
                Assert.Equal(-1, doc.LookupStringTable(" "));
            }
        }

        [Fact]
        public void ReadTypeTable()
        {
            Document doc;
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                var func1 = builder.Type.AddType("func1", 0, false, NodeArgumentTypeValues.U8);
                var func2 = builder.Type.AddType("func2", 0, true, NodeArgumentTypeValues.U8, NodeArgumentTypeValues.U8);
                var func3 = builder.Type.AddType("func3", 2, false, NodeArgumentTypeValues.STR);
                doc = ReadDocument(builder.Generate());
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
        public void ReadSimpleNode()
        {
            Document doc;
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                var func1 = builder.Type.AddType("node_a", 0, false, NodeArgumentTypeValues.U32);
                var func2 = builder.Type.AddType("node_b", 0, false,
                    NodeArgumentTypeValues.S8, NodeArgumentTypeValues.STR, NodeArgumentTypeValues.F32);
                var func3 = builder.Type.AddType("node_c", 0, false, NodeArgumentTypeValues.DAT);
                builder.Node.WriteNode(func1, 10u);
                builder.Node.WriteNode(func2, -1, "string", 0.1f);
                builder.Node.WriteNode(func3, new byte[] { 0, 1, 2, 3, 4 });
                doc = ReadDocument(builder.Generate());
            }
            {
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
                Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, Assert.Single(node3.ReadArguments()).GetData());
            }
        }

        [Fact]
        public void ReadChildren()
        {
            Document doc;
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                var type1 = builder.Type.AddType("node_a", 0, true);
                var type2 = builder.Type.AddType("node_b", 0, false);
                builder.Node.WriteNode(type1);
                var len1 = builder.Node.WriteChildrenLength();
                {
                    builder.Node.WriteNode(type2);
                    builder.Node.WriteNode(type1);
                    var len2 = builder.Node.WriteChildrenLength();
                    {
                        builder.Node.WriteNode(type1);
                        var len3 = builder.Node.WriteChildrenLength();
                        {
                            builder.Node.WriteNode(type2);
                        }
                        builder.Node.FixChildrenLength(len3);
                        builder.Node.WriteNode(type2);
                    }
                    builder.Node.FixChildrenLength(len2);
                }
                builder.Node.FixChildrenLength(len1);
                doc = ReadDocument(builder.Generate());
            }
            {
                var root = doc.AllNodes.ToArray();
                var n1 = Assert.Single(root);

                Assert.Equal("node_a", n1.NodeType.Name);
                var n1c = n1.Children.ToArray();
                Assert.Equal(2, n1c.Length);

                var n11 = n1c[0];
                Assert.Equal("node_b", n11.NodeType.Name);
                Assert.Empty(n11.Children);

                var n12 = n1c[1];
                Assert.Equal("node_a", n12.NodeType.Name);
                var n12c = n12.Children.ToArray();
                Assert.Equal(2, n12c.Length);

                var n121 = n12c[0];
                Assert.Equal("node_a", n121.NodeType.Name);
                var n1211 = Assert.Single(n121.Children);
                Assert.Equal("node_b", n1211.NodeType.Name);
                Assert.Empty(n1211.Children);

                var n122 = n12c[1];
                Assert.Equal("node_b", n122.NodeType.Name);
                Assert.Empty(n122.Children);
            }
        }
    }
}

//TODO test
//  node hierarchy (Next, Children, FindParent)
//  read ref, reffield
//  multibyte index
