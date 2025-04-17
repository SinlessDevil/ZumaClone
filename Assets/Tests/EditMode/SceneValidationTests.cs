using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
    public class SceneValidationTests
    {
	    [TestCaseSource(nameof(AllScenesPath))]
	    public void ScenesHasNotMissingComponents(string scenePath)
	    {
		    var gameObjectsWithMissingScripts =
			    AccumulateInvalidGameObject(scenePath, HasMissingScripts);

		    gameObjectsWithMissingScripts.Should().BeEmpty();
	    }

	    [TestCaseSource(nameof(AllScenesPath))]
	    public void ScenesHasNotMissingPrefabs(string scenePath)
	    {
		    var missingPrefabs =
			    AccumulateInvalidGameObject(scenePath, PrefabUtility.IsPrefabAssetMissing);

		    missingPrefabs.Should().BeEmpty();
	    }
	    
	    [TestCaseSource(nameof(AllScenesPath))]
	    public void ScenesHasNotComponentsWithNullFields(string scenePath)
	    {
		    var componentsWithNull =
			    AccumulateInvalidComponents(scenePath, HasNullFields);

		    componentsWithNull.Should().BeEmpty();
	    }

	    private static IEnumerable<string> AccumulateInvalidGameObject(string scenePath, Predicate<GameObject> isInvalid)
	    {
		    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
		    var invalidGameObjects = AccumulateInvalidGameObject(scene, isInvalid);
		    EditorSceneManager.CloseScene(scene, removeScene: true);
		
		    return invalidGameObjects;
	    }

	    private static IEnumerable<string> AccumulateInvalidGameObject(Scene scene, Predicate<GameObject> isInvalid) =>
		    AllGameObjectIn(scene)
			    .Where(gameObject => isInvalid(gameObject))
			    .GroupBy(gameObject => gameObject.name)
			    .Select(grouped => $"{grouped.Key} - {grouped.Count()}")
			    .ToList();

	    private static IEnumerable<string> AccumulateInvalidComponents(string scenePath, Predicate<MonoBehaviour> isInvalid)
	    {
		    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
		    var invalidGameObjects = AccumulateInvalidComponents(scene, isInvalid);
		    EditorSceneManager.CloseScene(scene, removeScene: true);

		    return invalidGameObjects;
	    }

	    private static IEnumerable<string> AccumulateInvalidComponents(Scene scene, Predicate<MonoBehaviour> isInvalid)
	    {
		    //Debug.Log($"{scene.path} has {AllGameObjectIn(scene).Count()} gameObjects and {AllComponentsIn(scene).Count()} components and {AllComponentsIn(scene).Count(c => isInvalid(c))} invalid");
		    return AllComponentsIn(scene)
			    .Where(component => isInvalid(component))
			    .GroupBy(component => $"{component.gameObject.name}({component.GetType().Name})")
			    .Select(grouped => $"{grouped.Key} - {grouped.Count()}")
			    .ToList();
	    }

	    private static IEnumerable<MonoBehaviour> AllComponentsIn(Scene scene) =>
		    AllGameObjectIn(scene)
			    .SelectMany(x => x.GetComponents<MonoBehaviour>());

	    private static IEnumerable<GameObject> AllGameObjectIn(Scene scene)
	    {
		    var gameObjects = new Queue<GameObject>(scene.GetRootGameObjects());

		    while (gameObjects.Count > 0)
		    {
			    GameObject gameObject = gameObjects.Dequeue();

			    yield return gameObject;

			    foreach (Transform child in gameObject.transform)
				    gameObjects.Enqueue(child.gameObject);
		    }
	    }

	    private static bool HasMissingScripts(GameObject gameObject) =>
		    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0;

	    private static IEnumerable<string> AllScenesPath() =>
		    AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
			    .Select(AssetDatabase.GUIDToAssetPath);

	    private bool HasNullFields(MonoBehaviour component)
	    {
		    return component.GetType()
			    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			    .Where(IsSerializable)
			    .Where(CanNotBeNull)
			    .Any(WithNullValue);

		    bool IsSerializable(FieldInfo field) => 
			    field.FieldType.IsSubclassOf(typeof(Object)) 
			    && (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null);
		    
		    bool CanNotBeNull(FieldInfo field) => 
			    field.GetCustomAttribute<NotNullAttribute>() != null;

		    bool WithNullValue(FieldInfo x)
		    {
			    object value = x.GetValue(component);
			    return value == null || (Object)value == null;
		    }
	    }
    }
}
