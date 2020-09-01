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
            serializedObject.Update();

            EditorGUILayout.LabelField("Borders");
            Vector2 bordersLT = cullingScrollRect.borderLeftTop;
            Vector2 bordersRB = cullingScrollRect.borderRightBottom;

            EditorGUI.indentLevel ++;
            //bordersLT.x = EditorGUILayout.FloatField(new GUIContent("Left"), bordersLT.x);
            bordersRB.x = EditorGUILayout.FloatField(new GUIContent("Right"), bordersRB.x);
            //bordersLT.y = EditorGUILayout.FloatField(new GUIContent("Top"), bordersLT.y);
            bordersRB.y = EditorGUILayout.FloatField(new GUIContent("Bottom"), bordersRB.y);
            EditorGUI.indentLevel--;

            if (bordersLT != cullingScrollRect.borderLeftTop)
            {
                cullingScrollRect.borderLeftTop = bordersLT;
                m_BordersLT.vector2Value = bordersLT;
            }
            if (bordersRB != cullingScrollRect.borderRightBottom)
            {
                cullingScrollRect.borderRightBottom = bordersRB;
                m_BordersRB.vector2Value = bordersRB;
            }
            serializedObject.ApplyModifiedProperties();

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

