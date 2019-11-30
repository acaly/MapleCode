using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapleCodeSharp.Builder
{
    public sealed class DataSectionBuilder
    {
        //Assume MemoryStream is safe to be left undisposed.
        private readonly MemoryStream _stream;

        public DataSectionBuilder(MemoryStream stream)
        {
            _stream = stream;
        }

        public int AppendRaw(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            var ret = (int)_stream.Position;
            _stream.Write(data, 0, data.Length);
            return ret;
        }

        public int AppendNumber(int val, int size)
        {
            var ret = AppendRaw(BitConverter.GetBytes(val));
            _stream.Seek(size - 4, SeekOrigin.Current);
            return ret;
        }

        public int AppendNumber(uint val, int size)
        {
            var ret = AppendRaw(BitConverter.GetBytes(val));
            _stream.Seek(size - 4, SeekOrigin.Current);
            return ret;
        }

        public byte[] Generate()
        {
            _stream.SetLength(_stream.Position);
            return _stream.ToArray();
        }

        public void Fix(int pos, int val, int size)
        {
            _stream.SetLength(_stream.Position);
            _stream.Seek(pos, SeekOrigin.Begin);
            _stream.Write(BitConverter.GetBytes(val), 0, size);
            _stream.Seek(0, SeekOrigin.End);
        }

        public void Fix(int pos, uint val, int size)
        {
            _stream.SetLength(_stream.Position);
            _stream.Seek(pos, SeekOrigin.Begin);
            _stream.Write(BitConverter.GetBytes(val), 0, size);
            _stream.Seek(0, SeekOrigin.End);
        }
    }
}
