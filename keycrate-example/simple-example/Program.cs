using Keycrate;
using System.Text.Json;

Console.WriteLine("=== Keycrate – Simple Demo ===\n");

var client = new KeycrateClient(
    host: "https://api.keycrate.dev",
    appId: "YOUR_APP_ID"
);

Console.Write(" (1) Authenticate   (2) Register   → ");
var choice = Console.ReadLine()?.Trim();

if (choice == "1")
{
    Console.Write("License key (or ENTER for username): ");
    var key = Console.ReadLine()?.Trim();

    Dictionary<string, object> resp = key?.Length > 0
        ? await client.AuthenticateAsync(new AuthenticateOptions { License = key })
        : await PromptUserPassAsync(client);

    PrintResult(resp);
}
else if (choice == "2")
{
    Console.Write("License key to bind: ");
    var lic = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Username: ");
    var user = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Password: ");
    var pass = Console.ReadLine()?.Trim() ?? "";

    var resp = await client.RegisterAsync(new RegisterOptions
    {
        License  = lic,
        Username = user,
        Password = pass
    });
    PrintResult(resp);
}
else
{
    Console.WriteLine("Invalid choice – exiting.");
}

// ------------------------------------------------------------
// Helpers
// ------------------------------------------------------------
static async Task<Dictionary<string, object>> PromptUserPassAsync(KeycrateClient client)
{
    Console.Write("Username: ");
    var u = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Password: ");
    var p = Console.ReadLine()?.Trim() ?? "";
    return await client.AuthenticateAsync(new AuthenticateOptions { Username = u, Password = p });
}

static void PrintResult(Dictionary<string, object> r)
{
    bool ok = ((JsonElement)r["success"]).GetBoolean();
    string msg = ((JsonElement)r["message"]).GetString()!;

    Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
    Console.WriteLine($"\n{(ok ? "SUCCESS" : "FAILED")}: {msg}");
    Console.ResetColor();
}