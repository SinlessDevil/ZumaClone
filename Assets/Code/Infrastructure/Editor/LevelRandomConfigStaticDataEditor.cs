using Code.StaticData.Levels;
using UnityEditor;

namespace Code.Infrastructure.Editor
{
    [CustomEditor(typeof(LevelRandomConfigStaticData))]
    public class LevelRandomConfigStaticDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemsProperty;
        private SerializedProperty _itemProbabilitiesProperty;

        private void OnEnable()
        {
            _itemsProperty = serializedObject.FindProperty("Items");
            _itemProbabilitiesProperty = serializedObject.FindProperty("ItemProbabilities");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_itemsProperty);

            EnsureProbabilityListMatchesItems();
            NormalizeProbabilities();
            DrawProbabilitySliders();

            serializedObject.ApplyModifiedProperties();
        }

        private void EnsureProbabilityListMatchesItems()
        {
            if (_itemProbabilitiesProperty.arraySize == _itemsProperty.arraySize)
                return;

            _itemProbabilitiesProperty.arraySize = _itemsProperty.arraySize;

            for (int i = 0; i < _itemsProperty.arraySize; i++)
            {
                SerializedProperty itemProperty = _itemsProperty.GetArrayElementAtIndex(i);
                SerializedProperty probabilityProperty = _itemProbabilitiesProperty.GetArrayElementAtIndex(i);

                if (probabilityProperty == null)
                {
                    continue;
                }

                SerializedProperty itemRef = probabilityProperty.FindPropertyRelative("Item");
                SerializedProperty probabilityValue = probabilityProperty.FindPropertyRelative("Probability");

                if (itemProperty.objectReferenceValue == null)
                {
                    continue;
                }
                
                if (itemRef.objectReferenceValue == null)
                {
                    itemRef.objectReferenceValue = itemProperty.objectReferenceValue;
                }

                if (probabilityValue.floatValue == 0)
                {
                    probabilityValue.floatValue = 100f / _itemsProperty.arraySize;
                }
            }
    
            serializedObject.ApplyModifiedProperties();
        }

        private void NormalizeProbabilities()
        {
            float total = 0;
            for (int i = 0; i < _itemProbabilitiesProperty.arraySize; i++)
            {
                SerializedProperty probability = _itemProbabilitiesProperty.GetArrayElementAtIndex(i);
                if (probability.propertyType == SerializedPropertyType.Float)
                {
                    total += probability.floatValue;
                }
            }

            if (total > 0)
            {
                for (int i = 0; i < _itemProbabilitiesProperty.arraySize; i++)
                {
                    SerializedProperty probability = _itemProbabilitiesProperty.GetArrayElementAtIndex(i);
                    if (probability.propertyType == SerializedPropertyType.Float)
                    {
                        probability.floatValue = (probability.floatValue / total) * 100f;
                    }
                }
            }
        }

        private void DrawProbabilitySliders()
        {
            for (int i = 0; i < _itemProbabilitiesProperty.arraySize; i++)
            {
                SerializedProperty itemProbability = _itemProbabilitiesProperty.GetArrayElementAtIndex(i);
                SerializedProperty probability = itemProbability.FindPropertyRelative("Probability");

                string itemName = _itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue != null
                    ? _itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue.name
                    : "Undefined Item";
                
                float newValue = EditorGUILayout.Slider(itemName, probability.floatValue, 0, 100);
                probability.floatValue = newValue;
            }
        }

    }
}