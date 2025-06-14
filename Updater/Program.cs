using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            var installer = new SfxInstaller();
            installer.Main();
        }

        //static void Main(string[] args)
        //{
        //    string extractPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        //    string currentExePath = Process.GetCurrentProcess().MainModule?.FileName
        //                            ?? throw new InvalidOperationException("Не удалось получить путь к EXE.");

        //    byte[] exeBytes = File.ReadAllBytes(currentExePath);
        //    string magic = "SFX_START"; // Уникальный маркер начала ZIP-данных

        //    // Поиск маркера в бинарнике
        //    int zipStart = FindMagicPosition(exeBytes, Encoding.ASCII.GetBytes(magic));
        //    if (zipStart == -1)
        //        throw new InvalidDataException("ZIP-данные не найдены.");

        //    using (var stream = new MemoryStream(exeBytes, zipStart + magic.Length, exeBytes.Length - zipStart - magic.Length))
        //    using (var archive = new ZipArchive(stream))
        //    {
        //        archive.ExtractToDirectory(extractPath, overwriteFiles: true);
        //        Console.WriteLine($"Файлы распакованы в: {extractPath}");
        //    }
        //}

        //private static int FindMagicPosition(byte[] data, byte[] magic)
        //{
        //    for (int i = data.Length - magic.Length; i >= 0; i--)
        //    {
        //        bool found = true;
        //        for (int j = 0; j < magic.Length; j++)
        //        {
        //            if (data[i + j] != magic[j])
        //            {
        //                found = false;
        //                break;
        //            }
        //        }
        //        if (found) return i;
        //    }
        //    return -1;
        //}
    }
}