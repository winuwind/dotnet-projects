using System.Net;
using System.Net.Sockets;
using SOCKS_Proxy.Proxy;

namespace SOCKS_Proxy.Control;

public class Handler(Socket clientSocket, Server server)
{
    private byte _command;
    
    private IProxy? _proxy = null;

    public void CloseConnection()
    {
        if (_proxy == null)
        {
            clientSocket.Close();
            clientSocket.Dispose();
        }
        else
        {
            _proxy.Close();
        }
        
        server.DeleteClient(this);
    }
    
    public async Task CreateConnection()
    {
        var bytes = new byte[1024];
        
        var count = await server.ReadExact(clientSocket, bytes, 0, 2, null);
        
        if (bytes[0] != AppConstant.SocksVersion || count < 2)
        {
            CloseConnection();
            return;
        }
        await server.ReadExact(clientSocket, bytes, 2, bytes[1], null);
        
        var numberMethod = Server.ChooseAuthMethod(bytes);
        
        bytes[0] = AppConstant.SocksVersion;
        bytes[1] = numberMethod;
        count = await server.Send(clientSocket, bytes, 0, 2, null);
        
        if (count <= 0)
        {
            CloseConnection();
            return;
        }
        
        if (numberMethod == AppConstant.NoMethodsAuthAvailable)
        {
            CloseConnection();
            return;
        }
        
        await Authentication(bytes, numberMethod);
        
        await server.ReadExact(clientSocket, bytes, 0, 4, null);
        if (bytes[0] != AppConstant.SocksVersion || bytes[2] != AppConstant.ReservedByte)
        {
            CloseConnection();
            return;
        }
        
        var type = bytes[3];
        switch (type)
        {
            case AppConstant.Ipv4Command: await server.ReadExact(clientSocket, bytes, 4, 4, null);
                break;
            case AppConstant.DnsCommand: await server.ReadExact(clientSocket, bytes, 4, 1, null);
                if (bytes[4] == 0x00)
                {
                    CloseConnection();
                    return;
                }
                await server.ReadExact(clientSocket, bytes, 5, bytes[4], null);
                break;
            case AppConstant.Ipv6Command: await server.ReadExact(clientSocket, bytes, 4, 16, null);
                break;
            default:
                bytes[1] = AppConstant.UnknownAddressType;
                bytes[3] = AppConstant.Ipv4Command; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
                await server.Send(clientSocket, bytes, 0, 10, null);
                CloseConnection();
                return;
        }
        
        IPAddress address;
        try
        {
            address = await server.ParseAddress(bytes);
        }
        catch
        {
            bytes[1] = AppConstant.ServerError;
            bytes[3] = AppConstant.Ipv4Command; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
            await server.Send(clientSocket, bytes, 0, 10, null);
            CloseConnection();
            return;
        }
        
        await server.ReadExact(clientSocket, bytes, 32, 2, null);
        var port = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 32));

        _command = bytes[1];
        switch (_command)
        {
            case AppConstant.ConnectCommand: await StreamConnection(address, port, bytes);
                break;
            case AppConstant.BindCommand: await BindPort(address, port, bytes);
                break;
            case AppConstant.UdpCommand: await AssociateUdpPort(address, port, bytes);
                break;
            default: 
                bytes[1] = AppConstant.ProtocolError;
                bytes[3] = AppConstant.Ipv4Command; bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00; bytes[8] = 0x00; bytes[9] = 0x00;
                await server.Send(clientSocket, bytes, 0, 10, null);
                CloseConnection();
                break;
        }
    }

    public async Task Work()
    {
        if (_proxy != null)
        {
            await _proxy.Work();
        }
    }

    private Task Authentication(byte[] bytes, byte numberMethod)
    {
        if (numberMethod == AppConstant.NoAuth)
        {
            return Task.CompletedTask;
        }
        else
        {
            //Заглушка - не реализованы другие методы
        }
        return Task.CompletedTask;
    }
    
    private async Task StreamConnection(IPAddress address, uint port, byte[] bytes)
    {
        _proxy = new TcpForwarder(server, this, clientSocket, address);
        await _proxy.Init(address, port, bytes);
    }

    private async Task BindPort(IPAddress address, uint port, byte[] bytes)
    {
        _proxy = new ConnectionListener(server, this, clientSocket, address);
        await _proxy.Init(address, port, bytes);
    }

    private async Task AssociateUdpPort(IPAddress address, uint port, byte[] bytes)
    {
        _proxy = new UdpForwarder(server, this,  clientSocket, address, port);
        await _proxy.Init(address, port, bytes);
    }
}