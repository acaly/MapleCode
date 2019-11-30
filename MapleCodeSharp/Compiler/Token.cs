using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    internal enum TokenType
    {
        Symbol,
        Number,
        Name,
        StringLiteral,
        KeyWordData,
    }

    internal struct Token
    {
        public TokenType TokenType;
        public string StringValue;
        public char CharValue;
    }
}
