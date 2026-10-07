namespace Protocol.Constants;

public static class AppConstants
{
    public const int MaxLenghtName = 4096 * 16;
    public const int DefaultPort = 1123;
    public const int MaxSizeData = 4096;
    public const int HeaderLenght = 40;
    
    public const long Terabyte = 1024L * 1024L * 1024L * 1024L;
    
    public const double Kilo = 1024.0;
    public const double Mega = 1024.0 * 1024.0;
    public const double Giga = 1024.0 * 1024.0 * 1024.0;
    public const double Tera = 1024.0 * 1024.0 * 1024.0 * 1024.0;
    public const double KiloR = 1.0 / Kilo;
    public const double MegaR = 1.0 / Mega;
    public const double GigaR = 1.0 / Giga;
}