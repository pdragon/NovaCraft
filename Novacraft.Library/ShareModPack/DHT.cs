using csdl;
using csdl.Enums;
using MonoTorrent;
using MonoTorrent.BEncoding;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Novacraft.Library.ShareModPack
{

    public record TorrentSessionState(
        string TorrentPath,
        csdl.Enums.TorrentState State,
        double Progress,
        long DownloadSpeed,
        long UploadSpeed,
        string StatusText
    );

    public class TorrentService : IDisposable
    {
        // ЖЁСТКО ЗАДАННЫЙ ПУТЬ
        private string TorrentFilePath = @"d:/temp/10/seed.torrent";
        private string ContentPath = @"d:/temp/10/seed/"; // папка, которую раздаём
        private bool IsPrivate = false;

        /// <summary>
        /// Создаёт .torrent, совместимый со старыми C++ библиотеками (V1 Only)
        /// MonoTorrent 3.0.2 compatible
        /// </summary>
        public static async Task<string> CreateAsync(string sourceFolder, string outputTorrentPath, string announceUrl = null)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Папка не найдена: {sourceFolder}");

            // Конструктор: только TorrentType. PieceLength — свойство!
            var creator = new TorrentCreator(TorrentType.V1Only)
            {
                PieceLength = 512 * 1024, // 512 KB куски (через свойство!)
                Announce = announceUrl ?? "udp://tracker.opentrackr.org:1337/announce",
                //Announce = "udp://127.0.0.1:1/announce",
                Private = false,
                Comment = "",
                CreatedBy = "",
                Publisher = "",
                PublisherUrl = "",
            };

            var source = new TorrentFileSource(sourceFolder);

            Console.WriteLine("⏳ Генерация торрента (V1 Only)...");

            // CreateAsync возвращает BEncodedDictionary
            var dict = await creator.CreateAsync(source);

            // 🔪 УДАЛЯЕМ ПОЛЯ, ВЫЗЫВАЮЩИЕ SEH В СТАРЫХ C++ БИБЛИОТЕКАХ

            // Корневые поля
            if (dict.ContainsKey("nodes")) dict.Remove("nodes");      // DHT nodes
            if (dict.ContainsKey("url-list")) dict.Remove("url-list"); // Web seeds

            // Внутренний info-словарь
            if (dict.TryGetValue("info", out BEncodedValue infoVal) && infoVal is BEncodedDictionary infoDict)
            {
                // Поля BitTorrent V2 (BEP 52) — ломают старые парсеры
                if (infoDict.ContainsKey("meta version")) infoDict.Remove("meta version");
                if (infoDict.ContainsKey("piece layers")) infoDict.Remove("piece layers");

                // Padding files — часто не понимаются
                if (infoDict.ContainsKey("padding file")) infoDict.Remove("padding file");

                // Source field — может вызывать проблемы
                if (infoDict.ContainsKey("source")) infoDict.Remove("source");
            }

            // Кодируем в байты
            byte[] data = dict.Encode();

            // Валидация: первый байт должен быть 'd' (0x64)
            if (data.Length == 0 || data[0] != 0x64)
                throw new InvalidOperationException("Генерация не удалась: некорректный Bencode.");

            await File.WriteAllBytesAsync(outputTorrentPath, data);

            Console.WriteLine($"✅ Торрент создан: {outputTorrentPath}");
            Console.WriteLine($"📊 Размер: {new FileInfo(outputTorrentPath).Length / 1024:F1} КБ");
            return outputTorrentPath;
        }

        // Хранилище сессий. Ключ = путь к .torrent
        private readonly ConcurrentDictionary<string, (TorrentManager Manager, CancellationTokenSource Cts)> Sessions = new();

        // Клиент (без подчёркивания, как вы просили)
        public readonly TorrentClient Client;

        public event Action<TorrentSessionState>? SessionUpdated;

        public TorrentService()
        {
            var config = new TorrentClientConfig
            {
                ForceEncryption = true,
                MaxConnections = 128,
                UserAgent = "NovacraftLauncher/1.0"
            };
            Client = new TorrentClient(config);
        }

        public async Task StartSeedAsync(string torrentPath, string contentPath, IProgress<TorrentSessionState>? progress = null)
        {
            torrentPath = Path.GetFullPath(torrentPath);
            contentPath = Path.GetFullPath(contentPath).TrimEnd('\\', '/');
            contentPath = @"D:\\tmp\\instances";

            if (!File.Exists(torrentPath))
                throw new FileNotFoundException($"Торрент не найден: {torrentPath}");
            if (!Directory.Exists(contentPath))
                throw new DirectoryNotFoundException($"Папка контента не найдена: {contentPath}");

            if (Sessions.ContainsKey(torrentPath)) return;

            var cts = new CancellationTokenSource();

            try
            {
                var torrentInfo = new TorrentInfo(torrentPath);

                // Диагностика строго через Metadata, как вы указали
                Console.WriteLine($"[DIAG] InfoHash: {torrentInfo.Metadata.InfoHash}");
                Console.WriteLine($"[DIAG] Name: {torrentInfo.Metadata.Name}");
                Console.WriteLine($"[DIAG] Files count: {torrentInfo.Files?.Count() ?? 0}");

                if (torrentInfo.Metadata.InfoHash == null)
                    throw new InvalidOperationException("csdl не извлекла InfoHash. Файл повреждён или несовместим.");

                // Точное соответствие вашему Seeder.cs
                Console.WriteLine("[DIAG] Вызов AttachTorrent...");
                var manager = Client.AttachTorrent(torrentInfo, contentPath);

                if (manager == null)
                    throw new InvalidOperationException("AttachTorrent вернул null. Проверьте пути и права доступа.");

                Sessions.TryAdd(torrentPath, (manager, cts));

                Console.WriteLine("=== ПРОВЕРКА ФАЙЛОВ ===");
                Console.WriteLine($"ContentPath: {contentPath}");
                Console.WriteLine($"Torrent files: {torrentInfo.Files?.Count() ?? 0}");

                if (torrentInfo.Files != null)
                {
                    int match = 0, mismatch = 0;
                    foreach (var file in torrentInfo.Files)
                    {
                        // 🔹 НОРМАЛИЗАЦИЯ ПУТИ: заменяем все слеши на системные
                        string relativePath = file.Path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                        string fullPath = Path.GetFullPath(Path.Combine(contentPath, relativePath));

                        bool exists = File.Exists(fullPath);
                        long actualSize = exists ? new FileInfo(fullPath).Length : -1;
                        long expectedSize = file.FileSize;

                        if (exists && actualSize == expectedSize)
                            match++;
                        else
                        {
                            mismatch++;
                            // 🔹 Выводим полный путь для отладки
                            Console.WriteLine($"❌ Mismatch: {file.Path}");
                            Console.WriteLine($"   Full path checked: {fullPath}");
                            Console.WriteLine($"   Expected: {expectedSize} bytes");
                            Console.WriteLine($"   Found: {(exists ? actualSize : -1)} bytes");
                        }
                    }
                    Console.WriteLine($"✅ Matched: {match}, Mismatched: {mismatch}");
                }
                Console.WriteLine("=== END CHECK ===");


                manager.Start();

                Console.WriteLine("[OK] Сессия создана. Запуск мониторинга...");
                _ = MonitorLoopAsync(torrentPath, manager, cts.Token, progress);
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"[CRITICAL] NRE внутри csdl. Стек: {ex.StackTrace}");
                throw new InvalidOperationException("Внутренняя ошибка csdl (NullReferenceException).", ex);
            }
        }

        public void Stop(string torrentPath)
        {
            if (Sessions.TryRemove(torrentPath, out var session))
                session.Cts.Cancel();
        }

        private async Task MonitorLoopAsync(string torrentPath, TorrentManager manager, CancellationToken ct, IProgress<TorrentSessionState>? progress)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var status = manager.GetCurrentStatus();

                    var state = new TorrentSessionState(
                        TorrentPath: torrentPath,
                        State: status.State,
                        Progress: status.Progress,
                        DownloadSpeed: status.DownloadRate,
                        UploadSpeed: status.UploadRate,
                        StatusText: status.State switch
                        {
                            TorrentState.Seeding => $"Раздача: ⬆️ {status.UploadRate / 1024:N0} КБ/с",
                            TorrentState.Downloading => $"Скачивание: {status.Progress:P0} ⬇️ {status.DownloadRate / 1024:N0} КБ/с",
                            TorrentState.CheckingFiles => $"Проверка файлов: {status.Progress:P0}",
                            TorrentState.Finished => "✅ Завершено",
                            _ => $"{status.State} ({status.Progress:P0})"
                        }
                    );

                    progress?.Report(state);
                    SessionUpdated?.Invoke(state);

                    // Лог только при изменении состояния или раз в 5 сек, чтобы не спамить
                    Console.WriteLine($"[DEBUG] {Path.GetFileName(torrentPath)} | {status.State} | P:{status.PeerCount} S:{status.SeedCount} | Up:{status.UploadRate / 1024:N0}KB/s");

                    await Task.Delay(1000, ct);
                }
            }
            finally
            {
                manager.Stop();
                Client.DetachTorrent(manager);
                Sessions.TryRemove(torrentPath, out _);
            }
        }

        public void Dispose()
        {
            foreach (var session in Sessions.Values) session.Cts.Cancel();
            Client.Dispose();
        }

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
