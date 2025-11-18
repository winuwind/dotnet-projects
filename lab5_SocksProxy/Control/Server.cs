using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DnsClient;
using SOCKS_Proxy.Proxy;

namespace SOCKS_Proxy.Control;

public class Server
{
    private readonly int _port;
    private readonly Socket _socket;
    private readonly Socket _socketDns;
    private readonly List<Handler> _clients = [];
    private readonly IPAddress _ip = IPAddress.Any;
    private readonly ConcurrentDictionary<short, TaskCompletionSource<IPAddress>> _pendingDns = new();
    private readonly ConcurrentDictionary<string, IPAddress> _dns = new();
    private readonly Selector _selector;
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _lock = new();

    private bool _isRunning = true;

    public Server(int port)
    {
        _selector = new Selector(this);
        
        var dns = new LookupClient();
        var server = dns.NameServers.First();

        var dnsEndpoint = new IPEndPoint(IPAddress.Parse(server.Address), server.Port);
        _socketDns = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socketDns.Connect(dnsEndpoint);
        Task.Run(async () => await DnsListenLoop());
        
        
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        _socket.Listen();

        var flag = false;
        var endPoint = (IPEndPoint?)_socket.LocalEndPoint;
        if (endPoint != null)
        {
            _port = endPoint.Port;
            _ip = endPoint.Address;
            flag = true;
        }
        else
        {
            _port = port;
        }

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
                if (!flag)
                {
                    flag = true;
                    _ip = addr;
                }
            }
        }
        Console.WriteLine(sb);
    }

    public IPAddress GetIp()
    {
        return _ip;
    }

    public int GetPort()
    {
        return _port;
    }

    public Socket GetListener()
    {
        return _socket;
    }
    
    public static string GetDns()
    {
        return Dns.GetHostName();
    }

    public static IPAddress[] GetIpAddress()
    {
        return Dns.GetHostAddresses(Dns.GetHostName());
    }

    public async Task<IPAddress> ParseAddress(byte[] bytes)
    {
        var type = bytes[3];
        IPAddress addr;
        if (type == AppConstant.Ipv4Command)
        {
            var addressFourBytes = new byte[4];
            Array.Copy(bytes, 4, addressFourBytes, 0, 4);
            addr = new IPAddress(addressFourBytes);
        }
        else if (type == AppConstant.DnsCommand)
        {
            var length = bytes[4];
            var host = Encoding.ASCII.GetString(bytes, 5, length);
            host = host.TrimEnd('\0');
            
            try
            {
                if (!_dns.TryGetValue(host, out addr))
                {
                    addr = await ResolveDns(host);
                    _dns[host] = addr;
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        else if (type == AppConstant.Ipv6Command)
        {
            var addressSixteenBytes = new byte[16];
            Array.Copy(bytes, 4, addressSixteenBytes, 0, 16);
            addr = new IPAddress(addressSixteenBytes);
        }
        else
        {
            throw new Exception("Unknown address type");
        }
        
        return addr;
    }

    public void Accept()
    {
        var clientSocket = _socket.Accept();
        var handler = new Handler(clientSocket, this);
        _clients.Add(handler);

        _ = Task.Run(async () =>
        {

            await handler.CreateConnection();
            await handler.Work();
        });
    }

    public void Start()
    {
        _selector.Work();
    }

    public void Stop()
    {
        _isRunning = false;
        _selector.Stop();
        
        var clientsCopy = _clients.ToArray();
        
        foreach (var handler in clientsCopy)
        {
            handler.CloseConnection();
        }
        
        _cts.Cancel();
        
        _socket.Close();
        _socket.Dispose();

        _socketDns.Close();
        _socketDns.Dispose();
    }

    public void DeleteClient(Handler handler)
    {
        lock (_lock)
        {
            foreach (var client in _clients.ToList().Where(client => Equals(client, handler)))
            {
                _clients.Remove(client);
                break;
            }
        }
    }
    
    public static byte ChooseAuthMethod(byte[] bytes)
    {
        var number = bytes[1];
        for (var i = 2; i < number + 2; i++)
        {
            if (bytes[i] == AppConstant.NoAuth)
            {
                return AppConstant.NoAuth;
            }
        }
        return AppConstant.NoMethodsAuthAvailable;
    }

    public Task<int> Send(Socket socket, byte[] buffer, int offset, int count, IPEndPoint? endPoint)
    {
        var tcs = new TaskCompletionSource<int>();
        var task = new TaskWrite(tcs, buffer, offset, count, endPoint);
        _selector.AddTaskWrite(socket, task);
        return tcs.Task;
    }

    public Task<int> ReadExact(Socket socket, byte[] buffer, int offset, int count, IPEndPoint? endPoint)
    {
        var tcs = new TaskCompletionSource<int>();
        var task = new TaskRead(tcs, buffer, offset, count, true, endPoint);
        _selector.AddTaskRead(socket, task);
        return tcs.Task;
    }
    
    public Task<int> Read(Socket socket, byte[] buffer, int offset, int count, IPEndPoint? endPoint)
    {
        var tcs = new TaskCompletionSource<int>();
        var task = new TaskRead(tcs, buffer, offset, count, false, endPoint);
        _selector.AddTaskRead(socket, task);
        return tcs.Task;
    }
    
    private async Task DnsListenLoop()
    {
        var buffer = new byte[512];
        while (_isRunning)
        {
            try
            {
                var result = await _socketDns.ReceiveAsync(buffer, SocketFlags.None, _cts.Token);
                var response = buffer[..result];
                var transactionId = (short)((response[0] << 8) | response[1]);
                var ip = ParseDnsResponse(response);

                if (_pendingDns.TryRemove(transactionId, out var tcs))
                {
                    tcs.SetResult(ip);
                }
            }
            catch
            {
                break;
            }
        }

        foreach (var tcs in _pendingDns.Values)
        {
            tcs.SetResult(IPAddress.Any);
        }
    }

    private static byte[] BuildDnsRequest(string hostName, short transactionId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(IPAddress.HostToNetworkOrder(transactionId));

        writer.Write((short) 0x001);
        writer.Write((short) 0x0100);

        writer.Write((short) 0x0000);
        writer.Write((short) 0x0000);
        writer.Write((short) 0x0000);

        foreach (var label in hostName.Split('.'))
        {
            writer.Write((byte) label.Length);
            writer.Write(Encoding.ASCII.GetBytes(label));
        }
        writer.Write((byte) 0x00);
        writer.Write((short) 0x0100);
        writer.Write((short) 0x0100);

        return ms.ToArray();
    }

    private static IPAddress ParseDnsResponse(byte[] response)
    {
        var offset = 0;
        
        var respId = (short) ((response[offset] << 8) | response[1]);
        offset += 2;

        var flags = (ushort) ((response[offset] << 8) | response[offset + 1]);
        offset += 2;

        var isResponse = (flags & 0x8000) != 0;
        if (!isResponse)
        {
            throw new Exception("Not a DNS response");
        }

        var qdcount = (ushort) ((response[offset] << 8) | response[offset + 1]);
        offset += 2;
        var ancount = (ushort) ((response[offset] << 8) | response[offset + 1]);
        offset += 6;

        for (var i = 0; i < qdcount; i++)
        {
            while (response[offset] != 0)
            {
                offset += response[offset] + 1;
            }
            offset += 1;

            offset += 4;
        }

        for (var i = 0; i < ancount; i++)
        {
            if ((response[offset] & 0xC0) == 0xC0)
            {
                offset += 2;
            }
            else
            {
                while (response[offset] != 0)
                {
                    offset += response[offset] + 1;
                }
                offset += 1;
            }

            var type = (ushort) ((response[offset] << 8) | response[offset + 1]);
            offset += 8;
            var rdlength = (ushort) ((response[offset] << 8) | response[offset + 1]);
            offset += 2;

            if (type == 1 && rdlength == 4)
            {
                var ipBytes = new byte[4];
                Array.Copy(response, offset, ipBytes, 0, 4);
                return new IPAddress(ipBytes);
            }
            
            if (type == 28 && rdlength == 16)
            {
                var ipBytes = new byte[16];
                Array.Copy(response, offset, ipBytes, 0, 4);
                return new IPAddress(ipBytes);
            }

            offset += rdlength;
        }
        
        throw new Exception("Record not found");
    }
    
    private async Task<IPAddress> ResolveDns(String hostName)
    {
        var rand = new Random();
        var transactionId = (short) rand.Next(short.MinValue, short.MaxValue);
        
        var tcs = new TaskCompletionSource<IPAddress>();
        _pendingDns[transactionId] = tcs;
        
        var request = BuildDnsRequest(hostName, transactionId);
        await _socketDns.SendAsync(request, SocketFlags.None, _cts.Token);

        return await tcs.Task;
    }
}