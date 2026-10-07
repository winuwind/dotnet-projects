using System.Net;

namespace SOCKS_Proxy.Proxy;

public interface IProxy
{
    public Task Work();

    public Task Init(IPAddress address, uint port, byte[] bytes);

    public void Close();
}