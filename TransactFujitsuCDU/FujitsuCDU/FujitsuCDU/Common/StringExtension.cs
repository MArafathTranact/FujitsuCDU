using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU.Common
{
    public static class StringExtension
    {
        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {
            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        public static string Left(this string @this, int count)
        {
            if (@this.Length <= count)
            {
                return @this;
            }
            else
            {
                return @this.Substring(0, count);
            }
        }
        public static string GetLast(this string source, int tail_length)
        {
            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }

    }
}
