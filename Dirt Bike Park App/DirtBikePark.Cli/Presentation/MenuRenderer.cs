using System;

namespace DirtBikePark.Cli.Presentation;

/// <summary>
/// Responsible for printing menus and formatted output to the console.
/// </summary>
public class MenuRenderer
{
    public void ShowWelcomeHeader()
    {
        Console.Clear();
        Console.WriteLine("==========================================");
        Console.WriteLine(" Dirt Bike Park Booking Portal");
        Console.WriteLine("==========================================");
        Console.WriteLine();
        Console.WriteLine("Type 'help' for a list of commands or 'quit' to exit.");
        Console.WriteLine();
    }

    public void ShowPrompt() => Console.Write("dbp > ");

    public void ShowHelp()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine("  help                           Show this help message");
        Console.WriteLine("  welcome                        Reopen the welcome screen to pick a park");
        Console.WriteLine("  parks list                     Browse the available parks");
        Console.WriteLine("  parks view <parkId>            View details for a park");
        Console.WriteLine("  cart                           Show your current cart");
        Console.WriteLine("  checkout                       Proceed to payment for your cart");
        Console.WriteLine("  quit                           Exit the application");
        Console.WriteLine();
        Console.WriteLine("Tip: Start with 'welcome' to follow the guided booking flow.");
        Console.WriteLine();
    }

    public void ShowError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ForegroundColor = originalColor;
    }

    public void ShowSuccess(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    public void ShowInfo(string message)
    {
        Console.WriteLine(message);
    }
}
