using MapleCodeSharp.Builder;
using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MapleCodeSharpTest
{
    public class BuildTest
    {
        [Fact]
        public void BuildSimpleNodes()
        {
            var builder = new DocumentBuilder(1, 1, 1, 1);
            var func1 = builder.TypeTable.AddType("node_a", 0, false, NodeArgumentTypeValues.U32);
            var func2 = builder.TypeTable.AddType("node_b", 0, false,
                NodeArgumentTypeValues.S8, NodeArgumentTypeValues.STR, NodeArgumentTypeValues.F32);
            var func3 = builder.TypeTable.AddType("node_c", 2, false, NodeArgumentTypeValues.DAT);
            builder.NodeSection.WriteNode(func1, 10u);
            builder.NodeSection.WriteNode(func2, -1, "string", 0.1f);
            builder.NodeSection.WriteNode(func3, new[] { "t1", "t2" }, new byte[] { 0, 1, 2, 3, 4 });
            var doc1 = Document.ReadFromData(builder.Generate());

            var doc2 = Document.ReadFromStream(typeof(BuildTest).Assembly
                .GetManifestResourceStream("MapleCodeSharpTest.TestFiles.SimpleNodes.dat"));
            Assert.True(DocumentComparer.Compare(doc1, doc2));
        }

        [Fact]
        public void BuildChilren()
        {
            var builder = new DocumentBuilder(1, 1, 1, 1);
            var type1 = builder.TypeTable.AddType("node_a", 0, true);
            var type2 = builder.TypeTable.AddType("node_b", 0, false);
            builder.NodeSection.WriteNode(type1); //n1
            var len1 = builder.NodeSection.StartChildrenList(); //start n1
            {
                builder.NodeSection.WriteNode(type2); //n11
                builder.NodeSection.WriteNode(type1); //n12
                var len2 = builder.NodeSection.StartChildrenList(); //start n12
                {
                    builder.NodeSection.WriteNode(type1); //n121
                    var len3 = builder.NodeSection.StartChildrenList(); //start n121
                    {
                        builder.NodeSection.WriteNode(type2); //n1211
                    }
                    builder.NodeSection.EndChildrenList(len3); //end n121
                    builder.NodeSection.WriteNode(type2); //n122
                }
                builder.NodeSection.EndChildrenList(len2); //end n12
            }
            builder.NodeSection.EndChildrenList(len1); //end n1
            var doc1 = Document.ReadFromData(builder.Generate());

            var doc2 = Document.ReadFromStream(typeof(BuildTest).Assembly
                .GetManifestResourceStream("MapleCodeSharpTest.TestFiles.Children.dat"));
            Assert.True(DocumentComparer.Compare(doc1, doc2));
        }

        [Fact]
        public void BuildReference()
        {
            var builder = new DocumentBuilder(1, 1, 1, 1);
            var t = builder.TypeTable.AddType("n", 0, false,
                NodeArgumentTypeValues.REF, NodeArgumentTypeValues.REFFIELD);

            var n1a1 = ArgumentBuilder.NewRefArg();
            var n1a2 = ArgumentBuilder.NewRefArg("x");
            var n1 = builder.NodeSection.WriteNode(t, n1a1, n1a2);

            var n2a1 = ArgumentBuilder.NewRefArg();
            var n2a2 = ArgumentBuilder.NewRefArg("y");
            var n2 = builder.NodeSection.WriteNode(t, n2a1, n2a2);

            builder.NodeSection.FixReference(n1a1, n1);
            builder.NodeSection.FixReference(n1a2, n2);
            builder.NodeSection.FixReference(n2a1, n1);
            builder.NodeSection.FixReference(n2a2, n2);

            var doc1 = Document.ReadFromData(builder.Generate());

            var doc2 = Document.ReadFromStream(typeof(BuildTest).Assembly
                .GetManifestResourceStream("MapleCodeSharpTest.TestFiles.Reference.dat"));
            Assert.True(DocumentComparer.Compare(doc1, doc2));
        }
    }
}
