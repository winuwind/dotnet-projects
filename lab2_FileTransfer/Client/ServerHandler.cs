namespace Client;

using System.Net.Sockets;
using ProtocolLibrary.Handler;

public class ServerHandler : AbstractHandler
{
    public ServerHandler(TcpClient client)
    {
        Stream = client.GetStream();
        IsRunning = true;
        TransferData = null;
        Id = -1;
    }
}