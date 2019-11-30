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
                var func3 = builder.Type.AddType("node_c", 2, false, NodeArgumentTypeValues.DAT);
                builder.Node.WriteNode(func1, 10u);
                builder.Node.WriteNode(func2, -1, "string", 0.1f);
                builder.Node.WriteNode(func3, new[] { "t1", "t2" }, new byte[] { 0, 1, 2, 3, 4 });
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
                Assert.Equal(new[] { "t1", "t2" }, node3.ReadGenericArgs());
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
                builder.Node.WriteNode(type1); //n1
                var len1 = builder.Node.WriteChildrenLength(); //start n1
                {
                    builder.Node.WriteNode(type2); //n11
                    builder.Node.WriteNode(type1); //n12
                    var len2 = builder.Node.WriteChildrenLength(); //start n12
                    {
                        builder.Node.WriteNode(type1); //n121
                        var len3 = builder.Node.WriteChildrenLength(); //start n121
                        {
                            builder.Node.WriteNode(type2); //n1211
                        }
                        builder.Node.FixChildrenLength(len3); //end n121
                        builder.Node.WriteNode(type2); //n122
                    }
                    builder.Node.FixChildrenLength(len2); //end n12
                }
                builder.Node.FixChildrenLength(len1); //end n1
                doc = ReadDocument(builder.Generate());
            }
            {
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
        }

        [Fact]
        public void ReadRef()
        {
            Document doc;
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                var t = builder.Type.AddType("n", 0, false,
                    NodeArgumentTypeValues.REF, NodeArgumentTypeValues.REFFIELD);

                var n1a1 = ArgumentBuilder.NewRefArg();
                var n1a2 = ArgumentBuilder.NewRefArg("x");
                var n1 = builder.Node.WriteNode(t, n1a1, n1a2);

                var n2a1 = ArgumentBuilder.NewRefArg();
                var n2a2 = ArgumentBuilder.NewRefArg("y");
                var n2 = builder.Node.WriteNode(t, n2a1, n2a2);

                n1a1.FixNodeRef(builder, n1);
                n1a2.FixNodeRef(builder, n2);
                n2a1.FixNodeRef(builder, n1);
                n2a2.FixNodeRef(builder, n2);

                doc = ReadDocument(builder.Generate());
            }
            {
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

        private void TestElementSize(int str, int type, int node, int data)
        {
            Document doc;
            {
                var builder = new DocumentBuilder(str, type, node, data);

                var t1 = builder.Type.AddType("t1", 1, true, NodeArgumentTypeValues.U16, NodeArgumentTypeValues.REF);
                var t2 = builder.Type.AddType("t2", 0, false, NodeArgumentTypeValues.STR, NodeArgumentTypeValues.DAT);

                var r = ArgumentBuilder.NewRefArg();
                builder.Node.WriteNode(t1, new[] { "t" }, 0u, r);
                var c1 = builder.Node.WriteChildrenLength();
                var n2 = builder.Node.WriteNode(t2, "string", new byte[] { 1, 2, 3 });
                r.FixNodeRef(builder, n2);
                builder.Node.FixChildrenLength(c1);

                doc = ReadDocument(builder.Generate());
            }
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
    }
}
