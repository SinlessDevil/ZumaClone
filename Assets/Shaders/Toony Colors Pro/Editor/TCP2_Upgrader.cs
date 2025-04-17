using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ToonyColorsPro.ShaderGenerator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonyColorsPro
{
    public class TCP2_Upgrader
    {
        #region Auto-upgrade from Hybrid Shader 1 to Hybrid Shader 2

        [MenuItem(Menu.MENU_PATH + "Upgrade Materials/From Hybrid Shader 1 to Hybrid Shader 2", false, 800)]
        static void MenuCheckForHybrid2Upgrade()
        {
            bool proceed = EditorUtility.DisplayDialog(UI_TitleMaterialUpgrade,
                "This will iterate over all the materials in your project, and upgrade those using the Hybrid Shader 1 to Hybrid Shader 2.\n\n" +
                "Please make a backup of your project or ensure you have no version control changes before proceeding.\n\n" +
                "Proceed?",
                "Yes", "No");

            if (proceed)
            {
                UpgradeMaterialsFromHybrid1ToHybrid2();
            }
        }

        const string Pref_DontCheckHybrid1ToHybrid2Session = "Pref_DontCheckHybrid1ToHybrid2Session";
        const string UI_TitleMaterialUpgrade = "Toony Colors Pro 2 Material Upgrade";

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            bool doCheck = !ProjectOptions.data.Upgrade_Hybrid1toHybrid2_Done;
            if (doCheck)
            {
                bool alreadyCheckedThisSession = SessionState.GetBool(Pref_DontCheckHybrid1ToHybrid2Session, false);
                if (!alreadyCheckedThisSession)
                {
                    EditorApplication.delayCall += () =>
                    {
                         CheckForUpgrade_Hybrid2();
                    };
                }
            }
        }

        static void CheckForUpgrade_Hybrid2()
        {
                string pathHybrid = AssetDatabase.GUIDToAssetPath("7ffc1bc2bbb1ef64f84b552b2d2f0619");
                bool hasShaderHybrid1 = !string.IsNullOrEmpty(pathHybrid);
                string pathHybridOutline = AssetDatabase.GUIDToAssetPath("ef3178aabaab247448c46c904e37bf43");
                bool hasShaderHybridOutline1 = !string.IsNullOrEmpty(pathHybridOutline);

                Shader hybridShader2 = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("edd7abf643fa4bc4e8561d4c280c97cf"));
                Shader hybridShader2Outline = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("df5bb027d94a6c44bb32b3c31ec1303f"));

                if (hybridShader2 == null || hybridShader2Outline == null)
                {
                    Debug.LogWarning(UI_TitleMaterialUpgrade + " failure: couldn't load 'Hybrid Shader 2' or 'Hybrid Shader 2 (Outline)'");
                    return;
                }

                if (!hasShaderHybrid1 || !hasShaderHybridOutline1)
                {
                    // Can't find either, assume Hybrid Shader 2 only exists
                    ProjectOptions.data.Upgrade_Hybrid1toHybrid2_Done = true;
                    ProjectOptions.SaveProjectOptions();
                    return;
                }

                // Has both/either, suggest migrating materials to new Hybrid 2 shader
                int answer = EditorUtility.DisplayDialogComplex(UI_TitleMaterialUpgrade,
                    "It looks like you may be using the old Hybrid Shader.\n" +
                    "Hybrid Shader 2 now exists and improves compatibility with the latest versions of URP (12+).\n\n" +
                    "Do you want to automatically scan all the materials in the project and migrate the old Hybrid Shader 1 to the new Hybrid Shader 2?\n\n(please make a backup of your project or ensure you have no version control changes before proceeding)",
                    "Yes", "No and don't ask again", "No");

                // Don't ask again:
                if (answer == 1)
                {
                    ProjectOptions.data.Upgrade_Hybrid1toHybrid2_Done = true;
                    ProjectOptions.SaveProjectOptions();
                    return;
                }

                // No: stop asking this session
                if (answer == 2)
                {
                    SessionState.SetBool(Pref_DontCheckHybrid1ToHybrid2Session, true);
                    return;
                }

                // Yes:
                if (answer == 0)
                {
                    UpgradeMaterialsFromHybrid1ToHybrid2();
                }
        }

        static void UpgradeMaterialsFromHybrid1ToHybrid2()
        {
            string pathHybrid = AssetDatabase.GUIDToAssetPath("7ffc1bc2bbb1ef64f84b552b2d2f0619");
            bool hasShaderHybrid1 = !string.IsNullOrEmpty(pathHybrid);
            string pathHybridOutline = AssetDatabase.GUIDToAssetPath("ef3178aabaab247448c46c904e37bf43");
            bool hasShaderHybridOutline1 = !string.IsNullOrEmpty(pathHybridOutline);

            Shader hybridShader2 = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("edd7abf643fa4bc4e8561d4c280c97cf"));
            Shader hybridShader2Outline = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("df5bb027d94a6c44bb32b3c31ec1303f"));

            Shader hybridShader = hasShaderHybrid1 ? AssetDatabase.LoadAssetAtPath<Shader>(pathHybrid) : null;
            Shader hybridShaderOutline = hasShaderHybridOutline1 ? AssetDatabase.LoadAssetAtPath<Shader>(pathHybridOutline) : null;

            // Iterate all project materials and change from Hybrid Shader 1 to Hybrid Shader 2:
            StringBuilder changedMaterials = new StringBuilder();
            int changedMaterialsCount = 0;
            try
            {
                IterateAllProjectMaterialsAndDo((mat, pathUnity) =>
                {
                    if (hasShaderHybrid1 && mat.shader == hybridShader)
                    {
                        mat.shader = hybridShader2;
                        EditorUtility.SetDirty(mat);

                        changedMaterials.AppendLine($"{mat.name}\t\t{pathUnity}");
                        changedMaterialsCount++;
                    }
                    else if (hasShaderHybridOutline1 && mat.shader == hybridShaderOutline)
                    {
                        mat.shader = hybridShader2Outline;
                        EditorUtility.SetDirty(mat);

                        changedMaterials.AppendLine($"{mat.name}\t\t{pathUnity}");
                        changedMaterialsCount++;
                    }
                });

                if (changedMaterialsCount > 0)
                {
                    Debug.Log(
                        $"<b>{UI_TitleMaterialUpgrade} Results</b>: {changedMaterialsCount} material{(changedMaterialsCount > 1 ? "s have" : " has")} been changed to Hybrid Shader 2:\n" +
                        $"<i>(open Editor Log through the Console menu to get full list if the message is truncated)</i>\n\n" +
                        $"{changedMaterials.ToString()}\n\n"
                    );
                }

                // Delete Hybrid Shader 1 files prompt:
                string messageHeader = changedMaterialsCount > 0 ? $"{changedMaterialsCount} material{(changedMaterialsCount > 1 ? "s have" : " has")} been changed to Hybrid Shader 2." : "No material has been found using the Hybrid Shader in your project (note: this is not an error or issue).";
                bool shouldDeleteHybrid1 = EditorUtility.DisplayDialog(UI_TitleMaterialUpgrade,
                    messageHeader + "\n\nIt is recommend to delete the old Hybrid Shader 1 from the project to avoid using it by mistake, in favor of the Hybrid Shader 2.\nShould I automatically delete it?",
                    "Yes (recommended)",
                    "No");
                if (shouldDeleteHybrid1)
                {
                    AssetDatabase.DeleteAsset(pathHybrid);
                    AssetDatabase.DeleteAsset(pathHybridOutline);

                    string pathHybridInclude = AssetDatabase.GUIDToAssetPath("77cc4099b4c0a5a4495b17ee94980292");
                    string pathHybridURPSupport = AssetDatabase.GUIDToAssetPath("5d83281ea07708940a9cf4fe0b981655");
                    if (!string.IsNullOrEmpty(pathHybridInclude)) AssetDatabase.DeleteAsset(pathHybridInclude);
                    if (!string.IsNullOrEmpty(pathHybridURPSupport)) AssetDatabase.DeleteAsset(pathHybridURPSupport);

                    string pathHybridFolder = AssetDatabase.GUIDToAssetPath("72d7c08a8dbc81840931a7ed1d13ec54");
                    if (!string.IsNullOrEmpty(pathHybridFolder)) AssetDatabase.DeleteAsset(pathHybridFolder);

                    string pathShaderPreprocessor = AssetDatabase.GUIDToAssetPath("014ac459d4371ad4dafb62708429613a");
                    if (!string.IsNullOrEmpty(pathShaderPreprocessor)) AssetDatabase.DeleteAsset(pathShaderPreprocessor);
                }

                // Migration done, don't automatically ask again
                ProjectOptions.data.Upgrade_Hybrid1toHybrid2_Done = true;
                ProjectOptions.SaveProjectOptions();
            }
            catch (Exception error)
            {
                Debug.LogError("Couldn't migrate materials from Hybrid Shader 1 to Hybrid Shader 2, error:\n" + error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion

        #region Upgrade from Legacy Desktop/Mobile to Hybrid Shader 2

        [MenuItem(Menu.MENU_PATH + "Upgrade Materials/From Legacy Desktop\\Mobile to Hybrid Shader 2", false, 801)]
        static void MenuUpgradeAllLegacyToHybrid2()
        {
            Shader hybridShader2 = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("edd7abf643fa4bc4e8561d4c280c97cf"));
            Shader hybridShader2Outline = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("df5bb027d94a6c44bb32b3c31ec1303f"));

            if (hybridShader2 == null || hybridShader2Outline == null)
            {
                Debug.LogWarning(UI_TitleMaterialUpgrade + " failure: couldn't load 'Hybrid Shader 2' or 'Hybrid Shader 2 (Outline)'");
                Debug.LogWarning("Couldn't load 'Hybrid Shader 2' or 'Hybrid Shader 2 Outline' shaders, do they exist in the project?");
                EditorApplication.Beep();
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(UI_TitleMaterialUpgrade,
                "This will iterate over all the materials in your project, and upgrade those using the Legacy/Desktop or Legacy/Mobile shaders to Hybrid Shader 2.\nProperties values will be migrated to the extend of what's possible, since shaders don't have full features parity.\n\n" +
                "Please make a backup of your project or ensure you have no version control changes before proceeding.\n\n" +
                "Proceed?",
                "Yes", "No");

            if (proceed)
            {
                int changedMaterialsCount = 0;
                StringBuilder changedMaterials = new StringBuilder();
                IterateAllProjectMaterialsAndDo((mat, pathUnity) =>
                {
                    if (UpgradeMaterialFromLegacyToHybrid2(mat, hybridShader2, hybridShader2Outline))
                    {
                        changedMaterialsCount++;
                        changedMaterials.AppendLine($"{mat.name}\t\t{pathUnity}");
                    }
                });

                if (changedMaterialsCount > 0)
                {
                    Debug.Log(
                        $"<b>{UI_TitleMaterialUpgrade} Results</b>: {changedMaterialsCount} material{(changedMaterialsCount > 1 ? "s" : "")} have been changed to Hybrid Shader 2:\n" +
                        $"<i>(open Editor Log through the Console menu to get full list if the message is truncated)</i>\n\n" +
                        $"{changedMaterials.ToString()}\n\n"
                    );
                }
            }
        }

        /*
        [MenuItem(Menu.MENU_PATH + "Upgrade Materials/Selected Materials/From Legacy Desktop\\Mobile to Hybrid Shader 2", false, 801)]
        static void MenuUpgradeSelectedLegacyToHybrid2()
        {
            foreach (var o in Selection.objects)
            {
                Material mat = o as Material;
                if (mat != null)
                {
                    UpgradeMaterialFromLegacyToHybrid2(mat);
                }
            }
        }
        */

        class Property_LegacyToHybrid2
        {
            readonly string nameLegacy;
            readonly string nameHybrid2;
            readonly ShaderPropertyType type;
            readonly Func<Material, object> specialMapping;

            object value;
            object value2;
            object value3;

            public Property_LegacyToHybrid2(string nameLegacy, string nameHybrid2, ShaderPropertyType type, Func<Material, object> specialMapping = null)
            {
                this.nameLegacy = nameLegacy;
                this.nameHybrid2 = nameHybrid2;
                this.type = type;
                this.specialMapping = specialMapping;

                value = null;
                value2 = null;
                value3 = null;
            }

            public void PreMigrateMaterial(Material mat)
            {
                if (!mat.HasProperty(nameLegacy))
                {
                    return;
                }

                if (specialMapping != null)
                {
                    value = specialMapping(mat);
                    return;
                }

                switch (type)
                {
                    case ShaderPropertyType.Color:
                        value = mat.GetColor(nameLegacy);
                        break;
                    case ShaderPropertyType.Vector:
                        value = mat.GetVector(nameLegacy);
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        value = mat.GetFloat(nameLegacy);
                        break;
                    case ShaderPropertyType.Texture:
                        value = mat.GetTexture(nameLegacy);
                        value2 = mat.GetTextureOffset(nameLegacy);
                        value3 = mat.GetTextureScale(nameLegacy);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public void PostMigrateMaterial(Material mat)
            {
                if (value == null)
                {
                    return;
                }

                switch (type)
                {
                    case ShaderPropertyType.Color:
                        mat.SetColor(nameHybrid2, (Color)this.value);
                        break;
                    case ShaderPropertyType.Vector:
                        mat.SetVector(nameHybrid2, (Vector4)this.value);
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        mat.SetFloat(nameHybrid2, (float)this.value);
                        break;
                    case ShaderPropertyType.Texture:
                        mat.SetTexture(nameHybrid2, (Texture)this.value);
                        mat.SetTextureOffset(nameHybrid2, (Vector2)this.value2);
                        mat.SetTextureScale(nameHybrid2, (Vector2)this.value3);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        class Keyword_LegacyToHybrid2
        {
            readonly string nameLegacy;
            readonly string nameHybrid2;
            readonly string propertyHybrid;

            bool shouldApplyKeyword;

            public Keyword_LegacyToHybrid2(string nameLegacy, string nameHybrid2, string propertyHybrid)
            {
                this.nameLegacy = nameLegacy;
                this.nameHybrid2 = nameHybrid2;
                this.propertyHybrid = propertyHybrid;
                shouldApplyKeyword = false;
            }

            public void PreMigrateMaterial(Material mat)
            {
                if (!mat.IsKeywordEnabled(nameLegacy))
                {
                    return;
                }

                shouldApplyKeyword = true;
                mat.DisableKeyword(nameLegacy);
            }

            public void PostMigrateMaterial(Material mat)
            {
                if (!shouldApplyKeyword) return;

                mat.EnableKeyword(nameHybrid2);
                if (propertyHybrid != null)
                {
                    mat.SetFloat(propertyHybrid, 1.0f);
                }
            }
        }

        static List<Property_LegacyToHybrid2> LegacyToHybrid2_PropertyMapping =>
            new List<Property_LegacyToHybrid2>()
            {
                new Property_LegacyToHybrid2("_Color", "_BaseColor", ShaderPropertyType.Color),
                new Property_LegacyToHybrid2("_MainTex", "_BaseMap", ShaderPropertyType.Texture),
                new Property_LegacyToHybrid2("_MatCap", "_MatCapTex", ShaderPropertyType.Texture),
                new Property_LegacyToHybrid2("_ReflectColor", "_ReflectionColor", ShaderPropertyType.Color, material =>
                {
                    Color reflectionColor = material.GetColor("_ReflectColor");
                    reflectionColor *= reflectionColor.a;
                    reflectionColor.a = 1;
                    return reflectionColor;
                }),
                new Property_LegacyToHybrid2("_RimColor", "_RimColor", ShaderPropertyType.Color, material =>
                {
                    Color rimColor = material.GetColor("_RimColor");
                    rimColor *= rimColor.a;
                    rimColor.a = 1;
                    return rimColor;
                }),
                new Property_LegacyToHybrid2("_ReflSmoothness", "_ReflectionSmoothness", ShaderPropertyType.Range),
                new Property_LegacyToHybrid2("_Shininess", "_SpecularToonSize", ShaderPropertyType.Range, material =>
                {
                    float shininess = material.GetFloat("_Shininess");
                    shininess = 1.1f / Mathf.Pow(shininess * 10, 0.9f) / 14f; // weird empirical formula... I'm not good at maths
                    return shininess;
                }),
                new Property_LegacyToHybrid2("_SpecSmooth", "_SpecularToonSmoothness", ShaderPropertyType.Range),
                new Property_LegacyToHybrid2("_SpecColor", "_SpecularColor", ShaderPropertyType.Color),
                new Property_LegacyToHybrid2("_TexLod", "_OutlineTextureLOD", ShaderPropertyType.Range),
                new Property_LegacyToHybrid2("_RampSmooth", "_RampSmoothing", ShaderPropertyType.Range, material =>
                {
                    float smoothing = material.GetFloat("_RampSmooth");
                    if (material.IsKeywordEnabled("TCP2_DISABLE_WRAPPED_LIGHT"))
                    {
                        smoothing /= 2.0f;
                    }

                    return smoothing;
                }),
                new Property_LegacyToHybrid2("_Outline", "_OutlineWidth", ShaderPropertyType.Range, material =>
                {
                    float outlineWidth = material.GetFloat("_Outline");
                    return outlineWidth * 2.0f;
                }),
                new Property_LegacyToHybrid2("_RampThreshold", "_RampThreshold", ShaderPropertyType.Range, material =>
                {
                    float threshold = material.GetFloat("_RampThreshold");
                    if (material.IsKeywordEnabled("TCP2_DISABLE_WRAPPED_LIGHT"))
                    {
                        threshold = threshold + ((1.0f - threshold) / 2.0f);
                    }

                    return threshold;
                }),
            };

        static List<Keyword_LegacyToHybrid2> LegacyToHybrid2_KeywordMapping =>
            new List<Keyword_LegacyToHybrid2>()
            {
                new Keyword_LegacyToHybrid2("TCP2_MC", "TCP2_MATCAP", "_UseMatCap"),
                new Keyword_LegacyToHybrid2("TCP2_MCMASK", "TCP2_MATCAP_MASK", "_UseMatCapMask"),
                new Keyword_LegacyToHybrid2("TCP2_BUMP", "_NORMALMAP", "_UseNormalMap"),
                new Keyword_LegacyToHybrid2("TCP2_OUTLINE_TEXTURED", "TCP2_OUTLINE_TEXTURED_VERTEX", "_OutlineTextureType"),
                new Keyword_LegacyToHybrid2("TCP2_REFLECTION", "TCP2_REFLECTIONS", "_UseReflections"),
                new Keyword_LegacyToHybrid2("TCP2_SPEC_TOON", "TCP2_SPECULAR_STYLIZED", "_SpecularType"),
            };

        static bool UpgradeMaterialFromLegacyToHybrid2(Material mat, Shader hybridShader2, Shader hybridShader2Outline)
        {
            var propertyMappings = LegacyToHybrid2_PropertyMapping;
            var keywordMappings = LegacyToHybrid2_KeywordMapping;

            if (mat.shader != null)
            {
                string name = mat.shader.name;
                if (name.Contains("Toony Colors Pro 2/Variants/Mobile")
                    || name.Contains("Toony Colors Pro 2/Variants/Desktop")
                    || name.Contains("Toony Colors Pro 2/Legacy/Mobile")
                    || name.Contains("Toony Colors Pro 2/Legacy/Desktop")
                    || name.Contains("Toony Colors Pro 2/Desktop")
                    || name.Contains("Toony Colors Pro 2/Mobile")
                )
                {
                    // Properties
                    foreach (var legacyToHybrid2 in propertyMappings)
                    {
                        legacyToHybrid2.PreMigrateMaterial(mat);
                    }
                    // Keywords
                    foreach (var legacyToHybrid2 in keywordMappings)
                    {
                        legacyToHybrid2.PreMigrateMaterial(mat);
                    }

                    bool isMobile = name.Contains("Mobile");
                    bool hasOutline = name.Contains("Outline") && !name.Contains("RimOutline");
                    bool hasSpecular = name.Contains("Specular");
                    bool hasReflection = name.Contains("Reflection");
                    bool hasRim = name.Contains("Rim") && !name.Contains("RimOutline");
                    bool hasMatcap = name.Contains("Matcap");
                    bool hasRampTexture = mat.IsKeywordEnabled("TCP2_RAMPTEXT");
                    bool constantSizeOutline = hasOutline && mat.IsKeywordEnabled("TCP2_OUTLINE_CONST_SIZE");

                    float outlineNormalsSource = 0;
                    float outlineNormalsDataType = 0;
                    if (hasOutline)
                    {
                        if (mat.IsKeywordEnabled("TCP2_COLORS_AS_NORMALS")) outlineNormalsSource = 1;
                        else if (mat.IsKeywordEnabled("TCP2_TANGENT_AS_NORMALS")) outlineNormalsSource = 2;
                        else if (mat.IsKeywordEnabled("TCP2_UV1_AS_NORMALS")) outlineNormalsSource = 3;
                        else if (mat.IsKeywordEnabled("TCP2_UV2_AS_NORMALS")) outlineNormalsSource = 4;
                        else if (mat.IsKeywordEnabled("TCP2_UV3_AS_NORMALS")) outlineNormalsSource = 5;
                        else if (mat.IsKeywordEnabled("TCP2_UV4_AS_NORMALS")) outlineNormalsSource = 6;

                        if (mat.IsKeywordEnabled("TCP2_UV_NORMALS_FULL")) outlineNormalsDataType = 0;
                        else if (mat.IsKeywordEnabled("TCP2_UV_NORMALS_ZW")) outlineNormalsDataType = 2;
                        else outlineNormalsDataType = 1;
                    }

                    // Apply Hybrid Shader 2
                    mat.shader = hasOutline ? hybridShader2Outline : hybridShader2;

                    // Properties
                    foreach (var legacyToHybrid2 in propertyMappings)
                    {
                        legacyToHybrid2.PostMigrateMaterial(mat);
                    }
                    if (hasOutline)
                    {
                        mat.SetFloat("_NormalsSource", outlineNormalsSource);
                        mat.SetFloat("_NormalsUVType", outlineNormalsDataType);

                        switch (outlineNormalsSource)
                        {
                            case 1: mat.EnableKeyword("TCP2_COLORS_AS_NORMALS"); break;
                            case 2: mat.EnableKeyword("TCP2_TANGENT_AS_NORMALS"); break;
                            case 3: mat.EnableKeyword("TCP2_UV1_AS_NORMALS"); break;
                            case 4: mat.EnableKeyword("TCP2_UV2_AS_NORMALS"); break;
                            case 5: mat.EnableKeyword("TCP2_UV3_AS_NORMALS"); break;
                            case 6: mat.EnableKeyword("TCP2_UV4_AS_NORMALS"); break;
                        }
                        switch (outlineNormalsDataType)
                        {
                            case 0: mat.EnableKeyword("TCP2_UV_NORMALS_FULL"); break;
                            case 2: mat.EnableKeyword("TCP2_UV_NORMALS_ZW"); break;
                        }
                    }
                    // Keywords
                    foreach (var legacyToHybrid2 in keywordMappings)
                    {
                        legacyToHybrid2.PostMigrateMaterial(mat);
                    }

                    // Apply keywords based on names/variants
                    if (constantSizeOutline)
                    {
                        mat.EnableKeyword("TCP2_OUTLINE_CONST_SIZE");
                        mat.SetFloat("_OutlinePixelSizeType", 1.0f);
                    }

                    if (isMobile)
                    {
                        mat.EnableKeyword("TCP2_MOBILE");
                        mat.SetFloat("_UseMobileMode", 1.0f);
                    }

                    if (hasSpecular)
                    {
                        mat.EnableKeyword("TCP2_SPECULAR");
                        mat.SetFloat("_UseSpecular", 1.0f);
                    }

                    if (hasReflection)
                    {
                        mat.EnableKeyword("TCP2_REFLECTIONS");
                        mat.SetFloat("_UseReflections", 1.0f);
                        mat.DisableKeyword("TCP2_REFLECTIONS_FRESNEL");
                        mat.SetFloat("_UseFresnelReflections", 0);
                    }

                    if (hasRim)
                    {
                        mat.EnableKeyword("TCP2_RIM_LIGHTING");
                        mat.SetFloat("_UseRim", 1.0f);
                        mat.DisableKeyword("TCP2_RIM_LIGHTING_LIGHTMASK");
                        mat.SetFloat("_UseRimLightMask", 0);
                    }

                    if (hasMatcap)
                    {
                        mat.EnableKeyword("TCP2_MATCAP");
                        mat.SetFloat("_UseMatCap", 1.0f);
                    }

                    if (hasRampTexture)
                    {
                        mat.EnableKeyword("TCP2_RAMPTEXT");
                        mat.SetFloat("_RampType", 4);
                    }

                    return true;
                }
            }

            return false;
        }

        #endregion

        static void IterateAllProjectMaterialsAndDo(Action<Material, string> action)
        {
            try
            {
                string[] materialPathsOS = Directory.GetFiles(Application.dataPath, "*.mat", SearchOption.AllDirectories);
                for (int i = 0, l = materialPathsOS.Length; i < l; i++)
                {
                    EditorUtility.DisplayProgressBar(UI_TitleMaterialUpgrade, $"Checking material {i}/{materialPathsOS.Length}", i / (float) l);
                    string pathOS = materialPathsOS[i];
                    string pathUnity = pathOS.Replace(Application.dataPath, "Assets");
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(pathUnity);
                    if (mat == null)
                    {
                        Debug.LogWarning($"Couldn't load material at path: '{pathUnity}'");
                        continue;
                    }

                    action(mat, pathUnity);
                }
            }
            catch (Exception error)
            {
                Debug.LogError("Couldn't iterate the project's materials, error:\n" + error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Resources.UnloadUnusedAssets();
            }
        }
    }
}