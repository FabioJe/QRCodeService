using MySql.Data.MySqlClient;
using System.Runtime.Caching;
namespace QRCodeService.DataPools.MySql
{
    public sealed class MySqlData : IDataInterface
    {
        private readonly string KEY_TABLE_NAME;
        private readonly string PUBLIC_KEY_SPACE;
        private readonly string PRIVATE_KEY_SPACE;
        private readonly string connectionString;
        private readonly string[] adminKeys;
        private readonly ObjectCache keyCache = MemoryCache.Default;
        private readonly object lockKey = new();

        public MySqlData(string connectionString, string tablePrefix, string[] adminKeys)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"\"{nameof(connectionString)}\" must not be NULL or a space character.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(tablePrefix))
            {
                throw new ArgumentException($"\"{nameof(tablePrefix)}\" must not be NULL or a space character.", nameof(tablePrefix));
            }

            PRIVATE_KEY_SPACE = Guid.NewGuid().ToString("N");
            PUBLIC_KEY_SPACE = Guid.NewGuid().ToString("N");
            KEY_TABLE_NAME = tablePrefix + "KEYS";
            this.connectionString = connectionString;
            this.adminKeys = adminKeys;
        }

        public bool CheckAdminKey(string apikey)
        {
            lock (lockKey)
                return adminKeys.Contains(apikey);
        }

        public void DeleteProfile(string profileid)
        {
            using (MySqlConnection connection = new(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"DELETE FROM {KEY_TABLE_NAME} WHERE `PROFID` = @profid";
                    command.Parameters.AddWithValue("@profid", profileid);
                    command.ExecuteNonQuery();
                }
            }
        }

        public string? GetPrivateKey(string profileid)
        {
            if (keyCache.Get(profileid, PRIVATE_KEY_SPACE) is not string publickey)
            {
                using (MySqlConnection connection = new(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT `PRIKEY` FROM {KEY_TABLE_NAME} WHERE `PROFID` = @profid";
                        command.Parameters.AddWithValue("@profid", profileid);
                        if (command.ExecuteScalar() is not string val)
                            return null;
                        keyCache.Add(profileid, val, DateTimeOffset.Now.AddMinutes(10), PRIVATE_KEY_SPACE);
                        return val;
                    }
                }
            }
            return publickey;
        }

        public string? GetPublicKey(string apikey)
        {
            if (keyCache.Get(apikey, PUBLIC_KEY_SPACE) is not string publickey)
            {
                using (MySqlConnection connection = new(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT `PKEY` FROM {KEY_TABLE_NAME} WHERE `APIKEY` = @apikey";
                        command.Parameters.AddWithValue("@apikey", apikey);
                        if (command.ExecuteScalar() is not string val)
                            return null;
                        keyCache.Add(apikey, val, DateTimeOffset.Now.AddMinutes(10), PUBLIC_KEY_SPACE);
                        return val;
                    }
                }
            }
            return publickey;

        }

        public void InsertNewProfile(string profileid, string apikey, string privateKey, string publicKey)
        {
            using (MySqlConnection connection = new(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO {KEY_TABLE_NAME} (`PROFID`,`APIKEY`,`PKEY`,`PRIKEY`) VALUES (@profid, @apikey, @pkey, @prikey)";
                    command.Parameters.AddWithValue("@profid", profileid);
                    command.Parameters.AddWithValue("@apikey", apikey);
                    command.Parameters.AddWithValue("@pkey", publicKey);
                    command.Parameters.AddWithValue("@prikey", privateKey);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
