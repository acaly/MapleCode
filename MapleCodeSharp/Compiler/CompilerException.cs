using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    public sealed class CompilerException : Exception
    {
        public CompilerException()
        {
        }

        public CompilerException(string message)
            : base(message)
        {
        }

        public CompilerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
