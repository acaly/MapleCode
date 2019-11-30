using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    internal static class Tokenizer
    {
        public static IEnumerable<Token> Create(IEnumerable<char> input)
        {
            var e = input.GetEnumerator();
            var sb = new StringBuilder();
            if (!e.MoveNext()) yield break;
            while (true)
            {
                var ch = e.Current;
                while (IsSpaceChar(ch))
                {
                    if (!e.MoveNext()) yield break;
                    ch = e.Current;
                }
                if (IsSymbolChar(ch))
                {
                    yield return new Token
                    {
                        TokenType = TokenType.Symbol,
                        CharValue = ch,
                    };
                    if (!e.MoveNext()) yield break;
                }
                else if (IsNumberChar(ch))
                {
                    sb.Clear();
                    sb.Append(ch);
                    while (true)
                    {
                        if (!e.MoveNext())
                        {
                            yield return new Token
                            {
                                TokenType = TokenType.Number,
                                StringValue = sb.ToString(),
                            };
                            yield break;
                        }
                        var cch = e.Current;
                        if (IsSpaceChar(cch) || IsSymbolChar(cch) && cch != '.')
                        {
                            yield return new Token
                            {
                                TokenType = TokenType.Number,
                                StringValue = sb.ToString(),
                            };
                            break;
                        }
                        sb.Append(cch);
                    }
                }
                else if (ch == '"')
                {
                    sb.Clear();
                    while (true)
                    {
                        if (!e.MoveNext())
                        {
                            throw new CompilerException("Invalid string literal");
                        }
                        var cch = e.Current;
                        if (cch == '\\')
                        {
                            uint utf32 = 0;
                            if (!e.MoveNext())
                            {
                                throw new CompilerException("Invalid string literal");
                            }
                            cch = e.Current;
                            switch (cch)
                            {
                                case '"':
                                    sb.Append('"');
                                    break;
                                case '\\':
                                    sb.Append('\\');
                                    break;
                                case 'n':
                                    sb.Append('\n');
                                    break;
                                case 'r':
                                    sb.Append('\r');
                                    break;
                                case 't':
                                    sb.Append('\t');
                                    break;
                                case 'u':
                                    cch = '\0';
                                    for (int i = 0; i < 4; ++i)
                                    {
                                        if (!e.MoveNext())
                                        {
                                            throw new CompilerException("Invalid string literal");
                                        }
                                        var ccch = e.Current;
                                        if (ccch >= '0' && ccch <= '9')
                                        {
                                            cch = (char)(cch * 10 + (ccch - '0'));
                                        }
                                        else
                                        {
                                            throw new CompilerException("Invalid string literal");
                                        }
                                    }
                                    sb.Append(cch);
                                    break;
                                case 'U':
                                    for (int i = 0; i < 8; ++i)
                                    {
                                        if (!e.MoveNext())
                                        {
                                            throw new CompilerException("Invalid string literal");
                                        }
                                        var ccch = e.Current;
                                        if (ccch >= '0' && ccch <= '9')
                                        {
                                            utf32 = utf32 * 10 + (uint)(ccch - '0');
                                        }
                                        else
                                        {
                                            throw new CompilerException("Invalid string literal");
                                        }
                                    }
                                    sb.Append(char.ConvertFromUtf32((int)utf32));
                                    break;
                                default:
                                    throw new CompilerException("Invalid string literal");
                            }
                        }
                        else if (cch == '"')
                        {
                            yield return new Token
                            {
                                TokenType = TokenType.StringLiteral,
                                StringValue = sb.ToString(),
                            };
                            if (!e.MoveNext())
                            {
                                yield break;
                            }
                            break;
                        }
                        else
                        {
                            sb.Append(cch);
                        }
                    }
                }
                else
                {
                    sb.Clear();
                    sb.Append(ch);
                    bool yieldBreak = false;
                    while (true)
                    {
                        if (!e.MoveNext())
                        {
                            yieldBreak = true;
                            break;
                        }
                        var cch = e.Current;
                        if (IsSymbolChar(cch) || IsSpaceChar(cch) || cch == '+' || cch == '-')
                        {
                            break;
                        }
                        sb.Append(cch);
                    }
                    var str = sb.ToString();
                    if (str.ToUpperInvariant() == "DATA")
                    {
                        yield return new Token
                        {
                            TokenType = TokenType.KeyWordData,
                        };
                    }
                    else
                    {
                        yield return new Token
                        {
                            TokenType = TokenType.Name,
                            StringValue = sb.ToString(),
                        };
                    }
                    if (yieldBreak)
                    {
                        yield break;
                    }
                }
            }
        }

        private static bool IsSymbolChar(char ch)
        {
            if (ch == '<' || ch == '>' || ch == ',' ||
                ch == '.' || ch == '{' || ch == '}' ||
                ch == ':' || ch == ';')
            {
                return true;
            }
            return false;
        }

        private static bool IsNumberChar(char ch)
        {
            return ch >= '0' && ch <= '9' || ch == '+' || ch == '-';
        }

        private static bool IsSpaceChar(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
        }
    }
}
