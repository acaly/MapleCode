using System;
using System.Collections.Generic;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    internal static class Preprocessor
    {
        public static IEnumerable<char> Create(IEnumerable<char> input)
        {
            var e = input.GetEnumerator();
            bool isComment = false;
            while (e.MoveNext())
            {
                var ch = e.Current;
                switch (ch)
                {
                    case '#':
                        isComment = true;
                        break;
                    case '\r':
                    case '\n':
                        isComment = false;
                        break;
                }
                if (!isComment) yield return e.Current;
            }
        }
    }
}
