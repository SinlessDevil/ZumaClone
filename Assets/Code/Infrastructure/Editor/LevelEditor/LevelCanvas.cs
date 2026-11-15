using Code.PathCreation;
using Code.StaticData.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Infrastructure.Editor
{
    internal sealed class LevelCanvas : VisualElement
    {
        private const int PlayerHandle = -2;
        private const int NoHandle = -1;
        private const float HitRadius = 12f;
        private static readonly Color GridColor = new Color32(205, 225, 228, 31);
        private static readonly Color MarkerBackground = new Color32(21, 28, 32, 255);
        private static readonly Color RoadColor = new Color32(200, 170, 113, 235);
        private static readonly Color AnchorColor = new Color32(255, 222, 160, 255);
        private static readonly Color ControlColor = new Color32(173, 197, 204, 255);
        private static readonly Color StartColor = new Color32(105, 204, 152, 255);
        private static readonly Color EndColor = new Color32(243, 140, 115, 255);
        private static readonly Color PlayerColor = new Color32(115, 187, 244, 255);

        private readonly LevelStaticData _level;
        private readonly LevelThemeStaticData _theme;
        private float _zoom = 1f;
        private Vector2 _pan;
        private int _selected = NoHandle;
        private int _pointer = -1;
        private bool _panning;
        private bool _edited;
        private Vector2 _pointerStart;
        private Vector2 _panStart;
        private Vector3 _pointStart;
        private int _undoGroup;

        private LevelGeometry Geometry => _level.Geometry;
        private BezierPath Path => Geometry.Path;
        private float Scale => Mathf.Max(1f, Mathf.Min(contentRect.width / 29f, contentRect.height / 16f)) * _zoom;
        private Vector2 Origin => contentRect.center + _pan;

        public LevelCanvas(LevelStaticData level, LevelThemeStaticData theme)
        {
            _level = level;
            _theme = theme;
            name = "levelCanvas";
            focusable = true;
            AddToClassList("map-canvas");
            RefreshTexture();
            generateVisualContent += Draw;
            this.AddManipulator(new MapPointerManipulator(this));
            RegisterCallback<WheelEvent>(OnZoom);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void Fit()
        {
            Bounds bounds = Path.PathBounds;
            bounds.Encapsulate(Geometry.PlayerPosition);
            float baseScale = Scale / _zoom;
            _zoom = Mathf.Clamp(Mathf.Min(contentRect.width / (bounds.size.x + 4f),
                contentRect.height / (bounds.size.z + 4f)) / baseScale, 0.2f, 5f);
            _pan = -new Vector2(bounds.center.x, -bounds.center.z) * Scale;
            MarkDirtyRepaint();
        }

        public void RefreshTexture()
        {
            Texture2D ground = Geometry.FieldTexture;
            if (ground == null && _theme != null)
                ground = _theme.Ground?.mainTexture as Texture2D;
            style.backgroundImage = ground != null ? new StyleBackground(ground) : new StyleBackground(StyleKeyword.None);
            MarkDirtyRepaint();
        }

        public void DeleteSelectedPoint()
        {
            if (_selected <= 0 || _selected >= Path.NumPoints - 1 || _selected % 3 != 0 || Path.NumSegments < 2)
                return;

            Undo.RecordObject(_level, "Delete path point");
            Path.DeleteSegment(_selected);
            _selected = NoHandle;
            LevelEditorModel.Save(_level);
            MarkDirtyRepaint();
        }

        private Vector2 ToCanvas(Vector3 point) => Origin + new Vector2(point.x, -point.z) * Scale;

        private Vector3 ToLevel(Vector2 position)
        {
            Vector2 point = (position - Origin) / Scale;
            return new Vector3(point.x, 0f, -point.y);
        }

        private void Draw(MeshGenerationContext context)
        {
            if (contentRect.width < 2f || contentRect.height < 2f || Path == null)
                return;

            Painter2D p = context.painter2D;
            DrawGrid(p);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            float roadWidth = Mathf.Max(2f, Geometry.RoadHalfWidth * 2f * Scale);
            DrawRoad(p, MarkerBackground, roadWidth + 4f);
            DrawRoad(p, RoadColor, roadWidth);

            p.strokeColor = ControlColor;
            p.lineWidth = 1f;
            for (int index = 0; index < Path.NumPoints - 1; index += 3)
            {
                DrawLine(p, ToCanvas(Path[index]), ToCanvas(Path[index + 1]));
                DrawLine(p, ToCanvas(Path[index + 3]), ToCanvas(Path[index + 2]));
            }

            for (int index = 1; index < Path.NumPoints - 1; index++)
            {
                bool anchor = index % 3 == 0;
                DrawPoint(p, ToCanvas(Path[index]), anchor ? 5f : 3f,
                    index == _selected ? Color.white : anchor ? AnchorColor : ControlColor);
            }

            foreach (Pose coin in Geometry.CoinSpawns)
                DrawPoint(p, ToCanvas(coin.position), 3f, AnchorColor);

            DrawGate(context, true, StartColor, "Start");
            DrawGate(context, false, EndColor, "End");
            Vector2 player = ToCanvas(Geometry.PlayerPosition);
            DrawPlayer(p, player, _selected == PlayerHandle ? Color.white : PlayerColor);
            context.DrawText("Frog", player + new Vector2(14f, -8f), 11f, PlayerColor, null);
        }

        private void DrawRoad(Painter2D p, Color color, float width)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            p.MoveTo(ToCanvas(Path[0]));
            for (int index = 1; index < Path.NumPoints; index += 3)
                p.BezierCurveTo(ToCanvas(Path[index]), ToCanvas(Path[index + 1]), ToCanvas(Path[index + 2]));
            p.Stroke();
        }

        private void DrawGrid(Painter2D p)
        {
            float units = Scale < 9f ? 5f : 1f;
            float space = units * Scale;
            p.strokeColor = GridColor;
            p.lineWidth = 1f;
            p.BeginPath();
            for (float x = Mathf.Repeat(Origin.x, space); x < contentRect.width; x += space)
            {
                p.MoveTo(new Vector2(x, 0f));
                p.LineTo(new Vector2(x, contentRect.height));
            }

            for (float y = Mathf.Repeat(Origin.y, space); y < contentRect.height; y += space)
            {
                p.MoveTo(new Vector2(0f, y));
                p.LineTo(new Vector2(contentRect.width, y));
            }
            p.Stroke();
        }

        private void DrawGate(MeshGenerationContext context, bool start, Color color, string label)
        {
            Pose gate = Geometry.GetGatePose(start);
            Vector2 marker = ToCanvas(gate.position);
            Vector2 anchor = ToCanvas(Path[start ? 0 : Path.NumPoints - 1]);
            Painter2D p = context.painter2D;
            p.strokeColor = color;
            p.lineWidth = 2f;
            DrawLine(p, anchor, marker);
            Vector3 forward = Geometry.GetGateFrame(start).rotation * Vector3.forward;
            Vector2 direction = new Vector2(forward.x, -forward.z);
            DrawLine(p, marker, marker + direction * 16f);
            DrawPoint(p, marker, 10f, MarkerBackground);
            if (start)
            {
                p.fillColor = color;
                p.BeginPath();
                p.MoveTo(marker + new Vector2(-6f, -1f));
                p.LineTo(marker + new Vector2(0f, -6f));
                p.LineTo(marker + new Vector2(6f, -1f));
                p.LineTo(marker + new Vector2(4f, -1f));
                p.LineTo(marker + new Vector2(4f, 5f));
                p.LineTo(marker + new Vector2(-4f, 5f));
                p.LineTo(marker + new Vector2(-4f, -1f));
                p.ClosePath();
                p.Fill();
                p.strokeColor = MarkerBackground;
                DrawLine(p, marker + new Vector2(0f, 4f), marker + new Vector2(0f, 1f));
            }
            else
            {
                DrawPoint(p, marker + new Vector2(0f, -2f), 5f, color);
                DrawPoint(p, marker + new Vector2(0f, 2f), 3.5f, color);
                DrawPoint(p, marker + new Vector2(-2f, -2f), 1.35f, MarkerBackground);
                DrawPoint(p, marker + new Vector2(2f, -2f), 1.35f, MarkerBackground);
            }
            context.DrawText(label, marker + new Vector2(13f, -15f), 11f, color, null);
        }

        private static void DrawPlayer(Painter2D p, Vector2 marker, Color color)
        {
            DrawPoint(p, marker, 10f, MarkerBackground);
            DrawPoint(p, marker + new Vector2(-4f, -3f), 3f, color);
            DrawPoint(p, marker + new Vector2(4f, -3f), 3f, color);
            DrawPoint(p, marker + new Vector2(0f, 1f), 6f, color);
            DrawPoint(p, marker + new Vector2(-3.5f, -3f), 1.2f, MarkerBackground);
            DrawPoint(p, marker + new Vector2(3.5f, -3f), 1.2f, MarkerBackground);
        }

        private static void DrawLine(Painter2D painter, Vector2 from, Vector2 to)
        {
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        private static void DrawPoint(Painter2D painter, Vector2 center, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointer >= 0 || (evt.button != 0 && evt.button != 1 && evt.button != 2))
                return;

            Vector2 position = this.WorldToLocal((Vector2)evt.position);
            Focus();
            if (evt.button == 0 && evt.shiftKey)
            {
                InsertPoint(position);
                evt.StopPropagation();
                return;
            }

            _panning = evt.button != 0;
            if (!_panning)
            {
                _selected = FindHandle(position);
                MarkDirtyRepaint();
                if (_selected == NoHandle)
                    return;
                _pointStart = _selected == PlayerHandle ? Geometry.PlayerPosition : Path[_selected];
            }

            _pointer = evt.pointerId;
            _pointerStart = position;
            _panStart = _pan;
            this.CapturePointer(_pointer);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointer)
                return;

            Vector2 position = this.WorldToLocal((Vector2)evt.position);
            if (_panning)
            {
                _pan = _panStart + position - _pointerStart;
            }
            else
            {
                Vector3 point = _pointStart + ToLevel(position) - ToLevel(_pointerStart);
                if (!CanMovePoint(point) || (!_edited && (point - _pointStart).sqrMagnitude < 0.000001f))
                    return;

                if (!_edited)
                {
                    Undo.IncrementCurrentGroup();
                    _undoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Move level handle");
                    _edited = true;
                }
                Undo.RecordObject(_level, "Move level handle");
                if (_selected == PlayerHandle)
                {
                    Geometry.PlayerPosition = point;
                }
                else
                {
                    if (_selected % 3 != 0 && Path.ControlPointMode == BezierPath.ControlMode.Automatic)
                        Path.ControlPointMode = BezierPath.ControlMode.Free;
                    Path.MovePoint(_selected, point);
                }
            }

            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == _pointer)
                this.ReleasePointer(_pointer);
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != _pointer)
                return;

            _pointer = -1;
            if (_edited)
            {
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(_undoGroup);
                LevelEditorModel.Save(_level);
                _edited = false;
            }
        }

        private void OnZoom(WheelEvent evt)
        {
            if (_pointer >= 0)
                return;

            Vector2 cursor = this.WorldToLocal((Vector2)evt.mousePosition);
            Vector3 before = ToLevel(cursor);
            _zoom = Mathf.Clamp(_zoom * Mathf.Pow(1.06f, -evt.delta.y), 0.2f, 8f);
            _pan += cursor - ToCanvas(before);
            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete || _pointer >= 0)
                return;

            DeleteSelectedPoint();
            evt.StopPropagation();
        }

        private bool CanMovePoint(Vector3 position)
        {
            if (_selected < 0 || _selected % 3 != 0)
                return true;

            return (_selected < 3 || (position - Path[_selected - 3]).sqrMagnitude > 0.00001f) &&
                (_selected + 3 >= Path.NumPoints || (position - Path[_selected + 3]).sqrMagnitude > 0.00001f);
        }

        private int FindHandle(Vector2 position)
        {
            float best = HitRadius * HitRadius;
            int result = NoHandle;
            FindCloser(Geometry.PlayerPosition, PlayerHandle);
            FindCloser(Geometry.GetGatePose(true).position, 0);
            FindCloser(Geometry.GetGatePose(false).position, Path.NumPoints - 1);
            for (int index = 0; index < Path.NumPoints; index++)
                FindCloser(Path[index], index);
            return result;

            void FindCloser(Vector3 candidate, int handle)
            {
                float distance = (ToCanvas(candidate) - position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    result = handle;
                }
            }
        }

        private void InsertPoint(Vector2 position)
        {
            const int steps = 32;
            float best = HitRadius * HitRadius;
            int segment = -1;
            float time = 0f;
            for (int index = 0; index < Path.NumSegments; index++)
            {
                Vector3[] points = Path.GetPointsInSegment(index);
                Vector2 from = ToCanvas(points[0]);
                for (int step = 1; step <= steps; step++)
                {
                    Vector2 to = ToCanvas(CubicBezierUtility.EvaluateCurve(points, step / (float)steps));
                    Vector2 direction = to - from;
                    float fraction = direction.sqrMagnitude > 0f ?
                        Mathf.Clamp01(Vector2.Dot(position - from, direction) / direction.sqrMagnitude) : 0f;
                    float distance = (position - (from + direction * fraction)).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        segment = index;
                        time = (step - 1 + fraction) / steps;
                    }
                    from = to;
                }
            }

            if (segment < 0 || time < 0.01f || time > 0.99f)
                return;

            Vector3 anchor = CubicBezierUtility.EvaluateCurve(Path.GetPointsInSegment(segment), time);
            if ((anchor - Path[segment * 3]).sqrMagnitude < 0.00001f ||
                (anchor - Path[segment * 3 + 3]).sqrMagnitude < 0.00001f)
                return;

            Undo.RecordObject(_level, "Insert path point");
            Path.ControlPointMode = BezierPath.ControlMode.Free;
            Path.SplitSegment(anchor, segment, time);
            _selected = segment * 3 + 3;
            LevelEditorModel.Save(_level);
            MarkDirtyRepaint();
        }

        private sealed class MapPointerManipulator : PointerManipulator
        {
            private readonly LevelCanvas _canvas;

            public MapPointerManipulator(LevelCanvas canvas)
            {
                _canvas = canvas;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(_canvas.OnPointerDown);
                target.RegisterCallback<PointerMoveEvent>(_canvas.OnPointerMove);
                target.RegisterCallback<PointerUpEvent>(_canvas.OnPointerUp);
                target.RegisterCallback<PointerCaptureOutEvent>(_canvas.OnCaptureOut);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerDownEvent>(_canvas.OnPointerDown);
                target.UnregisterCallback<PointerMoveEvent>(_canvas.OnPointerMove);
                target.UnregisterCallback<PointerUpEvent>(_canvas.OnPointerUp);
                target.UnregisterCallback<PointerCaptureOutEvent>(_canvas.OnCaptureOut);
            }
        }
    }
}
