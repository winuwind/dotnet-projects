using System.Net;
using Snakes;

namespace Snake.Config;

public static class AppConstant
{
    public const int MulticastPort = 9192;
    public const int DelayAnnouncementMsg = 1000;
    public const int DelayDeleteAnnouncementMsg = 5000;
    
    public static readonly IPAddress MulticastAddress = IPAddress.Parse("239.192.0.4");

    public static GameState.Types.Coord Size { get; set; } = new GameState.Types.Coord
    {
        X = 100,
        Y = 100
    };

    public static int StaticFood { get; set; } = 1;

    public static int StateDelayMs
    {
        get => _stateDelayMs;
        set
        {
            _stateDelayMs = value;
            DelayBetweenMsgs = _stateDelayMs / 10;
            DelayBetweenRepeatMsg = _stateDelayMs / 10;
            PlayerTimeout = 8 * _stateDelayMs / 10;
        }
    }

    public static int DelayBetweenMsgs { get; private set; } = 100;

    public static int DelayBetweenRepeatMsg { get; private set; } = 100;

    public static int PlayerTimeout { get; private set; } = 800;

    private static int _stateDelayMs = 1000;
}