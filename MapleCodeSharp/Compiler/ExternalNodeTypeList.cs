using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    public sealed class ExternalNodeTypeList
    {
        internal readonly Dictionary<string, List<MapleCodeCompiler.NodeType>> _nodeTypes =
            new Dictionary<string, List<MapleCodeCompiler.NodeType>>();

        public void Add(int index, string name, int gericArgCount, byte[] arguments, bool hasChildren)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (gericArgCount < 0 || gericArgCount > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(gericArgCount));
            }
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }
            if (arguments.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(arguments));
            }

            if (!_nodeTypes.TryGetValue(name, out var list))
            {
                list = new List<MapleCodeCompiler.NodeType>();
                _nodeTypes.Add(name, list);
            }
            list.Add(new MapleCodeCompiler.NodeType
            {
                TypeIndex = index,
                GenericCount = (byte)gericArgCount,
                Args = arguments.ToArray(),
                HasChildren = hasChildren,
            });
        }
    }
}
