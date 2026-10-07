using System.Net;
using System.Net.Sockets;
using Serilog;
using SOCKS_Proxy.Control;

namespace SOCKS_Proxy.Proxy;

public class TcpForwarder(Server server, Handler handler, Socket socket, IPAddress address)
    : IProxy
{
    protected readonly Server MyServer = server;
    protected readonly Socket SocketSource = socket;
    protected readonly Handler Handler = handler;
    protected readonly Socket SocketDest = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    
    protected bool IsRunning = true;

    public void Close()
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
            Log.Warning("TcpFprwarder.Close(): error when closed SocketDest: " + e.Message);
        }
        Handler.CloseConnection();
    }

    public async Task Init(IPAddress address, uint port, byte[] bytes)
    {
        try
        {
            await SocketDest.ConnectAsync(address, (int) port);
        }
        catch (SocketException ex)
        {
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            switch (ex.SocketErrorCode)
            {
                case SocketError.NetworkUnreachable:
                    bytes[AppConstant.IndexErrorType] = AppConstant.NetworkUnreachable;
                    break;
                case SocketError.HostUnreachable:
                    bytes[AppConstant.IndexErrorType] = AppConstant.HostUnreachable;
                    break;
                case SocketError.ConnectionRefused:
                    bytes[AppConstant.IndexErrorType] = AppConstant.ConnectionRefused;
                    break;
                default:
                    bytes[AppConstant.IndexErrorType] = AppConstant.ServerError;
                    break;
            }
            await MyServer.Send(SocketSource, bytes, AppConstant.IndexVersion, 10, null);
            Close();
            return;
        }

        var sizeBytes = 0;
        bytes[AppConstant.IndexVersion] = AppConstant.SocksVersion;
        bytes[AppConstant.IndexErrorType] = AppConstant.Success;
        bytes[AppConstant.IndexReserved] = AppConstant.ReservedByte;
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
            bytes[AppConstant.IndexErrorType] = AppConstant.Ipv4Command;
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

    public Task Work()
    {
        _ = Task.Run(async () => await ForwardTcp(SocketSource, SocketDest));
        _ = Task.Run(async () => await ForwardTcp(SocketDest, SocketSource));
        return Task.CompletedTask;
    }

    protected async Task ForwardTcp(Socket socketFirst, Socket socketSecond)
    {
        var buffer = new byte[AppConstant.SizeBuffer];

        while (IsRunning)
        {
            var bytesRead = await MyServer.Read(socketFirst, buffer, 0, buffer.Length, null);
            
            if (bytesRead <= 0)
            {
                Close();
                return;
            }
            await MyServer.Send(socketSecond, buffer, 0, bytesRead, null);
        }
    }
}