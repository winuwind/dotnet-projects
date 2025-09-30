namespace Protocol.Printer;

public static class Printer
{
    private static readonly Lock SyncConsole = new();
    
    public static void Print(string? msg, int cursorLeft, int cursorTop)
    {
        lock (SyncConsole)
        {
            if (cursorLeft < 0 || cursorTop < 0)
            {
                if (msg != null)
                {
                    Console.WriteLine(msg);
                }
                else
                {
                    Console.WriteLine();
                }
            }
            else
            {
                if (msg == null) return;
                var (savePositionLeft, savePositionTop) = Console.GetCursorPosition(); 
                var lines = msg.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    Console.SetCursorPosition(cursorLeft, cursorTop + i); 
                    Console.Write(lines[i].PadRight(Console.WindowWidth - cursorLeft));
                } 
                Console.SetCursorPosition(savePositionLeft, savePositionTop);
            }
        }
    }
}