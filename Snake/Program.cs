using Snake.Control;
using SnakeGame.GUI;

namespace SnakeGame;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var controller = new Controller();
        
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(controller));
    }
}