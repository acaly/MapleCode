﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace MapleCodeSharpTest.TestBuilder
{
    class TypeTableBuilder
    {
        private struct TableEntry : IEquatable<TableEntry>
        {
            public int Index;

            public string Name;
            public int NameIndex;
            public int Generic;
            public byte[] ArgTypes;
            public int ArgTypesDataOffset;
            public bool HasChild;

            public bool Equals(TableEntry other)
            {
                if (Name != other.Name) return false;
                if (Generic != other.Generic) return false;
                if (HasChild != other.HasChild) return false;
                if (ArgTypes.Length != other.ArgTypes.Length) return false;
                for (int i = 0; i < ArgTypes.Length; ++i)
                {
                    if (ArgTypes[i] != other.ArgTypes[i]) return false;
                }
                return true;
            }
        }

        private static readonly int[] _size = new int[] { 0, 1, 2, 4 };

        public TypeTableBuilder(int stringSizeMode, int dataSizeMode,
            Func<string, int> stringBuilderFunc, DataSectionBuilder dataBuilder)
        {
            _stringSizeMode = stringSizeMode;
            _dataSizeMode = dataSizeMode;
            _stringBuilder = stringBuilderFunc;
            _dataBuilder = dataBuilder;
        }

        private readonly Func<string, int> _stringBuilder;
        private readonly DataSectionBuilder _dataBuilder;
        private readonly Dictionary<string, List<int>> _typeDictionary = new Dictionary<string, List<int>>();
        private readonly List<TableEntry> _typeList = new List<TableEntry>();
        private readonly int _stringSizeMode, _dataSizeMode;

        public int AddType(string name, int genericCount, bool hasChild, params byte[] types)
        {
            int index = _typeList.Count;
            var newType = new TableEntry
            {
                Index = index,
                Name = name,
                NameIndex = _stringBuilder(name),
                Generic = genericCount,
                ArgTypes = types,
                HasChild = hasChild,
            };
            if (_typeDictionary.TryGetValue(name, out var list))
            {
                foreach (var ii in list)
                {
                    if (newType.Equals(_typeList[ii]))
                    {
                        return ii;
                    }
                }
            }
            else
            {
                _typeDictionary.Add(name, new List<int>());
            }

            newType.ArgTypesDataOffset = _dataBuilder.AppendRaw(new byte[] { (byte)types.Length });
            _dataBuilder.AppendRaw(types);

            _typeList.Add(newType);
            _typeDictionary[name].Add(index);
            return index;
        }

        public int GetGenericCountForType(int index)
        {
            return _typeList[index].Generic;
        }

        public byte[] GetArgTypesForType(int index)
        {
            return _typeList[index].ArgTypes;
        }

        public byte[] Generate()
        {
            int entrySize = _size[_stringSizeMode] + _size[_dataSizeMode] + 2;
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            foreach (var ee in _typeList)
            {
                bw.Write(ee.NameIndex);
                ms.Seek(_size[_stringSizeMode] - 4, SeekOrigin.Current);

                bw.Write(ee.ArgTypesDataOffset);
                ms.Seek(_size[_dataSizeMode] - 4, SeekOrigin.Current);

                bw.Write((byte)ee.Generic);
                bw.Write(ee.HasChild ? (byte)1 : (byte)0);
            }
            ms.SetLength(ms.Position);
            return ms.ToArray();
        }
    }
}
