using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using static System.Net.FtpWebRequest;
//using Spectre.Console;
using System.IO;
using System.Text.RegularExpressions;

namespace Novacraft.Library.ShareModPack
{
    public struct Entity
    {
        public bool isFile;
        public long size;
        public string name;
    }
    public class FTP
    {
        public bool DownloadFile(Uri serverUri)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return false;
            }
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Console.WriteLine("Delete status: {0}", response.StatusDescription);
            response.Close();
            return true;
        }

        public void UploadFile(string path, string url, Action<int> action, NetworkCredential credential)
        {
            int stepSize = 1048576;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = true;

            request.Credentials = credential;

            long fileSize = new FileInfo(path).Length;
            //response.Close();
            request.UseBinary = true;
            request.Method = WebRequestMethods.Ftp.UploadFile;

            //using (Stream ftpStream = request.GetResponse().GetResponseStream())
            using (Stream ftpStream = request.GetRequestStream())
            using (Stream fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                    byte[] buffer = new byte[stepSize];
               //     task.MaxValue = fileSize / stepSize;
                    int read;
                byte currentPart = 0;
                while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ftpStream.Write(buffer, 0, read);
                    action(currentPart);
                    //Console.WriteLine("Downloaded {0} bytes", fileStream.Position);
                    //task.Increment(1);
                    currentPart++;
                    //    task.Description = $"Uploading file {url}:{(int)task.Speed} MB/sec";
                }
                //});
            }
        }

        public List<Entity> ListFiles(string url)
        {
            const string regexStr = @"([\-d])([rwx]{9})(\s+)+([0-1])(\s+)([a-zA-Z0-9]+)(\s+)([a-zA-Z0-9]+)(\s+)([0-9]+)(\s+)+([a-zA-Z]{3})(\s+)([0-9]{1,2})(\s+)(\d{2,})([:]{0,1})(\d{2,})(\s+)([a-zA-Z0-9\s.]+)";
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(url);
            //ftpRequest.Credentials = new NetworkCredential("anonymous", "janeDoe@contoso.com");

            //ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());

            List<Entity> directories = new List<Entity>();

            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                Regex regex = new Regex(regexStr);
                MatchCollection matches = regex.Matches(line);
                if (matches.Count > 0)
                {
                    Entity entity = new Entity();
                    entity.name = matches[0].Groups[20].Value;
                    entity.isFile = matches[0].Groups[1].Value == "-";
                    entity.size = Int32.Parse(matches[0].Groups[10].Value);
                    directories.Add(entity);

                    //foreach (Match match in matches)
                    //    Console.WriteLine(match.Value);
                }

                line = streamReader.ReadLine();
            }

            streamReader.Close();
            return directories;
        }
    }
    


}
