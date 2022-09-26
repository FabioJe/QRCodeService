using System.Security.Cryptography;
using System.Text;

namespace QRCodeService
{
    public static class StringEncoder
    {
        public static (string PublicKey, string PrivateKey) GetNewKeyPair()
        {
            var rsa = new RSACryptoServiceProvider();
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);
            return (publicKey, privateKey);
        }

        public static string Decrypt(string data, string privateKey)
        {
            var encoder = new UnicodeEncoding();
            byte[] bytes = Convert.FromBase64String(data);
            var slc = bytes.Slices(128, true);
            var output = new List<byte>(bytes.Length / 2);
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            foreach (var item in slc)
            {
                var decryptedByte = rsa.Decrypt(item, false);
                output.AddRange(decryptedByte);
            }
            return encoder.GetString(output.ToArray());
        }

        public static string Encrypt(string data, string publicKey)
        {
            var encoder = new UnicodeEncoding();
            var dataToEncrypt = encoder.GetBytes(data);
            var slc = dataToEncrypt.Slices(64, true);
            List<byte> output = new List<byte>(dataToEncrypt.Length * 2);
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            foreach (var item in slc)
            {
                var encryptedByteArray = rsa.Encrypt(item, false);
                output.AddRange(encryptedByteArray);
            }
            return Convert.ToBase64String(output.ToArray());
        }

        private static T[] CopySlice<T>(this T[] source, int index, int length, bool padToLength = false)
        {
            int n = length;
            T[]? slice = null;

            if (source.Length < index + length)
            {
                n = source.Length - index;
                if (padToLength)
                {
                    slice = new T[length];
                }
            }

            if (slice == null) slice = new T[n];
            Array.Copy(source, index, slice, 0, n);
            return slice;
        }
        private static IEnumerable<T[]> Slices<T>(this T[] source, int count, bool padToLength = false)
        {
            for (var i = 0; i < source.Length; i += count)
                yield return source.CopySlice(i, count, padToLength);
        }
    }
}
