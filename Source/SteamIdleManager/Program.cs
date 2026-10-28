using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SteamIdleManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Limpa o log anterior (opcional - pode comentar para manter histórico)
            // Logger.ClearLog();
            
            Logger.LogInfo("=== Dubbox — Steam Hora Farmada iniciado ===");
            Logger.LogInfo($"Versão: {Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.LogInfo($"Diretório: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
            
            Logger.LogInfo("=== Dubbox — Steam Hora Farmada finalizado ===");
        }
    }
}

