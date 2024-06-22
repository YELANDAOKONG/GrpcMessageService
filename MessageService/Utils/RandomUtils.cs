namespace MessageService.Utils;

public class RandomUtils
{
    public static int GetRandomNumber()
    {
        Random random = new Random();
        return random.Next(0, 100000000);
    }

    public static string RandomToken()
    {
        return Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
    }
}