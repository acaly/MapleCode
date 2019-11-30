using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Builder
{
    public sealed class NodeBuilder
    {
        private readonly DocumentBuilder _builder;

        public NodeBuilder(DocumentBuilder documentBuilder)
        {
            _builder = documentBuilder;
        }

        public NodePosition WriteNode(int type, params ArgumentBuilder[] args)
        {
            return WriteNode(type, Array.Empty<string>(), args);
        }

        public NodePosition WriteNode(int type, string[] generics, params ArgumentBuilder[] args)
        {
            if (generics == null)
            {
                throw new ArgumentNullException(nameof(generics));
            }
            var ret = _builder.NodeData.AppendRaw(Array.Empty<byte>());
            _builder.NodeData.AppendNumber(type, _builder.SizeType);
            foreach (var g in generics)
            {
                _builder.NodeData.AppendNumber(_builder.StringTable.AddString(g), _builder.SizeStr);
            }
            var types = _builder.TypeTable.GetArgTypesForType(type);
            for (int i = 0; i < args.Length; ++i)
            {
                args[i].Generate(_builder, types[i]);
            }
            return new NodePosition(ret);
        }

        public ChildrenListPosition StartChildrenList()
        {
            return new ChildrenListPosition(_builder.NodeData.AppendNumber(0, _builder.SizeNode));
        }

        public void EndChildrenList(ChildrenListPosition list)
        {
            if (list.IsEmpty)
            {
                throw new ArgumentNullException(nameof(list));
            }
            var pos = list.Position;
            var end = _builder.NodeData.AppendRaw(Array.Empty<byte>());
            var len = end - pos - _builder.SizeNode;
            _builder.NodeData.Fix(pos, len, _builder.SizeNode);
        }

        public void FixReference(ArgumentBuilder arg, NodePosition targetNode)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }
            if (targetNode.IsEmpty)
            {
                throw new ArgumentNullException(nameof(targetNode));
            }
            arg.FixNodeRef(_builder, targetNode.Position);
        }
    }
}
