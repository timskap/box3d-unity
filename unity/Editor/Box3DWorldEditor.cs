// SPDX-License-Identifier: MIT

using UnityEditor;
using UnityEngine;

namespace Box3D.Editor
{
    /// <summary>
    /// Inspector for Box3DWorld: warns about duplicate worlds and shows live
    /// simulation stats in play mode.
    /// </summary>
    [CustomEditor(typeof(Box3DWorld))]
    public sealed class Box3DWorldEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var world = (Box3DWorld)target;

            if (CountEnabledWorlds() > 1)
            {
                EditorGUILayout.HelpBox(
                    "Multiple enabled Box3D World components found. Bodies and colliders attach " +
                    "to the first enabled world (Box3DWorld.Main); keep a single world per scene.",
                    MessageType.Warning);
            }

            if (Application.isPlaying && world.IsCreated)
            {
                EditorGUILayout.Space(4.0f);
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Awake Bodies", world.AwakeBodyCount);
                    EditorGUILayout.Toggle("Is Main World", Box3DWorld.Main == world);
                }
            }
        }

        static int CountEnabledWorlds()
        {
#if UNITY_2023_1_OR_NEWER
            Box3DWorld[] worlds = Object.FindObjectsByType<Box3DWorld>(FindObjectsSortMode.None);
#else
            Box3DWorld[] worlds = Object.FindObjectsOfType<Box3DWorld>();
#endif
            int enabled = 0;
            foreach (Box3DWorld world in worlds)
            {
                if (world.isActiveAndEnabled)
                {
                    ++enabled;
                }
            }

            return enabled;
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
