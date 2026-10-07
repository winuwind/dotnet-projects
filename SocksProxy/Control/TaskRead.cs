using System.Net;

namespace SOCKS_Proxy.Control;

public struct TaskRead(TaskCompletionSource<int> tcs, byte[] buffer, int offset, int count, bool flagNeedFull, IPEndPoint? endPoint)
{
    public readonly TaskCompletionSource<int> Tcs = tcs;
    public readonly byte[] Buffer = buffer;
    public readonly bool FlagNeedFull = flagNeedFull;
    
    public EndPoint? EndPoint = endPoint;
    public int Count = count;
    public int Offset = offset;
}