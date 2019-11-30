using MapleCodeSharp.Compiler;
using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace MapleCodeSharpTest
{
    public class CompilerTest
    {
        private static Document ReadDocument(params byte[] data)
        {
            using var s = new MemoryStream(data);
            return Document.ReadFromStream(s);
        }

        [Fact]
        public void CompileEmpty()
        {
            var str = "";
            var data = MapleCodeCompiler.Compile(str);

            var doc = ReadDocument(data);
            Assert.Empty(doc.AllNodes.ToArray());
        }

        [Fact]
        public void CompileNumber()
        {
            var str = "n1 0; n2 1; n3 -2; n4 0.5; n5 -0.25;";
            var data = MapleCodeCompiler.Compile(str);

            var doc = ReadDocument(data);
            var nodes = doc.AllNodes.ToArray();
            Assert.Equal(5, nodes.Length);

            Assert.Equal("n1", nodes[0].NodeType.Name);
            Assert.Equal("U32", Assert.Single(nodes[0].NodeType.ArgumentTypes).Name);
            Assert.Equal(0u, nodes[0].ReadArguments()[0].GetUnsigned());

            Assert.Equal("n2", nodes[1].NodeType.Name);
            Assert.Equal("U32", Assert.Single(nodes[1].NodeType.ArgumentTypes).Name);
            Assert.Equal(1u, nodes[1].ReadArguments()[0].GetUnsigned());

            Assert.Equal("n3", nodes[2].NodeType.Name);
            Assert.Equal("S32", Assert.Single(nodes[2].NodeType.ArgumentTypes).Name);
            Assert.Equal(-2, nodes[2].ReadArguments()[0].GetSigned());

            Assert.Equal("n4", nodes[3].NodeType.Name);
            Assert.Equal("F32", Assert.Single(nodes[3].NodeType.ArgumentTypes).Name);
            Assert.Equal(0.5f, nodes[3].ReadArguments()[0].GetFloat());

            Assert.Equal("n5", nodes[4].NodeType.Name);
            Assert.Equal("F32", Assert.Single(nodes[4].NodeType.ArgumentTypes).Name);
            Assert.Equal(-0.25f, nodes[4].ReadArguments()[0].GetFloat());
        }

        [Fact]
        public void CompileNumberPostfix()
        {
            var str = "n1 1u8, 2u16, 3u32; n2 4s8, 5s16, 6s32; n3 7f;";
            var data = MapleCodeCompiler.Compile(str);

            var doc = ReadDocument(data);
            var nodes = doc.AllNodes.ToArray();
            Assert.Equal(3, nodes.Length);

            Assert.Equal("n1", nodes[0].NodeType.Name);
            Assert.Equal(3, nodes[0].NodeType.ArgumentTypes.Count);
            Assert.Equal("U8", nodes[0].NodeType.ArgumentTypes[0].Name);
            Assert.Equal("U16", nodes[0].NodeType.ArgumentTypes[1].Name);
            Assert.Equal("U32", nodes[0].NodeType.ArgumentTypes[2].Name);
            Assert.Equal(1u, nodes[0].ReadArguments()[0].GetUnsigned());
            Assert.Equal(2u, nodes[0].ReadArguments()[1].GetUnsigned());
            Assert.Equal(3u, nodes[0].ReadArguments()[2].GetUnsigned());

            Assert.Equal("n2", nodes[1].NodeType.Name);
            Assert.Equal(3, nodes[1].NodeType.ArgumentTypes.Count);
            Assert.Equal("S8", nodes[1].NodeType.ArgumentTypes[0].Name);
            Assert.Equal("S16", nodes[1].NodeType.ArgumentTypes[1].Name);
            Assert.Equal("S32", nodes[1].NodeType.ArgumentTypes[2].Name);
            Assert.Equal(4, nodes[1].ReadArguments()[0].GetSigned());
            Assert.Equal(5, nodes[1].ReadArguments()[1].GetSigned());
            Assert.Equal(6, nodes[1].ReadArguments()[2].GetSigned());

            Assert.Equal("n3", nodes[2].NodeType.Name);
            Assert.Equal("F32", Assert.Single(nodes[2].NodeType.ArgumentTypes).Name);
            Assert.Equal(7f, Assert.Single(nodes[2].ReadArguments()).GetFloat());
        }

        [Fact]
        public void CompileData()
        {
            var str = "n1 data u8 { 1, 2 }, data u16 { 3, 4 }, data u32 { 5, 6 }; " +
                "n2 data s8 { 0, -1 }, data s16 { -2, 3 }, data s32 { 4, -5 }; " +
                "n3 data f32 { 0, 1, -0.25, 1E-8 }; " +
                "n4 data hex { 00 11 2233 ffeeddcc F0 };";
            var data = MapleCodeCompiler.Compile(str);

            var doc = ReadDocument(data);
            var nodes = doc.AllNodes.ToArray();
            Assert.Equal(4, nodes.Length);

            var n1 = nodes[0];
            Assert.Equal("n1", n1.NodeType.Name);
            Assert.Equal(3, n1.NodeType.ArgumentTypes.Count);
            Assert.Equal("DAT", n1.NodeType.ArgumentTypes[0].Name);
            Assert.Equal("DAT", n1.NodeType.ArgumentTypes[1].Name);
            Assert.Equal("DAT", n1.NodeType.ArgumentTypes[2].Name);
            var args1 = n1.ReadArguments();
            Assert.Equal(new byte[] { 1, 2 }, args1[0].GetData());
            Assert.Equal(new byte[] { 3, 0, 4, 0 }, args1[1].GetData());
            Assert.Equal(new byte[] { 5, 0, 0, 0, 6, 0, 0, 0 }, args1[2].GetData());

            var n2 = nodes[1];
            Assert.Equal("n2", n2.NodeType.Name);
            Assert.Equal(3, n2.NodeType.ArgumentTypes.Count);
            Assert.Equal("DAT", n2.NodeType.ArgumentTypes[0].Name);
            Assert.Equal("DAT", n2.NodeType.ArgumentTypes[1].Name);
            Assert.Equal("DAT", n2.NodeType.ArgumentTypes[2].Name);
            var args2 = n2.ReadArguments();
            Assert.Equal(new byte[] { 0, 255 }, args2[0].GetData());
            Assert.Equal(new byte[] { 254, 255, 3, 0 }, args2[1].GetData());
            Assert.Equal(new byte[] { 4, 0, 0, 0, 251, 255, 255, 255 }, args2[2].GetData());

            var n3 = nodes[2];
            Assert.Equal("n3", n3.NodeType.Name);
            Assert.Single(n3.NodeType.ArgumentTypes);
            var args3dat = n3.ReadArguments()[0].GetData();
            Assert.Equal(16, args3dat.Length);
            Assert.Equal(0f, BitConverter.ToSingle(args3dat, 0));
            Assert.Equal(1f, BitConverter.ToSingle(args3dat, 4));
            Assert.Equal(-0.25f, BitConverter.ToSingle(args3dat, 8));
            Assert.Equal(1E-8f, BitConverter.ToSingle(args3dat, 12));

            var n4 = nodes[3];
            Assert.Equal("n4", n4.NodeType.Name);
            Assert.Single(n4.NodeType.ArgumentTypes);
            var args4dat = n4.ReadArguments()[0].GetData();
            Assert.Equal(new byte[] { 0x00, 0x11, 0x22, 0x33, 0xff, 0xee, 0xdd, 0xcc, 0xF0 }, args4dat);
        }

        [Fact]
        public void CompileRef()
        {
            var str = "r1: n1 0, r2, r2.x; r2: n1 1, r1, r1.y; r3: n1 2, r3, r3.z;";
            var data = MapleCodeCompiler.Compile(str);

            var doc = ReadDocument(data);
            var nodes = doc.AllNodes.ToArray();
            Assert.Equal(3, nodes.Length);

            var n1 = nodes[0];
            var n2 = nodes[1];
            var n3 = nodes[2];

            var args1 = n1.ReadArguments();
            Assert.Equal(0u, args1[0].GetUnsigned());
            Assert.Equal("REF", n1.NodeType.ArgumentTypes[1].Name);
            Assert.Equal(n2, args1[1].GetNode());
            Assert.Equal("REFFIELD", n1.NodeType.ArgumentTypes[2].Name);
            Assert.Equal(n2, args1[2].GetNode());
            Assert.Equal("x", args1[2].GetField());

            var args2 = n2.ReadArguments();
            Assert.Equal(1u, args2[0].GetUnsigned());
            Assert.Equal("REF", n2.NodeType.ArgumentTypes[1].Name);
            Assert.Equal(n1, args2[1].GetNode());
            Assert.Equal("REFFIELD", n2.NodeType.ArgumentTypes[2].Name);
            Assert.Equal(n1, args2[2].GetNode());
            Assert.Equal("y", args2[2].GetField());

            var args3 = n3.ReadArguments();
            Assert.Equal(2u, args3[0].GetUnsigned());
            Assert.Equal("REF", n3.NodeType.ArgumentTypes[1].Name);
            Assert.Equal(n3, args3[1].GetNode());
            Assert.Equal("REFFIELD", n3.NodeType.ArgumentTypes[2].Name);
            Assert.Equal(n3, args3[1].GetNode());
            Assert.Equal("z", args3[2].GetField());
        }

        [Fact]
        public void CompileChild()
        {
            var str = "n1 { n2 { n3; n4 { n5; n6 {} n7; } } n8; } n9;";
            var data = MapleCodeCompiler.Compile(str);

            var doc = ReadDocument(data);
            var nodes = doc.AllNodes.ToArray();
            Assert.Equal(2, nodes.Length);

            var n1 = nodes[0];
            Assert.Equal("n1", n1.NodeType.Name);
            Assert.True(n1.NodeType.HasChildren);
            var n1c = n1.Children.ToArray();
            Assert.Equal(2, n1c.Length);

            var n2 = n1c[0];
            Assert.Equal("n2", n2.NodeType.Name);
            Assert.True(n2.NodeType.HasChildren);
            var n2c = n2.Children.ToArray();
            Assert.Equal(2, n2c.Length);

            var n3 = n2c[0];
            Assert.Equal("n3", n3.NodeType.Name);
            Assert.False(n3.NodeType.HasChildren);
            Assert.Empty(n3.Children);

            var n4 = n2c[1];
            Assert.Equal("n4", n4.NodeType.Name);
            Assert.True(n4.NodeType.HasChildren);
            var n4c = n4.Children.ToArray();
            Assert.Equal(3, n4c.Length);

            var n5 = n4c[0];
            Assert.Equal("n5", n5.NodeType.Name);
            Assert.False(n5.NodeType.HasChildren);
            Assert.Empty(n5.Children);

            var n6 = n4c[1];
            Assert.Equal("n6", n6.NodeType.Name);
            Assert.True(n6.NodeType.HasChildren);
            Assert.Empty(n6.Children);

            var n7 = n4c[2];
            Assert.Equal("n7", n7.NodeType.Name);
            Assert.False(n7.NodeType.HasChildren);
            Assert.Empty(n7.Children);

            var n8 = n1c[1];
            Assert.Equal("n8", n8.NodeType.Name);
            Assert.False(n8.NodeType.HasChildren);
            Assert.Empty(n8.Children);

            var n9 = nodes[1];
            Assert.Equal("n9", n9.NodeType.Name);
            Assert.False(n9.NodeType.HasChildren);
            Assert.Empty(n9.Children);
        }
    }
}

//TODO compile without type list
//TODO array
