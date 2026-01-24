using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            string ExecPath = SfxInstaller.GetExecutablePath();
            var installer = new SfxInstaller();
            // Если лаунчер не установлен то скачиваем лаунчер и делаем все проверки и установки.
            if (installer.Installing())
            {
                // Если архив распакован то устанавливаем сборку запуская лаунчер из командной строки, видимо консольный есть смысл автоматизировать таким образом.
            }
            

        }
    }
}