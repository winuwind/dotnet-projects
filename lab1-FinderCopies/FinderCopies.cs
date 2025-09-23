namespace FinderCopies;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Collections.Concurrent;

public class FinderCopies
{
    private const double SendInterval = 100; 
    private const double Timeout = 5;
    private const double DelayDisplayTable = 3;
    
    private AddressFamily _family = AddressFamily.InterNetwork;
    
    private readonly ConcurrentDictionary<string, (string ip, DateTime lastSeen)> _peers = new ConcurrentDictionary<string, (string ip, DateTime lastSeen)>();
    private UdpClient _recvClient;
    private UdpClient _sendClient = new UdpClient();

    private readonly string _instanceId = Guid.NewGuid().ToString();

    private static string _multicastGroupAddr = "239.255.0.1";
    private static int _port = 25565;
    private static bool _isIPv6;
    private static volatile bool _running = true;

    private record Heartbeat(string AppName, string InstanceId);
    
    public FinderCopies(string ip, int port)
    {
        _multicastGroupAddr = ip;
        _isIPv6 = _multicastGroupAddr.Contains(':');
        _port = port;
    }

    private void CreateRecvClient()
    {
        _recvClient = new UdpClient(_family);

        _recvClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _recvClient.Client.Bind(new IPEndPoint(_isIPv6 ? IPAddress.IPv6Any : IPAddress.Any, _port));

        if (_isIPv6)
        {
            _recvClient.JoinMulticastGroup(IPAddress.Parse(_multicastGroupAddr));
        }
        else
        {
            _recvClient.JoinMulticastGroup(IPAddress.Parse(_multicastGroupAddr), IPAddress.Any);
        }
    }
    
    private void CreateSendClient()
    {
        _sendClient = new UdpClient(_family);
        _sendClient.MulticastLoopback = true;
        _sendClient.Client.SetSocketOption(_isIPv6 ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP, 
            SocketOptionName.MulticastTimeToLive, 1);

    }
    private void SendMessageToGroup()
    {
        var dest = new IPEndPoint(IPAddress.Parse(_multicastGroupAddr), _port);
        while (_running)
        {
            var msg = new Heartbeat("finder_copies", _instanceId);
            var data = JsonSerializer.SerializeToUtf8Bytes(msg);
            _sendClient.Send(data, data.Length, dest);
            Thread.Sleep(TimeSpan.FromMilliseconds(SendInterval));
        }
        var msgLast = new Heartbeat("finder_copies", _instanceId);
        var dataLast = JsonSerializer.SerializeToUtf8Bytes(msgLast);
        _sendClient.Send(dataLast, dataLast.Length, dest);
    }

    private void UpdateDeviceTable(bool isUpdated)
    {
        var now = DateTime.UtcNow;
        var expired = _peers.Where(kv => (now - kv.Value.lastSeen).TotalSeconds > Timeout).Select(kv => kv.Key).ToList();
        foreach (var key in expired)
        {
            Console.WriteLine($"Device with instanceId {key} left group");
            _peers.TryRemove(key, out _);
        }

        if (expired.Count > 0 || isUpdated)
        {
            Console.WriteLine($"\n=== Alive copies ({_peers.Count}): ===");

            foreach (var instanceId in _peers.Keys)
            {
                Console.WriteLine(" - " + _peers[instanceId].ip + "; " + instanceId);
            }

            Console.WriteLine("========================\n");
        }
    }

    private void RecvMessageFromGroup()
    {
        var timeStart = DateTime.UtcNow;
        var isDisplayed = false;
        while (_running)
        {
            var isUpdated = false;
            try
            {
                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                var res =  _recvClient.Receive(ref remoteEp);
                var sourceIp = remoteEp.Address.ToString();
                try
                {
                    var msg = JsonSerializer.Deserialize<Heartbeat>(res);
                    if (msg == null || msg.AppName != "finder_copies")
                    {
                        continue;
                    }

                    if (_peers.ContainsKey(msg.InstanceId))
                    {
                        if (_peers[msg.InstanceId].ip != sourceIp)
                        {
                            isUpdated = true;
                            Console.WriteLine(
                                $"Device with instanceId {msg.InstanceId} change IP from {_peers[msg.InstanceId].ip}  to {sourceIp}");
                        }
                    }
                    else
                    {
                        isUpdated = true;
                        if (msg.InstanceId != _instanceId && isDisplayed)
                        {
                            Console.WriteLine(
                                $"Device with instanceId {msg.InstanceId} and IP {sourceIp} joined to group");
                        }
                    }

                    _peers[msg.InstanceId] = (sourceIp, DateTime.UtcNow);
                }
                catch
                {
                    //
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receive: {ex.Message}");
            }

            if (DateTime.UtcNow.Subtract(timeStart).TotalSeconds < DelayDisplayTable)
            {
                continue;
            }

            if (!isDisplayed)
            {
                isUpdated = true;
            }
            isDisplayed = true;
            UpdateDeviceTable(isUpdated);
        }
    }
    public void Execute()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _running = false;
        };
        
        _family = _isIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        CreateRecvClient();
        CreateSendClient();
        
        var senderThread = new Thread( _ => SendMessageToGroup());
        senderThread.Start();

        Console.WriteLine($"Instance {_instanceId} is running, group {_multicastGroupAddr}:{_port}");
        
        RecvMessageFromGroup();
        
        senderThread.Join();

        _recvClient.DropMulticastGroup(IPAddress.Parse(_multicastGroupAddr));
        _recvClient.Close();
        _sendClient.Close();
    }
}
