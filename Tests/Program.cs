using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamIdleManager;
namespace SteamIdleManager
{
    internal static class Logger
    {
        internal static void LogDebug(string text) { }
        internal static void LogInfo(string text) { }
        internal static void LogError(string text, Exception ex = null) { }
    }
}
class Program
{
    static int passed;
    static void Check(bool condition, string title) { if (!condition) throw new Exception(title); Console.WriteLine("PASS " + title); passed++; }
    static int Main(string[] args)
    {
        try { Run(args[0]).GetAwaiter().GetResult(); Console.WriteLine(passed + " checks passed"); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static async Task Run(string helper)
    {
        Check(SessionManager.ParseIds("10, 20;10\n30\t40").SequenceEqual(new uint[] { 10, 20, 30, 40 }), "separators and deduplication");
        foreach (string invalid in new[] { "", "0", "-1", "+1", "1.5", "abc", "4294967296", "10,wrong" })
        {
            bool rejected = false; try { SessionManager.ParseIds(invalid); } catch (FormatException) { rejected = true; }
            Check(rejected, "reject invalid input: " + invalid);
        }
        Check(GameSession.FormatElapsed(TimeSpan.FromHours(49) + TimeSpan.FromSeconds(62)) == "49:01:02", "timer beyond 24 hours");
        var manager = new SessionManager(helper);
        bool missing = false; try { manager.ValidateDependencies(); } catch (FileNotFoundException) { missing = true; }
        Check(missing, "missing dependencies reported");
        using (var external = Process.Start(new ProcessStartInfo(helper, "999") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }))
        {
            try
            {
                await manager.StartAsync(new uint[] { 10, 20, 10 }, CancellationToken.None);
                Check(manager.Sessions.Count == 2 && manager.Sessions.All(s => s.IsActive), "start two processes and ignore duplicates");
                var first = manager.Sessions[0].Process;
                await manager.StartAsync(new uint[] { 10 }, CancellationToken.None);
                Check(ReferenceEquals(first, manager.Sessions[0].Process), "keep already active process");
                Check(await manager.StopAsync(manager.Sessions[0]) && !manager.Sessions[0].IsActive && manager.Sessions[1].IsActive, "stop only selected process");
                await manager.StartAsync(new uint[] { 10 }, CancellationToken.None);
                Check(manager.Sessions.Count == 2 && manager.Sessions[0].IsActive, "restart stopped game without duplicate row");
                Check(await manager.StopAllAsync() && manager.Sessions.All(s => !s.IsActive), "stop all owned processes");
                Check(!external.HasExited, "unrelated steam-idle process remains alive");
                await manager.StartAsync(new uint[] { 42, 43 }, CancellationToken.None);
                Check(manager.Sessions.Single(s => s.AppId == 42).Status == "Falha (código 7)", "nonzero exit reported");
                Check(manager.Sessions.Single(s => s.AppId == 43).Status == "Encerrado", "normal exit reported");
                using (var cancellation = new CancellationTokenSource(150))
                    await manager.StartAsync(new uint[] { 11, 12, 13 }, cancellation.Token);
                Check(manager.Sessions.Any(s => s.AppId == 11) && !manager.Sessions.Any(s => s.AppId == 12), "cancel queued starts");
                Check(await manager.StopAllAsync() && manager.Sessions.All(s => !s.IsActive), "cancelled startup leaves no process after stop");
            }
            finally { await manager.StopAllAsync(); if (!external.HasExited) { external.Kill(); external.WaitForExit(); } }
        }
    }
}
