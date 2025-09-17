namespace FinderCopies;

public class MainClass
{

    public static void Main(string[] args)
    {
        string ip;
        int port;
        
        (ip, port) = ParserArguments.Parse(args);
        
        var finderCopies = new FinderCopies(ip, port);
        finderCopies.Execute();
    }
}