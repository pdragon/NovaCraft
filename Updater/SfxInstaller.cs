using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Updater
{
    public class SfxInstaller
    {
        public void Main()
        {
            try
            {
                // 1. Получаем путь к исполняемому файлу
                string exePath = GetExecutablePath();
                //var exePathSplit = exePath.Split();
                //string tempZip = Path.Combine(Path.GetTempPath(), exePathSplit[exePathSplit.Length - 1]);

                //string exePath = "D:\\Documents\\Coding\\projects\\CSharp\\NovaCraft\\Updater\\bin\\Debug\\net8.0\\Updater1.exe";
                //string exePath = "D:\\Documents\\Coding\\projects\\CSharp\\NovaCraft\\Updater\\bin\\Debug\\net8.0\\Updater1.exe";

                // 2. Получаем размер файла
                long totalSize = new FileInfo(exePath).Length;
                if (totalSize < 1024)
                {
                    throw new InvalidDataException("File too small");
                }

                // 3. Читаем последние 8 байт (размер распаковщика)
                byte[] sizeBytes = new byte[8];
                using (FileStream fs = new FileStream(exePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(-8, SeekOrigin.End);
                    fs.Read(sizeBytes, 0, 8);
                }

                if (sizeBytes.Where(a => a == 0).Count() == 8)
                {
                    throw new InvalidDataException("File not contain archive");
                }

                long launcherSize = BitConverter.ToInt64(sizeBytes, 0);
                long zipStart = launcherSize;
                long zipSize = totalSize - launcherSize - 8;

                // 4. Проверка валидности размеров
                if (launcherSize <= 0 || zipSize <= 0 || zipStart + zipSize > totalSize)
                {
                    throw new InvalidOperationException("Invalid bundle structure");
                }

                // 5. Извлекаем и распаковываем архив
                string tempZip = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".zip");
                ExtractFileSegment(exePath, zipStart, zipSize, tempZip);

                //string installDir = Path.Combine(
                //    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                //    "MyApp");
                string installDir = Path.Combine(
                    Environment.CurrentDirectory,
                    "MyApp");
                Console.WriteLine(installDir);
                ZipFile.ExtractToDirectory(tempZip, installDir);
                File.Delete(tempZip);

                //// 6. Запускаем приложение
                //Process.Start(Path.Combine(installDir, "MyApp.exe"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Installation failed: {ex.Message}");
                Console.ReadKey();
            }
        }

        // Кроссплатформенное определение пути к исполняемому файлу
        public static string GetExecutablePath()
        {
            // Способ 1: Для .NET 5+ и Single-File приложений
            if (Environment.ProcessPath is string path && !string.IsNullOrEmpty(path))
                return path;

            // Способ 2: Для .NET Framework и классических приложений
            try
            {
                return System.Reflection.Assembly.GetEntryAssembly()?.Location!;
            }
            catch { }

            // Способ 3: Через аргументы командной строки (Windows)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Environment.GetCommandLineArgs()[0];
            }

            throw new NotSupportedException("Unable to determine executable path");
        }

        // Извлечение сегмента файла
        private void ExtractFileSegment(string source, long start, long length, string target)
        {
            using (FileStream inStream = new FileStream(source, FileMode.Open, FileAccess.Read))
            using (FileStream outStream = new FileStream(target, FileMode.Create))
            {
                inStream.Seek(start, SeekOrigin.Begin);
                byte[] buffer = new byte[81920];
                long bytesRemaining = length;

                while (bytesRemaining > 0)
                {
                    int read = inStream.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesRemaining));
                    if (read == 0) break;

                    outStream.Write(buffer, 0, read);
                    bytesRemaining -= read;
                }
            }
        }
    }
}