using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextExtraction
{
    internal static class FilterData
    {
        public static string RemoveSpecialCharacters(this string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(str, @"[^\w\s(#\-@&$:\/.,|)]", string.Empty, RegexOptions.IgnoreCase).TrimEnd();
        }
    }
}
