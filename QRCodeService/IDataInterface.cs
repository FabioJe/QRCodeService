namespace QRCodeService
{
    public interface IDataInterface
    {
        string? GetPrivateKey(string profileid);
        string? GetPublicKey(string apikey);
        void InsertNewProfile(string profileid, string apikey, string privateKey, string publicKey);
        void DeleteProfile(string profileid);
        bool CheckAdminKey(string apikey);
    }
}
