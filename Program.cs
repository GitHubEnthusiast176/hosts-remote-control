using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    private static string token = "";
    private static string owner = "";
    private static string repo = "";
    private static string filePath = "";
    private static int intervalSeconds = 30;
    private static int logRetentionDays = 7;   // настройка для хранения логов
    private static List<string> hostsEntries = new List<string>();

    private const string hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
    private static string logPath = Path.Combine(AppContext.BaseDirectory, "control.log");
    private static string localStatusPath = Path.Combine(AppContext.BaseDirectory, "local_status.txt");

    private static DateTime lastLogCleanup = DateTime.MinValue;

    static async Task Main()
    {
        try
        {
            LoadConfig();
            CleanupLog();   // проверка и очистка логов при запуске
            Log("App started");

            while (true)
            {
                try
                {
                    string remoteStatus = await GetStatusFromGitHub();
                    string localStatus = GetLocalStatus();

                    if (remoteStatus != localStatus)
                    {
                        if (remoteStatus == "0")
                        {
                            BlockHosts();
                            SetLocalStatus("0");
                        }
                        else if (remoteStatus == "1")
                        {
                            AllowHosts();
                            SetLocalStatus("1");
                        }
                        else
                        {
                            Log($"Unknown remote status: {remoteStatus}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error in loop: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
            }
        }
        catch (Exception ex)
        {
            Log($"Fatal error: {ex}");
        }
    }

    // ---------------- CONFIG ----------------

    static void LoadConfig()
    {
        var config = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText("config.json"));

        token = config["token"].ToString();
        owner = config["owner"].ToString();
        repo = config["repo"].ToString();
        filePath = config["filePath"].ToString();
        intervalSeconds = int.Parse(config["intervalSeconds"].ToString());

        if (config.ContainsKey("hostsEntries"))
        {
            hostsEntries = JsonSerializer.Deserialize<List<string>>(config["hostsEntries"].ToString()) ?? new List<string>();
        }

        if (config.ContainsKey("logRetentionDays"))
        {
            int.TryParse(config["logRetentionDays"].ToString(), out logRetentionDays);
        }
    }

    // ---------------- STATUS ----------------

    static string GetLocalStatus()
    {
        if (!File.Exists(localStatusPath))
            return "";
        return File.ReadAllText(localStatusPath).Trim();
    }

    static void SetLocalStatus(string status)
    {
        File.WriteAllText(localStatusPath, status);
    }

    static async Task<string> GetStatusFromGitHub()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("control-app", "1.0"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{filePath}";
        var response = await client.GetStringAsync(url);

        using var doc = JsonDocument.Parse(response);
        var content = doc.RootElement.GetProperty("content").GetString();

        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content));
        return decoded.Trim();
    }

    // ---------------- HOSTS ----------------

    static void BlockHosts()
    {
        var lines = File.ReadAllLines(hostsPath);
        using var writer = new StreamWriter(hostsPath, false);

        foreach (var line in lines)
        {
            if (!IsManagedLine(line))
                writer.WriteLine(line);
        }

        foreach (var host in hostsEntries)
        {
            writer.WriteLine($"127.0.0.1 {host} # BLOCKED-BY-APP");
        }

        FlushDns();
        Log("Status = BLOCK (hosts updated)");
    }

    static void AllowHosts()
    {
        var lines = File.ReadAllLines(hostsPath);
        using var writer = new StreamWriter(hostsPath, false);

        foreach (var line in lines)
        {
            if (line.Contains("# BLOCKED-BY-APP"))
            {
                if (!line.TrimStart().StartsWith("#"))
                    writer.WriteLine($"# {line}");
                else
                    writer.WriteLine(line);
            }
            else
            {
                writer.WriteLine(line);
            }
        }

        FlushDns();
        Log("Status = ALLOW (hosts commented)");
    }

    static bool IsManagedLine(string line)
    {
        foreach (var host in hostsEntries)
        {
            if (line.Contains(host, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ---------------- DNS ----------------

    static void FlushDns()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            Log("DNS cache flushed");
        }
        catch (Exception ex)
        {
            Log($"FlushDNS error: {ex}");
        }
    }

    // ---------------- LOG ----------------

    static void Log(string message)
    {
        try
        {
            // Раз в сутки проверяем и чистим лог
            if ((DateTime.Now - lastLogCleanup).TotalHours >= 24)
            {
                CleanupLog();
                lastLogCleanup = DateTime.Now;
            }

            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }

    static void CleanupLog()
    {
        try
        {
            if (!File.Exists(logPath)) return;

            var lines = File.ReadAllLines(logPath);
            var cutoff = DateTime.Now.AddDays(-logRetentionDays);

            var filtered = new List<string>();
            foreach (var line in lines)
            {
                if (line.Length >= 19 && DateTime.TryParse(line.Substring(0, 19), out var ts))
                {
                    if (ts >= cutoff)
                        filtered.Add(line);
                }
                else
                {
                    filtered.Add(line);
                }
            }

            File.WriteAllLines(logPath, filtered);
            // ⚠️ Больше не вызываем Log() здесь → убрали рекурсию
        }
        catch
        {
            // ignore
        }
    }
}
