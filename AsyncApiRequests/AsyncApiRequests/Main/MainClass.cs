using System.Runtime.InteropServices;
using System.Text;
using AsyncApiRequests.Location;

namespace AsyncApiRequests.Main;

public class MainClass
{
    public static async Task Main(string[] args)
    {
        await ConsoleHandler();
    }

    private static async Task ConsoleHandler()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.InputEncoding = Encoding.GetEncoding(866);
            Console.OutputEncoding = Encoding.GetEncoding(866);
        }
        while (true)
        {
            Console.Write("Enter name of location which you want to search (\"exit\" for exit): ");
            var command = Console.ReadLine();
            if (command == "exit")
            {
                return;
            }
            if (command != null)
            {
                var locInfo = new LocationInfo(command);
                await locInfo.GetInfo();
                locInfo.PrintInfo();
            }
        }
    }
}