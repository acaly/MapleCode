using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Reader
{
    //Node argument type <-> byte mapping need to be consistent in:
    //  NodeArgumentTypeValues
    //  NodeArgumentType._types

    public enum NodeArgumentTypeClass
    {
        #pragma warning disable CA1720 // Identifier contains type name
        Signed,
        Unsigned,
        Float,
        String,
        Data,
        Ref,
        RefField,
        #pragma warning restore CA1720 // Identifier contains type name
    }

    public static class NodeArgumentTypeValues
    {
        public const byte U8 = 0;
        public const byte U16 = 1;
        public const byte U32 = 2;
        public const byte S8 = 3;
        public const byte S16 = 4;
        public const byte S32 = 5;
        public const byte F32 = 6;
        public const byte STR = 7;
        public const byte DAT = 8;
        public const byte REF = 9;
        public const byte REFFIELD = 10;
    }

    public abstract class NodeArgumentType
    {
        public abstract string Name { get; }
        public abstract NodeArgumentTypeClass TypeClass { get; }
        public abstract int GetByteSize(Document doc);

        internal virtual int GetSigned(NodeArgument argument) => throw new InvalidOperationException();
        internal virtual uint GetUnsigned(NodeArgument argument) => throw new InvalidOperationException();
        internal virtual float GetFloat(NodeArgument argument) => throw new InvalidOperationException();
        internal virtual string GetString(NodeArgument argument) => throw new InvalidOperationException();
        internal virtual byte[] GetData(NodeArgument argument) => throw new InvalidOperationException();
        internal virtual Node GetNode(NodeArgument argument) => throw new InvalidOperationException();
        internal virtual string GetField(NodeArgument argument) => throw new InvalidOperationException();

        private NodeArgumentType() { }

        public override string ToString()
        {
            return Name;
        }

        private abstract class NodeArgumentTypeUnsigned : NodeArgumentType
        {
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.Unsigned;
        }

        private class NodeArgumentTypeU8 : NodeArgumentTypeUnsigned
        {
            public override string Name => "U8";
            public override int GetByteSize(Document doc) => 1;

            internal override uint GetUnsigned(NodeArgument argument)
            {
                return argument.Document._data[argument.AbsOffset];
            }
        }

        private class NodeArgumentTypeU16 : NodeArgumentTypeUnsigned
        {
            public override string Name => "U16";
            public override int GetByteSize(Document doc) => 2;

            internal override uint GetUnsigned(NodeArgument argument)
            {
                return BitConverter.ToUInt16(argument.Document._data, argument.AbsOffset);
            }
        }

        private class NodeArgumentTypeU32 : NodeArgumentTypeUnsigned
        {
            public override string Name => "U32";
            public override int GetByteSize(Document doc) => 4;

            internal override uint GetUnsigned(NodeArgument argument)
            {
                return BitConverter.ToUInt32(argument.Document._data, argument.AbsOffset);
            }
        }

        private abstract class NodeArgumentTypeSigned : NodeArgumentType
        {
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.Signed;
        }

        private class NodeArgumentTypeS8 : NodeArgumentTypeSigned
        {
            public override string Name => "S8";
            public override int GetByteSize(Document doc) => 1;

            internal override int GetSigned(NodeArgument argument)
            {
                return (sbyte)argument.Document._data[argument.AbsOffset];
            }
        }

        private class NodeArgumentTypeS16 : NodeArgumentTypeSigned
        {
            public override string Name => "S16";
            public override int GetByteSize(Document doc) => 2;

            internal override int GetSigned(NodeArgument argument)
            {
                return BitConverter.ToInt16(argument.Document._data, argument.AbsOffset);
            }
        }

        private class NodeArgumentTypeS32 : NodeArgumentTypeSigned
        {
            public override string Name => "S32";
            public override int GetByteSize(Document doc) => 4;

            internal override int GetSigned(NodeArgument argument)
            {
                return BitConverter.ToInt32(argument.Document._data, argument.AbsOffset);
            }
        }

        private class NodeArgumentTypeF32 : NodeArgumentType
        {
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.Float;
            public override string Name => "F32";
            public override int GetByteSize(Document doc) => 4;

            internal override float GetFloat(NodeArgument argument)
            {
                return BitConverter.ToSingle(argument.Document._data, argument.AbsOffset);
            }
        }

        private class NodeArgumentTypeStr : NodeArgumentType
        {
            public override string Name => "STR";
            public override int GetByteSize(Document doc) => doc.StringIndexSize;
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.String;

            internal override string GetString(NodeArgument argument)
            {
                var offset = argument.AbsOffset;
                var index = argument.Document._readStrIndex(ref offset);
                return argument.Document.RetrieveString(index);
            }
        }

        private class NodeArgumentTypeDat : NodeArgumentType
        {
            public override string Name => "DAT";
            public override int GetByteSize(Document doc) => doc.DataOffsetSize * 2;
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.Data;

            internal override byte[] GetData(NodeArgument argument)
            {
                var offset = argument.AbsOffset;
                var pos1 = argument.Document._readDataOffset(ref offset);
                var pos2 = argument.Document._readDataOffset(ref offset);
                byte[] ret = new byte[pos2 - pos1];
                argument.Document.CopyData(pos1, ret, 0, ret.Length);
                return ret;
            }
        }

        private class NodeArgumentTypeRef : NodeArgumentType
        {
            public override string Name => "REF";
            public override int GetByteSize(Document doc) => doc.NodeOffsetSize;
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.Ref;

            internal override Node GetNode(NodeArgument argument)
            {
                var offset = argument.AbsOffset;
                var node = argument.Document._readNodeOffset(ref offset);
                return new Node(argument.Document, node);
            }
        }

        private class NodeArgumentTypeRefField : NodeArgumentType
        {
            public override string Name => "REFFIELD";
            public override int GetByteSize(Document doc) => doc.NodeOffsetSize + doc.StringIndexSize;
            public override NodeArgumentTypeClass TypeClass => NodeArgumentTypeClass.RefField;

            internal override Node GetNode(NodeArgument argument)
            {
                var offset = argument.AbsOffset;
                var node = argument.Document._readNodeOffset(ref offset);
                return new Node(argument.Document, node);
            }

            internal override string GetField(NodeArgument argument)
            {
                var offset = argument.AbsOffset;
                var node = argument.Document._readNodeOffset(ref offset);
                var field = argument.Document._readStrIndex(ref offset);
                return argument.Document.RetrieveString(field);
            }
        }

        public static readonly NodeArgumentType U8 = new NodeArgumentTypeU8();
        public static readonly NodeArgumentType U16 = new NodeArgumentTypeU16();
        public static readonly NodeArgumentType U32 = new NodeArgumentTypeU32();
        public static readonly NodeArgumentType S8 = new NodeArgumentTypeS8();
        public static readonly NodeArgumentType S16 = new NodeArgumentTypeS16();
        public static readonly NodeArgumentType S32 = new NodeArgumentTypeS32();
        public static readonly NodeArgumentType F32 = new NodeArgumentTypeF32();
        public static readonly NodeArgumentType STR = new NodeArgumentTypeStr();
        public static readonly NodeArgumentType DAT = new NodeArgumentTypeDat();
        public static readonly NodeArgumentType REF = new NodeArgumentTypeRef();
        public static readonly NodeArgumentType REFFIELD = new NodeArgumentTypeRefField();

        private static readonly NodeArgumentType[] _types = new[]
        {
            U8, U16, U32, S8, S16, S32, F32, STR, DAT, REF, REFFIELD,
        };

        internal static NodeArgumentType GetTypeFromInt(byte val)
        {
            if (val > _types.Length)
            {
                throw new ReaderException("Invalid argument type");
            }
            return _types[val];
        }
    }
}
