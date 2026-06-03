using System.IO;

namespace DetailTrack.Helpers
{
    public static class FileHelper
    {
        private static string FilesFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Files");

        public static string EnsureFilesFolderExists()
        {
            Directory.CreateDirectory(FilesFolder);
            return FilesFolder;
        }

        public static string SaveFileAndGetRelativePath(string sourceFilePath, int requestId, string fileType)
        {
            EnsureFilesFolderExists();

            string ext = Path.GetExtension(sourceFilePath);
            string uniqueName = $"REQ_{requestId}_{Guid.NewGuid():N}{ext}";
            string destAbsolutePath = Path.Combine(FilesFolder, uniqueName);

            File.Copy(sourceFilePath, destAbsolutePath, true);

            return Path.Combine("Data", "Files", uniqueName);
        }

        public static string GetAbsolutePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            if (Path.IsPathRooted(relativePath))
                return relativePath;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }

        public static bool FileExists(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            string absPath = GetAbsolutePath(relativePath);
            return File.Exists(absPath);
        }

        public static bool OpenFile(string relativePath)
        {
            if (!FileExists(relativePath))
                return false;

            try
            {
                string absPath = GetAbsolutePath(relativePath);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = absPath,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
