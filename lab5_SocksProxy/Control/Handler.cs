using System.Net;
using System.Net.Sockets;
using Serilog;
using SOCKS_Proxy.Proxy;

namespace SOCKS_Proxy.Control;

public class Handler(Socket clientSocket, Server server)
{
    private byte _command;
    private IProxy? _proxy = null;
    private bool _isClosed =  false;

    public void CloseConnection()
    {
        if (_isClosed)
        {
            return;
        }
        _isClosed = true;
        _proxy?.Close();
        try
        {
            clientSocket.Close();
        }
        catch(Exception e)
        {
            Log.Warning("Handler.CloseConnection(): error when closed clientSocket: " + e.Message);
        }
        server.DeleteClient(this);
    }
    
    public async Task CreateConnection()
    {
        var bytes = new byte[AppConstant.SizeBuffer];
        
        var count = await server.ReadExact(clientSocket, bytes, 0, 2, null);
        
        if (bytes[AppConstant.IndexVersion] != AppConstant.SocksVersion || count < 2)
        {
            CloseConnection();
            return;
        }
        
        await server.ReadExact(clientSocket, bytes, 2, bytes[AppConstant.IndexNumberMethods], null);
        
        var numberMethod = Server.ChooseAuthMethod(bytes);
        
        bytes[AppConstant.IndexVersion] = AppConstant.SocksVersion;
        bytes[AppConstant.IndexMethod] = numberMethod;
        count = await server.Send(clientSocket, bytes, AppConstant.IndexVersion, 2, null);
        
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
        
        await server.ReadExact(clientSocket, bytes, AppConstant.IndexVersion, 4, null);
        if (bytes[AppConstant.IndexVersion] != AppConstant.SocksVersion || bytes[AppConstant.IndexReserved] != AppConstant.ReservedByte)
        {
            CloseConnection();
            return;
        }
        
        var type = bytes[AppConstant.IndexTypeAddress];
        switch (type)
        {
            case AppConstant.Ipv4Command: await server.ReadExact(clientSocket, bytes, AppConstant.IndexAddress, 4, null);
                break;
            case AppConstant.DnsCommand: await server.ReadExact(clientSocket, bytes, AppConstant.IndexAddress, 1, null);
                if (bytes[AppConstant.IndexAddress] == 0x00)
                {
                    CloseConnection();
                    return;
                }
                await server.ReadExact(clientSocket, bytes, AppConstant.IndexAddress + 1, bytes[4], null);
                break;
            case AppConstant.Ipv6Command: await server.ReadExact(clientSocket, bytes, AppConstant.IndexAddress, 16, null);
                break;
            default:
                bytes[AppConstant.IndexErrorType] = AppConstant.UnknownAddressType;
                bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
                await server.Send(clientSocket, bytes, AppConstant.IndexVersion, 10, null);
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
            bytes[AppConstant.IndexErrorType] = AppConstant.HostUnreachable;
            bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
            await server.Send(clientSocket, bytes, AppConstant.IndexVersion, 10, null);
            CloseConnection();
            return;
        }
        
        await server.ReadExact(clientSocket, bytes, AppConstant.IndexPort, 2, null);
        var port = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, AppConstant.IndexPort));

        _command = bytes[AppConstant.IndexCommand];
        switch (_command)
        {
            case AppConstant.ConnectCommand: await StreamConnection(address, port, bytes);
                break;
            case AppConstant.BindCommand: await BindPort(address, port, bytes);
                break;
            case AppConstant.UdpCommand: await AssociateUdpPort(address, port, bytes);
                break;
            default: 
                bytes[AppConstant.IndexErrorType] = AppConstant.ProtocolError;
                bytes[AppConstant.IndexTypeAddress] = AppConstant.Ipv4Command;
                await server.Send(clientSocket, bytes, AppConstant.IndexVersion, 10, null);
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
        return Task.FromException(new NotSupportedException($"Auth method {numberMethod} is not implemented."));
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