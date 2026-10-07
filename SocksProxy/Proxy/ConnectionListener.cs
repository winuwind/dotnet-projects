using System.Net;
using System.Net.Sockets;
using Serilog;
using SOCKS_Proxy.Control;

namespace SOCKS_Proxy.Proxy;

public class ConnectionListener : TcpForwarder, IProxy
{
    private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public new void Close()
    {
        if (!IsRunning)
        {
            return;
        }
        
        IsRunning = false;
        try
        {
            SocketDest.Close();
        }
        catch(Exception e)
        {
            Log.Warning("ConnectionListener.Close(): error when closed SocketDest: " + e.Message);
        }
        try
        {
            _socket.Close();
        }
        catch(Exception e)
        {
            Log.Warning("ConnectionListener.Close(): error when closed _socket: " + e.Message);
        }
        Handler.CloseConnection();
    }
    
    public ConnectionListener(Server server, Handler handler, Socket socket, IPAddress address) : base(server, handler, socket, address)
    {
        SocketDest.Bind(new IPEndPoint(server.GetIp(), 0));
        SocketDest.Listen();
    }

    public new async Task Init(IPAddress address, uint port, byte[] bytes)
    {
        var sizeBytes = 0;
        bytes[AppConstant.IndexErrorType] = AppConstant.Success;
        var endPoint = (IPEndPoint?) SocketDest.LocalEndPoint;
        if (endPoint?.AddressFamily == AddressFamily.InterNetwork)
        {
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            endPoint.Address.GetAddressBytes().CopyTo(bytes, AppConstant.IndexAddress);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) endPoint.Port)).CopyTo(bytes, AppConstant.IndexAddress + 4);
            sizeBytes = 10;
        }
        else if (endPoint?.AddressFamily == AddressFamily.InterNetworkV6)
        {
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv6Command;
            endPoint.Address.GetAddressBytes().CopyTo(bytes, AppConstant.IndexAddress);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) endPoint.Port)).CopyTo(bytes, AppConstant.IndexAddress + 16);
            sizeBytes = 22;
        }
        else
        {
            bytes[AppConstant.IndexErrorType] = AppConstant.ServerError;
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            await MyServer.Send(SocketSource, bytes, AppConstant.IndexVersion, 10, null);
            Close();
            return;
        }
        
        var bytesSend = await MyServer.Send(SocketSource, bytes, AppConstant.IndexVersion, sizeBytes, null);
        if (bytesSend <= 0)
        {
            Close();
        }
    }

    public new async Task Work()
    {
        _socket = await SocketDest.AcceptAsync();
        
        var bytes = new byte[22];
        bytes[AppConstant.IndexVersion] = AppConstant.SocksVersion; 
        bytes[AppConstant.IndexErrorType] = AppConstant.Success;
        bytes[AppConstant.IndexReserved] = AppConstant.ReservedByte;
        
        var remote = (IPEndPoint?) _socket.RemoteEndPoint;
        var sizeBytes = 0;
        
        if (remote?.AddressFamily == AddressFamily.InterNetwork)
        {
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            remote.Address.GetAddressBytes().CopyTo(bytes, AppConstant.IndexAddress);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) remote.Port)).CopyTo(bytes, AppConstant.IndexAddress + 4);
            sizeBytes = 10;
        }
        else if (remote?.AddressFamily == AddressFamily.InterNetworkV6)
        {
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv6Command;
            remote.Address.GetAddressBytes().CopyTo(bytes, AppConstant.IndexAddress);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) remote.Port)).CopyTo(bytes, AppConstant.IndexAddress + 16);
            sizeBytes = 22;
        }
        else
        {
            Log.Warning("Connection listener: New client has type address: " + remote?.AddressFamily);
            bytes[AppConstant.IndexErrorType] = AppConstant.ServerError;
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            await MyServer.Send(SocketSource, bytes, AppConstant.IndexVersion, 10, null);
            Close();
            return;
        }
        
        await MyServer.Send(SocketSource, bytes, AppConstant.IndexVersion, sizeBytes, null);
        
        
        _ = Task.Run(async () => await ForwardTcp(SocketSource, _socket));
        _ = Task.Run(async () => await ForwardTcp(_socket, SocketSource));
    }
}