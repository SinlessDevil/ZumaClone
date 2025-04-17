using Code.Logic.Zuma.Level;
using UnityEditor;
using UnityEngine;

namespace Code.Infrastructure.Editor
{
    [CustomEditor(typeof(LevelHolder))]
    public class LevelHolderEditor : UnityEditor.Editor
    {
        private LevelHolder _levelHolder;
        private SerializedProperty pathCreatorProp;
        private SerializedProperty levelStartProp;
        private SerializedProperty levelEndProp;

        private void OnEnable()
        {
            _levelHolder = (LevelHolder)target;
            pathCreatorProp = serializedObject.FindProperty("<PathCreator>k__BackingField");
            levelStartProp = serializedObject.FindProperty("<LevelStart>k__BackingField");
            levelEndProp = serializedObject.FindProperty("<LevelEnd>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (GUILayout.Button("Update Start & End Positions"))
            {
                UpdateStartAndEndPositions();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateStartAndEndPositions()
        {
            if (_levelHolder.PathCreator == null)
            {
                Debug.LogWarning("PathCreator is not assigned!");
                return;
            }

            var path = _levelHolder.PathCreator.path;
            if (path == null)
            {
                Debug.LogWarning("Path is null in PathCreator!");
                return;
            }

            if (_levelHolder.LevelStart != null)
            {
                _levelHolder.LevelStart.transform.position = path.GetPoint(0);
                _levelHolder.LevelStart.transform.rotation = Quaternion.LookRotation(path.GetDirection(0));
            }

            if (_levelHolder.LevelEnd != null)
            {
                int lastIndex = path.NumPoints - 1;
                _levelHolder.LevelEnd.transform.position = path.GetPoint(lastIndex);
                _levelHolder.LevelEnd.transform.rotation = Quaternion.LookRotation(path.GetDirection(lastIndex));
            }
        }
    }
}
