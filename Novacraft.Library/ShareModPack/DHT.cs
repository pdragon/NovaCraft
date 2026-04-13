using csdl;
using csdl.Enums;
using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Novacraft.Library.ShareModPack
{
    public class DHT
    {
        // ЖЁСТКО ЗАДАННЫЙ ПУТЬ
        private string TorrentFilePath = @"d:/temp/10/seed.torrent";
        private string ContentPath = @"d:/temp/10/seed/"; // папка, которую раздаём
        private bool IsPrivate = false;

        /// <summary>
        /// Создаёт .torrent файл из папки. Работает строго на MonoTorrent 3.0.2+
        /// </summary>
        public static async Task<string> CreateAsync(string sourceFolder, string outputTorrentPath, string announceUrl = null)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Папка не найдена: {sourceFolder}");

            var creator = new TorrentCreator
            {
                Announce = announceUrl ?? "udp://tracker.opentrackr.org:1337/announce",
                Comment = "Created by Novacraft",
                Publisher = "Novacraft",
                Private = false // false = работает DHT/PEX (критично для локалки)
            };

            var source = new TorrentFileSource(sourceFolder);
            Console.WriteLine("⏳ Хэширование файлов...");

            // В 3.0.2 возвращается словарь метаданных
            var metadataDict = await creator.CreateAsync(source);

            // 👇 Кодируем словарь в байты (стандартный формат .torrent)
            byte[] torrentData = metadataDict.Encode();

            await File.WriteAllBytesAsync(outputTorrentPath, torrentData);
            var file = Path.GetFullPath(outputTorrentPath);
            Console.WriteLine($"✅ Готово: {file}");
            return file;
        }

        public DHT(string torrentPath, string contentPath)
        {
            TorrentFilePath = torrentPath;
            ContentPath = contentPath;

        }

        //public async Task Seeder()
        //{
        //    // Инициализация клиента
        //    var options = new TorrentClientConfig
        //    {
        //        ForceEncryption = true,
        //        MaxConnections = 100,
        //        UserAgent = "NovacraftLauncher/1.0"
        //    };

        //    using var client = new TorrentClient(options);

        //    // Включаем DHT и Local Peer Discovery (для локальной сети)
        //    // (по умолчанию должны быть включены, но для верности)
        //    // В csdl нет прямых свойств DhtEnabled, но можно через конфиг

        //    // Загружаем торрент
        //    var torrentInfo = new TorrentInfo(TorrentFilePath);

        //    // 2. ПОЛУЧАЕМ ИМЯ ТОРРЕНТА (обход проблемы)
        //    string torrentName = "Неизвестно";

        //    // Способ 1: Если есть доступ к Files (самый надежный)
        //    if (torrentInfo.Files != null && torrentInfo.Files.Any())
        //    {
        //        // Имя часто совпадает с именем папки первого файла
        //        var firstFile = torrentInfo.Files.First();
        //        torrentName = Path.GetDirectoryName(firstFile.Path) ?? Path.GetFileName(firstFile.Path);
        //        Console.WriteLine($"Имя из файлов: {torrentName}");
        //    }
        //    else
        //    {
        //        // Способ 2: Используем имя файла .torrent как запасной вариант
        //        torrentName = Path.GetFileNameWithoutExtension(TorrentFilePath);
        //        Console.WriteLine($"Имя из файла .torrent: {torrentName}");
        //    }

        //    // Альтернативный Способ 3 (если metadata работает):
        //    // Если в вашей версии есть доступ к метаданным, имя может лежать там.
        //    // Например: var nameFromMeta = torrentInfo.Metadata["name"]; // (гипотетически)
        //    // Но Способ 1 — самый надёжный.

        //    Console.WriteLine($"Раздаём: {torrentName}");

        //    // 3. Прикрепляем и стартуем
        //    var manager = client.AttachTorrent(torrentInfo, ContentPath);
        //    manager.Start();

        //    Console.WriteLine("Загрузка запущена. Нажмите Ctrl+C для остановки.");

        //    // Бесконечное ожидание
        //    var tcs = new TaskCompletionSource<bool>();
        //    Console.CancelKeyPress += (s, e) =>
        //    {
        //        e.Cancel = true;
        //        tcs.SetResult(true);
        //    };

        //    await tcs.Task;

        //    manager.Stop();
        //    client.DetachTorrent(manager);
        //    Console.WriteLine("Загрузка остановлена.");
        //}

        //async public Task Leecher()
        //{
        //    using var client = new TorrentClient();
        //    var torrentInfo = new TorrentInfo(TorrentFilePath);

        //    var manager = client.AttachTorrent(torrentInfo, ContentPath);
        //    manager.Start();

        //    // Ожидаем завершения загрузки
        //    var tcs = new TaskCompletionSource();
        //    using (new Timer(CheckProgress, null, TimeSpan.Zero, TimeSpan.FromSeconds(1)))
        //    {
        //        await tcs.Task;
        //    }

        //    void CheckProgress(object state)
        //    {
        //        var status = manager.GetCurrentStatus();
        //        //Console.Write($"\rПрогресс: {status.Progress:P1}  Скорость: {status.DownloadSpeed / 1024} КБ/с   ");
        //        Console.Write($"\rПрогресс: {status.Progress:P1}");

        //        if (status.State == TorrentState.Finished || status.State == TorrentState.Seeding)
        //        {
        //            Console.WriteLine("\nЗагрузка завершена!");
        //            tcs.SetResult();
        //        }
        //    }
        //}
    }
}
