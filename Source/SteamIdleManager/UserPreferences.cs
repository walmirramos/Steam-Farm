using System;
using System.IO;
using System.Xml.Linq;

namespace SteamIdleManager
{
    internal sealed class UserPreferences
    {
        internal static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BDKSteamHourFarm-Modern");
        private static string SettingsPath { get { return Path.Combine(DirectoryPath, "settings.xml"); } }
        public string GameIds = "";
        public bool Dark = true;
        public static UserPreferences Load()
        {
            var preferences = new UserPreferences();
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var root = XDocument.Load(SettingsPath).Root;
                    preferences.GameIds = (string)root.Element("GameIds") ?? "";
                    preferences.Dark = (string)root.Element("Dark") != "false";
                }
            }
            catch (Exception ex) { Logger.LogWarning("Não foi possível carregar preferências: " + ex.Message); }
            return preferences;
        }
        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                string temporary = SettingsPath + ".tmp";
                new XDocument(new XElement("Preferences", new XElement("GameIds", GameIds), new XElement("Dark", Dark))).Save(temporary);
                File.Copy(temporary, SettingsPath, true);
                File.Delete(temporary);
                return true;
            }
            catch (Exception ex) { Logger.LogError("Não foi possível salvar preferências", ex); return false; }
        }
    }
}
