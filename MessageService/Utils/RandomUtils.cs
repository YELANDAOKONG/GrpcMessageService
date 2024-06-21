namespace MessageService.Utils;

public class RandomUtils
{
    public static int GetRandomNumber()
    {
        Random random = new Random();
        return random.Next(0, 100000000);
    }
}