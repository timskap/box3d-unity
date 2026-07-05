// SPDX-License-Identifier: MIT

using UnityEditor;
using UnityEngine;

namespace Box3D.Editor
{
    /// <summary>
    /// Inspector for all Box3D colliders: surfaces setup problems (missing or
    /// non-readable meshes, mesh colliders on dynamic bodies, no world in the scene)
    /// and offers fit/rebuild actions.
    /// </summary>
    [CustomEditor(typeof(Box3DCollider), true)]
    [CanEditMultipleObjects]
    public sealed class Box3DColliderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length != 1)
            {
                return;
            }

            var collider = (Box3DCollider)target;
            DrawWarnings(collider);
            DrawActions(collider);
        }

        static void DrawWarnings(Box3DCollider collider)
        {
            if (!Application.isPlaying && CountWorlds() == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Box3D World in the scene. Add one (GameObject > Box3D > Physics World) " +
                    "before entering play mode.",
                    MessageType.Info);
            }

            switch (collider)
            {
                case Box3DConvexCollider convex:
                    WarnAboutMesh(convex.ResolveMesh(), "convex hull");
                    break;

                case Box3DMeshCollider meshCollider:
                    WarnAboutMesh(meshCollider.ResolveMesh(), "triangle mesh");
                    Box3DBody body = collider.GetComponentInParent<Box3DBody>();
                    if (body != null && body.bodyType == Box3DBodyType.Dynamic)
                    {
                        EditorGUILayout.HelpBox(
                            "Mesh colliders only generate contacts on static geometry. This collider is " +
                            "under a Dynamic Box3D Body; use a Box3D Convex Collider instead.",
                            MessageType.Warning);
                    }

                    break;

                case Box3DSphereCollider _:
                case Box3DCapsuleCollider _:
                    Vector3 scale = collider.transform.lossyScale;
                    float min = Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    float max = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    if (max > min * 1.01f)
                    {
                        EditorGUILayout.HelpBox(
                            "Non-uniform transform scale: the radius is taken from the largest scale axis, " +
                            "so the shape stays round and may not match the render mesh.",
                            MessageType.Info);
                    }

                    break;
            }
        }

        static void WarnAboutMesh(Mesh mesh, string usage)
        {
            if (mesh == null)
            {
                EditorGUILayout.HelpBox(
                    $"No mesh found for the {usage}. Assign a Mesh or add a MeshFilter to this GameObject.",
                    MessageType.Error);
            }
            else if (!mesh.isReadable)
            {
                EditorGUILayout.HelpBox(
                    $"Mesh '{mesh.name}' is not readable, so the {usage} cannot be built in a player. " +
                    "Enable Read/Write in the mesh import settings.",
                    MessageType.Warning);
            }
        }

        static void DrawActions(Box3DCollider collider)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool primitive = collider is Box3DBoxCollider || collider is Box3DSphereCollider ||
                                 collider is Box3DCapsuleCollider;
                if (primitive && GUILayout.Button("Fit to Render Bounds"))
                {
                    Undo.RecordObject(collider, "Fit Collider to Render Bounds");
                    switch (collider)
                    {
                        case Box3DBoxCollider box:
                            box.FitToRenderBounds();
                            break;
                        case Box3DSphereCollider sphere:
                            sphere.FitToRenderBounds();
                            break;
                        case Box3DCapsuleCollider capsule:
                            capsule.FitToRenderBounds();
                            break;
                    }

                    EditorUtility.SetDirty(collider);
                }

                if (Application.isPlaying && collider.IsCreated && GUILayout.Button("Rebuild Shape"))
                {
                    collider.Rebuild();
                }
            }
        }

        static int CountWorlds()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<Box3DWorld>(FindObjectsSortMode.None).Length;
#else
            return Object.FindObjectsOfType<Box3DWorld>().Length;
#endif
        }
    }
}
