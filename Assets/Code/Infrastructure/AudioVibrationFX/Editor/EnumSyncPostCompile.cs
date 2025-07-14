using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Code
{
    [InitializeOnLoad]
    public static class EnumSyncPostCompile
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            Debug.Log("🔄 Scripts recompiled. Trying to sync enums...");

            var soundWindow = Resources.FindObjectsOfTypeAll<Code.Infrastructure.AudioVibrationFX.Editor.SoundLibraryEditorWindow>().FirstOrDefault();
            if (soundWindow != null)
            {
                soundWindow.UpdateSoundTypesAfterReload();
                Debug.Log("✅ SoundTypes synced!");
            }
            else
            {
                Debug.LogWarning("⚠️ SoundLibraryEditorWindow not open. Skipping SoundTypes sync.");
            }

            var vibrationWindow = Resources.FindObjectsOfTypeAll<Code.Infrastructure.AudioVibrationFX.Editor.VibrationLibraryEditorWindow>().FirstOrDefault();
            if (vibrationWindow != null)
            {
                vibrationWindow.UpdateVibrationTypesAfterReload();
                Debug.Log("✅ VibrationTypes synced!");
            }
            else
            {
                Debug.LogWarning("⚠️ VibrationLibraryEditorWindow not open. Skipping VibrationTypes sync.");
            }
        }
    }
}