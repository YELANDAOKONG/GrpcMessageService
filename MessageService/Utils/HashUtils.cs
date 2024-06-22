using System.Security.Cryptography;

namespace MessageService.Utils;

public class HashUtils
{
    public static string GetSha256(byte[] data)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] hash = sha256Hash.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
    
    public static string GetSha384(byte[] data)
    {
        using (SHA384 sha384Hash = SHA384.Create())
        {
            byte[] hash = sha384Hash.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
    
    public static string GetSha512(byte[] data)
    {
        using (SHA512 sha512Hash = SHA512.Create())
        {
            byte[] hash = sha512Hash.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}