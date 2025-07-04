using UnityEngine;

namespace Code.PathCreation
{
    /// Stores state data for the path creator editor
    [System.Serializable]
    public class PathCreatorData
    {
        public event System.Action bezierOrVertexPathModified;
        public event System.Action bezierCreated;

        [SerializeField] BezierPath _bezierPath;
        VertexPath _vertexPath;

        [SerializeField] bool vertexPathUpToDate;

        // vertex path settings
        public float vertexPathMaxAngleError = .3f;
        public float vertexPathMinVertexSpacing = 0.01f;

        // bezier display settings
        public bool showTransformTool = true;
        public bool showPathBounds;
        public bool showPerSegmentBounds;
        public bool displayAnchorPoints = true;
        public bool displayControlPoints = true;
        public float bezierHandleScale = 1;
        public bool globalDisplaySettingsFoldout;
        public bool keepConstantHandleSize;

        // vertex display settings
        public bool showNormalsInVertexMode;
        public bool showBezierPathInVertexMode;

        // Editor display states
        public bool showDisplayOptions;
        public bool showPathOptions = true;
        public bool showVertexPathDisplayOptions;
        public bool showVertexPathOptions = true;
        public bool showNormals;
        public bool showNormalsHelpInfo;
        public int tabIndex;

        public void Initialize(bool defaultIs2D)
        {
            if (_bezierPath == null)
            {
                CreateBezier(Vector3.zero, defaultIs2D);
            }

            vertexPathUpToDate = false;
            _bezierPath.OnModified -= BezierPathEdited;
            _bezierPath.OnModified += BezierPathEdited;
        }

        public void ResetBezierPath(Vector3 centre, bool defaultIs2D = false)
        {
            CreateBezier(centre, defaultIs2D);
        }

        private void CreateBezier(Vector3 centre, bool defaultIs2D = false)
        {
            if (_bezierPath != null)
            {
                _bezierPath.OnModified -= BezierPathEdited;
            }

            PathSpace space = (defaultIs2D) ? PathSpace.xy : PathSpace.xyz;
            _bezierPath = new BezierPath(centre, false, space);

            _bezierPath.OnModified += BezierPathEdited;
            vertexPathUpToDate = false;

            bezierOrVertexPathModified?.Invoke();

            bezierCreated?.Invoke();
        }

        public BezierPath bezierPath
        {
            get { return _bezierPath; }
            set
            {
                _bezierPath.OnModified -= BezierPathEdited;
                vertexPathUpToDate = false;
                _bezierPath = value;
                _bezierPath.OnModified += BezierPathEdited;

                bezierOrVertexPathModified?.Invoke();

                bezierCreated?.Invoke();
            }
        }

        // Get the current vertex path
        public VertexPath GetVertexPath(Transform transform)
        {
            // create new vertex path if path was modified since this vertex path was created
            if (vertexPathUpToDate && _vertexPath != null) 
                return _vertexPath;
            
            vertexPathUpToDate = true;
            _vertexPath = new VertexPath(bezierPath, transform, vertexPathMaxAngleError,
                vertexPathMinVertexSpacing);

            return _vertexPath;
        }

        public void PathTransformed()
        {
            if (bezierOrVertexPathModified != null)
            {
                bezierOrVertexPathModified();
            }
        }

        public void VertexPathSettingsChanged()
        {
            vertexPathUpToDate = false;
            bezierOrVertexPathModified?.Invoke();
        }

        public void PathModifiedByUndo()
        {
            vertexPathUpToDate = false;
            bezierOrVertexPathModified?.Invoke();
        }

        private void BezierPathEdited()
        {
            vertexPathUpToDate = false;
            bezierOrVertexPathModified?.Invoke();
        }
    }
}