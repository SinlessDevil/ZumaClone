using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Code.Infrastructure.Editor
{
    public class InfrastructureMenuItem
    {
        private const string GameRunnerPath = "Infrastructure/GameRunner";

        [MenuItem("GameObject/Infrastructure/GameRunner", false, 10)]
        public static void CreateSceneContext(MenuCommand menuCommand)
        {
            var gameRunnerPrefab = Resources.Load(GameRunnerPath);
            var root = PrefabUtility.InstantiatePrefab(gameRunnerPrefab) as GameObject;
            Selection.activeGameObject = root;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}