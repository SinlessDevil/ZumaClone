using Code.StaticData;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Infrastructure.Editor
{
    [CustomEditor(typeof(GameplayInputStaticData))]
    public sealed class GameplayInputStaticDataEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            SerializedProperty area = serializedObject.FindProperty(nameof(GameplayInputStaticData.GameplayInputArea));
            if (area == null)
            {
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }


            root.Add(new PropertyField(area));
            root.Add(CreateInputAreaPreview(area));
            return root;
        }

        private static VisualElement CreateInputAreaPreview(SerializedProperty property)
        {
            VisualElement preview = new();
            preview.Add(new Label("16:9 preview · drag the area or its lower-right corner"));

            VisualElement screen = new() { name = "gameplay-input-screen" };
            screen.style.marginTop = 4f;
            screen.style.marginBottom = 8f;
            screen.style.backgroundColor = new StyleColor(new Color32(34, 38, 45, 255));
            screen.style.overflow = Overflow.Hidden;

            VisualElement zone = new() { name = "gameplay-input-area" };
            zone.style.position = Position.Absolute;
            zone.style.backgroundColor = new StyleColor(new Color32(42, 115, 138, 255));
            screen.Add(zone);

            VisualElement resizeHandle = new() { name = "gameplay-input-resize", tooltip = "Resize gameplay input area" };
            resizeHandle.style.position = Position.Absolute;
            resizeHandle.style.right = 0f;
            resizeHandle.style.bottom = 0f;
            resizeHandle.style.width = 12f;
            resizeHandle.style.height = 12f;
            resizeHandle.style.backgroundColor = new StyleColor(new Color32(137, 216, 226, 255));
            zone.Add(resizeHandle);
            preview.Add(screen);

            void Draw(Rect value)
            {
                Rect rect = GameplayInputStaticData.ClampGameplayInputArea(value);
                zone.style.left = Length.Percent(rect.xMin * 100f);
                zone.style.top = Length.Percent((1f - rect.yMax) * 100f);
                zone.style.width = Length.Percent(rect.width * 100f);
                zone.style.height = Length.Percent(rect.height * 100f);
            }

            Draw(property.rectValue);
            screen.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (!Mathf.Approximately(evt.newRect.width, evt.oldRect.width))
                    screen.style.height = evt.newRect.width * 9f / 16f;
            });
            screen.TrackPropertyValue(property, changed => Draw(changed.rectValue));

            int? pointer = null;
            int undoGroup = 0;
            bool resizing = false;
            Vector2 start = default;
            Rect initial = default;

            screen.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || pointer.HasValue || (evt.target != zone && evt.target != resizeHandle))
                    return;

                pointer = evt.pointerId;
                resizing = evt.target == resizeHandle;
                start = screen.WorldToLocal((Vector2)evt.position);
                initial = property.rectValue;
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Edit Gameplay Input Area");
                screen.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            screen.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pointerId != pointer || screen.contentRect.width <= 0f || screen.contentRect.height <= 0f)
                    return;

                Vector2 delta = screen.WorldToLocal((Vector2)evt.position) - start;
                delta = new Vector2(delta.x / screen.contentRect.width, -delta.y / screen.contentRect.height);
                Rect moved = initial;
                if (resizing)
                {
                    moved.xMax = Mathf.Clamp(initial.xMax + delta.x, initial.xMin, 1f);
                    moved.yMin = Mathf.Clamp(initial.yMin + delta.y, 0f, initial.yMax);
                }
                else
                {
                    moved.position += delta;
                }

                moved = GameplayInputStaticData.ClampGameplayInputArea(moved);
                property.serializedObject.UpdateIfRequiredOrScript();
                property.rectValue = moved;
                property.serializedObject.ApplyModifiedProperties();
                Draw(moved);
                evt.StopPropagation();
            });

            screen.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId == pointer)
                    screen.ReleasePointer(evt.pointerId);
            });

            screen.RegisterCallback<PointerCaptureOutEvent>(evt =>
            {
                if (evt.pointerId != pointer)
                    return;

                Undo.CollapseUndoOperations(undoGroup);
                pointer = null;
            });

            return preview;
        }
    }
}
