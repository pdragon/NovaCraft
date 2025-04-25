using Renci.SshNet;
using System;
using System.IO;

//using System.Collections.Generic;
//using System.Linq;
using System.Net;
using System.Text;
//using System.Threading.Tasks;

namespace Novacraft.Library.ShareModPack
{
    public class Ssh
    {
        private ScpClient Scp;
        public Ssh(string host, int port, string username, string password) {
            Scp = new ScpClient(host, port, username, password);
        }
        public void Upload(DirectoryInfo sourcePath, string destPath)
        {
            Scp.Connect();
            Scp.Upload(sourcePath, destPath);
            Scp.Disconnect();
            //return;
        }
    }
}
