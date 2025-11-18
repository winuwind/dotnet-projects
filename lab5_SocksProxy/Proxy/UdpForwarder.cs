using System.Net;
using System.Net.Sockets;
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
        _isRunning = false;

        socket.Close();
        socket.Dispose();
        
        _socketDest.Close();
        _socketDest.Dispose();
    }

    public async Task Init(IPAddress address, uint port, byte[] bytes)
    {
        var endPoint = new IPEndPoint(server.GetIp(), 0);
        _socketDest.Bind(endPoint);
        
        var localEndPoint = (IPEndPoint?) _socketDest.LocalEndPoint;
        
        var sizeBytes = 0;
        bytes[1] = AppConstant.Success;
        
        if (localEndPoint?.AddressFamily == AddressFamily.InterNetwork)
        {
            _addressUdpClient = localEndPoint.Address;
            _portUdpClient = localEndPoint.Port;
            
            bytes[3] = AppConstant.Ipv4Command;
            localEndPoint.Address.GetAddressBytes().CopyTo(bytes, 4);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) localEndPoint.Port)).CopyTo(bytes, 8);
            sizeBytes = 10;
        }
        else if (localEndPoint?.AddressFamily == AddressFamily.InterNetworkV6)
        {
            _addressUdpClient = localEndPoint.Address;
            _portUdpClient = localEndPoint.Port;
            
            bytes[3] = AppConstant.Ipv6Command;
            localEndPoint.Address.GetAddressBytes().CopyTo(bytes, 4);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) localEndPoint.Port)).CopyTo(bytes, 20);
            sizeBytes = 22;
        }
        else
        {
            bytes[1] = AppConstant.ServerError;
            bytes[3] = AppConstant.Ipv4Command; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
            await server.Send(socket, bytes, 0, 10, null);
            Close();
            handler.CloseConnection();
            return;
        }
        
        var bytesSend = await server.Send(socket, bytes, 0, sizeBytes, null);
        if (bytesSend <= 0)
        {
            Close();
            handler.CloseConnection();
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
        var bytes =  new byte[65536];

        while (_isRunning)
        {
            var remote = new IPEndPoint(addressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            
            var bytesRead = await server.Read(_socketDest, bytes, 0, bytes.Length, remote);

            if (bytesRead <= 0)
            {
                Close();
                handler.CloseConnection();
                return;
            }
            
            if (remote.Address.Equals(_addressUdpClient))
            {
                if (bytes[0] != AppConstant.ReservedByte || bytes[1] != AppConstant.ReservedByte || bytes[2] != AppConstant.NoFragmentation)
                {
                    continue;
                }
                
                server.ParseAddress(bytes).ContinueWith(async completed =>
                {
                    var addr = completed.Result;
                    
                    var index = 4;
                    if (bytes[3] == AppConstant.Ipv4Command)
                    {
                        index += 4;
                    }
                    else if (bytes[3] == AppConstant.DnsCommand)
                    {
                        index += bytes[4] + 1;
                    }
                    else if (bytes[3] == AppConstant.Ipv6Command)
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
                heads[0] = AppConstant.ReservedByte; 
                heads[1] = AppConstant.ReservedByte;
                heads[2] = AppConstant.NoFragmentation;
                heads[3] = AppConstant.Ipv4Command;

                if (remote.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    heads[3] = AppConstant.Ipv6Command;
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