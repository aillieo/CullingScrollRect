using UnityEditor.AnimatedValues;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.UI;

namespace AillieoUtils
{

    [CustomEditor(typeof(CullingScrollRect), true)]
    [CanEditMultipleObjects]

    

    public class CullingScrollRectEditor : ScrollRectEditor
    {
        SerializedProperty m_Borders;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Borders);
            base.OnInspectorGUI();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Borders = serializedObject.FindProperty("m_Borders");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
    }

}

