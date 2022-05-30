using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DebugApp.Misc
{
    public static class StringExtensions
    {
        public static string Filter(this string str, List<char> charsToRemove)
        {
            var chars = $"[{String.Concat(charsToRemove)}]";
            return Regex.Replace(str, $"[{String.Concat(charsToRemove)}]", String.Empty);
        }
    }
}
