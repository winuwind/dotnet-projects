using Serilog.Events;
using Snake.Control;
using Snake.GUI;

namespace Snake;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        var controller = new Controller(ParseLogLevel(args));
        
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(controller));
    }
    
    static LogEventLevel ParseLogLevel(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--log=", StringComparison.OrdinalIgnoreCase))
            {
                var value = arg.Substring("--log=".Length).ToLowerInvariant();
                return value switch
                {
                    "verbose" => LogEventLevel.Verbose,
                    "debug"   => LogEventLevel.Debug,
                    "info"    => LogEventLevel.Information,
                    "warn"    => LogEventLevel.Warning,
                    "error"   => LogEventLevel.Error,
                    "fatal"   => LogEventLevel.Fatal,
                    _         => LogEventLevel.Information
                };
            }
        }

        return LogEventLevel.Warning;
    }
}