using System.Net.Sockets;
using SOCKS_Proxy.Control;

namespace SOCKS_Proxy.Main;

public class MainClass
{
    private static bool IsWorking = true;
    
    public static void Main(string[] args)
    {
        Console.Clear();
        
        var port = ReadPort(args);
        
        var server = new Server(port);
        Task.Run(() => server.Start());
        
        Console.CancelKeyPress += (_, e) =>
        {
            IsWorking = false;
            e.Cancel = true;
            server.Stop();
        };
        
        ReadCommands(server);
    }
    
    private static int ReadPort(string[] args)
    {
        if (args.Length <= 0) return 0;
        try
        {
            return int.Parse(args[0]);
        }
        catch (Exception)
        {
            Console.WriteLine("USAGE:\ndotnet Server.dll <port>\n");
            Environment.Exit(-1);
        }
        return 0;
    }
    
    private static void ReadCommands(Server server)
    {
        while (IsWorking)
        {
            var str = Console.ReadLine();
            if (str != null)
            {
                switch (str)
                {
                    case "port": Console.WriteLine("Port: " + server.GetPort()); break;
                    case "ip": var addresses = Server.GetIpAddress();
                        foreach (var addr in addresses)
                        {
                            if (addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                Console.WriteLine(addr.ToString());
                            }
                        }
                        break;
                    case "dns": Console.WriteLine("DNS: " + Server.GetDns()); break;
                    case "exit": server.Stop(); return;
                }
            }
            else
            {
                server.Stop();
                return;
            }
        }
    }
}