using System.Net;
using System.Net.Sockets;

using SOCKS_Proxy.Control;

namespace SOCKS_Proxy.Proxy;

public class ConnectionListener : TcpForwarder, IProxy
{
    private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public new void Close()
    {
        IsRunning = false;
        
        SocketSource.Close();
        SocketSource.Dispose();
        
        SocketDest.Close();
        SocketDest.Dispose();
        
        _socket.Close();
        _socket.Dispose();
    }
    
    public ConnectionListener(Server server, Handler handler, Socket socket, IPAddress address) : base(server, handler, socket, address)
    {
        SocketDest.Bind(new IPEndPoint(server.GetIp(), 0));
        SocketDest.Listen();
    }

    public new async Task Init(IPAddress address, uint port, byte[] bytes)
    {
        var sizeBytes = 0;
        bytes[1] = 0x00;
        var endPoint = (IPEndPoint?) SocketDest.LocalEndPoint;
        if (endPoint?.AddressFamily == AddressFamily.InterNetwork)
        {
            bytes[3] = 0x01;
            endPoint.Address.GetAddressBytes().CopyTo(bytes, 4);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) endPoint.Port)).CopyTo(bytes, 8);
            sizeBytes = 10;
        }
        else if (endPoint?.AddressFamily == AddressFamily.InterNetworkV6)
        {
            bytes[3] = 0x04;
            endPoint.Address.GetAddressBytes().CopyTo(bytes, 4);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) endPoint.Port)).CopyTo(bytes, 20);
            sizeBytes = 22;
        }
        else
        {
            bytes[1] = 0x01;
            bytes[3] = 0x01; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
            await MyServer.Send(SocketSource, bytes, 0, 10, null);
            Close();
            Handler.CloseConnection();
            return;
        }
        
        var bytesSend = await MyServer.Send(SocketSource, bytes, 0, sizeBytes, null);
        if (bytesSend <= 0)
        {
            Close();
            Handler.CloseConnection();
        }
    }

    public new async Task Work()
    {
        _socket = await SocketDest.AcceptAsync();
        
        var bytes = new byte[22];
        bytes[0] = 0x05; 
        bytes[1] = 0x00;
        bytes[2] = 0x00;
        
        var remote = (IPEndPoint?) _socket.RemoteEndPoint;
        var sizeBytes = 0;
        
        if (remote?.AddressFamily == AddressFamily.InterNetwork)
        {
            bytes[3] = 0x01;
            remote.Address.GetAddressBytes().CopyTo(bytes, 4);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) remote.Port)).CopyTo(bytes, 8);
            sizeBytes = 10;
        }
        else if (remote?.AddressFamily == AddressFamily.InterNetworkV6)
        {
            bytes[3] = 0x04;
            remote.Address.GetAddressBytes().CopyTo(bytes, 4);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) remote.Port)).CopyTo(bytes, 20);
            sizeBytes = 22;
        }
        else
        {
            bytes[1] = 0x01;
            bytes[3] = 0x01; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
            await MyServer.Send(SocketSource, bytes, 0, 10, null);
            Close();
            Handler.CloseConnection();
            return;
        }
        
        await MyServer.Send(SocketSource, bytes, 0, sizeBytes, null);
        
        var thread1 = new Thread(() =>
        {
            ForwardTcp(SocketSource, SocketDest).GetAwaiter().GetResult();
        });
        thread1.Start();
        
        var thread2 = new Thread(() =>
        {
            ForwardTcp(SocketDest, SocketSource).GetAwaiter().GetResult();
        });
        thread2.Start();
    }
}