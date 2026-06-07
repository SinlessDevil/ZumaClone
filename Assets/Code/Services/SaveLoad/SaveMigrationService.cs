using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Code.Services.SaveLoad
{
    public static class SaveMigrationService
    {
        private const string PlayerPrefsKey    = "PlayerData";
        private const string LegacyBackupKey   = "PlayerData_legacy_backup";
        private const string JsonFileName      = "player_data.json";
        private const string LegacyFileSuffix  = ".legacy_backup";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunMigration()
        {
            MigratePlayerPrefs();
            MigrateJsonFile();
        }

        // ── PlayerPrefs ───────────────────────────────────────────────────────────

        private static void MigratePlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;

            string raw = PlayerPrefs.GetString(PlayerPrefsKey);

            string json;
            try
            {
                byte[] bytes = Convert.FromBase64String(raw);
                json = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Unreadable — clear it
                ClearPlayerPrefs(raw);
                return;
            }

            if (!IsLegacySirenixJson(json)) return;

            ClearPlayerPrefs(raw);
        }

        private static void ClearPlayerPrefs(string rawBackup)
        {
            if (!PlayerPrefs.HasKey(LegacyBackupKey))
            {
                PlayerPrefs.SetString(LegacyBackupKey, rawBackup);
                PlayerPrefs.Save();
            }

            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
            Debug.LogWarning("[SaveMigration] Legacy Sirenix PlayerPrefs detected and cleared. " +
                             $"Raw backup saved under key '{LegacyBackupKey}'. Progress has been reset.");
        }

        // ── JSON file ─────────────────────────────────────────────────────────────

        private static void MigrateJsonFile()
        {
            string path = Path.Combine(Application.persistentDataPath, JsonFileName);
            if (!File.Exists(path)) return;

            string content;
            try
            {
                content = File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                // Unreadable binary — back up and remove
                BackupAndDeleteJsonFile(path);
                return;
            }

            if (!IsLegacySirenixJson(content)) return;

            BackupAndDeleteJsonFile(path);
        }

        private static void BackupAndDeleteJsonFile(string path)
        {
            string backupPath = path + LegacyFileSuffix;
            try
            {
                if (!File.Exists(backupPath))
                    File.Copy(path, backupPath);

                File.Delete(path);
                Debug.LogWarning("[SaveMigration] Legacy Sirenix JSON file detected and cleared. " +
                                 $"Backup saved at: {backupPath}. Progress has been reset.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveMigration] Failed to backup/delete legacy JSON: {e.Message}");
            }
        }

        // ── Detection ─────────────────────────────────────────────────────────────

        // Sirenix DataFormat.JSON always emits a "$type" discriminator field.
        // JsonUtility never does — so this check is reliable.
        private static bool IsLegacySirenixJson(string json) =>
            json.TrimStart().StartsWith("{") && json.Contains("\"$type\"");
    }
}
