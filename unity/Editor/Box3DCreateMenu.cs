// SPDX-License-Identifier: MIT

using UnityEditor;
using UnityEngine;

namespace Box3D.Editor
{
    /// <summary>
    /// GameObject > Box3D creation menu: ready-to-simulate objects with render mesh,
    /// body, and collider already set up. A Box3D World is created when missing.
    /// </summary>
    public static class Box3DCreateMenu
    {
        [MenuItem("GameObject/Box3D/Physics World", false, 10)]
        static void CreateWorld(MenuCommand command)
        {
            var go = new GameObject("Box3D World", typeof(Box3DWorld));
            Finish(go, command);
        }

        [MenuItem("GameObject/Box3D/Dynamic Box", false, 11)]
        static void CreateDynamicBox(MenuCommand command)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Dynamic Box", command);
            go.AddComponent<Box3DBody>();
            go.AddComponent<Box3DBoxCollider>();
        }

        [MenuItem("GameObject/Box3D/Dynamic Sphere", false, 12)]
        static void CreateDynamicSphere(MenuCommand command)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Sphere, "Dynamic Sphere", command);
            go.AddComponent<Box3DBody>();
            go.AddComponent<Box3DSphereCollider>();
        }

        [MenuItem("GameObject/Box3D/Dynamic Capsule", false, 13)]
        static void CreateDynamicCapsule(MenuCommand command)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Capsule, "Dynamic Capsule", command);
            go.AddComponent<Box3DBody>();
            go.AddComponent<Box3DCapsuleCollider>();
        }

        [MenuItem("GameObject/Box3D/Static Ground", false, 14)]
        static void CreateStaticGround(MenuCommand command)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Static Ground", command);
            go.transform.localScale = new Vector3(20.0f, 1.0f, 20.0f);
            go.AddComponent<Box3DBoxCollider>(); // no body: static, like Unity colliders
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, MenuCommand command)
        {
            EnsureWorldExists();
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            // Box3D colliders replace the Unity collider CreatePrimitive adds.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Finish(go, command);
            return go;
        }

        static void EnsureWorldExists()
        {
#if UNITY_2023_1_OR_NEWER
            bool hasWorld = Object.FindFirstObjectByType<Box3DWorld>() != null;
#else
            bool hasWorld = Object.FindObjectOfType<Box3DWorld>() != null;
#endif
            if (!hasWorld)
            {
                var world = new GameObject("Box3D World", typeof(Box3DWorld));
                Undo.RegisterCreatedObjectUndo(world, "Create Box3D World");
            }
        }

        static void Finish(GameObject go, MenuCommand command)
        {
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeGameObject = go;
        }
    }
}
