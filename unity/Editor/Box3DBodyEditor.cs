// SPDX-License-Identifier: MIT

using UnityEditor;
using UnityEngine;

namespace Box3D.Editor
{
    /// <summary>
    /// Inspector for Box3DBody: draws the freeze axes as compact constraint rows
    /// (like Rigidbody) and shows live state with quick actions in play mode.
    /// </summary>
    [CustomEditor(typeof(Box3DBody))]
    [CanEditMultipleObjects]
    public sealed class Box3DBodyEditor : UnityEditor.Editor
    {
        static readonly string[] FreezeFields =
        {
            "freezePositionX", "freezePositionY", "freezePositionZ",
            "freezeRotationX", "freezeRotationY", "freezeRotationZ",
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var excluded = new string[FreezeFields.Length + 1];
            excluded[0] = "m_Script";
            FreezeFields.CopyTo(excluded, 1);
            DrawPropertiesExcluding(serializedObject, excluded);

            EditorGUILayout.Space(2.0f);
            EditorGUILayout.LabelField("Constraints", EditorStyles.boldLabel);
            DrawFreezeRow("Freeze Position", FreezeFields[0], FreezeFields[1], FreezeFields[2]);
            DrawFreezeRow("Freeze Rotation", FreezeFields[3], FreezeFields[4], FreezeFields[5]);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying && targets.Length == 1 && target is Box3DBody body && body.IsCreated)
            {
                DrawRuntimeSection(body);
            }
        }

        void DrawFreezeRow(string label, string xField, string yField, string zField)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                DrawFreezeToggle(serializedObject.FindProperty(xField), "X");
                DrawFreezeToggle(serializedObject.FindProperty(yField), "Y");
                DrawFreezeToggle(serializedObject.FindProperty(zField), "Z");
                GUILayout.FlexibleSpace();
            }
        }

        static void DrawFreezeToggle(SerializedProperty property, string label)
        {
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.ToggleLeft(label, property.boolValue, GUILayout.Width(32.0f));
            if (EditorGUI.EndChangeCheck())
            {
                property.boolValue = value;
            }

            EditorGUI.showMixedValue = false;
        }

        static void DrawRuntimeSection(Box3DBody body)
        {
            EditorGUILayout.Space(4.0f);
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Mass (kg)", body.mass);
                EditorGUILayout.Vector3Field("Linear Velocity (m/s)", body.linearVelocity);
                EditorGUILayout.Vector3Field("Angular Velocity (rad/s)", body.angularVelocity);
                EditorGUILayout.Toggle("Awake", body.IsAwake);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Wake Up"))
                {
                    body.WakeUp();
                }

                if (GUILayout.Button("Sleep"))
                {
                    body.Sleep();
                }

                if (GUILayout.Button("Recompute Mass"))
                {
                    body.RecomputeMass();
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
