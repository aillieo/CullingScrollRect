using UnityEditor.AnimatedValues;
using UnityEditor;
using UnityEngine;
using UnityEditor.UI;

namespace AillieoUtils
{

    [CustomEditor(typeof(CullingScrollRect), true)]
    [CanEditMultipleObjects]



    public class CullingScrollRectEditor : ScrollRectEditor
    {
        private SerializedProperty m_BordersLT;
        private SerializedProperty m_BordersRB;
        private CullingScrollRect cullingScrollRect;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Borders");
            Vector2 bordersLT = cullingScrollRect.borderLeftTop;
            Vector2 bordersRB = cullingScrollRect.borderRightBottom;

            EditorGUI.indentLevel ++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_BordersLT.FindPropertyRelative("x"), new GUIContent("Left"));
            EditorGUILayout.PropertyField(m_BordersRB.FindPropertyRelative("x"), new GUIContent("Right"));
            EditorGUILayout.PropertyField(m_BordersLT.FindPropertyRelative("y"), new GUIContent("Top"));
            EditorGUILayout.PropertyField(m_BordersRB.FindPropertyRelative("y"), new GUIContent("Bottom"));
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUI.indentLevel--;

            if(changed)
            {
                serializedObject.ApplyModifiedProperties();
                cullingScrollRect.borderLeftTop = m_BordersLT.vector2Value;
                cullingScrollRect.borderRightBottom = m_BordersRB.vector2Value;
            }

            // serializedObject.Update();

            base.OnInspectorGUI();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_BordersLT = serializedObject.FindProperty("m_BordersLT");
            m_BordersRB = serializedObject.FindProperty("m_BordersRB");
            cullingScrollRect = target as CullingScrollRect;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
    }

}

