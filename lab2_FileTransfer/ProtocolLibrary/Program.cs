void WriteAt(int left, int top, string msg)
{
    // Убираем переносы, чтобы текст не "уехал вниз"
    var pos = Console.GetCursorPosition();
    
    var lines = msg.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        Console.SetCursorPosition(left, top + i);
        Console.Write(lines[i].PadRight(Console.WindowWidth - left));
    }
    Console.SetCursorPosition(pos.Left, pos.Top);
}

// Console.Clear();

var pos = Console.GetCursorPosition();
Console.WriteLine(pos);

Console.WriteLine("Client #1:");
Console.WriteLine("Client #2:");
Console.WriteLine("Client #3:");

pos = Console.GetCursorPosition();

Console.WriteLine(pos);

// потом в любом месте программы
// WriteAt(0, 2, "Client #2: Speed 120 KB/s");
WriteAt(0, 2, "Client #1: Speed 1510 KB/s");

// pos = Console.GetCursorPosition();
// Console.WriteLine(pos);
Console.WriteLine("Test");