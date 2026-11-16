using System;
using System.Collections.Generic;
using Code.StaticData.Levels;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Infrastructure.Editor
{
    internal sealed class LevelEditorWindow : EditorWindow
    {
        private const string ThemePreviews = "Assets/ResourcesStatic/Packages/Maps/Previews";
        private const string ViewPath = "Assets/Code/Infrastructure/Editor/LevelEditor/LevelEditorWindow.uxml";
        private LevelEditorModel _model;
        private ScrollView _chapters;
        private VisualElement _workspace;
        private VisualElement _levelPicker;
        private Button _create;
        private Button _delete;
        private Button _createChapter;

        [MenuItem("Tools/Level Editor")]
        public static void Open()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(820f, 450f);
        }

        public void CreateGUI()
        {
            _model = new LevelEditorModel();
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewPath).CloneTree(rootVisualElement);
            _chapters = rootVisualElement.Q<ScrollView>("chapterList");
            _workspace = rootVisualElement.Q("workspace");
            _levelPicker = rootVisualElement.Q("levelPicker");
            _create = rootVisualElement.Q<Button>("createButton");
            _delete = rootVisualElement.Q<Button>("deleteButton");
            _createChapter = rootVisualElement.Q<Button>("createChapterButton");
            _create.clicked += ShowCreateLevel;
            _delete.clicked += DeleteSelected;
            _createChapter.clicked += ShowCreateChapter;
            Refresh();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void Refresh()
        {
            _chapters.Clear();
            foreach (ChapterStaticData chapter in _model.Chapters)
            {
                Button button = new Button(() =>
                {
                    _model.OpenChapter(chapter);
                    Refresh();
                })
                {
                    tooltip = $"Chapter {chapter.ChapterId}"
                };
                button.Add(new Label(chapter.Theme != null ? chapter.Theme.DisplayName : $"Chapter {chapter.ChapterId}")
                    { pickingMode = PickingMode.Ignore });
                Label details = new Label($"{chapter.ChapterId:00}   ·   {chapter.Levels.Count} levels")
                    { pickingMode = PickingMode.Ignore };
                details.AddToClassList("chapter-detail");
                button.Add(details);
                button.AddToClassList("chapter-button");
                button.EnableInClassList("selected-chapter", chapter == _model.Chapter);
                _chapters.Add(button);
            }

            _workspace.Clear();
            bool canEdit = !EditorApplication.isPlayingOrWillChangePlaymode;
            BuildLevelPicker();
            _create.SetEnabled(canEdit && _model.Chapters.Count > 0);
            _createChapter.SetEnabled(canEdit);
            _delete.SetEnabled(canEdit && _model.CanDelete);

            if (_model.Level != null)
                BuildLevelEditor(_model.Level, canEdit);
            else
                _workspace.Add(new Label("This chapter is empty. Use Create to add a level."));
        }

        private void BuildLevelPicker()
        {
            _levelPicker.Clear();
            List<LevelStaticData> levels = _model.Chapter?.Levels;
            if (levels == null || levels.Count == 0)
            {
                Button empty = new Button { text = "No levels" };
                empty.SetEnabled(false);
                _levelPicker.Add(empty);
                return;
            }

            PopupField<LevelStaticData> picker = new PopupField<LevelStaticData>(levels, _model.Level,
                FormatLevel, FormatLevel) { name = "levelList", tooltip = "Select a level" };
            picker.RegisterValueChangedCallback(change =>
            {
                _model.OpenLevel(change.newValue);
                Refresh();
            });
            _levelPicker.Add(picker);
        }

        private static string FormatLevel(LevelStaticData level) => $"#{level.LevelId:00}   {level.LevelName}";

        private void BuildLevelEditor(LevelStaticData level, bool canEdit)
        {
            VisualElement metadata = new VisualElement();
            metadata.AddToClassList("metadata");
            TextField title = new TextField("Name") { value = level.LevelName, name = "levelName" };
            title.AddToClassList("level-name");
            IntegerField id = new IntegerField("ID") { value = level.LevelId, name = "levelId" };
            id.AddToClassList("level-id");
            PopupField<ChapterStaticData> chapter = MakeChapterField(_model.Chapters, _model.Chapter);
            chapter.AddToClassList("chapter-field");
            Label feedback = new Label();
            feedback.AddToClassList("warning");
            Button apply = new Button(() =>
            {
                if ((id.value != level.LevelId || chapter.value != _model.Chapter) &&
                    !EditorUtility.DisplayDialog("Change level ID or chapter?",
                        "Existing saves link to the original chapter and level ID. This change retires that pair.",
                        "Apply", "Cancel"))
                    return;

                _model.EditLevel(title.value, id.value, chapter.value);
                Refresh();
            })
            {
                text = "Apply",
                name = "applyLevel"
            };

            void UpdateMetadata()
            {
                string error = LevelEditorModel.Validate(title.value, id.value, chapter.value, level);
                feedback.text = error ?? "";
                feedback.style.display = error == null ? DisplayStyle.None : DisplayStyle.Flex;
                apply.SetEnabled(error == null &&
                    (title.value.Trim() != level.LevelName || id.value != level.LevelId || chapter.value != _model.Chapter));
            }

            title.RegisterValueChangedCallback(_ => UpdateMetadata());
            id.RegisterValueChangedCallback(_ => UpdateMetadata());
            chapter.RegisterValueChangedCallback(_ => UpdateMetadata());
            UpdateMetadata();
            metadata.Add(title);
            metadata.Add(id);
            metadata.Add(chapter);
            metadata.Add(apply);
            metadata.SetEnabled(canEdit);
            _workspace.Add(metadata);
            _workspace.Add(feedback);

            VisualElement mapWorkspace = new VisualElement();
            mapWorkspace.AddToClassList("map-workspace");
            VisualElement panel = new VisualElement();
            panel.AddToClassList("map-panel");
            LevelThemeStaticData theme = _model.Chapter.Theme;
            LevelCanvas canvas = new LevelCanvas(level, theme);
            VisualElement controls = new VisualElement();
            controls.AddToClassList("canvas-toolbar");
            controls.Add(new Button(canvas.Fit) { text = "Fit map" });
            Toggle addPoint = new Toggle("Add point")
            {
                name = "addPointToggle",
                tooltip = "Click the road to insert a point. Turn this off to drag points. Shift-click is a shortcut."
            };
            addPoint.RegisterValueChangedCallback(change => canvas.SetAddPointMode(change.newValue));
            controls.Add(addPoint);
            controls.Add(new Button(canvas.DeleteSelectedPoint) { text = "Delete point" });
            panel.Add(controls);
            panel.Add(canvas);
            Label hint = new Label("Add point or Shift-click the road · Drag handles to move · Scroll to zoom · Middle-drag to pan");
            hint.AddToClassList("map-hint");
            panel.Add(hint);

            ScrollView surfaces = new ScrollView();
            surfaces.AddToClassList("surface-sidebar");
            BuildThemePreview(surfaces, theme);
            Label surfaceTitle = new Label("Surface overrides");
            surfaceTitle.AddToClassList("section-title");
            surfaces.Add(surfaceTitle);
            surfaces.Add(MakeTextureField(level, "Ground", "fieldTexture", level.Geometry.FieldTexture,
                theme != null ? theme.Ground?.mainTexture : null,
                texture =>
                {
                    level.Geometry.FieldTexture = texture;
                    canvas.RefreshTexture();
                }));
            surfaces.Add(MakeTextureField(level, "Track", "roadTexture", level.Geometry.RoadTexture,
                theme != null ? theme.Track?.mainTexture : null, texture => level.Geometry.RoadTexture = texture));

            mapWorkspace.Add(panel);
            mapWorkspace.Add(surfaces);
            mapWorkspace.SetEnabled(canEdit);
            _workspace.Add(mapWorkspace);
        }

        private static void BuildThemePreview(VisualElement surfaces, LevelThemeStaticData theme)
        {
            if (theme == null)
                return;

            Label title = new Label(theme.DisplayName);
            title.AddToClassList("section-title");
            surfaces.Add(title);
            string path = $"{ThemePreviews}/{theme.name}_Board.png";
            Texture2D board = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (board != null)
            {
                Image preview = new Image { image = board, scaleMode = ScaleMode.ScaleToFit };
                preview.AddToClassList("theme-preview");
                surfaces.Add(preview);
            }
        }

        private static VisualElement MakeTextureField(LevelStaticData level, string title, string name, Texture2D texture,
            Texture fallback, Action<Texture2D> update)
        {
            VisualElement row = new VisualElement();
            ObjectField field = new ObjectField(title)
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false,
                value = texture,
                tooltip = "Leave empty to use the chapter's surface.",
                name = name
            };
            field.AddToClassList("texture-field");

            Image preview = new Image { image = texture != null ? texture : fallback, scaleMode = ScaleMode.ScaleToFit };
            preview.AddToClassList("texture-preview");
            field.RegisterValueChangedCallback(change =>
            {
                Undo.RecordObject(level, "Change " + title.ToLowerInvariant() + " texture");
                Texture2D chosen = (Texture2D)change.newValue;
                update(chosen);
                preview.image = chosen != null ? chosen : fallback;
                LevelEditorModel.Save(level);
            });
            row.Add(field);
            row.Add(preview);
            return row;
        }

        private void ShowCreateLevel() => LevelCreationPopup.Open(_model, false, Refresh);

        private void ShowCreateChapter() => LevelCreationPopup.Open(_model, true, Refresh);

        private void DeleteSelected()
        {
            if (!_model.CanDelete || !EditorUtility.DisplayDialog("Delete level?",
                $"Delete #{_model.Level.LevelId} {_model.Level.LevelName} from Chapter {_model.Chapter.ChapterId}?",
                "Delete", "Cancel"))
                return;

            try
            {
                _model.DeleteLevel();
                Refresh();
            }
            catch (InvalidOperationException error)
            {
                EditorUtility.DisplayDialog("Could not delete level", error.Message, "OK");
            }
        }

        private void OnUndoRedo()
        {
            if (_model == null || _workspace == null)
                return;

            LevelStaticData changed = _model.Level;
            _model.Reload();
            if (changed != null && EditorUtility.IsDirty(changed))
                AssetDatabase.SaveAssetIfDirty(changed);
            foreach (ChapterStaticData chapter in _model.Chapters)
                if (EditorUtility.IsDirty(chapter))
                    AssetDatabase.SaveAssetIfDirty(chapter);

            Refresh();
        }

        private void OnProjectChange()
        {
            if (_model == null || _workspace == null)
                return;

            _model.Reload();
            Refresh();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (_model != null && _workspace != null)
                Refresh();
        }

        internal static PopupField<ChapterStaticData> MakeChapterField(List<ChapterStaticData> chapters,
            ChapterStaticData active)
        {
            return new PopupField<ChapterStaticData>("Chapter", chapters, active,
                chapter => chapter == null ? "—" : chapter.ChapterId.ToString(),
                chapter => $"Chapter {chapter.ChapterId}");
        }
    }
}
