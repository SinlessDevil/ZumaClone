using UnityEditor;
using UnityEngine;

namespace Code.Infrastructure.Editor
{
    public static class StaticDataMenuTool
    {
        [MenuItem("GameEditors/📈 Balance Editor %F1", priority = 7)]
        public static void OpenBalance() => 
            SelectObject(at: "Assets/Resources/StaticData/Balance/Balance.asset"); 
        
        [MenuItem("GameEditors/⚙ GameConfig %F3")]
        public static void OpenGameConfig() => 
            SelectObject(at: "Assets/Resources/StaticData/Balance/GameConfig.asset");

        private static void SelectObject(string at)
        {
            Object targetAsset = AssetDatabase
                .LoadAssetAtPath<Object>(at);

            Selection.activeObject = targetAsset;
            EditorGUIUtility.PingObject(targetAsset);
        }
    }
}