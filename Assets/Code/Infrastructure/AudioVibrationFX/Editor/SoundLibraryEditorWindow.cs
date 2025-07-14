using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Infrastructure.AudioVibrationFX.Services.Music;
using Code.Infrastructure.AudioVibrationFX.Services.Sound;
using Code.Infrastructure.AudioVibrationFX.StaticData;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Code.Infrastructure.AudioVibrationFX.Editor
{
    public class SoundLibraryEditorWindow : OdinEditorWindow
    {
        private const string SoundPath = "Assets/Code/Infrastructure/AudioVibrationFX/Services/Sound/";
        private const string MusicPath = "Assets/Code/Infrastructure/AudioVibrationFX/Services/Music/";
        
        private const string NameSpaceSound = "namespace Code.Infrastructure.AudioVibrationFX.Services.Sound";
        private const string NameSpaceMusic = "namespace Code.Infrastructure.AudioVibrationFX.Services.Music";
        
        [MenuItem("Tools/AudioVibrationKit/Sound Library")]
        private static void OpenWindow()
        {
            GetWindow<SoundLibraryEditorWindow>().Show();
        }

        private SoundsData _soundsData;
        
        [BoxGroup("Existing Enums"), ReadOnly]
        [MultiLineProperty(4)]
        [SerializeField]
        private string _sound2DTypes;
        
        [BoxGroup("Existing Enums"), ReadOnly]
        [MultiLineProperty(4)]
        [SerializeField]
        private string _sound3DTypes;

        [BoxGroup("Existing Enums"), ReadOnly]
        [MultiLineProperty(4)]
        [SerializeField]
        private string _musicTypes;
        
        [BoxGroup("2D Sounds")]
        [ShowInInspector, Searchable]
        [ListDrawerSettings(
            Expanded = true,
            DraggableItems = true,
            ShowPaging = true,
            ListElementLabelName = "Name"
        )]
        private List<SoundData> _sounds2DDataEditable;

        [BoxGroup("3D Sounds")]
        [ShowInInspector, Searchable]
        [ListDrawerSettings(
            Expanded = true,
            DraggableItems = true,
            ShowPaging = true,
            ListElementLabelName = "Name"
        )]
        private List<Sound3DData> _sounds3DDataEditable;

        [BoxGroup("Music")]
        [ShowInInspector, Searchable]
        [ListDrawerSettings(
            Expanded = true,
            DraggableItems = true,
            ShowPaging = true,
            ListElementLabelName = "Name"
        )]
        private List<SoundData> _musicDataEditable;

        private string _namespaceCodeInfrastructureAudiovibrationfxServicesSound;

        [BoxGroup("Generation")]
        [Button("Generate Enums", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        private void GenerateEnums()
        {
            GenerateSound2DEnumFile("Sound2DType.cs", "Sound2DType", _sounds2DDataEditable);
            GenerateSound3DEnumFile("Sound3DType.cs", "Sound3DType", _sounds3DDataEditable);
            GenerateMusicEnumFile("MusicType.cs", "MusicType", _musicDataEditable);
            SaveSoundsData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private void GenerateSound2DEnumFile(string fileName, string enumName, List<SoundData> soundList)
        {
            var enumPath = $"{SoundPath}{fileName}";
            GenerateEnumFileBase(enumPath, enumName, NameSpaceSound, soundList, TypeSound.Sound2D);
        }

        private void GenerateSound3DEnumFile(string fileName, string enumName, List<Sound3DData> soundList)
        {
            var enumPath = $"{SoundPath}{fileName}";
            List<SoundData> baseList = soundList.Cast<SoundData>().ToList();
            GenerateEnumFileBase(enumPath, enumName, NameSpaceSound ,baseList, TypeSound.Sound3D);
        }
        
        private void GenerateMusicEnumFile(string fileName, string enumName, List<SoundData> soundList)
        {
            var enumPath = $"{MusicPath}{fileName}";
            GenerateEnumFileBase(enumPath, enumName, NameSpaceMusic, soundList, TypeSound.Music);
        }
        
        private void GenerateEnumFileBase(string enumPath, string enumName, string nameSpace ,List<SoundData> soundList, TypeSound typeSound)
        {
            var names = soundList
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Trim())
                .Distinct()
                .ToList();

            using (var writer = new StreamWriter(enumPath))
            {
                writer.WriteLine("using System;");
                writer.WriteLine();
                writer.WriteLine(nameSpace);
                writer.WriteLine("{");
                writer.WriteLine("    [Serializable]");
                writer.WriteLine($"    public enum {enumName}");
                writer.WriteLine("    {");
                writer.WriteLine("        Unknown = -1,");

                for (int i = 0; i < names.Count; i++)
                {
                    writer.WriteLine($"        {names[i]} = {i},");
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }
            
            foreach (var sound in soundList)
            {
                var enumNameSanitized = sound.Name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Trim();

                switch (typeSound)
                {
                    case TypeSound.Sound2D when Enum.TryParse(enumNameSanitized, out Sound2DType sound2DType):
                        sound.Sound2DType = sound2DType;
                        break;
                    case TypeSound.Sound3D when Enum.TryParse(enumNameSanitized, out Sound3DType sound3DType):
                        sound.Sound3DType = sound3DType;
                        break;
                    case TypeSound.Music when Enum.TryParse(enumNameSanitized, out MusicType musicType):
                        sound.MusicType = musicType;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typeSound), typeSound, null);
                }
            }

            EditorUtility.SetDirty(_soundsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{enumName} enum generated and assigned successfully!");
        }
        
        public void UpdateSoundTypesAfterReload()
        {
            UpdateSoundTypes();
            SaveSoundsData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();

            var loaded = Resources.Load<SoundsData>("StaticData/Sounds/Sounds");

            if (loaded != null)
            {
                _soundsData = loaded;
                _sounds2DDataEditable = _soundsData.Sounds2DData;
                _sounds3DDataEditable = _soundsData.Sounds3DData;
                _musicDataEditable = _soundsData.MusicData;
            }
            else
            {
                Debug.LogError("SoundsData asset not found at Resources/StaticData/Sounds/Sounds.asset");
                _sounds2DDataEditable = new List<SoundData>();
                _sounds3DDataEditable = new List<Sound3DData>();
                _musicDataEditable = new List<SoundData>();
            }

            _sound2DTypes = GetEnumValues(typeof(Sound2DType));
            _sound3DTypes = GetEnumValues(typeof(Sound3DType));
            _musicTypes = GetEnumValues(typeof(MusicType));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SaveSoundsData();
        }
        
        private void SaveSoundsData()
        {
            if (_soundsData != null)
                EditorUtility.SetDirty(_soundsData);
        }

        private void UpdateSoundTypes()
        {
            foreach (var sound in _sounds2DDataEditable)
                sound.Sound2DType = Enum.TryParse(sound.Name, out Sound2DType parsedType) ? parsedType : Sound2DType.Unknown;

            foreach (var sound in _sounds3DDataEditable)
                sound.Sound3DType = Enum.TryParse(sound.Name, out Sound3DType parsedType) ? parsedType : Sound3DType.Unknown;

            foreach (var music in _musicDataEditable)
                music.MusicType = Enum.TryParse(music.Name, out MusicType parsedType) ? parsedType : MusicType.Unknown;
            
            EditorUtility.SetDirty(_soundsData);
            AssetDatabase.SaveAssets();
        }
        
        private string GetEnumValues(Type enumType)
        {
            return string.Join(", ", Enum.GetNames(enumType));
        }
        
        private enum TypeSound
        {
            Sound2D,
            Sound3D,
            Music
        }
    }
}
