using System;
using UnityEditor;
using UnityEngine;

namespace ToonyColorsPro
{
    namespace CustomShaderImporter
    {
        public class TCP2_ShaderPostProcessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                CleanTCP2Shaders(importedAssets);
            }

            static void CleanTCP2Shaders(string[] paths)
            {
                foreach (var assetPath in paths)
                {
                    if (!assetPath.EndsWith(TCP2_ShaderImporter.FILE_EXTENSION, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;
                    if (shader != null)
                    {
                        ShaderUtil.ClearShaderMessages(shader);
                        if (!ShaderUtil.ShaderHasError(shader))
                        {
                            ShaderUtil.RegisterShader(shader);
                        }
                    }
                }
            }
        }
    }
}