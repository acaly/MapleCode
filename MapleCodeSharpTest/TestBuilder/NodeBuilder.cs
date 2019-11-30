using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharpTest.TestBuilder
{
    class NodeBuilder
    {
        private readonly DocumentBuilder _builder;

        public NodeBuilder(DocumentBuilder documentBuilder)
        {
            _builder = documentBuilder;
        }

        public int WriteNode(int type, params ArgumentBuilder[] args)
        {
            return WriteNode(type, new string[0], args);
        }

        public int WriteNode(int type, string[] generics, params ArgumentBuilder[] args)
        {
            var ret = _builder.NodeData.AppendRaw(new byte[0]);
            _builder.NodeData.AppendNumber(type, _builder.SizeType);
            foreach (var g in generics)
            {
                _builder.NodeData.AppendNumber(_builder.String.AddString(g), _builder.SizeStr);
            }
            var types = _builder.Type.GetArgTypesForType(type);
            for (int i = 0; i < args.Length; ++i)
            {
                args[i].Generate(_builder, types[i]);
            }
            return ret;
        }

        public void WriteChildrenLength()
        {
            _builder.NodeData.AppendNumber(0, _builder.SizeNode);
        }

        public void FixChildrenLength(int pos)
        {
            var end = _builder.NodeData.AppendRaw(new byte[0]);
            var len = end - pos - _builder.SizeNode;
            _builder.NodeData.Fix(pos, len, _builder.SizeNode);
        }
    }
}
