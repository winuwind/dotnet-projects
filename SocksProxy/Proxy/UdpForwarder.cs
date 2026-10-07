using System.Net;
using System.Net.Sockets;
using Serilog;
using SOCKS_Proxy.Control;

namespace SOCKS_Proxy.Proxy;

public class UdpForwarder(Server server, Handler handler, Socket socket, IPAddress address, uint port)
    : IProxy
{
    private readonly Socket _socketDest = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
    
    private IPAddress _addressUdpClient = address;
    private int _portUdpClient = (int) port;

    private bool _isRunning = true;

    public void Close()
    {
        if (!_isRunning)
        {
            return;
        }
        
        _isRunning = false;
        try
        {
            _socketDest.Close();
        }
        catch(Exception e)
        {
            Log.Warning("UdpForwarder.Close(): error when closed _socketDest: " + e.Message);
        }
        handler.CloseConnection();
    }

    public async Task Init(IPAddress address, uint port, byte[] bytes)
    {
        var endPoint = new IPEndPoint(server.GetIp(), 0);
        _socketDest.Bind(endPoint);
        
        var localEndPoint = (IPEndPoint?) _socketDest.LocalEndPoint;
        
        var sizeBytes = 0;
        bytes[AppConstant.IndexErrorType] = AppConstant.Success;
        
        if (localEndPoint?.AddressFamily == AddressFamily.InterNetwork)
        {
            _addressUdpClient = localEndPoint.Address;
            _portUdpClient = localEndPoint.Port;
            
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            localEndPoint.Address.GetAddressBytes().CopyTo(bytes, AppConstant.IndexAddress);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) localEndPoint.Port)).CopyTo(bytes, AppConstant.IndexAddress + 4);
            sizeBytes = 10;
        }
        else if (localEndPoint?.AddressFamily == AddressFamily.InterNetworkV6)
        {
            _addressUdpClient = localEndPoint.Address;
            _portUdpClient = localEndPoint.Port;
            
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv6Command;
            localEndPoint.Address.GetAddressBytes().CopyTo(bytes, AppConstant.IndexAddress);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) localEndPoint.Port)).CopyTo(bytes, AppConstant.IndexAddress + 16);
            sizeBytes = 22;
        }
        else
        {
            Log.Error("Udp Forwarder: New socket has type address: " + localEndPoint?.AddressFamily);
            bytes[AppConstant.IndexErrorType] = AppConstant.ServerError;
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            await server.Send(socket, bytes, AppConstant.IndexVersion, 10, null);
            Close();
            return;
        }
        
        var bytesSend = await server.Send(socket, bytes, AppConstant.IndexVersion, sizeBytes, null);
        if (bytesSend <= 0)
        {
            Close();
        }
    }
    
    public Task Work()
    {
        
        _ = Task.Run(async () => await ForwardUdp(AddressFamily.InterNetwork));
        _ = Task.Run(async () => await ForwardUdp(AddressFamily.InterNetworkV6));
        return Task.CompletedTask;
    }

    private async Task ForwardUdp(AddressFamily addressFamily)
    {
        var bytes =  new byte[AppConstant.MaxSizeUdpDGram];

        while (_isRunning)
        {
            var remote = new IPEndPoint(addressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            
            var bytesRead = await server.Read(_socketDest, bytes, 0, bytes.Length, remote);

            if (bytesRead <= 0)
            {
                Close();
                return;
            }
            
            if (remote.Address.Equals(_addressUdpClient))
            {
                if (bytes[AppConstant.IndexUdpFirstReserved] != AppConstant.ReservedByte ||
                    bytes[AppConstant.IndexUdpSecondReserved] != AppConstant.ReservedByte ||
                    bytes[AppConstant.IndexUdpFragmentation] != AppConstant.NoFragmentation)
                {
                    continue;
                }
                
                Task<IPAddress> task;
                try
                {
                    task = server.ParseAddress(bytes);
                }
                catch
                {
                    continue;
                }
                task.ContinueWith(async completed =>
                {
                    var addr = completed.Result;
                    
                    var index = AppConstant.IndexAddress;
                    if (bytes[AppConstant.IndexTypeAddress] == AppConstant.Ipv4Command)
                    {
                        index += 4;
                    }
                    else if (bytes[AppConstant.IndexTypeAddress] == AppConstant.DnsCommand)
                    {
                        index += bytes[AppConstant.IndexAddress] + 1;
                    }
                    else if (bytes[AppConstant.IndexTypeAddress] == AppConstant.Ipv6Command)
                    {
                        index += 16;
                    }
                
                
                    remote = new IPEndPoint(addr, IPAddress.HostToNetworkOrder(BitConverter.ToUInt16(bytes, index)));
                    index += 2;
                
                    await server.Send(_socketDest, bytes, index, bytesRead - index, remote);
                });
            }
            else
            {
                var heads = new byte[4];
                heads[AppConstant.IndexUdpFirstReserved] = AppConstant.ReservedByte; 
                heads[AppConstant.IndexUdpSecondReserved] = AppConstant.ReservedByte;
                heads[AppConstant.IndexUdpFragmentation] = AppConstant.NoFragmentation;
                heads[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;

                if (remote.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    heads[AppConstant.IndexTypeAddress] = AppConstant.Ipv6Command;
                }

                var addr = remote.Address.GetAddressBytes();
                var port = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) remote.Port));
                
                var packet = new byte[heads.Length + addr.Length + port.Length + bytesRead];
                
                var span = packet.AsSpan();
                
                heads.AsSpan().CopyTo(span);
                addr.AsSpan().CopyTo(span[heads.Length..]);
                port.AsSpan().CopyTo(span[(heads.Length + addr.Length)..]);
                bytes.AsSpan(0, bytesRead).CopyTo(span[(heads.Length + addr.Length + port.Length)..]);
                
                remote = new IPEndPoint(_addressUdpClient, _portUdpClient);
                
                await server.Send(_socketDest, bytes, 0, bytesRead, remote);
            }
        }
    }
}