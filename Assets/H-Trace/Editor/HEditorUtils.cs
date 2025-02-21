#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace H_Trace.Editor
{
	public static class HEditorUtils
	{
        public readonly struct FoldoutScope : IDisposable
        {
            private readonly bool wasIndent;

            public FoldoutScope(AnimBool value, out bool shouldDraw, string label, bool indent = true, SerializedProperty toggle = null)
            {
                value.target = Foldout(value.target, label, toggle);
                shouldDraw = EditorGUILayout.BeginFadeGroup(value.faded);
                if (shouldDraw && indent)
                {
                    Indent();
                    wasIndent = true;
                }
                else
                {
                    wasIndent = false;
                }
            }

            public void Dispose()
            {
                if (wasIndent)
                    EndIndent();
                EditorGUILayout.EndFadeGroup();
            }
        }

        public static void HorizontalLine(float height = 1, float width = -1, Vector2 margin = new Vector2())
        {
            GUILayout.Space(margin.x);

            var rect = EditorGUILayout.GetControlRect(false, height);
            if (width > -1)
            {
                var centerX = rect.width / 2;
                rect.width = width;
                rect.x += centerX - width / 2;
            }

            Color color = EditorStyles.label.active.textColor;
            color.a = 0.5f;
            EditorGUI.DrawRect(rect, color);

            GUILayout.Space(margin.y);
        }

        public static bool Foldout(bool value, string label, SerializedProperty toggle = null)
        {
            bool _value;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            if (toggle != null && !toggle.boolValue)
            {
                EditorGUI.BeginDisabledGroup(true);
                _value = EditorGUILayout.Toggle(value, EditorStyles.foldout);
                EditorGUI.EndDisabledGroup();

                _value = false;
            }
            else
            {
                _value = EditorGUILayout.Toggle(value, EditorStyles.foldout);
            }

            if (toggle != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(toggle, GUIContent.none, GUILayout.Width(20));
                if (EditorGUI.EndChangeCheck() && toggle.boolValue)
                    _value = true;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();
            rect.x += 20;
            rect.width -= 20;

            if (toggle != null && !toggle.boolValue)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            }

            return _value;
        }

        public static void Indent()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            EditorGUILayout.BeginVertical();
        }

        public static void EndIndent()
        {
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif