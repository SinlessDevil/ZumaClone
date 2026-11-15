using UnityEngine;
using UnityEngine.Rendering;

namespace Code.PathCreation 
{
    public class RoadMeshCreator : PathSceneTool 
    {
        [Space(10)] [Header ("Road settings")]
        public float roadWidth = .4f;
        [Range (0, .5f)] public float thickness = .15f;
        public bool flattenSurface;
        [Space(10)] [Header ("Material settings")]
        public Material roadMaterial;
        public Material undersideMaterial;
        public float textureTiling = 1;

        [SerializeField] private MeshFilter _renderTarget;
        private GameObject _meshHolder;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;

        public GameObject MeshHolder => _meshHolder;
        public Texture SurfaceTexture { get; set; }

        public static void ApplySurface(MeshRenderer renderer, Texture texture, float? tiling = null)
        {
            Material material = renderer.sharedMaterial;
            Shader shader = material.shader;
            string property = "_MainTex";
            for (int index = 0; index < shader.GetPropertyCount(); index++)
            {
                if ((shader.GetPropertyFlags(index) & ShaderPropertyFlags.MainTexture) != 0)
                {
                    property = shader.GetPropertyName(index);
                    break;
                }
            }

            if (!material.HasProperty(property))
                return;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            if (texture != null)
                block.SetTexture(property, texture);
            if (tiling.HasValue)
            {
                Vector2 offset = material.GetTextureOffset(property);
                block.SetVector(property + "_ST", new Vector4(1f, tiling.Value, offset.x, offset.y));
            }
            renderer.SetPropertyBlock(block, 0);
        }

        protected override void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }

