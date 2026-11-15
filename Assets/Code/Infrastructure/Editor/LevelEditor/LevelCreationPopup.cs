using System;
using Code.StaticData.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Infrastructure.Editor
{
    internal sealed class LevelCreationPopup : EditorWindow
    {
        private const string StylesPath = "Assets/Code/Infrastructure/Editor/LevelEditor/LevelEditorWindow.uss";
        private LevelEditorModel _model;
        private Action _onCreate;
        private bool _createChapter;

        public static void Open(LevelEditorModel model, bool createChapter, Action onCreate)
        {
            LevelCreationPopup popup = CreateInstance<LevelCreationPopup>();
            popup._model = model;
            popup._onCreate = onCreate;
            popup._createChapter = createChapter;
            popup.titleContent = new GUIContent(createChapter ? "Create Chapter" : "Create Level");
            popup.minSize = new Vector2(340f, 155f);
            popup.maxSize = new Vector2(550f, 220f);
            popup.ShowUtility();
        }

        public void CreateGUI()
        {
            if (_model == null)
            {
                Close();
                return;
            }

            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesPath));
            VisualElement form = new VisualElement();
            form.AddToClassList("creation-form");
            rootVisualElement.Add(form);

            Label feedback = new Label();
            feedback.AddToClassList("warning");
            if (_createChapter)
                AddChapterForm(form, feedback);
            else
                AddLevelForm(form, feedback);
            form.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                    Close();
            });
        }

        private void AddLevelForm(VisualElement form, Label feedback)
        {
            TextField title = new TextField("Name") { name = "newLevelName" };
            IntegerField id = new IntegerField("Level ID")
            {
                value = LevelEditorModel.SuggestLevelId(_model.Chapter),
                name = "newLevelId"
            };
            PopupField<ChapterStaticData> chapter = LevelEditorWindow.MakeChapterField(_model.Chapters, _model.Chapter);
            Button create = new Button(() => Finish(() => _model.CreateLevel(title.value, id.value, chapter.value), feedback))
            {
                text = "Create",
                name = "confirmCreate"
            };

            void Validate()
            {
                string error = LevelEditorModel.Validate(title.value, id.value, chapter.value);
                feedback.text = error ?? "";
                create.SetEnabled(error == null);
            }

            title.RegisterValueChangedCallback(_ => Validate());
            id.RegisterValueChangedCallback(_ => Validate());
            chapter.RegisterValueChangedCallback(change =>
            {
                id.value = LevelEditorModel.SuggestLevelId(change.newValue);
                Validate();
            });
            Validate();

            form.Add(title);
            form.Add(id);
            form.Add(chapter);
            form.Add(feedback);
            form.Add(create);
            title.schedule.Execute(title.Focus);
        }

        private void AddChapterForm(VisualElement form, Label feedback)
        {
            IntegerField id = new IntegerField("Chapter ID") { value = _model.SuggestChapterId(), name = "newChapterId" };
            IntegerField temple = new IntegerField("Temple ID") { value = 1 };
            form.Add(id);
            form.Add(temple);
            form.Add(feedback);
            form.Add(new Button(() => Finish(() => _model.CreateChapter(id.value, temple.value), feedback))
            {
                text = "Create",
                name = "confirmCreate"
            });
        }

        private void Finish(Action create, Label feedback)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                feedback.text = "Exit Play Mode to make changes.";
                return;
            }

            try
            {
                create();
                _onCreate();
                Close();
            }
            catch (InvalidOperationException error)
            {
                feedback.text = error.Message;
            }
        }
    }
}
