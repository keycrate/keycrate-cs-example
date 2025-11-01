using Keycrate;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Runtime.Versioning;

Console.WriteLine("=== Keycrate – Full Demo ===\n");

// ------------------------------------------------------------------
// 1. Real HWID (via WMI)
// ------------------------------------------------------------------
string hwid = Hwid.Get();
Console.WriteLine($"Your HWID: {hwid}\n");

// ------------------------------------------------------------------
// 2. Client
// ------------------------------------------------------------------
var client = new KeycrateClient(
    host: "https://api.keycrate.dev",
    appId: "YOUR_APP_ID"
);
// ------------------------------------------------------------------
// 3. Login
// ------------------------------------------------------------------
var (loggedIn, licenseKey) = await LoginAsync(client, hwid);
if (!loggedIn)
{
    Console.WriteLine("\nAccess denied – goodbye.");
    return;
}

Console.WriteLine("\nWelcome! You have access.\n");

// ------------------------------------------------------------------
// 4. Post-login menu
// ------------------------------------------------------------------
while (true)
{
    Console.Write("Type 'register' or 'exit': ");
    var cmd = Console.ReadLine()?.Trim().ToLowerInvariant();
    if (cmd == "exit") { Console.WriteLine("Bye!"); break; }
    if (cmd == "register") { await RegisterAsync(client, licenseKey!); break; }
    Console.WriteLine("Invalid command.");
}

// ------------------------------------------------------------------
// IMPLEMENTATIONS
// ------------------------------------------------------------------
static async Task<(bool success, string? licenseKey)> LoginAsync(KeycrateClient c, string hwid)
{
    Console.WriteLine("=== Login ===");
    Console.Write("License key (press ENTER for username/password): ");
    var key = Console.ReadLine()?.Trim();

    Dictionary<string, object> resp;

    try
    {
        resp = !string.IsNullOrEmpty(key)
            ? await c.AuthenticateAsync(new AuthenticateOptions { License = key, Hwid = hwid })
            : await UsernameLoginAsync(c, hwid);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection error: {ex.Message}");
        return (false, null);
    }

    bool success = ((JsonElement)resp["success"]).GetBoolean();
    if (!success)
    {
        string msg = ((JsonElement)resp["message"]).GetString()!;
        JsonElement? data = resp.TryGetValue("data", out var d) && d is JsonElement je ? je : null;
        PrintError(msg, data);
        return (false, null);
    }

    Console.WriteLine("\nLogin successful!\n");
    return (true, ((JsonElement)resp["data"]).GetProperty("key").GetString());
}

static async Task<Dictionary<string, object>> UsernameLoginAsync(KeycrateClient c, string hwid)
{
    Console.Write("Username: ");
    var u = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Password: ");
    var p = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
    {
        Console.WriteLine("Both fields required.");
        Environment.Exit(0);
    }
    return await c.AuthenticateAsync(new AuthenticateOptions { Username = u, Password = p, Hwid = hwid });
}

static async Task RegisterAsync(KeycrateClient c, string license)
{
    Console.WriteLine("\n=== Register Username & Password ===");
    Console.Write("Username: "); var u = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Password: "); var p = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
    {
        Console.WriteLine("Can't be empty.");
        return;
    }

    try
    {
        var resp = await c.RegisterAsync(new RegisterOptions
        {
            License = license,
            Username = u,
            Password = p
        });

        bool ok = ((JsonElement)resp["success"]).GetBoolean();
        string msg = ((JsonElement)resp["message"]).GetString()!;
        Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"\n{(ok ? "SUCCESS" : "FAILED")}: {msg}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Register failed: {ex.Message}");
    }
}

static void PrintError(string msg, JsonElement? data)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nAuthentication failed: {msg}");
    Console.ResetColor();

    switch (msg)
    {
        case "LICENSE_NOT_FOUND":
            Console.WriteLine("License key not found – double-check it.");
            break;
        case "INVALID_USERNAME_OR_PASSWORD":
            Console.WriteLine("Wrong username or password.");
            break;
        case "LICENSE_NOT_ACTIVE":
            Console.WriteLine("License is not active – contact support.");
            break;
        case "DEVICE_ALREADY_REGISTERED_WITH_OTHER_LICENSE":
            Console.WriteLine("This device is already bound to another license.");
            break;
        case "LICENSE_EXPIRED":
            if (data.HasValue && data.Value.TryGetProperty("expires_at", out var exp) && exp.GetString() is { } iso)
            {
                if (DateTime.TryParse(iso.Replace("Z", "+00:00"), null, DateTimeStyles.AssumeUniversal, out var dt))
                {
                    Console.WriteLine($"License expired on: {dt:yyyy-MM-dd HH:mm:ss} UTC");
                }
                else
                {
                    Console.WriteLine("License has expired (invalid date format).");
                }
            }
            else
            {
                Console.WriteLine("License has expired.");
            }
            break;
        case "HWID_MISMATCH":
            Console.WriteLine("HWID does not match the registered device.");
            if (data.HasValue && data.Value.TryGetProperty("hwid_reset_allowed", out var allow) && allow.GetBoolean())
            {
                if (data.Value.TryGetProperty("last_hwid_reset_at", out var lastProp) && lastProp.GetString() is { } lastStr &&
                    data.Value.TryGetProperty("hwid_reset_cooldown", out var cdProp) && cdProp.TryGetInt32(out int cd))
                {
                    if (DateTime.TryParse(lastStr.Replace("Z", "+00:00"), null, DateTimeStyles.AssumeUniversal, out var lastDt))
                    {
                        var ago = (DateTime.UtcNow - lastDt).TotalSeconds;
                        var left = cd - (int)ago;
                        Console.WriteLine(left > 0 ? $"Reset available in {left} seconds." : "HWID reset is now available.");
                    }
                    else
                    {
                        Console.WriteLine("Try resetting HWID (invalid timestamp).");
                    }
                }
                else
                {
                    Console.WriteLine("Try resetting HWID.");
                }
            }
            else
            {
                Console.WriteLine("HWID reset not allowed.");
            }
            break;
        default:
            Console.WriteLine($"Unexpected error: {msg}. Contact support.");
            break;
    }
}

// ------------------------------------------------------------------
// Real HWID via System.Management (WMI)
// ------------------------------------------------------------------
static class Hwid
{
    [SupportedOSPlatform("windows")]
    public static string Get()
    {
        var parts = new[]
        {
            GetCpuId(),
            GetBiosSerial(),
            GetDiskSerial()
        };

        string combined = string.Join("|", parts.Where(s => !string.IsNullOrEmpty(s)));
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return BitConverter.ToString(hash).Replace("-", "").ToLower()[..16];
    }

    [SupportedOSPlatform("windows")]
    private static string GetCpuId() => QueryWmi("Win32_Processor", "ProcessorId")?.Trim() ?? "";

    [SupportedOSPlatform("windows")]
    private static string GetBiosSerial() => QueryWmi("Win32_BIOS", "SerialNumber")?.Trim() ?? "";

    [SupportedOSPlatform("windows")]
    private static string GetDiskSerial() => QueryWmi("Win32_DiskDrive", "SerialNumber", "WHERE Index=0")?.Trim() ?? "";

    [SupportedOSPlatform("windows")]
    private static string? QueryWmi(string className, string property, string? where = null)
    {
        try
        {
            string query = $"SELECT {property} FROM {className}";
            if (where != null) query += " " + where;
            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
                return obj[property]?.ToString();
        }
        catch { }
        return null;
    }
}