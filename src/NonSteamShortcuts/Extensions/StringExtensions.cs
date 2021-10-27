using System.IO;
using System.Text.RegularExpressions;

namespace NonSteamShortcuts.Extensions
{
    internal static class StringExtensions
    {
        public static string FixSeparators(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var newPath = path.Replace('\\', Path.DirectorySeparatorChar);
            newPath = newPath.Replace('/', Path.DirectorySeparatorChar);
            return Regex.Replace(newPath, string.Format(@"\{0}+", Path.DirectorySeparatorChar), Path.DirectorySeparatorChar.ToString());
        }
    }
}
