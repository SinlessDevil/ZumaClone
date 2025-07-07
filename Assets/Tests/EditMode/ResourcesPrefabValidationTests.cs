using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public class ResourcesPrefabValidationTests
    {
        [Test]
        public void AllPrefabsInResourcesAreValid()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" });

            List<string> brokenPrefabs = new();

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    brokenPrefabs.Add(path);
                    continue;
                }

                var missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
                if (missing > 0)
                {
                    brokenPrefabs.Add($"{path} (Missing Scripts: {missing})");
                }
            }

            brokenPrefabs.Should().BeEmpty("Some prefabs in Resources have missing scripts or are broken:\n" +
                                           string.Join("\n", brokenPrefabs));
        }
    }
}