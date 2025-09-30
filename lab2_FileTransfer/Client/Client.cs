using System.Net;
using System.Net.Sockets;
using Protocol.Constants;

namespace Client;

public class Client
{
    private readonly TcpClient _client;
    private readonly ServerHandler _serverHandler;
    private readonly Thread _thread;

    private static bool _running = true;
    
    private TransferData? _transferData;

    public static void Start(string[] args)
    {
        Console.Clear();
        
        var (port, ip, dns) = ParseArgs(args);

        Client client;
        if(ip != null)
        {
            client = new Client(ip, port);
        }
        else
        {
            client = new Client(dns, port);
        }
        
        _running = true;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _running = false;
            client.Exit();
        };
        
        ReadCommands(client);
    }

    public void Exit()
    {
        _running = false;
        _serverHandler.Close();
        _client.Close();
        _thread.Join();
        Environment.Exit(0);
    }

    private Client(IPAddress ip, int port)
    {
        _client = new TcpClient();
        _client.Connect(ip, port);
        _serverHandler = new ServerHandler(_client);
        _thread = new Thread(_ => _serverHandler.Receive());
        _thread.Start();
        _transferData = null;
    }
    
    private Client(string dns, int port)
    {
        _client = new TcpClient();
        _client.Connect(dns, port);
        _serverHandler = new ServerHandler(_client);
        _thread = new Thread(_ => _serverHandler.Receive());
        _thread.Start();
        _transferData = null;
    }

    private void SendFile(string filepath)
    {
        if (!File.Exists(filepath))
        {
            Console.WriteLine($"File {filepath} does not exist");
            return;
        }
        var fileInfo = new FileInfo(filepath);
        if (fileInfo.Name.Length > AppConstants.MaxLenghtName || fileInfo.Length > AppConstants.Terabyte)
        {
            Console.WriteLine($"File {filepath} does not supported");
            return;
        }
        
        _transferData = new TransferData(this, _serverHandler, filepath);
        _serverHandler.SetTransferData(_transferData);
        _transferData.SendFile();
    }

    private void Cancel()
    {
        _transferData?.Cancel();
    }
    
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet Client.dll <params>" +
                          "\n\t\"-a\" - ip address of server" +
                          "\n\t\"-p\\\" - port of server\"" +
                          "\n\t\"-d\" - domain name of server" +
                          "\nYou must specify port, also you must specify ip or dns.");
        
        Environment.Exit(-1);
    }

    private static (int, IPAddress?, string?) ParseArgs(string[] args)
    {
        IPAddress? ip = null;
        var port = -1;
        string? dns = null;
        if (args.Length == 0)
        {
            PrintUsage();
        }
        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-a":
                        ip = IPAddress.Parse(args[++i]);
                        break;
                    case "-p":
                        port = int.Parse(args[++i]);
                        break;
                    case "-d":
                        dns = args[++i];
                        break;
                    default:
                        PrintUsage();
                        break;
                }
            }
        }
        catch
        {
            PrintUsage();
        }

        if ((ip == null && dns == null) || port == -1)
        {
            PrintUsage();
        }
        return (port, ip, dns);
    }

    private static void ReadCommands(Client client)
    {
        while (_running)
        {
            var str = Console.ReadLine();
            if (str != null)
            {
                var strs = str.Split(' ');
                switch (strs[0])
                {
                    case "exit":
                        _running = false;
                        client.Exit();
                        break;
                    case "send" when strs.Length > 1:
                        ThreadPool.QueueUserWorkItem(_ => client.SendFile(strs[1]));
                        break;
                    case "cancel":
                        client.Cancel();
                        break;
                }
            }
        }
    }
}