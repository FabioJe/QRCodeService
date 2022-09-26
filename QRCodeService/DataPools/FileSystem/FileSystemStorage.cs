using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QRCodeService.DataPools.FileSystem
{
    public class FileSystemStorage : IDataInterface
    {
        private readonly object filelockObject = new();
        private readonly object insertlockObject = new();
        private readonly FileData fileData;
        private readonly string file;
        private readonly string[] adminKeys;
        private readonly object lockKey = new();

        public FileSystemStorage(string file, string[] adminKeys)
        {
            var fileInfo = new FileInfo(file);
            this.file = fileInfo.FullName;
            if (File.Exists(file))
            {
                var text = File.ReadAllText(file, Encoding.Unicode);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var objectD = JsonSerializer.Deserialize<FileData>(text);
                    if (objectD is null)
                        objectD = new();
                    fileData = objectD;
                }
                else
                    fileData = new();
                
            }
            else
                fileData = new();
            this.adminKeys = adminKeys;
        }


        private void SaveFile()
        {
            lock (filelockObject)
            {
                var text = JsonSerializer.Serialize(fileData, new JsonSerializerOptions { WriteIndented = false });
                File.WriteAllText(file, text, Encoding.Unicode);
            }

        }

        public string? GetPrivateKey(string profileid)
        {
            if (fileData.Collection is null)
                return null;
            for (int i = fileData.Collection.Count - 1; i >= 0; i--)
            {
                if (fileData.Collection[i].ProfileID == profileid) return fileData.Collection[i].PrivateKey;
            }
            return null;
        }

        public string? GetPublicKey(string apikey)
        {
            if (fileData.Collection is null)
                return null;
            for (int i = fileData.Collection.Count - 1; i >= 0; i--)
            {
                if (fileData.Collection[i].ApiKey == apikey) return fileData.Collection[i].PublicKey;
            }
            return null;
        }

        public void InsertNewProfile(string profileid, string apikey, string privateKey, string publicKey)
        {
            lock (insertlockObject)
            {
                if (fileData.Collection is null)
                    fileData.Collection = new();
                var objectData = fileData.Collection.FirstOrDefault(x => x.ProfileID == profileid);
                if (objectData is null)
                    fileData.Collection.Add(new DataTuble { ApiKey = apikey, PrivateKey = privateKey, PublicKey = publicKey, ProfileID = profileid });
                else
                    throw new ArgumentException($"Profile with ID '{profileid}' already exists!");
            }
            SaveFile();
        }

        public void DeleteProfile(string profileid)
        {
            if (fileData.Collection is null) return;
            lock (insertlockObject)
            {
                var item = fileData.Collection.FirstOrDefault(x => x.ProfileID == profileid);
                if (item is not null)
                    fileData.Collection.Remove(item);
            }

            SaveFile();
        }

        public bool CheckAdminKey(string apikey)
        {
            lock (lockKey)
                return adminKeys.Contains(apikey);
        }
    }
}
