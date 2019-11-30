using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MapleCodeSharp.Reader
{
    public sealed class NodeType
    {
        public Document Document { get; }
        public int Index { get; }
        public string Name { get; }
        public int GenericArgCount { get; }
        public ReadOnlyCollection<NodeArgumentType> ArgumentTypes { get; }
        public bool HasChildren { get; }
        public int TotalSize { get; }

        internal NodeType(Document document, int index, int name, int args, int genericCount, bool hasChildren)
        {
            Document = document;
            Index = index;
            Name = document.RetrieveString(name);
            GenericArgCount = genericCount;
            HasChildren = hasChildren;

            byte[] buffer = new byte[256];
            document.CopyData(args, buffer, 0, 1);
            int argCount = buffer[0];
            document.CopyData(args + 1, buffer, 0, argCount);
            ArgumentTypes = Array.AsReadOnly(buffer.Take(argCount)
                .Select(ii => NodeArgumentType.GetTypeFromInt(ii))
                .ToArray());
            TotalSize = document.TypeIndexSize + document.StringIndexSize * GenericArgCount +
                ArgumentTypes.Sum(type => type.GetByteSize(document));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            if (GenericArgCount > 0)
            {
                sb.Append('<');
                sb.Append(GenericArgCount);
                sb.Append('>');
            }
            sb.Append('(');
            bool isFirst = true;
            foreach (var arg in ArgumentTypes)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(arg.Name);
            }
            sb.Append(')');
            if (HasChildren)
            {
                sb.Append(" {}");
            }
            else
            {
                sb.Append(';');
            }
            return sb.ToString();
        }
    }
}
