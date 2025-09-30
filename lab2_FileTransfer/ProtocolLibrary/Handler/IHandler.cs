namespace Protocol.Handler;

using Protocol;

public interface IHandler
{

    public void SetTransferData(ITransfer transferData);
    public void Send(byte[] bytes);
    public void Receive();
    public void Close();
    public int GetId();
}