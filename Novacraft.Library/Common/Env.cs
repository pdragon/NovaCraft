using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novacraft.Library.Common
{
    public class Env
    {
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

        public static string CopyUpdaterToTemp()
        {
            // 1. Получаем путь к директории текущего исполняемого файла
            string currentAppDir = AppContext.BaseDirectory;

            // 2. Формируем полный путь к Updater.exe
            string updaterPath = Path.Combine(currentAppDir, "Updater.exe");

            // 3. Проверяем существование файла
            if (!File.Exists(updaterPath))
            {
                throw new FileNotFoundException($"Файл Updater.exe не найден в директории: {currentAppDir}");
            }

            // 4. Создаем путь для копии во временной директории
            string tempFilePath = Path.Combine(Path.GetTempPath(), "Updater.exe");

            // 5. Копируем с перезаписью существующего файла
            File.Copy(updaterPath, tempFilePath, overwrite: true);

            Console.WriteLine($"Updater.exe скопирован в: {tempFilePath}");
            return tempFilePath;
        }
    }
}
