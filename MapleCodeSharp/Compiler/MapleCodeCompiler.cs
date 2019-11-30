using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    public sealed class MapleCodeCompiler
    {
        private readonly IEnumerator<Token> _tokenInput;

        private readonly StringTable _stringTable;
        private readonly TypeTable _typeTable;
        private readonly DataSection _dataSection;
        private readonly DataSection _nodeSection;

        private bool _inputEOS;

        private readonly Dictionary<string, int> _namedNode = new Dictionary<string, int>();
        private readonly Dictionary<string, List<NodeType>> _nodeTypes = new Dictionary<string, List<NodeType>>();
        private readonly bool _lockNodeTypes = false;

        private readonly List<int> _fixNodeLabelPos = new List<int>();
        private readonly List<string> _fixNodeLabelName = new List<string>();

        private readonly List<byte> _nodeArgTypes = new List<byte>();

        internal struct NodeType
        {
            public int TypeIndex;
            public byte GenericCount;
            public bool HasChildren;
            public byte[] Args;
        }

        internal static int GetByteSize(int count)
        {
            if (count <= 256)
            {
                return 1;
            }
            else if (count <= 65536)
            {
                return 2;
            }
            else
            {
                return 4;
            }
        }

        public static byte[] Compile(IEnumerable<char> input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            return new MapleCodeCompiler(input).Compile();
        }

        public static byte[] Compile(ExternalNodeTypeList typeList, IEnumerable<char> input)
        {
            if (typeList == null)
            {
                throw new ArgumentNullException(nameof(typeList));
            }
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            return new MapleCodeCompiler(typeList, input).Compile();
        }

        private MapleCodeCompiler(IEnumerable<char> input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            _tokenInput = Tokenizer.Create(Preprocessor.Create(input)).GetEnumerator();
            _dataSection = new DataSection();
            _stringTable = new StringTable(_dataSection);
            _typeTable = new TypeTable(_stringTable, _dataSection);
            _nodeSection = new DataSection();
        }

        private MapleCodeCompiler(ExternalNodeTypeList typeList, IEnumerable<char> input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            _tokenInput = Tokenizer.Create(Preprocessor.Create(input)).GetEnumerator();
            _dataSection = new DataSection();
            _stringTable = new StringTable(_dataSection);
            _typeTable = new TypeTable(_stringTable, _dataSection);
            _nodeSection = new DataSection();

            foreach (var e in typeList._nodeTypes)
            {
                _nodeTypes.Add(e.Key, e.Value);
            }
            typeList._nodeTypes.Clear();
            _lockNodeTypes = true;
        }

        private byte[] Compile()
        {
            Parse();

            //Fix node ref
            for (int i = 0; i < _fixNodeLabelPos.Count; ++i)
            {
                if (!_namedNode.TryGetValue(_fixNodeLabelName[i], out var val))
                {
                    throw new CompilerException("Named node not found");
                }
                _nodeSection.FixSingleSlot(_fixNodeLabelPos[i], (uint)val);
            }
            //TODO if we support array argument, we need to fix node ref in data section too.

            int strSize = _stringTable.StringIndexSize, typeSize = _typeTable.TypeIndexSize;
            int dataSize = 1, nodeSize = 1, newDataSize, newNodeSize;
            if (_lockNodeTypes)
            {
                typeSize = GetByteSize(_nodeTypes.SelectMany(e => e.Value).Select(t => t.TypeIndex).Max());
            }
            do
            {
                newDataSize = _dataSection.TryCalculateSize(strSize, typeSize, nodeSize, dataSize);
                newNodeSize = _nodeSection.TryCalculateSize(strSize, typeSize, nodeSize, dataSize);
            } while (dataSize != newDataSize || nodeSize != newNodeSize);

            int[] sizeModeArray = new[] { 0, 1, 2, 0, 3 };
            int sizeMode = sizeModeArray[strSize] | sizeModeArray[typeSize] << 2 |
                sizeModeArray[nodeSize] << 4 | sizeModeArray[dataSize] << 6;

            _dataSection.GenerateOffset(strSize, typeSize, nodeSize, dataSize);
            _nodeSection.GenerateOffset(strSize, typeSize, nodeSize, dataSize);

            _dataSection.FixOffset(DataSection.SlotType.DataIndex, _dataSection);
            _nodeSection.FixOffset(DataSection.SlotType.DataIndex, _dataSection);
            _dataSection.FixOffset(DataSection.SlotType.NodeIndex, _nodeSection);
            _nodeSection.FixOffset(DataSection.SlotType.NodeIndex, _nodeSection);

            var str = _stringTable.Generate(dataSize);
            var type = _typeTable.Generate(strSize, dataSize);
            var node = _nodeSection.Generate(strSize, typeSize, nodeSize, dataSize);
            var data = _dataSection.Generate(strSize, typeSize, nodeSize, dataSize);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((byte)sizeMode);
            bw.Write(str.Length);
            ms.Seek(strSize - 4, SeekOrigin.Current);
            bw.Write(type.Length);
            ms.Seek(typeSize - 4, SeekOrigin.Current);
            bw.Write(node.Length);
            ms.Seek(nodeSize - 4, SeekOrigin.Current);
            bw.Write(data.Length);
            ms.Seek(dataSize - 4, SeekOrigin.Current);

            bw.Write(str);
            bw.Write(type);
            bw.Write(node);
            bw.Write(data);

            bw.Flush();
            return ms.ToArray();
        }

        private void Parse()
        {
            if (TryMoveNext())
            {
                ParseNodeList();
                if (!_inputEOS)
                {
                    throw new CompilerException("Expect node");
                }
            }
        }

        private void ParseNodeList()
        {
            while (true)
            {
                if (_inputEOS || _tokenInput.Current.TokenType != TokenType.Name)
                {
                    return;
                }

                var nodePos = _nodeSection.Position;
                _nodeSection.Add(0, DataSection.SlotType.TypeIndex);

                var name = _tokenInput.Current.StringValue;
                MoveNext();

                if (IsSymbol(':'))
                {
                    if (_namedNode.ContainsKey(name))
                    {
                        throw new CompilerException("Duplicate node label");
                    }
                    _namedNode.Add(name, nodePos);
                    MoveNext();
                    Expect(TokenType.Name);
                    name = _tokenInput.Current.StringValue;
                    MoveNext();
                }

                var genericArgCount = 0;
                if (IsSymbol('<'))
                {
                    MoveNext();

                    //Don't allow empty argument list
                    Expect(TokenType.Name);
                    genericArgCount += 1;
                    var strIndex = _stringTable.AddString(_tokenInput.Current.StringValue);
                    _nodeSection.Add((uint)strIndex, DataSection.SlotType.StringIndex);
                    MoveNext();
                    
                    while (IsSymbol(','))
                    {
                        MoveNext();

                        Expect(TokenType.Name);
                        genericArgCount += 1;
                        strIndex = _stringTable.AddString(_tokenInput.Current.StringValue);
                        _nodeSection.Add((uint)strIndex, DataSection.SlotType.StringIndex);

                        MoveNext();
                    }
                    Expect('>');
                    MoveNext();
                }

                _nodeArgTypes.Clear();

                if (!IsSymbol('{') && !IsSymbol(';'))
                {
                    ParseArgument();
                    while (IsSymbol(','))
                    {
                        MoveNext();
                        ParseArgument();
                    }
                    if (!IsSymbol('{'))
                    {
                        Expect(';');
                    }
                }

                _nodeSection.FixSingleSlot(nodePos,
                    FindNodeType(name, genericArgCount, IsSymbol('{')));

                if (IsSymbol('{'))
                {
                    //Write children node length.
                    var lengthEndPos = _nodeSection.Position;
                    _nodeSection.Add(0, DataSection.SlotType.NodeLengthEnd);

                    MoveNext();
                    ParseNodeList();

                    _nodeSection.FixSingleSlot(lengthEndPos, (uint)_nodeSection.Position);

                    Expect('}');
                }

                TryMoveNext();
            }
        }

        private uint FindNodeType(string name, int genericCount, bool hasChildren)
        {
            if (!_nodeTypes.TryGetValue(name, out var typeOverloadList))
            {
                typeOverloadList = new List<NodeType>();
                _nodeTypes.Add(name, typeOverloadList);
            }
            foreach (var type in typeOverloadList)
            {
                if (type.GenericCount != genericCount ||
                    type.HasChildren != hasChildren ||
                    type.Args.Length != _nodeArgTypes.Count)
                {
                    continue;
                }
                for (int i = 0; i < type.Args.Length; ++i)
                {
                    if (type.Args[i] != _nodeArgTypes[i])
                    {
                        continue;
                    }
                }
                return (uint)type.TypeIndex;
            }
            if (_lockNodeTypes)
            {
                throw new CompilerException("Undefined node type");
            }
            var ret = _typeTable.Add(name, _nodeArgTypes, genericCount, hasChildren);
            typeOverloadList.Add(new NodeType
            {
                GenericCount = (byte)genericCount,
                Args = _nodeArgTypes.ToArray(),
                HasChildren = hasChildren,
                TypeIndex = ret,
            });
            return (uint)ret;
        }

        private void ParseArgument()
        {
            switch (_tokenInput.Current.TokenType)
            {
                case TokenType.StringLiteral:
                {
                    _nodeArgTypes.Add(NodeArgumentTypeValues.STR);
                    var strIndex = _stringTable.AddString(_tokenInput.Current.StringValue);
                    _nodeSection.Add((uint)strIndex, DataSection.SlotType.StringIndex);
                    MoveNext();
                    break;
                }
                case TokenType.Name:
                {
                    var nodeName = _tokenInput.Current.StringValue;
                    _fixNodeLabelPos.Add(_nodeSection.Position);
                    _fixNodeLabelName.Add(nodeName);
                    _nodeSection.Add(0, DataSection.SlotType.NodeIndex);
                    MoveNext();
                    if (IsSymbol('.'))
                    {
                        MoveNext();
                        Expect(TokenType.Name);
                        var strIndex = _stringTable.AddString(_tokenInput.Current.StringValue);
                        _nodeSection.Add((uint)strIndex, DataSection.SlotType.StringIndex);
                        _nodeArgTypes.Add(NodeArgumentTypeValues.REFFIELD);
                        MoveNext();
                    }
                    else
                    {
                        _nodeArgTypes.Add(NodeArgumentTypeValues.REF);
                    }
                    break;
                }
                case TokenType.Number:
                {
                    ParseNumber();
                    break;
                }
                case TokenType.KeyWordData:
                {
                    ParseDataList();
                    break;
                }
                default:
                {
                    throw new CompilerException("Expect argument");
                }
            }
        }

        private void ParseNumber()
        {
            //A general format:
            //digits '.' digits {'E'|'e'} {'+'|'-'} digits format
            //******************************************** parse segment
            var str = _tokenInput.Current.StringValue;
            int len = 0;
            if (len < str.Length && (str[len] == '+' || str[len] == '-'))
            {
                len += 1;
            }
            while (len < str.Length && str[len] >= '0' && str[len] <= '9')
            {
                len += 1;
            }
            if (len < str.Length && str[len] == '.')
            {
                len += 1;
            }
            while (len < str.Length && str[len] >= '0' && str[len] <= '9')
            {
                len += 1;
            }
            if (len < str.Length && (str[len] == 'E' || str[len] == 'e'))
            {
                len += 1;
                if (len < str.Length && (str[len] == '+' || str[len] == '-'))
                {
                    len += 1;
                }
                while (len < str.Length && str[len] >= '0' && str[len] <= '9')
                {
                    len += 1;
                }
            }
            var s1 = str.Substring(0, len);
            var s2 = str.Substring(len);
            if (s2.Length == 0)
            {
                if (s1.Contains('.', StringComparison.Ordinal))
                {
                    s2 = "f";
                }
                else if (s1[0] == '+' || s1[0] == '-')
                {
                    s2 = "s32";
                }
                else
                {
                    s2 = "u32";
                }
            }
            if (s2 == "f")
            {
                if (!float.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.AddInt8(BitConverter.GetBytes(val));
                _nodeArgTypes.Add(NodeArgumentTypeValues.F32);
            }
            else if (s2 == "u8")
            {
                if (!byte.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.Add(val, DataSection.SlotType.Int8);
                _nodeArgTypes.Add(NodeArgumentTypeValues.U8);
            }
            else if (s2 == "u16")
            {
                if (!ushort.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.Add(val, DataSection.SlotType.Int16);
                _nodeArgTypes.Add(NodeArgumentTypeValues.U16);
            }
            else if (s2 == "u32")
            {
                if (!uint.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.Add(val, DataSection.SlotType.Int32);
                _nodeArgTypes.Add(NodeArgumentTypeValues.U32);
            }
            else if (s2 == "s8")
            {
                if (!sbyte.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.Add((byte)val, DataSection.SlotType.Int8);
                _nodeArgTypes.Add(NodeArgumentTypeValues.S8);
            }
            else if (s2 == "s16")
            {
                if (!short.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.Add((ushort)val, DataSection.SlotType.Int16);
                _nodeArgTypes.Add(NodeArgumentTypeValues.S16);
            }
            else if (s2 == "s32")
            {
                if (!int.TryParse(s1, out var val))
                {
                    throw new CompilerException("Invalid number");
                }
                _nodeSection.Add((uint)val, DataSection.SlotType.Int32);
                _nodeArgTypes.Add(NodeArgumentTypeValues.S32);
            }
            else
            {
                throw new CompilerException("Invalid number");
            }
            MoveNext();
        }

        private void ParseDataList()
        {
            //Maybe we should extend the format to support array, but not here.
            //Support u8 u16 u32 s8 s16 s32 f32 hex

            MoveNext();
            Expect(TokenType.Name);
            var type = _tokenInput.Current.StringValue;
            MoveNext();

            Expect('{');
            MoveNext();

            _nodeSection.Add((uint)_dataSection.Position, DataSection.SlotType.DataIndex);

            if (type == "hex")
            {
                //Hex format does not have ',' between items and can have no space.
                //00 04 08 0C FF = 0004080CFF
                //Accept both number and name.
                while (!IsSymbol('}'))
                {
                    var tokenType = _tokenInput.Current.TokenType;
                    var str = _tokenInput.Current.StringValue;
                    if (tokenType != TokenType.Number && tokenType != TokenType.Name ||
                        str.Length % 2 != 0)
                    {
                        throw new CompilerException("Invalid hex data");
                    }
                    for (int i = 0; i < str.Length; i += 2)
                    {
                        int val = HexCharToInt(str[i]) * 16 + HexCharToInt(str[i + 1]);
                        _dataSection.Add((uint)val, DataSection.SlotType.Int8);
                    }
                    MoveNext();
                }
            }
            else
            {
                Action parseAction;
                if (type == "u8")
                {
                    parseAction = ParseDataItemU8;
                }
                else if (type == "u16")
                {
                    parseAction = ParseDataItemU16;
                }
                else if (type == "u32")
                {
                    parseAction = ParseDataItemU32;
                }
                else if (type == "s8")
                {
                    parseAction = ParseDataItemS8;
                }
                else if (type == "s16")
                {
                    parseAction = ParseDataItemS16;
                }
                else if (type == "s32")
                {
                    parseAction = ParseDataItemS32;
                }
                else if (type == "f32")
                {
                    parseAction = ParseDataItemF32;
                }
                else
                {
                    throw new CompilerException("Unknown data type");
                }

                while (!IsSymbol('}'))
                {
                    parseAction();
                    MoveNext();
                    if (!IsSymbol(','))
                    {
                        Expect('}');
                        break;
                    }
                    MoveNext();
                }
            }

            _nodeSection.Add((uint)_dataSection.Position, DataSection.SlotType.DataIndex);
            _nodeArgTypes.Add(NodeArgumentTypeValues.DAT);

            MoveNext();
        }

        private static int HexCharToInt(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return ch - '0';
            }
            else if (ch >= 'a' && ch <= 'f')
            {
                return ch - 'a' + 10;
            }
            else if (ch >= 'A' && ch <= 'F')
            {
                return ch - 'A' + 10;
            }
            throw new CompilerException("Invalid hex data");
        }

        private void ParseDataItemU8()
        {
            if (!byte.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            _dataSection.Add(val, DataSection.SlotType.Int8);
        }

        private void ParseDataItemU16()
        {
            if (!ushort.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            _dataSection.Add(val, DataSection.SlotType.Int16);
        }

        private void ParseDataItemU32()
        {
            if (!uint.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            _dataSection.Add(val, DataSection.SlotType.Int32);
        }

        private void ParseDataItemS8()
        {
            if (!sbyte.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            _dataSection.Add((byte)val, DataSection.SlotType.Int8);
        }

        private void ParseDataItemS16()
        {
            if (!short.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            _dataSection.Add((ushort)val, DataSection.SlotType.Int16);
        }

        private void ParseDataItemS32()
        {
            if (!int.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            _dataSection.Add((uint)val, DataSection.SlotType.Int32);
        }

        private void ParseDataItemF32()
        {
            if (!float.TryParse(_tokenInput.Current.StringValue, out var val))
            {
                throw new CompilerException("Invalid data");
            }
            var bitConvInt32 = BitConverter.ToUInt32(BitConverter.GetBytes(val), 0);
            _dataSection.Add(bitConvInt32, DataSection.SlotType.Int32);
        }

        private void Expect(TokenType type)
        {
            if (_inputEOS || _tokenInput.Current.TokenType != type)
            {
                throw new CompilerException($"Expect {type}");
            }
        }

        private void Expect(char ch)
        {
            if (_inputEOS ||
                _tokenInput.Current.TokenType != TokenType.Symbol ||
                _tokenInput.Current.CharValue != ch)
            {
                throw new CompilerException($"Expect {ch}");
            }
        }

        private void MoveNext()
        {
            if (!_tokenInput.MoveNext())
            {
                throw new CompilerException("Unexpected EOS");
            }
        }

        private bool TryMoveNext()
        {
            var success = _tokenInput.MoveNext();
            _inputEOS = !success;
            return success;
        }

        private bool IsSymbol(char ch)
        {
            return !_inputEOS &&
                _tokenInput.Current.TokenType == TokenType.Symbol &&
                _tokenInput.Current.CharValue == ch;
        }
    }
}
