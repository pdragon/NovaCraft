using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blowaunch.Library.UsableClasses.ShareModPack
{
    public class ExportFileParams
    {
        
        public enum ShareType
        {
            //Ftp,
            //Http,
            //Ssh,
            //DropMeFiles,
            BitTorrent,
            Synthing
        }

        public class NamedPair
        {
            public string Name { get; set; } = string.Empty;
            public ShareType Value { get; set; }
        }

        public static List<NamedPair> ShareTypeList = new List<NamedPair>() {
            //new NamedPair(){ Name = ShareType.Ftp.ToString(), Value = ShareType.Ftp },
            //new NamedPair(){ Name = ShareType.Http.ToString(), Value = ShareType.Http },
            //new NamedPair(){ Name = ShareType.Ssh.ToString(), Value = ShareType.Ssh },
            //new NamedPair(){ Name = ShareType.DropMeFiles.ToString(), Value = ShareType.DropMeFiles },
            new NamedPair(){ Name = ShareType.BitTorrent.ToString(), Value = ShareType.BitTorrent },
            new NamedPair(){ Name = ShareType.Synthing.ToString(), Value = ShareType.Synthing },
        };

        public class ShareAccount
        {
            [JsonProperty("guid")] public string Guid { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("login")] public string Login { get; set; }
            [JsonProperty("password")] public string Password { get; set; }
            [JsonProperty("server")] public string Server { get; set; }
            [JsonProperty("need_auth")] public bool NeedAuth { get; set; }
            [JsonProperty("upload_dir")] public string UploadDir { get; set; }
            /// <summary>
            /// if http selected then ShareAccount is upload only credentials
            /// </summary>
            public ShareType UploadThrough { get; set; }
        }

        [JsonProperty("type")] public ShareType Type { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("account")] public ShareAccount Account { get; set; }
        [JsonProperty("instance_uuid")] public string InstanceUUID { get; set; }

        static public List<ShareAccount> LoadConfig()
        {
            string filePath = Path.Combine(Library.FilesManager.Directories.Root, $"share.json");
            var config = JsonConvert.DeserializeObject<List<ShareAccount>>(File.ReadAllText(filePath));
            if (config != null)
            {
                return config;
            }
            return new List<ShareAccount>();
        }
    }
}
