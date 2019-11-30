using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapleCodeSharp.Reader
{
    public sealed class ReaderException : IOException
    {
        public ReaderException()
        {
        }

        public ReaderException(string message)
            : base(message)
        {
        }

        public ReaderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
