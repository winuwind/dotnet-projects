namespace SOCKS_Proxy.Proxy;

public static class AppConstant
{
    public const byte SocksVersion = 0x05;
    
    public const byte ReservedByte = 0x00;

    public const byte NoAuth = 0x00;
    public const byte NoMethodsAuthAvailable = 0xFF;
    
    public const byte ConnectCommand = 0x01;
    public const byte BindCommand = 0x02;
    public const byte UdpCommand = 0x03;
    
    public const byte Ipv4Command = 0x01;
    public const byte DnsCommand = 0x03;
    public const byte Ipv6Command = 0x04;

    public const byte Success = 0x00;
    public const byte ServerError = 0x01;
    public const byte NetworkUnreachable = 0x03;
    public const byte HostUnreachable = 0x04;
    public const byte ConnectionRefused = 0x05;
    public const byte ProtocolError = 0x07;
    public const byte UnknownAddressType = 0x08;
    
    public const byte NoFragmentation = 0x00;
}