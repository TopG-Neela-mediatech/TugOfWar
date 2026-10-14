using System.Security.Cryptography;
using System.Text;

using System.IO;
using System;
public class EncyptedDecryptedData 
{
    
      private static readonly string EncryptionKey = "TMK0CPL$YSC00L@N##LA"; 
  public static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            Array.Resize(ref keyBytes, 16); // Ensure the key is 16 bytes
            aes.Key = keyBytes;
            aes.IV = new byte[16];

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (var writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(plainText);
                    }
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    // Decryption
    public static string Decrypt(string encryptedText)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            Array.Resize(ref keyBytes, 16); // Ensure the key is 16 bytes
            aes.Key = keyBytes;
            aes.IV = new byte[16];

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
            {
                using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }

     public static byte[] Encrypt(byte[] plainBytes)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = new byte[16]; // Initialization vector (16 bytes)
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                }
                return ms.ToArray();
            }
        }
    }

    
    public static byte[] Decrypt(byte[] cipherBytes)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = new byte[16]; // Initialization vector (16 bytes)
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                var result = new MemoryStream();
                cs.CopyTo(result);
                return result.ToArray();
            }
        }
    }

}
