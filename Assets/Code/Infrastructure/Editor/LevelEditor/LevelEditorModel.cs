using System;
using System.Collections.Generic;
using System.Linq;
using Code.PathCreation;
using Code.Services.Factories;
using Code.StaticData.Levels;
using UnityEditor;
using UnityEngine;

namespace Code.Infrastructure.Editor
{
    internal sealed class LevelEditorModel
    {
        private const string ChaptersFolder = "Assets/Resources/" + ResourcePath.ChaptersStaticDataPath;
        private const string LevelsFolder = "Assets/Resources/" + ResourcePath.LevelsStaticDataPath;

        public List<ChapterStaticData> Chapters { get; private set; }
        public ChapterStaticData Chapter { get; private set; }
        public LevelStaticData Level { get; private set; }

        public LevelEditorModel()
        {
            Reload();
        }

        public void Reload()
        {
            Chapters = Resources.LoadAll<ChapterStaticData>(ResourcePath.ChaptersStaticDataPath)
                .OrderBy(chapter => chapter.ChapterId).ToList();

            if (Level != null)
            {
                ChapterStaticData owner = Chapters.Find(chapter => chapter.Levels.Contains(Level));
                if (owner != null)
                {
                    Chapter = owner;
                    return;
                }
            }

            ChapterStaticData firstPlayable = Chapters.Find(chapter => chapter.Levels.Count > 0);
            OpenChapter(Chapters.Contains(Chapter) ? Chapter : firstPlayable ?? Chapters.FirstOrDefault());
        }

        public void OpenChapter(ChapterStaticData chapter)
        {
            Chapter = chapter;
            Level = chapter != null ? chapter.Levels.FirstOrDefault() : null;
        }

        public void OpenLevel(LevelStaticData level) => Level = level;

        public static int SuggestLevelId(ChapterStaticData chapter) => chapter == null ? 1 :
            chapter.Levels.Select(level => level.LevelId).Concat(chapter.RetiredLevelIds).DefaultIfEmpty().Max() + 1;

        public int SuggestChapterId() => Chapters.Select(chapter => chapter.ChapterId).DefaultIfEmpty().Max() + 1;

        public static string Validate(string name, int id, ChapterStaticData chapter, LevelStaticData current = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Enter a level name.";
            if (chapter == null)
                return "Select a chapter.";
            if (id < 1)
                return "The level ID must be positive.";
            if (chapter.Levels.Exists(level => level != current && level.LevelId == id))
                return "This ID is already in use in this chapter.";
            if (chapter.RetiredLevelIds.Contains(id) && (current == null || !chapter.Levels.Contains(current) ||
                current.LevelId != id))
                return "This ID belongs to a removed level and is reserved for old saves.";
            return null;
        }

        public ChapterStaticData CreateChapter(int id, int temple)
        {
            if (id < 1 || temple < 1 || Chapters.Exists(chapter => chapter.ChapterId == id))
                throw new InvalidOperationException("Enter a unique positive chapter ID and a positive temple ID.");

            ChapterStaticData chapter = ScriptableObject.CreateInstance<ChapterStaticData>();
            chapter.ChapterId = id;
            chapter.TempleId = temple;
            AssetDatabase.CreateAsset(chapter, AssetDatabase.GenerateUniqueAssetPath(ChaptersFolder + $"/Chapter_{id}.asset"));
            Reload();
            OpenChapter(chapter);
            return chapter;
        }

        public LevelStaticData CreateLevel(string name, int id, ChapterStaticData chapter)
        {
            EnsureValid(name, id, chapter, null);
            LevelStaticData source = chapter.Levels.FirstOrDefault() ??
                Chapters.SelectMany(item => item.Levels).FirstOrDefault();
            if (source == null)
                throw new InvalidOperationException("A level with score and random settings is required as a default.");

            LevelStaticData level = ScriptableObject.CreateInstance<LevelStaticData>();
            level.LevelName = name.Trim();
            level.LevelId = id;
            level.LevelTypeId = source.LevelTypeId;
            level.LevelConfig = new LevelConfig
            {
                CountItem = source.LevelConfig.CountItem,
                ScoreConfig = source.LevelConfig.ScoreConfig,
                RandomConfig = source.LevelConfig.RandomConfig
            };
            level.Geometry.Path = new BezierPath(new[]
            {
                new Vector3(-10f, 0f, -4f),
                new Vector3(8f, 0f, -4f),
                new Vector3(9f, 0f, 4f),
                new Vector3(-6f, 0f, 4f),
                new Vector3(-7f, 0f, 0.5f)
            }, false, PathSpace.xz);
            if (chapter.Theme == null)
            {
                level.Geometry.FieldTexture = source.Geometry.FieldTexture;
                level.Geometry.RoadTexture = source.Geometry.RoadTexture;
            }
            string path = AssetDatabase.GenerateUniqueAssetPath(LevelsFolder + $"/Level_{chapter.ChapterId}_{id}.asset");
            AssetDatabase.CreateAsset(level, path);
            chapter.Levels.Add(level);
            Save(chapter);
            Chapter = chapter;
            Level = level;
            return level;
        }

        public void EditLevel(string name, int id, ChapterStaticData chapter)
        {
            EnsureValid(name, id, chapter, Level);
            ChapterStaticData previous = Chapter;
            bool move = chapter != previous;
            bool changeId = Level.LevelId != id;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Edit level details");
            Undo.RecordObject(Level, "Edit level details");

            if (move || changeId)
            {
                Undo.RecordObject(previous, "Edit level details");
                previous.RetiredLevelIds.Add(Level.LevelId);
            }

            if (move)
            {
                Undo.RecordObject(chapter, "Edit level details");
                previous.Levels.Remove(Level);
                chapter.Levels.Add(Level);
            }

            Level.LevelId = id;
            Level.LevelName = name.Trim();
            Chapter = chapter;
            Save(Level);
            Save(previous);
            if (move)
                Save(chapter);
        }

        public bool CanDelete => Level != null && Chapters.Sum(chapter => chapter.Levels.Count) > 1;

        public void DeleteLevel()
        {
            if (!CanDelete)
                throw new InvalidOperationException("Keep at least one playable level in the game.");

            LevelStaticData level = Level;
            int index = Chapter.Levels.IndexOf(level);
            Chapter.Levels.RemoveAt(index);
            Chapter.RetiredLevelIds.Add(level.LevelId);
            if (!AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(level)))
            {
                Chapter.Levels.Insert(index, level);
                Chapter.RetiredLevelIds.Remove(level.LevelId);
                throw new InvalidOperationException("Unity could not delete the level asset.");
            }

            Save(Chapter);
            Level = Chapter.Levels.Count > 0 ? Chapter.Levels[Mathf.Min(index, Chapter.Levels.Count - 1)] : null;
            if (Level == null)
                OpenChapter(Chapters.Find(chapter => chapter.Levels.Count > 0));
        }

        public static void Save(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void EnsureValid(string name, int id, ChapterStaticData chapter, LevelStaticData level)
        {
            string error = Validate(name, id, chapter, level);
            if (error != null)
                throw new InvalidOperationException(error);
        }
    }
}
