using System.Net;

namespace SOCKS_Proxy.Control;

public struct TaskWrite(TaskCompletionSource<int> tcs, byte[] buffer, int offset, int count, IPEndPoint? endPoint)
{
    public readonly TaskCompletionSource<int> Tcs = tcs;
    public readonly byte[] Buffer = buffer;
    public readonly EndPoint? EndPoint = endPoint;
    
    public int Count = count;
    public int Offset = offset;
}