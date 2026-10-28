using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteamIdleManager
{
    public sealed class GameSession
    {
        public uint AppId { get; internal set; }
        public string Status { get; internal set; }
        internal Process Process;
        internal readonly Stopwatch Clock = new Stopwatch();
        public bool IsActive { get { return Process != null; } }
        public string Elapsed { get { return FormatElapsed(Clock.Elapsed); } }
        public static string FormatElapsed(TimeSpan elapsed)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                (long)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
        }
    }

    // State changes stay on the caller's synchronization context (the UI thread).
    public sealed class SessionManager
    {
        private readonly string helperPath;
        private readonly List<GameSession> sessions = new List<GameSession>();
        public IReadOnlyList<GameSession> Sessions { get { return sessions; } }
        public SessionManager(string helperPath) { this.helperPath = Path.GetFullPath(helperPath); }
        public static uint[] ParseIds(string text)
        {
            var tokens = (text ?? "").Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) throw new FormatException("Informe pelo menos um AppID.");
            var ids = new List<uint>();
            foreach (string token in tokens)
            {
                uint id;
                if (!uint.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out id) || id == 0)
                    throw new FormatException("AppID inválido: " + token + ". Use apenas números inteiros positivos.");
                if (!ids.Contains(id)) ids.Add(id);
            }
            return ids.ToArray();
        }
        public void ValidateDependencies()
        {
            string directory = Path.GetDirectoryName(helperPath);
            foreach (string file in new[] { Path.GetFileName(helperPath), "steam_api64.dll", "Steamworks.NET.dll", "steam-idle.exe.config" })
                if (!File.Exists(Path.Combine(directory, file)))
                    throw new FileNotFoundException("Arquivo necessário ausente: " + file + ". Extraia novamente a pasta completa.");
        }
        public async Task<int> StartAsync(IEnumerable<uint> ids, CancellationToken cancellation)
        {
            int started = 0;
            foreach (uint id in ids.Distinct())
            {
                if (cancellation.IsCancellationRequested) break;
                Refresh();
                if (sessions.Any(s => s.AppId == id && s.IsActive)) continue;
                var session = sessions.FirstOrDefault(s => s.AppId == id);
                if (session == null) { session = new GameSession { AppId = id }; sessions.Add(session); }
                session.Clock.Reset();
                session.Status = "Iniciando";
                Process process = null;
                try
                {
                    process = new Process { StartInfo = new ProcessStartInfo {
                        FileName = helperPath, Arguments = id.ToString(CultureInfo.InvariantCulture),
                        WorkingDirectory = Path.GetDirectoryName(helperPath),
                        UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true, RedirectStandardError = true
                    }};
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) Logger.LogDebug("[AppID " + id + "] " + e.Data); };
                    // Steam writes informational diagnostics to stderr too; exit codes determine failure.
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) Logger.LogDebug("[AppID " + id + " / stderr] " + e.Data); };
                    if (!process.Start()) throw new InvalidOperationException("O processo não pôde ser iniciado.");
                    session.Process = process;
                    session.Clock.Start();
                    Logger.LogInfo("Iniciado AppID " + id + ", PID " + process.Id);
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await Task.Delay(1000, cancellation);
                    Refresh();
                    if (session.IsActive) { session.Status = "Em execução"; started++; }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger.LogError("Falha ao iniciar AppID " + id, ex);
                    if (session.Process != null) await StopAsync(session);
                    else if (process != null) process.Dispose();
                    if (!session.IsActive) session.Status = "Falha ao iniciar";
                }
            }
            return started;
        }
        public void Refresh()
        {
            foreach (var session in sessions.Where(s => s.IsActive))
            {
                if (!session.Process.HasExited) continue;
                int code = session.Process.ExitCode;
                session.Status = code == 0 ? "Encerrado" : "Falha (código " + code + ")";
                Logger.LogInfo("AppID " + session.AppId + " encerrado, código " + code);
                Release(session);
            }
        }
        private static void Release(GameSession session)
        {
            session.Clock.Stop();
            session.Process.Dispose();
            session.Process = null;
        }
        public async Task<bool> StopAsync(GameSession session)
        {
            var process = session.Process;
            if (process == null) return true;
            try
            {
                if (!process.HasExited) process.Kill();
                var timeout = Stopwatch.StartNew();
                while (session.Process == process && !process.HasExited && timeout.ElapsedMilliseconds < 3000) await Task.Delay(50);
                if (session.Process == process)
                {
                    if (!process.HasExited) throw new TimeoutException("O processo não respondeu ao encerramento.");
                    Release(session);
                }
                session.Status = "Parado";
                return true;
            }
            catch (Exception ex)
            {
                session.Status = "Falha ao parar";
                Logger.LogError("Falha ao parar AppID " + session.AppId, ex);
                return false;
            }
        }
        public async Task<bool> StopAllAsync()
        {
            bool success = true;
            foreach (var session in sessions.ToArray()) if (!await StopAsync(session)) success = false;
            return success;
        }
    }
}
