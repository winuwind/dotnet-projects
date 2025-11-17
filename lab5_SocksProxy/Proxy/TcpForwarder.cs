using System.Net;
using System.Net.Sockets;

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
        IsRunning = false;
        
        SocketSource.Close();
        SocketSource.Dispose();
        
        SocketDest.Close();
        SocketDest.Dispose();
    }

    public async Task Init(IPAddress address, uint port, byte[] bytes)
    {
        try
        {
            await SocketDest.ConnectAsync(address, (int) port);
        }
        catch (SocketException ex)
        {
            bytes[3] = 0x01; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
            switch (ex.SocketErrorCode)
            {
                case SocketError.NetworkUnreachable:
                    bytes[1] = 0x03;
                    break;
                case SocketError.HostUnreachable:
                    bytes[1] = 0x04;
                    break;
                case SocketError.ConnectionRefused:
                    bytes[1] = 0x05;
                    break;
                default:
                    bytes[1] = 0x01;
                    break;
            }
            await MyServer.Send(SocketSource, bytes, 0, 10, null);
            Close();
            Handler.CloseConnection();
            return;
        }

        var sizeBytes = 0;
        bytes[0] = 0x05;
        bytes[1] = 0x00;
        bytes[2] = 0x00;
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

    public Task Work()
    {
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
        return Task.CompletedTask;
    }

    protected async Task ForwardTcp(Socket socketFirst, Socket socketSecond)
    {
        var buffer = new byte[4096];

        while (IsRunning)
        {
            var bytesRead = await MyServer.Read(socketFirst, buffer, 0, buffer.Length, null);
            
            if (bytesRead <= 0)
            {
                Close();
                Handler.CloseConnection();
                return;
            }
            await MyServer.Send(socketSecond, buffer, 0, bytesRead, null);
        }
    }
}