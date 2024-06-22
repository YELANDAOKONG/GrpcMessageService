using System.Text;

namespace MessageService.Utils;

public class SignUtils
{
    public static string AuthorizationPasswordSign(string password, long timestamp)
    {
        long realTimestamp = timestamp / (1000 * 30);
        string result = "";
        result = HashUtils.GetSha384(
            Encoding.UTF8.GetBytes(realTimestamp.ToString() + password + realTimestamp.ToString())
        );
        result = HashUtils.GetSha512(
            Encoding.UTF8.GetBytes(realTimestamp.ToString() + result + realTimestamp.ToString())
        );
        return  result;
    }
    
    public static string AuthorizationTokenSign(string token, long timestamp)
    {
        long realTimestamp = timestamp / (1000 * 30);
        string result = "";
        result = HashUtils.GetSha256(
            Encoding.UTF8.GetBytes(realTimestamp.ToString() + token + realTimestamp.ToString())
        );
        result = HashUtils.GetSha384(
            Encoding.UTF8.GetBytes(realTimestamp.ToString() + result + realTimestamp.ToString())
        );
        result = HashUtils.GetSha512(
            Encoding.UTF8.GetBytes(realTimestamp.ToString() + result + realTimestamp.ToString())
        );
        return  result;
    }
    
    
}