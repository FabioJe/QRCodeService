namespace QRCodeService.Models
{
    public sealed class CreateApiKey
    {
        public CreateApiKey(string apiKey, string publicKey, string profilID)
        {
            ApiKey = apiKey;
            PublicKey = publicKey;
            ProfilID = profilID;
        }

        public string ApiKey { get; set; }
        public string PublicKey { get; set; }
        public string ProfilID { get; set; }
    }
}
