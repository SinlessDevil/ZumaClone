// #define SHOW_EXPORT_BUTTON
// #define SHOW_TOGGLED_OPTIONS

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ToonyColorsPro.ShaderGenerator;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine.Rendering;

namespace ToonyColorsPro
{
    namespace CustomShaderImporter
    {
        [ScriptedImporter(0, FILE_EXTENSION)]
        public class TCP2_ShaderImporter : ScriptedImporter
        {
            public const string FILE_EXTENSION = "tcp2shader";

            public string detectedRenderPipeline = "Built-In Render Pipeline";
            public int strippedLinesCount = 0;
            public string shaderSourceCode;
            public string shaderName;
            public string[] shaderErrors;
            public ulong variantCount;
            public ulong variantCountUsed;

            [SerializeField] internal List<ShaderOption> availableOptions;
            [SerializeField] internal List<string> toggledOptions = new List<string>();

            internal enum ShaderOptionCategory
            {
                Unity,
                TCP2
            }
            [Serializable]
            internal class ShaderOption
            {
                [SerializeField] internal GUIContent label;
                [SerializeField] internal ShaderOptionCategory category;
                [SerializeField] internal bool isOffOption;

                internal string GetCategoryLabel()
                {
                    switch (category)
                    {
                        case ShaderOptionCategory.Unity:
                            return "Unity features";
                        case ShaderOptionCategory.TCP2:
                            return "Toony Colors Pro 2 features";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            enum ComparisonOperator
            {
                Equal,
                Greater,
                GreaterOrEqual,
                Less,
                LessOrEqual
            }

#if UNITY_2022_2_OR_NEWER
            const int URP_VERSION = 14;
#elif UNITY_2021_2_OR_NEWER
            const int URP_VERSION = 12;
#elif UNITY_2021_1_OR_NEWER
            const int URP_VERSION = 11;
#elif UNITY_2020_3_OR_NEWER
            const int URP_VERSION = 10;
#else
            const int URP_VERSION = 7;
#endif

            static ComparisonOperator ParseComparisonOperator(string symbols)
            {
                switch (symbols)
                {
                    case "==": return ComparisonOperator.Equal;
                    case "<=": return ComparisonOperator.LessOrEqual;
                    case "<": return ComparisonOperator.Less;
                    case ">": return ComparisonOperator.Greater;
                    case ">=": return ComparisonOperator.GreaterOrEqual;
                    default: throw new Exception("Invalid comparison operator: " + symbols);
                }
            }

            static bool CompareWithOperator(int value1, int value2, ComparisonOperator comparisonOperator)
            {
                switch (comparisonOperator)
                {
                    case ComparisonOperator.Equal: return value1 == value2;
                    case ComparisonOperator.Greater: return value1 > value2;
                    case ComparisonOperator.GreaterOrEqual: return value1 >= value2;
                    case ComparisonOperator.Less: return value1 < value2;
                    case ComparisonOperator.LessOrEqual: return value1 <= value2;
                    default: throw new Exception("Invalid comparison operator value: " + comparisonOperator);
                }
            }

            bool StartsOrEndWithSpecialTag(string line)
            {
                bool startsWithTag = (line.Length > 4 && line[0] == '/' && line[1] == '*' && line[2] == '*' && line[3] == '*');
                if (startsWithTag) return true;

                int l = line.Length-1;
                bool endsWithTag = (line.Length > 4 && line[l] == '/' && line[l-1] == '*' && line[l-2] == '*' && line[l-3] == '*');
                return endsWithTag;
            }

            ShaderOption ParseOptionFromLine(string line)
            {
                string singleLineOptionText = null;
                return ParseOptionFromLine(line, ref singleLineOptionText);
            }

            ShaderOption ParseOptionFromLine(string line, ref string singleLineOptionText)
            {
                int tagStart = line.IndexOf("/***", StringComparison.Ordinal);
                int firstQuote = line.IndexOf('"') + 1;
                int lastQuote = line.LastIndexOf('"');

                singleLineOptionText = line.Substring(0, tagStart).TrimEnd();

                string expression = line.Substring(firstQuote, lastQuote - firstQuote);
                string tooltip = null;
                if (expression.Contains("|"))
                {
                    var split = expression.Split('|');
                    expression = split[0].Trim('"');
                    tooltip = split[1].Trim('"');
                }

                var category = ShaderOptionCategory.Unity;
                if (expression.StartsWith("TCP2"))
                {
                    category = ShaderOptionCategory.TCP2;
                    expression = expression.Replace("TCP2 ", "");
                }

                return new ShaderOption()
                {
                    label = new GUIContent(expression, tooltip),
                    category = category,
                    isOffOption = line.Contains("/*** OPTION_OFF")
                };
            }

            public override void OnImportAsset(AssetImportContext context)
            {
                bool isUsingURP = Utils.IsUsingURP();

                detectedRenderPipeline = isUsingURP ? "Universal Render Pipeline" : "Built-In Render Pipeline";

                StringWriter shaderSource = new StringWriter();
                string[] sourceLines = File.ReadAllLines(context.assetPath);
                Stack<bool> excludeCurrentLines = new Stack<bool>();
                strippedLinesCount = 0;

                // Prepass
                // - gather all available options
                availableOptions = new List<ShaderOption>();
                for (int i = 0; i < sourceLines.Length; i++)
                {
                    string line = sourceLines[i];
                    if (StartsOrEndWithSpecialTag(line))
                    {
                        if (line.StartsWith("/*** OPTION") || (line.EndsWith("***/") && line.Contains("/*** OPTION")))
                        {
                            var option = ParseOptionFromLine(line);
                            if (!availableOptions.Exists(o => o.label.text == option.label.text))
                            {
                                availableOptions.Add(option);
                            }
                        }
                    }
                }
                availableOptions.Sort((item1, item2) => { return String.Compare( item1.category + item1.label.text, item2.category + item2.label.text, StringComparison.Ordinal); });

                for (int i = 0; i < sourceLines.Length; i++)
                {
                    bool excludeThisLine = excludeCurrentLines.Count > 0 && excludeCurrentLines.Peek();

                    string line = sourceLines[i];
                    if (StartsOrEndWithSpecialTag(line))
                    {
                        if (line.StartsWith("/*** BIRP ***/"))
                        {
                            excludeCurrentLines.Push(excludeThisLine || isUsingURP);
                        }
                        else if (line.StartsWith("/*** URP ***/"))
                        {
                            excludeCurrentLines.Push(excludeThisLine || !isUsingURP);
                        }
                        else if (line.StartsWith("/*** URP_VERSION "))
                        {
                            string subline = line.Substring("/*** URP_VERSION ".Length);
                            int spaceIndex = subline.IndexOf(' ');
                            string version = subline.Substring(spaceIndex, subline.LastIndexOf(' ') - spaceIndex);
                            string op = subline.Substring(0, spaceIndex);

                            var compOp = ParseComparisonOperator(op);
                            int compVersion = int.Parse(version);

                            bool isCorrectURP = CompareWithOperator(URP_VERSION, compVersion, compOp);
                            excludeCurrentLines.Push(excludeThisLine || !isCorrectURP);
                        }
                        else if (line.StartsWith("/*** OPTION"))
                        {
                            var option = ParseOptionFromLine(line);
                            bool exclude = toggledOptions.Contains(option.label.text);
                            if (line.StartsWith("/*** OPTION_OFF:"))
                            {
                                exclude = !exclude;
                            }
                            excludeCurrentLines.Push(excludeThisLine || exclude);
                        }
                        else if (line.EndsWith("***/") && line.Contains("/*** OPTION"))
                        {
                            // OPTION modifier on single line
                            string singleLine = null;
                            var option = ParseOptionFromLine(line, ref singleLine);
                            bool exclude = toggledOptions.Contains(option.label.text);
                            if (line.Contains("/*** OPTION_OFF:"))
                            {
                                exclude = !exclude;
                            }

                            if (!excludeThisLine && !exclude)
                            {
                                shaderSource.WriteLine(singleLine);
                            }
                            else
                            {
                                strippedLinesCount++;
                            }
                        }
                        else if (excludeThisLine && line.StartsWith("/*** END"))
                        {
                            excludeCurrentLines.Pop();
                        }
                        else if (!excludeThisLine && line.StartsWith("/*** #define URP_VERSION ***/"))
                        {
                            shaderSource.WriteLine("\t\t\t#define URP_VERSION " + URP_VERSION);
                        }
                    }
                    else
                    {
                        if (excludeThisLine)
                        {
                            strippedLinesCount++;
                            continue;
                        }

                        shaderSource.WriteLine(line);
                    }
                }

                // Get source code and extract name
                shaderSourceCode = shaderSource.ToString();
                int idx = shaderSourceCode.IndexOf("Shader \"", StringComparison.InvariantCulture) + 8;
                int idx2 = shaderSourceCode.IndexOf('"', idx);
                shaderName = shaderSourceCode.Substring(idx, idx2 - idx);
                shaderErrors = null;

                var shader = ShaderUtil.CreateShaderAsset(context, shaderSourceCode, true);

                if (ShaderUtil.ShaderHasError(shader))
                {
                    string[] shaderSourceLines = shaderSourceCode.Split(new char[] {'\n'}, StringSplitOptions.None);
                    var errors = ShaderUtil.GetShaderMessages(shader);
                    shaderErrors = System.Array.ConvertAll(errors, err => string.Format("{0} (line {1})", err.message, err.line));
                    foreach (ShaderMessage error in errors)
                    {
                        string message = error.line <= 0 ?
                            string.Format("Shader Error in '{0}' (in file '{2}')\nError: {1}\n", shaderName, error.message, error.file) :
                            string.Format("Shader Error in '{0}' (line {2} in file '{3}')\nError: {1}\nLine: {4}\n", shaderName, error.message, error.line, error.file, shaderSourceLines[error.line-1]);
                        if (error.severity == ShaderCompilerMessageSeverity.Warning)
                        {
                            Debug.LogWarning(message);
                        }
                        else
                        {
                            Debug.LogError(message);
                        }
                    }
                }
                else
                {
                    ShaderUtil.ClearShaderMessages(shader);
                }

                context.AddObjectToAsset("MainAsset", shader);
                context.SetMainObject(shader);

                // Try to count variant using reflection:
                // internal static extern ulong GetVariantCount(Shader s, bool usedBySceneOnly);
                variantCount = 0;
                variantCountUsed = 0;
                var getVariantCountReflection = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic);
                if (getVariantCountReflection != null)
                {
                    try
                    {
                        object result = getVariantCountReflection.Invoke(null, new object[] {shader, false});
                        variantCount = (ulong)result;
                        result = getVariantCountReflection.Invoke(null, new object[] {shader, true});
                        variantCountUsed = (ulong)result;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        namespace Inspector
        {
            [CustomEditor(typeof(TCP2_ShaderImporter)), CanEditMultipleObjects]
            public class TCP2ShaderImporter_Editor : Editor
            {
                TCP2_ShaderImporter Importer { get { return (TCP2_ShaderImporter) this.target; } }

                List<string> tempDisabledOptions = new List<string>();
                bool hasModifications;

                void CheckModifications()
                {
                    hasModifications = tempDisabledOptions.Count != Importer.toggledOptions.Count || tempDisabledOptions.Except(Importer.toggledOptions).Any();
                }

                void OnEnable()
                {
                    tempDisabledOptions = new List<string>(Importer.toggledOptions);
                }

                // From: UnityEditor.ShaderInspectorPlatformsPopup
                static string FormatCount(ulong count)
                {
                    bool flag = count > 1000000000uL;
                    string result;
                    if (flag)
                    {
                        result = (count / 1000000000.0).ToString("f2", CultureInfo.InvariantCulture.NumberFormat) + "B";
                    }
                    else
                    {
                        bool flag2 = count > 1000000uL;
                        if (flag2)
                        {
                            result = (count / 1000000.0).ToString("f2", CultureInfo.InvariantCulture.NumberFormat) + "M";
                        }
                        else
                        {
                            bool flag3 = count > 1000uL;
                            if (flag3)
                            {
                                result = (count / 1000.0).ToString("f2", CultureInfo.InvariantCulture.NumberFormat) + "k";
                            }
                            else
                            {
                                result = count.ToString();
                            }
                        }
                    }
                    return result;
                }

                public override void OnInspectorGUI()
                {
                    bool isUsingURP = Utils.IsUsingURP();
                    serializedObject.Update();

                    GUILayout.Label(TCP2_GUI.TempContent(Importer.shaderName));
                    string variantsText = "";
                    if (Importer.variantCount > 0 && Importer.variantCountUsed > 0)
                    {
                        variantsText = string.Format("\nVariants (currently used): <b>{0}</b>\nVariants (including unused): <b>{1}</b>", FormatCount(Importer.variantCountUsed), FormatCount(Importer.variantCount));
                    }
                    GUILayout.Label(TCP2_GUI.TempContent(string.Format("Detected render pipeline: <b>{0}</b>\nStripped lines: <b>{1}</b>{2}",
                        Importer.detectedRenderPipeline,
                        Importer.strippedLinesCount,
                        variantsText)
                    ), TCP2_GUI.HelpBoxRichTextStyle);

                    if (Importer.shaderErrors != null && Importer.shaderErrors.Length > 0)
                    {
                        GUILayout.Space(4);
                        var color = GUI.color;
                        GUI.color = new Color32(0xFF, 0x80, 0x80, 0xFF);
                        GUILayout.Label(TCP2_GUI.TempContent(string.Format("<b>Errors:</b>\n{0}", string.Join("\n", Importer.shaderErrors))), TCP2_GUI.HelpBoxRichTextStyle);
                        GUI.color = color;
                    }

                    bool compiledForURP = Importer.detectedRenderPipeline.Contains("Universal");
                    if ((isUsingURP && !compiledForURP) || (!isUsingURP && compiledForURP))
                    {
                        GUILayout.Space(4);
                        Color guiColor = GUI.color;
                        GUI.color *= Color.yellow;
                        EditorGUILayout.HelpBox("The detected render pipeline doesn't match the pipeline this shader was compiled for!\nPlease reimport the shaders for them to work in the current render pipeline.", MessageType.Warning);
                        if (GUILayout.Button("Reimport Shader"))
                        {
                            ReimportShader();
                        }
                        GUI.color = guiColor;
                    }

                    GUILayout.Space(4);

                    if (GUILayout.Button(TCP2_GUI.TempContent("View Source"), GUILayout.ExpandWidth(false)))
                    {
                        string path = Application.temporaryCachePath + "/" + Importer.shaderName.Replace("/", "-") + "_Source.shader";
                        if (File.Exists(path))
                        {
                            File.SetAttributes(path, FileAttributes.Normal);
                        }

                        File.WriteAllText(path, Importer.shaderSourceCode);
                        File.SetAttributes(path, FileAttributes.ReadOnly);
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 0);
                    }

#if SHOW_EXPORT_BUTTON
                    GUILayout.Space(8);

                    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(importer.shaderSourceCode));
                    {
                        if (GUILayout.Button("Export .shader file", GUILayout.ExpandWidth(false)))
                        {
                            string savePath = EditorUtility.SaveFilePanel("Export TCP2 shader", Application.dataPath, "Hybrid Shader","shader");
                            if (!string.IsNullOrEmpty(savePath))
                            {
                                File.WriteAllText(savePath, importer.shaderSourceCode);
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();
#endif

                    if (Importer.availableOptions.Count > 0)
                    {
                        GUILayout.Space(4);
                        TCP2_GUI.SeparatorSimple();
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            GUILayout.Label(TCP2_GUI.TempContent("Options:"));
                            GUILayout.Label(TCP2_GUI.TempContent("Disable options you know you won't use to reduce the number of shader variants and improve compilation times, built file size and runtime memory usage."), SGUILayout.Styles.GrayMiniLabelWrap);
                            GUILayout.Space(4);
                            int lastCategory = -1;
                            foreach (var option in Importer.availableOptions)
                            {
                                if (option.label.text.Contains("URP") && !isUsingURP)
                                {
                                    continue;
                                }

                                if ((int) option.category != lastCategory)
                                {
                                    lastCategory = (int)option.category;
                                    GUILayout.Label( TCP2_GUI.TempContent(option.GetCategoryLabel()), SGUILayout.Styles.OrangeBoldLabel);
                                }

                                bool disabled = tempDisabledOptions.Contains(option.label.text);
                                bool change;
                                if (option.isOffOption)
                                {
                                    change = GUILayout.Toggle(disabled, option.label, GUILayout.ExpandWidth(false));
                                }
                                else
                                {
                                    change = !GUILayout.Toggle(!disabled, option.label, GUILayout.ExpandWidth(false));
                                }

                                if (change != disabled)
                                {
                                    if (change)
                                    {
                                        tempDisabledOptions.Add(option.label.text);
                                        CheckModifications();
                                    }
                                    else
                                    {
                                        tempDisabledOptions.Remove(option.label.text);
                                        CheckModifications();
                                    }
                                }
                            }

                            EditorGUI.BeginDisabledGroup(!hasModifications);
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Revert"))
                                    {
                                        OnEnable();
                                        CheckModifications();
                                    }

                                    if (GUILayout.Button("Apply"))
                                    {
                                        var disabledOptionsProperty = serializedObject.FindProperty(nameof(TCP2_ShaderImporter.toggledOptions));
                                        disabledOptionsProperty.ClearArray();
                                        disabledOptionsProperty.arraySize = tempDisabledOptions.Count;
                                        for (int i = 0; i < tempDisabledOptions.Count; i++)
                                        {
                                            disabledOptionsProperty.GetArrayElementAtIndex(i).stringValue = tempDisabledOptions[i];
                                        }
                                        serializedObject.ApplyModifiedProperties();
                                        ReimportShader();
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndVertical();
                    }

#if SHOW_TOGGLED_OPTIONS
                    GUILayout.Label("Toggled options:", EditorStyles.boldLabel);
                    foreach (var option in Importer.toggledOptions)
                    {
                        GUILayout.Label(option);
                    }
#endif

                    serializedObject.ApplyModifiedProperties();
                }

                void ReimportShader()
                {
                    foreach (UnityEngine.Object t in targets)
                    {
                        string path = AssetDatabase.GetAssetPath(t);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    }
                    OnEnable();
                    CheckModifications();
                }
            }
        }
    }
}