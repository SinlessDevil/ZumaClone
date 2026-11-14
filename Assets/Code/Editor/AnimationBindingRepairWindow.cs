using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Editor
{
    public sealed class AnimationBindingRepairWindow : EditorWindow
    {
        private const string UndoName = "Repair Animation Binding Paths";
        private const string RootPathLabel = "(Animator root)";

        [SerializeField] private Animator _animator;
        private ScrollView _results;
        private HelpBox _message;

        [MenuItem("Tools/Animation/Repair Binding Paths")]
        private static void Open()
        {
            AnimationBindingRepairWindow window = GetWindow<AnimationBindingRepairWindow>("Repair Animation Bindings");
            Animator selectedAnimator = Selection.activeGameObject?.GetComponentInParent<Animator>(true);
            if (selectedAnimator != null)
            {
                window._animator = selectedAnimator;
            }

            window.Rebuild();
        }

        public void CreateGUI() => Rebuild();

        private void OnEnable() => Undo.undoRedoPerformed += RefreshAfterUndo;

        private void OnDisable() => Undo.undoRedoPerformed -= RefreshAfterUndo;

        private void RefreshAfterUndo()
        {
            if (_results != null)
            {
                Scan();
            }
        }

        private void Rebuild()
        {
            rootVisualElement.Clear();

            ObjectField animatorField = new ObjectField("Animator")
            {
                objectType = typeof(Animator),
                value = _animator
            };
            animatorField.RegisterValueChangedCallback(evt =>
            {
                _animator = (Animator)evt.newValue;
                Scan();
            });
            rootVisualElement.Add(animatorField);
            rootVisualElement.Add(new Button(Scan) { text = "Rescan" });
            rootVisualElement.Add(new HelpBox(
                "Choose a new target for each missing path. Changes affect all uses of the listed clips.",
                HelpBoxMessageType.Info));

            _message = new HelpBox();
            _results = new ScrollView();
            rootVisualElement.Add(_message);
            rootVisualElement.Add(_results);
            Scan();
        }

        private void Scan()
        {
            _results.Clear();
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                _message.text = "Assign an Animator with a controller.";
                _message.messageType = HelpBoxMessageType.Info;
                return;
            }

            Transform root = _animator.transform;
            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            string[] missingPaths = clips.SelectMany(GetBindings).Select(binding => binding.path)
                .Where(path => path.Length != 0 && root.Find(path) == null)
                .Distinct().OrderBy(path => path, StringComparer.Ordinal).ToArray();

            _message.text = missingPaths.Length == 0
                ? "All animation paths resolve on this Animator."
                : $"Missing paths: {missingPaths.Length}";
            _message.messageType = missingPaths.Length == 0 ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            if (missingPaths.Length == 0)
            {
                return;
            }

            List<string> targets = root.GetComponentsInChildren<Transform>(true)
                .Select(target => AnimationUtility.CalculateTransformPath(target, root))
                .GroupBy(path => path).Where(group => group.Count() == 1)
                .Select(group => group.Key).Where(path => path.Length != 0 && root.Find(path) != null)
                .OrderBy(path => path, StringComparer.Ordinal).ToList();
            targets.Insert(0, RootPathLabel);

            foreach (string oldPath in missingPaths)
            {
                string clipNames = string.Join(", ", clips.Where(clip => GetBindings(clip).Any(binding => binding.path == oldPath))
                    .Select(clip => clip.name));
                DropdownField targetField = new DropdownField("New target", new List<string>(targets), -1);
                Button applyButton = new Button(() => Apply(oldPath, targetField.index == 0 ? "" : targetField.value))
                {
                    text = "Repair Path"
                };
                applyButton.SetEnabled(false);
                targetField.RegisterValueChangedCallback(_ => applyButton.SetEnabled(targetField.index >= 0));
                Box row = new Box();
                row.Add(new Label(oldPath));
                row.Add(new Label(clipNames));
                row.Add(targetField);
                row.Add(applyButton);
                _results.Add(row);
            }
        }

        private void Apply(string oldPath, string newPath)
        {
            try
            {
                int count = RepairPath(_animator, oldPath, newPath);
                Scan();
                _message.text = $"Updated {count} clip(s): {oldPath} → {(newPath.Length == 0 ? RootPathLabel : newPath)}";
                _message.messageType = HelpBoxMessageType.Info;
            }
            catch (InvalidOperationException exception)
            {
                _message.text = exception.Message;
                _message.messageType = HelpBoxMessageType.Error;
            }
        }

        internal static int RepairPath(Animator animator, string oldPath, string newPath)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(oldPath)
                || animator.transform.Find(oldPath) != null || !ResolvesUniquely(animator.transform, newPath))
            {
                throw new InvalidOperationException("The hierarchy changed. Rescan and choose a valid replacement.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips.Distinct()
                .Where(clip => GetBindings(clip).Any(binding => binding.path == oldPath)).ToArray();
            if (clips.Length == 0)
            {
                throw new InvalidOperationException("The animation bindings changed. Rescan the Animator.");
            }

            foreach (AnimationClip clip in clips)
            {
                string path = AssetDatabase.GetAssetPath(clip);
                if (!AssetDatabase.IsMainAsset(clip) || !path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)
                    || !AssetDatabase.IsOpenForEdit(clip))
                {
                    throw new InvalidOperationException($"{clip.name} is read-only or imported. Use an editable .anim clip.");
                }

                EditorCurveBinding[] bindings = GetBindings(clip).ToArray();
                foreach (EditorCurveBinding binding in bindings.Where(binding => binding.path == oldPath))
                {
                    EditorCurveBinding replacement = WithPath(binding, newPath);
                    if (bindings.Any(existing => existing.Equals(replacement)))
                    {
                        throw new InvalidOperationException($"{clip.name} already animates {replacement.propertyName} at the new path.");
                    }

                    if (AnimationUtility.GetAnimatedObject(animator.gameObject, replacement) == null)
                    {
                        throw new InvalidOperationException($"The target is missing the {binding.type.Name} for {binding.propertyName}.");
                    }
                }
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            Undo.RegisterCompleteObjectUndo(clips, UndoName);
            try
            {
                foreach (AnimationClip clip in clips)
                {
                    MoveCurves(clip, AnimationUtility.GetCurveBindings(clip), oldPath, newPath,
                        AnimationUtility.GetEditorCurve, AnimationUtility.SetEditorCurves);
                    MoveCurves(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip), oldPath, newPath,
                        AnimationUtility.GetObjectReferenceCurve, AnimationUtility.SetObjectReferenceCurves);
                    EditorUtility.SetDirty(clip);
                }
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }

            foreach (AnimationClip clip in clips)
            {
                AssetDatabase.SaveAssetIfDirty(clip);
            }

            return clips.Length;
        }

        private static IEnumerable<EditorCurveBinding> GetBindings(AnimationClip clip) =>
            AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip));

        private static bool ResolvesUniquely(Transform root, string path) =>
            path.Length == 0 || root.Find(path) != null && root.GetComponentsInChildren<Transform>(true)
                .Count(target => AnimationUtility.CalculateTransformPath(target, root) == path) == 1;

        private static EditorCurveBinding WithPath(EditorCurveBinding binding, string path)
        {
            binding.path = path;
            return binding;
        }

        private static void MoveCurves<T>(
            AnimationClip clip, EditorCurveBinding[] bindings, string oldPath, string newPath,
            Func<AnimationClip, EditorCurveBinding, T> getCurve, Action<AnimationClip, EditorCurveBinding[], T[]> setCurves)
            where T : class
        {
            EditorCurveBinding[] originals = bindings.Where(binding => binding.path == oldPath).ToArray();
            if (originals.Length == 0)
            {
                return;
            }

            T[] values = originals.Select(binding => getCurve(clip, binding)).ToArray();
            EditorCurveBinding[] replacements = originals.Select(binding => WithPath(binding, newPath)).ToArray();
            setCurves(clip, replacements.Concat(originals).ToArray(), values.Concat(new T[originals.Length]).ToArray());
        }
    }
}
