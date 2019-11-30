using MapleCodeSharp.Builder;
using MapleCodeSharp.Compiler;
using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MapleCodeSharpTest
{
    public class ExternalTypeListTest
    {
        [Fact]
        public void Compile()
        {
            var code = "r1: node1 0u8, 1f, data hex { 00 a0 00 ef } { node2<t> r1.x; }";
            Document doc1, docTypes, doc2;
            doc1 = Document.ReadFromData(MapleCodeCompiler.Compile(code));
            ExternalNodeTypeList typeList = new ExternalNodeTypeList();
            {
                var builder = new DocumentBuilder(1, 1, 1, 1);
                var n1 = builder.TypeTable.AddType("node1", 0, true,
                    NodeArgumentTypeValues.U8, NodeArgumentTypeValues.F32, NodeArgumentTypeValues.DAT);
                var n2 = builder.TypeTable.AddType("node2", 1, false, NodeArgumentTypeValues.REFFIELD);
                docTypes = Document.ReadFromData(builder.Generate());
                typeList.Add(n1, "node1", 0, new byte[]
                    { NodeArgumentTypeValues.U8, NodeArgumentTypeValues.F32, NodeArgumentTypeValues.DAT }, true);
                typeList.Add(n2, "node2", 1, new byte[] { NodeArgumentTypeValues.REFFIELD }, false);
            }
            doc2 = Document.ReadFromData(MapleCodeCompiler.Compile(typeList, code), docTypes);
            Assert.True(DocumentComparer.Compare(doc1, doc2));
        }
    }
}
