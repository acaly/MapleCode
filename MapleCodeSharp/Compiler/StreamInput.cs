using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapleCodeSharp.Compiler
{
    public static class StreamInput
    {
        public static IEnumerable<char> Create(Stream stream)
        {
            using var reader = new StreamReader(stream);
            while (true)
            {
                var r = reader.Read();
                if (r == -1) yield break;
                yield return (char)r;
            }
        }
    }
}