            base.OnDestroy();
        }
        
        protected override void PathUpdated() 
        {
            if (pathCreator != null) 
            {
                AssignMeshComponents();
                AssignMaterials();
                CreateRoadMesh();
            }
        }

        private void CreateRoadMesh() 
        {
            Vector3[] verts = new Vector3[Path.NumPoints * 8];
            Vector2[] uvs = new Vector2[verts.Length];
            Vector3[] normals = new Vector3[verts.Length];

            int numTris = 2 * (Path.NumPoints - 1) + ((Path.isClosedLoop) ? 2 : 0);
            int[] roadTriangles = new int[numTris * 3];
            int[] underRoadTriangles = new int[numTris * 3];
            int[] sideOfRoadTriangles = new int[numTris * 2 * 3];

            int vertIndex = 0;
            int triIndex = 0;

            // Vertices for the top of the road are layed out:
            // 0  1
            // 8  9
            // and so on... So the triangle map 0,8,1 for example, defines a triangle from top left to bottom left to bottom right.
            int[] triangleMap = { 0, 8, 1, 1, 8, 9 };
            int[] sidesTriangleMap = { 4, 6, 14, 12, 4, 14, 5, 15, 7, 13, 15, 5 };

            bool usePathNormals = !(Path.space == PathSpace.xyz && flattenSurface);
            float sideUvWidth = Mathf.Clamp(thickness / Mathf.Max(0.01f, 2f * Mathf.Abs(roadWidth)), 0.01f, 1f);

            for (int i = 0; i < Path.NumPoints; i++) {
                Vector3 localUp = (usePathNormals) ? Vector3.Cross (Path.GetTangent (i), Path.GetNormal (i)) : Path.up;
                Vector3 localRight = (usePathNormals) ? Path.GetNormal (i) : Vector3.Cross (localUp, Path.GetTangent (i));

                // Find position to left and right of current path vertex
                Vector3 vertSideA = Path.GetPoint (i) - localRight * Mathf.Abs (roadWidth);
                Vector3 vertSideB = Path.GetPoint (i) + localRight * Mathf.Abs (roadWidth);

                // Add top of road vertices
                verts[vertIndex + 0] = vertSideA;
                verts[vertIndex + 1] = vertSideB;
                // Add bottom of road vertices
                verts[vertIndex + 2] = vertSideA - localUp * thickness;
                verts[vertIndex + 3] = vertSideB - localUp * thickness;

                // Duplicate vertices to get flat shading for sides of road
                verts[vertIndex + 4] = verts[vertIndex + 0];
                verts[vertIndex + 5] = verts[vertIndex + 1];
                verts[vertIndex + 6] = verts[vertIndex + 2];
                verts[vertIndex + 7] = verts[vertIndex + 3];

                // Keep the longitudinal UV aligned and give the sides a nonzero width for normal mapping.
                float pathTime = Path.times[i];
                uvs[vertIndex + 0] = uvs[vertIndex + 2] = uvs[vertIndex + 4] = new Vector2(0f, pathTime);
                uvs[vertIndex + 1] = uvs[vertIndex + 3] = uvs[vertIndex + 5] = new Vector2(1f, pathTime);
                uvs[vertIndex + 6] = new Vector2(sideUvWidth, pathTime);
                uvs[vertIndex + 7] = new Vector2(1f - sideUvWidth, pathTime);

                // Top of road normals
                normals[vertIndex + 0] = localUp;
                normals[vertIndex + 1] = localUp;
                // Bottom of road normals
                normals[vertIndex + 2] = -localUp;
                normals[vertIndex + 3] = -localUp;
                // Sides of road normals
                normals[vertIndex + 4] = -localRight;
                normals[vertIndex + 5] = localRight;
                normals[vertIndex + 6] = -localRight;
                normals[vertIndex + 7] = localRight;

                // Set triangle indices
                if (i < Path.NumPoints - 1 || Path.isClosedLoop) {
                    for (int j = 0; j < triangleMap.Length; j++) {
                        roadTriangles[triIndex + j] = (vertIndex + triangleMap[j]) % verts.Length;
                        // reverse triangle map for under road so that triangles wind the other way and are visible from underneath
                        underRoadTriangles[triIndex + j] = (vertIndex + triangleMap[triangleMap.Length - 1 - j] + 2) % verts.Length;
                    }
                    for (int j = 0; j < sidesTriangleMap.Length; j++) {
                        sideOfRoadTriangles[triIndex * 2 + j] = (vertIndex + sidesTriangleMap[j]) % verts.Length;
                    }

                }

                vertIndex += 8;
                triIndex += 6;
            }

            _mesh.Clear ();
            _mesh.vertices = verts;
            _mesh.uv = uvs;
            _mesh.normals = normals;
            _mesh.subMeshCount = 3;
            _mesh.SetTriangles (roadTriangles, 0);
            _mesh.SetTriangles (underRoadTriangles, 1);
            _mesh.SetTriangles (sideOfRoadTriangles, 2);
            _mesh.RecalculateBounds ();
            if (roadMaterial != null && roadMaterial.IsKeywordEnabled("_NORMALMAP"))
                _mesh.RecalculateTangents();
        }

        // Add MeshRenderer and MeshFilter components to this gameobject if not already attached
        private void AssignMeshComponents() 
        {
            if (_renderTarget != null)
            {
                _meshHolder = _renderTarget.gameObject;
                _meshFilter = _renderTarget;
                _meshRenderer = _renderTarget.GetComponent<MeshRenderer>();
                if (_mesh == null)
                    _mesh = new Mesh();
                _meshFilter.sharedMesh = _mesh;
                return;
            }

            if (_meshHolder == null) 
            {
                _meshHolder = new GameObject ("Road Mesh Holder");
            }

            _meshHolder.transform.rotation = Quaternion.identity;
            _meshHolder.transform.position = Vector3.zero;
            _meshHolder.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!_meshHolder.gameObject.GetComponent<MeshFilter>()) 
            {
                _meshHolder.gameObject.AddComponent<MeshFilter>();
            }
            
            if (!_meshHolder.GetComponent<MeshRenderer>()) 
            {
                _meshHolder.gameObject.AddComponent<MeshRenderer>();
            }

            _meshRenderer = _meshHolder.GetComponent<MeshRenderer>();
            _meshFilter = _meshHolder.GetComponent<MeshFilter>();
            if (_mesh == null) 
            {
                _mesh = new Mesh();
            }
            
            _meshFilter.sharedMesh = _mesh;
        }

        private void AssignMaterials() 
        {
            if (roadMaterial != null && undersideMaterial != null) 
            {
                _meshRenderer.sharedMaterials = new Material[] { roadMaterial, undersideMaterial, undersideMaterial };
                ApplySurface(_meshRenderer, SurfaceTexture, textureTiling);
            }
        }
    }
}