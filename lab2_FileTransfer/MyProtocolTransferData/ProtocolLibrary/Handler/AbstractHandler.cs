namespace ProtocolLibrary.Handler;

using System.Net.Sockets;
using Protocol;

public abstract class AbstractHandler : IHandler
{
    private static readonly Lock Sync = new Lock();
    
    protected NetworkStream Stream = null!;
    protected ITransfer? TransferData;
    protected bool IsRunning;
    protected int Id;


    public void SetTransferData(ITransfer transferData)
    {
        TransferData = transferData;
    }

    public void Send(byte[] bytes)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            lock (Sync)
            {
                if (IsRunning)
                {
                    Stream.Write(bytes, 0, bytes.Length);
                }
            }
        });
    }
    
    private void ReadExact(byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            var bytesRead = Stream.Read(buffer, offset + totalRead, count - totalRead);
            if (bytesRead == 0)
            {
                throw new IOException("Closed connection");
            }
            totalRead += bytesRead;
        }
    }

    public void Receive()
    {
        try
        {
            while (IsRunning && Thread.CurrentThread.IsAlive)
            {
                var sizeSegmentBytes = new byte[8];
                
                ReadExact(sizeSegmentBytes, 0, sizeSegmentBytes.Length);
                var segmentSize = BitConverter.ToInt64(sizeSegmentBytes, 0);
                var bytes = new byte[segmentSize - 8];
                ReadExact(bytes, 0, bytes.Length);
                if (TransferData != null)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                        TransferData.ReceiveSegment(bytes, segmentSize, Id));
                }
            }
        }
        catch (Exception)
        {
            // Console.WriteLine("Connection closed");
            IsRunning = false;
        }
    }

    public int GetId()
    {
        return Id;
    }
    
    public void Close()
    {
        IsRunning = false;
        if (TransferData != null)
        {
            var bytes = ITransfer.Serialize(TransferData.ExitSegment());
            try
            {
                Stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        Stream.Close();
    }
}