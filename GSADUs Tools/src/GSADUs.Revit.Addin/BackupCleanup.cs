using System.IO;
using System.Text.RegularExpressions;

namespace GSADUs.Revit.Addin
{
    internal static class FileCleanup
    {
        static readonly Regex RvtBackupRx =
            new Regex(@"\.\d{3,4}\.rvt$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int DeleteRvtBackups(string directory)
        {
            if (!Directory.Exists(directory)) return 0;
            int n = 0;
            foreach (var file in Directory.EnumerateFiles(directory, "*.rvt", SearchOption.TopDirectoryOnly))
            {
                if (RvtBackupRx.IsMatch(file))
                    try { File.Delete(file); n++; } catch { }
            }
            return n;
        }
    }
}
