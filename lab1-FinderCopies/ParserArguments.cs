namespace FinderCopies;

public class ParserArguments
{
    public static (string, int) Parse(string[] args)
    {
        var ip = "239.255.0.1";
        var port = 25565;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-a":
                {
                    if (i + 1 < args.Length)
                    {
                        ip = args[i + 1];
                    }
                    else
                    {
                        Console.WriteLine("USAGE: dotnet run\n" +
                                          "-a <multicast_group_ip> - for select ip address of multicast group\n" +
                                          "-p <multicast_group_port> - for select port of multicast group\n" +
                                          "-h or --help - for print this text");
                        Environment.Exit(1);
                    }
                    break;
                }
                case "-p":
                {
                    if (i + 1 < args.Length)
                    {
                        port = int.Parse(args[i + 1]);
                    }
                    else
                    {
                        Console.WriteLine("USAGE: dotnet run\n" +
                                          "-a - for select ip address\n" +
                                          "-p - for select port\n" +
                                          "-h or --help - for print this text");
                        Environment.Exit(1);
                    }
                    break;
                }
                case "-h" or  "--help":
                {
                    Console.WriteLine("USAGE: dotnet run\n" +
                                      "-a - for select ip address\n" +
                                      "-p - for select port\n" +
                                      "-h or --help - for print this text");
                    Environment.Exit(1);
                    break;
                }
            }
        }
        
        return (ip, port);
    }
}