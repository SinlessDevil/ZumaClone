using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using TestMode = UnityEditor.TestTools.TestRunner.Api.TestMode;

namespace Code.Editor.Tests
{
    public class TestsToolWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Code/Editor/Tests/TestsToolWindow.uxml";
        private const string UssPath  = "Assets/Code/Editor/Tests/TestsToolWindow.uss";

        private readonly List<GroupedTestGroup> _groupedEditMode = new();
        private readonly List<GroupedTestGroup> _groupedPlayMode = new();

        private VisualElement _editModeContainer;
        private VisualElement _playModeContainer;

        [MenuItem("Tools/Tests Tool Window", false, 2000)]
        private static void OpenWindow()
        {
            var window = GetWindow<TestsToolWindow>("TestsToolWindow");
            window.position = new Rect(100, 100, 1600, 700);
            window.Show();
        }

        public void CreateGUI()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath));

            _editModeContainer = rootVisualElement.Q<VisualElement>("edit-mode-container");
            _playModeContainer = rootVisualElement.Q<VisualElement>("play-mode-container");

            rootVisualElement.Q<Button>("find-btn").clicked           += RefreshTests;
            rootVisualElement.Q<Button>("add-ignore-btn").clicked     += AddIgnoreAttributeTests;
            rootVisualElement.Q<Button>("remove-ignore-btn").clicked  += RemoveIgnoreAttributeTests;
            rootVisualElement.Q<Button>("open-runner-btn").clicked    += OpenTestRunner;

            RefreshTests();
        }

        // ── Actions ───────────────────────────────────────────────────────────────

        private void RefreshTests()
        {
            _groupedEditMode.Clear();
            _groupedPlayMode.Clear();

            var api = new TestRunnerApi();
            api.RetrieveTestList(TestMode.EditMode, root =>
            {
                CollectTestsRecursive(root, TestPlatform.EditMode);
                RebuildContainer(_editModeContainer, _groupedEditMode);
            });
            api.RetrieveTestList(TestMode.PlayMode, root =>
            {
                CollectTestsRecursive(root, TestPlatform.PlayMode);
                RebuildContainer(_playModeContainer, _groupedPlayMode);
            });
        }

        private void AddIgnoreAttributeTests()
        {
            var tests = _groupedEditMode.Concat(_groupedPlayMode)
                .SelectMany(g => g.Tests)
                .Where(t => !t.Enabled && t.IsChanged);
            AddIgnoreAttributes(tests);
        }

        private void RemoveIgnoreAttributeTests()
        {
            var tests = _groupedEditMode.Concat(_groupedPlayMode)
                .SelectMany(g => g.Tests)
                .Where(t => t.Enabled && t.IsChanged);
            RemoveIgnoreAttributes(tests);
        }

        private void OpenTestRunner()
        {
            var type = Type.GetType("UnityEditor.TestTools.TestRunner.TestRunnerWindow,UnityEditor.TestRunner");
            if (type != null)
                GetWindow(type, false, "Test Runner");
            else
                Debug.LogWarning("Test Runner Window not found. Make sure 'Test Framework' package is installed.");
        }

        // ── UI rebuild ────────────────────────────────────────────────────────────

        private static void RebuildContainer(VisualElement container, List<GroupedTestGroup> groups)
        {
            if (container == null) return;
            container.Clear();
            foreach (var group in groups)
                container.Add(BuildAssemblyGroup(group));
        }

        private static VisualElement BuildAssemblyGroup(GroupedTestGroup group)
        {
            var foldout = new Foldout
            {
                text  = $"{group.AssemblyName}  ({group.Tests.Count})",
                value = false
            };
            foldout.AddToClassList("assembly-foldout");

            // header
            var header = new VisualElement();
            header.AddToClassList("table-header-row");
            header.Add(MakeHeaderLabel("Full Name", "col-fullname"));
            header.Add(MakeHeaderLabel("Enabled",   "col-enabled"));
            header.Add(MakeHeaderLabel("Changed",   "col-changed"));
            foldout.Add(header);

            foreach (var test in group.Tests)
                foldout.Add(BuildTestRow(test));

            return foldout;
        }

        private static VisualElement BuildTestRow(TestCaseConfig test)
        {
            var row = new VisualElement();
            row.AddToClassList("test-row");
            row.EnableInClassList("row-disabled", !test.Enabled);

            var nameField = new TextField { value = test.FullName, isReadOnly = true };
            nameField.AddToClassList("col-fullname");

            var enabledToggle = new Toggle { value = test.Enabled };
            enabledToggle.AddToClassList("col-enabled");

            var changedLabel = new Label("⚑ changed");
            changedLabel.AddToClassList("col-changed");
            changedLabel.style.display = test.IsChanged ? DisplayStyle.Flex : DisplayStyle.None;

            enabledToggle.RegisterValueChangedCallback(evt =>
            {
                test.Enabled = evt.newValue;
                row.EnableInClassList("row-disabled", !evt.newValue);
                changedLabel.style.display = test.IsChanged ? DisplayStyle.Flex : DisplayStyle.None;
            });

            row.Add(nameField);
            row.Add(enabledToggle);
            row.Add(changedLabel);
            return row;
        }

        private static Label MakeHeaderLabel(string text, string cssClass)
        {
            var l = new Label(text);
            l.AddToClassList("header-label");
            l.AddToClassList(cssClass);
            return l;
        }

        // ── Collect tests ─────────────────────────────────────────────────────────

        private void CollectTestsRecursive(ITestAdaptor adaptor, TestPlatform platform)
        {
            if (!adaptor.HasChildren && !string.IsNullOrEmpty(adaptor.FullName))
            {
                var config       = new TestCaseConfig(adaptor.FullName, adaptor.RunState != RunState.Ignored);
                var assemblyName = adaptor.TypeInfo?.Assembly?.GetName()?.Name ?? "Unknown";
                var groupList    = platform == TestPlatform.EditMode ? _groupedEditMode : _groupedPlayMode;

                var group = groupList.FirstOrDefault(g => g.AssemblyName == assemblyName);
                if (group == null)
                {
                    group = new GroupedTestGroup(assemblyName);
                    groupList.Add(group);
                }

                group.Tests.Add(config);
            }
            else
            {
                foreach (var child in adaptor.Children)
                    CollectTestsRecursive(child, platform);
            }
        }

        // ── Ignore attribute logic ────────────────────────────────────────────────

        private static void AddIgnoreAttributes(IEnumerable<TestCaseConfig> tests)
        {
            var testsByFile = GroupTestsByFile(tests);

            foreach (var kvp in testsByFile)
            {
                var lines    = File.ReadAllLines(kvp.Key).ToList();
                bool modified = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    foreach (var methodName in kvp.Value)
                    {
                        if (!lines[i].Contains($" {methodName}(")) continue;

                        int attrStart = FindAttrStart(lines, i);

                        bool alreadyIgnored = false;
                        for (int j = attrStart; j < i; j++)
                        {
                            if (!lines[j].Contains("Ignore(")) continue;
                            alreadyIgnored = true;
                            break;
                        }
                        if (alreadyIgnored) continue;

                        for (int j = attrStart; j < i; j++)
                        {
                            if (!(lines[j].Contains("[Test") || lines[j].Contains("[UnityTest"))) continue;
                            if (!lines[j].Contains("]")) continue;

                            lines[j] = lines[j].Replace("]", ", Ignore(\"Disabled via TestsToolWindow\")]");
                            modified = true;
                            Debug.Log($"[TestsTool] Added Ignore: {methodName}");
                            break;
                        }
                    }
                }

                if (modified) File.WriteAllLines(kvp.Key, lines);
            }

            AssetDatabase.Refresh();
        }

        private static void RemoveIgnoreAttributes(IEnumerable<TestCaseConfig> tests)
        {
            var testsByFile = GroupTestsByFile(tests);

            foreach (var kvp in testsByFile)
            {
                var lines     = File.ReadAllLines(kvp.Key).ToList();
                bool modified = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    foreach (var methodName in kvp.Value)
                    {
                        if (!lines[i].Contains($" {methodName}(")) continue;

                        int attrStart = FindAttrStart(lines, i);

                        for (int j = attrStart; j < i; j++)
                        {
                            if (!lines[j].Contains("Ignore(\"Disabled via TestsToolWindow\")")) continue;
                            lines[j]  = lines[j].Replace(", Ignore(\"Disabled via TestsToolWindow\")", "");
                            modified  = true;
                            Debug.Log($"[TestsTool] Removed Ignore: {methodName}");
                        }
                    }
                }

                if (modified) File.WriteAllLines(kvp.Key, lines);
            }

            AssetDatabase.Refresh();
        }

        private static Dictionary<string, List<string>> GroupTestsByFile(IEnumerable<TestCaseConfig> tests)
        {
            var result = new Dictionary<string, List<string>>();
            var allFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            foreach (var test in tests)
            {
                var methodName = test.FullName.Split('.').Last();
                foreach (var file in allFiles)
                {
                    if (!File.ReadAllText(file).Contains($" {methodName}(")) continue;
                    if (!result.ContainsKey(file))
                        result[file] = new List<string>();
                    result[file].Add(methodName);
                    break;
                }
            }

            return result;
        }

        private static int FindAttrStart(List<string> lines, int methodLine)
        {
            int i = methodLine - 1;
            while (i >= 0 && lines[i].Trim().StartsWith("["))
                i--;
            return i + 1;
        }
    }

    [Serializable]
    public class TestCaseConfig
    {
        public string FullName;
        public bool   IsChanged;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled  = value;
                IsChanged = true;
            }
        }

        private bool _enabled;

        public TestCaseConfig(string fullName, bool enabled)
        {
            FullName = fullName;
            _enabled = enabled;
        }
    }

    [Serializable]
    public class GroupedTestGroup
    {
        public string              AssemblyName;
        public List<TestCaseConfig> Tests = new();

        public GroupedTestGroup(string assemblyName) => AssemblyName = assemblyName;
    }
}
