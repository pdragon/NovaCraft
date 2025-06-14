using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;


namespace Novacraft.Library
{
    public class FolderArchiver
    {
        public enum OperationType
        {
            Pack,
            Extract,
            Unknown
        }
        public void Start(OperationType opType, string source, string output, List<string> excludePatterns = null)
        {
            //CreateZip(source, output, excludePatterns);
            Console.WriteLine($"Archive created: {output}");
            switch (opType)
            {
                case OperationType.Pack:
                    CreateZip(source, output, excludePatterns);
                    break;

                case OperationType.Extract:
                    ExtractZip(source, output);
                    Console.WriteLine($"Archive extracted to: {output}");
                    break;

                default:
                    Console.WriteLine($"Unknown command: {source}");
                    break;
            }
        }

        void CreateZip(string sourceDir, string outputZip, List<string> excludePatterns)
        {
            if (File.Exists(outputZip))
                File.Delete(outputZip);

            using (var zipToOpen = new FileStream(outputZip, FileMode.Create))
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                var allFiles = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories);
                foreach (var filePath in allFiles)
                {
                    string relativePath = Path.GetRelativePath(sourceDir, filePath);
                    if (IsExcluded(relativePath, excludePatterns))
                        continue;

                    archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.NoCompression);
                }
            }
        }  
        
        bool IsExcluded(string relativePath, List<string> excludeFiles)
        {
            if (excludeFiles == null)
            {
                return false;
            }
            foreach (var filename in excludeFiles)
            {

                var normalizedPat = filename.Replace('/', Path.DirectorySeparatorChar)
                                        .Replace('\\', Path.DirectorySeparatorChar);
                var folderArr = Path.GetDirectoryName(relativePath).Split(Path.DirectorySeparatorChar);
                var folder =  folderArr.Count() > 0 ? folderArr[0] : "";
                if (folder == normalizedPat)
                {
                    return true;
                }
                if (normalizedPat == relativePath)
                {
                    return true;
                }
            }
            return false;
        }

        //bool IsExcluded(string relativePath, List<string> patterns)
        //{
        //    if(patterns == null)
        //    {
        //        return false;
        //    }

        //    foreach (var pat in patterns)
        //    {
        //        // Normalize pattern for directory separators
        //        var normalizedPat = pat.Replace('/', Path.DirectorySeparatorChar)
        //                                .Replace('\\', Path.DirectorySeparatorChar);

        //        // Simple wildcard match: supports '*' only
        //        if (WildcardMatch(relativePath, normalizedPat))
        //            return true;
        //    }
        //    return false;
        //}

        //bool WildcardMatch(string text, string pattern)
        //{
        //    // Split by wildcard
        //    var parts = pattern.Split('*');
        //    int pos = 0;
        //    bool first = true;
        //    foreach (var part in parts)
        //    {
        //        if (string.IsNullOrEmpty(part))
        //        {
        //            // '**' or '*' edge cases
        //            first = false;
        //            continue;
        //        }
        //        int idx = text.IndexOf(part, pos, StringComparison.OrdinalIgnoreCase);
        //        if (idx < 0 || (first && idx != 0))
        //            return false;
        //        pos = idx + part.Length;
        //        first = false;
        //    }
        //    // If pattern does not end with '*', ensure match reaches end
        //    if (!pattern.EndsWith("*", StringComparison.Ordinal))
        //        return pos == text.Length;
        //    return true;
        //}

        void ExtractZip(string zipFilePath, string outputDir)
        {
            if (!File.Exists(zipFilePath))
            {
                Console.WriteLine($"Error: File not found: {zipFilePath}");
                return;
            }
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // Распаковка всего архива
            ZipFile.ExtractToDirectory(zipFilePath, outputDir);
        }

        /// <summary>
        /// Создаёт уникальную временную папку и возвращает её путь.
        /// </summary>
        public static string CreateUniqueTempDirectory(string subfolderName = null)
        {
            // Базовый путь к системной временной папке
            string baseTemp = Path.GetTempPath();

            // Опционально: вложенная папка для вашего приложения
            if (!string.IsNullOrEmpty(subfolderName))
                baseTemp = Path.Combine(baseTemp, subfolderName);

            // Уникальное имя папки
            string uniqueDir = Path.Combine(baseTemp, Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(uniqueDir);
            return uniqueDir;
        }

        public static void CreateInstaller(string launcherExe, string zipPath, string outputPath)
        {
            byte[] launcherBytes = File.ReadAllBytes(launcherExe);
            byte[] zipBytes = File.ReadAllBytes(zipPath);

            // Получаем размер распаковщика в виде 8 байт (long)
            byte[] sizeBytes = BitConverter.GetBytes((long)launcherBytes.Length);

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                // Порядок записи:
                fs.Write(launcherBytes, 0, launcherBytes.Length);  // 1. Распаковщик
                fs.Write(zipBytes, 0, zipBytes.Length);            // 2. ZIP-архив
                fs.Write(sizeBytes, 0, sizeBytes.Length);          // 3. Размер распаковщика (8 байт)
            }
        }
    }
}
