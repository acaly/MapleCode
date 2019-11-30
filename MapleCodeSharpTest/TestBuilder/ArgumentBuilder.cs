using MapleCodeSharp.Reader;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharpTest.TestBuilder
{
    class ArgumentBuilder
    {
        private byte[] _arrayData;
        private float? _floatData;
        private uint? _uintData;
        private int? _intData;
        private string _strData;
        private bool _isNodeRef;
        private int _nodeRefFixPos;

        public static ArgumentBuilder NewRefArg()
        {
            return new ArgumentBuilder
            {
                _isNodeRef = true,
            };
        }

        public static ArgumentBuilder NewRefArg(string field)
        {
            return new ArgumentBuilder
            {
                _isNodeRef = true,
                _strData = field,
            };
        }

        public static implicit operator ArgumentBuilder(int val)
        {
            return new ArgumentBuilder
            {
                _intData = val,
            };
        }

        public static implicit operator ArgumentBuilder(uint val)
        {
            return new ArgumentBuilder
            {
                _uintData = val,
            };
        }

        public static implicit operator ArgumentBuilder(float val)
        {
            return new ArgumentBuilder
            {
                _floatData = val,
            };
        }

        public static implicit operator ArgumentBuilder(byte[] val)
        {
            return new ArgumentBuilder
            {
                _arrayData = val,
            };
        }

        public static implicit operator ArgumentBuilder(string val)
        {
            return new ArgumentBuilder
            {
                _strData = val,
            };
        }

        public void Generate(DocumentBuilder document, byte type)
        {
            switch (type)
            {
                case NodeArgumentTypeValues.U8:
                    document.NodeData.AppendNumber(_uintData.Value, 1);
                    break;
                case NodeArgumentTypeValues.U16:
                    document.NodeData.AppendNumber(_uintData.Value, 2);
                    break;
                case NodeArgumentTypeValues.U32:
                    document.NodeData.AppendNumber(_uintData.Value, 4);
                    break;
                case NodeArgumentTypeValues.S8:
                    document.NodeData.AppendNumber(_intData.Value, 1);
                    break;
                case NodeArgumentTypeValues.S16:
                    document.NodeData.AppendNumber(_intData.Value, 2);
                    break;
                case NodeArgumentTypeValues.S32:
                    document.NodeData.AppendNumber(_intData.Value, 4);
                    break;
                case NodeArgumentTypeValues.F32:
                    document.NodeData.AppendRaw(BitConverter.GetBytes(_floatData.Value));
                    break;
                case NodeArgumentTypeValues.STR:
                {
                    var strIndex = document.String.AddString(_strData);
                    document.NodeData.AppendNumber(strIndex, document.SizeStr);
                    break;
                }
                case NodeArgumentTypeValues.DAT:
                {
                    var dataStart = document.Data.AppendRaw(_arrayData);
                    var dataEnd = document.Data.AppendRaw(new byte[0]);
                    document.NodeData.AppendNumber(dataStart, document.SizeData);
                    document.NodeData.AppendNumber(dataEnd, document.SizeData);
                    break;
                }
                case NodeArgumentTypeValues.REF:
                {
                    _nodeRefFixPos = document.NodeData.AppendNumber(0, document.SizeNode);
                    break;
                }
                case NodeArgumentTypeValues.REFFIELD:
                {
                    _nodeRefFixPos = document.NodeData.AppendNumber(0, document.SizeNode);
                    document.NodeData.AppendNumber(document.String.AddString(_strData), document.SizeStr);
                    break;
                }
                default:
                    throw new Exception();
            }
        }

        public void FixNodeRef(DocumentBuilder document, int target)
        {
            if (!_isNodeRef) throw new InvalidOperationException();
            document.NodeData.Fix(_nodeRefFixPos, target, document.SizeNode);
        }
    }
}
