namespace Protocol.Printer;

public static class Printer
{
    private static int _cursorLeft = 0;
    private static int _cursorTop = 0;
    
    private static readonly Lock SyncConsole = new();

    public static (int Left, int Top) GetCursorPosition()
    {
        return (_cursorLeft, _cursorTop);
    }
    
    public static void Print(string? msg, int cursorLeft, int cursorTop)
    {
        lock (SyncConsole)
        {
            if (cursorLeft < 0 || cursorTop < 0)
            {
                if (msg != null)
                {
                    var lines = msg?.Split('\n');
                    _cursorTop += lines?.Length ?? 0;
                    Console.WriteLine(msg);
                }
                else
                {
                    _cursorTop++;
                    Console.WriteLine();
                }
            }
            else
            {
                if (msg == null) return;
                var lines = msg.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    Console.SetCursorPosition(cursorLeft, cursorTop + i); 
                    Console.Write(lines[i].PadRight(Console.WindowWidth - cursorLeft));
                }
                Console.SetCursorPosition(_cursorLeft, _cursorTop);
            }
        }
    }
}