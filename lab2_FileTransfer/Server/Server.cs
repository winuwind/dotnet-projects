using System.Text;
using Protocol.Constants;
using Protocol.Printer;

namespace Server;

using System.Net;
using System.Net.Sockets;

public class Server
{
    private readonly TcpListener _listener;
    private readonly Dictionary<int, ClientHandler> _clientHandlers;
    private readonly Dictionary<int, string> _filePaths;
    private readonly Dictionary<int, FileStream> _fileStreams;
    private readonly Dictionary<int, DateTime> _timesLastSegment;
    private readonly int _port;
    
    private static readonly Dictionary<int, object> FileStreamsSync = new Dictionary<int, object>();

    private static bool _isWorking;

    private Thread? _threadRecv;
    private Thread? _threadControl;
    private int _lastIdHandler;

    public static void Start(string[] args)
    {
        Console.Clear();
        
        var port = ReadPort(args);
        
        var server = new Server(port);
        var threadServer = new Thread(_ => server.StartServer());
        threadServer.Start();
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            server.StopServer();
            _isWorking = false;
            threadServer.Join();
        };
        
        ReadCommands(server);
        
        threadServer.Join();
    }

    public void Cancel(int id)
    {
        
        _fileStreams[id].Close();
        _fileStreams[id].Dispose();
        _fileStreams.Remove(id);
        if (File.Exists(_filePaths[id]))
        {
            File.Delete(_filePaths[id]);
        }
        _filePaths.Remove(id);
        _timesLastSegment.Remove(id);
        FileStreamsSync.Remove(id);
        _clientHandlers[id].SetTransferData(new TransferData(_clientHandlers[id], this));
    }

    public void Final(int id)
    {
        if (!_fileStreams.TryGetValue(id, out var value))
        {
            return;
        }
        Printer.Print($"File {_filePaths[id]} successfully wrote", -1, -1);
        value.Close();
        value.Dispose();
        _fileStreams.Remove(id);
        _filePaths.Remove(id);
        _timesLastSegment.Remove(id);
        FileStreamsSync.Remove(id);
        _clientHandlers[id].SetTransferData(new TransferData(_clientHandlers[id], this));
        
    }

    public void CloseHandler(int id)
    {
        if (_fileStreams.TryGetValue(id, out var value))
        {
            value.Close();
            value.Dispose();
            _fileStreams.Remove(id);
            _filePaths.Remove(id);
            _timesLastSegment.Remove(id);
            FileStreamsSync.Remove(id);
        }
        _clientHandlers[id].Close();
        _clientHandlers.Remove(id);
    }

    public void SetPartFile(byte[] part, long sizePart, long numberPart, int maxSizePart, int id)
    {
        lock (FileStreamsSync[id])
        {
            var offset = maxSizePart * numberPart;
            _fileStreams[id].Seek(offset, SeekOrigin.Begin);
            _fileStreams[id].Write(part, 0, (int)sizePart);
        }
    }

    public void SetFileInfo(long size, string fileName, int id)
    {
        Directory.CreateDirectory("uploads");
        fileName = Path.Combine("uploads", fileName).Split('\0')[0];
        if (File.Exists(fileName))
        {
            var index = fileName.LastIndexOf('.');
            var number = 0;
            while (true)
            {
                var newFileName = fileName[..(index == -1 ? fileName.Length : index)] +
                                  " (" + number++ + ")" + fileName[(index == -1 ? fileName.Length : index)..];
                if (!File.Exists(newFileName))
                {
                    fileName = newFileName;
                    break;
                }
            }
        }
        _filePaths[id] = fileName;
        _fileStreams[id] =  new FileStream(_filePaths[id], FileMode.CreateNew,  FileAccess.Write, FileShare.None);
        _fileStreams[id].SetLength(size);
        _timesLastSegment[id] = DateTime.Now;
        FileStreamsSync[id] = new object();
    }
    
    private Server(int port)
    {
        _port = port;
        _isWorking = true;
        _filePaths = new Dictionary<int, string>();
        _fileStreams = new Dictionary<int, FileStream>();
        _timesLastSegment = new Dictionary<int, DateTime>();
        _clientHandlers = new Dictionary<int, ClientHandler>();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        
        var hostName = Dns.GetHostName();
        
        var sb = new StringBuilder();
        sb.AppendFormat($"Server started on {hostName}\n");
        sb.AppendFormat($"Port {port}\n");
        sb.AppendFormat("Ip addresses:\n");

        var addresses = Dns.GetHostAddresses(hostName);

        foreach (var addr in addresses)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                sb.AppendFormat($"  {addr}\n");
            }
        }
        Printer.Print(sb.ToString(), -1, -1);
    }

    private int GetPort()
    {
        return _port;
    }

    private static string GetDns()
    {
        return Dns.GetHostName();
    }

    private static IPAddress[] GetIpAddress()
    {
        return Dns.GetHostAddresses(Dns.GetHostName());
    }
    
    private void StartServer()
    {
        try
        {
            while (_isWorking)
            {
                var tcpClient = _listener.AcceptTcpClient();
                var handler = new ClientHandler(tcpClient, _lastIdHandler, this);
                _clientHandlers[_lastIdHandler++] = handler;
                _threadRecv = new Thread(_ => handler.Receive());
                _threadRecv.Start();
                _threadControl = new Thread(_ => ControlTimes());
                _threadControl.Start();
            }
        }
        catch (Exception)
        {
            _isWorking = false;
            StopServer();
        }
    }

    private void ControlTimes()
    {
        while (_isWorking)
        {
            foreach (var keyValue in _timesLastSegment.Where(keyValue => (DateTime.Now - keyValue.Value).Seconds > 5 && _filePaths.ContainsKey(keyValue.Key)))
            {
                switch (_clientHandlers[keyValue.Key].AskSegments())
                {
                    case -1: Cancel(keyValue.Key);
                        break;
                    case 1:
                        Final(keyValue.Key);
                        break;
                }
            }

            Thread.Sleep(500);
        }
    }

    private void StopServer()
    {
        _isWorking = false;
        for(var id = 0; id < _clientHandlers.Count; id++)
        {
            if (_fileStreams.TryGetValue(id, out var value))
            {
                value.Close();
                value.Dispose();
            }
            _clientHandlers[id].Close();
        }
        _listener.Stop();
        _threadControl?.Join();
        _threadRecv?.Join();
    }

    private static void ReadCommands(Server server)
    {
        while (_isWorking)
        {
            var str = Console.ReadLine();
            if (str != null)
            {
                switch (str)
                {
                    case "port": Printer.Print("Port: " + server.GetPort(), -1, -1); break;
                    case "ip": var addresses = GetIpAddress();
                        foreach (var addr in addresses)
                        {
                            if (addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                Printer.Print(addr.ToString(), -1, -1);
                            }
                        }
                        break;
                    case "dns": Printer.Print("DNS: " + GetDns(), -1, -1); break;
                    case "exit": server.StopServer(); break;
                }
            }
        }
    }

    private static int ReadPort(string[] args)
    {
        if (args.Length <= 0) return AppConstants.DefaultPort;
        try
        {
            return int.Parse(args[0]);
        }
        catch (Exception)
        {
            Printer.Print("USAGE:\ndotnet Server.dll <port>\n", -1, -1);
            Environment.Exit(-1);
        }
        return AppConstants.DefaultPort;
    }
}