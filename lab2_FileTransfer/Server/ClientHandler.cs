namespace Server;

using System.Net.Sockets;

using Protocol.Handler;

public class ClientHandler : AbstractHandler
{
    public ClientHandler(TcpClient client, int id, Server server)
    {
        Stream = client.GetStream();
        IsRunning = true;
        TransferData = new TransferData(this, server);
        Id = id;
    }

    public int AskSegments()
    {
        return TransferData.AskSegments();
    }
}