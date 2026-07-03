// SPDX-License-Identifier: MIT

using System;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Convex hull collision shape built from a Unity mesh. Suitable for dynamic bodies.
    /// The hull is computed from the mesh vertices and simplified to at most Max Vertices.
    /// </summary>
    [AddComponentMenu("Box3D/Box3D Convex Collider")]
    public sealed class Box3DConvexCollider : Box3DCollider
    {
        [Tooltip("Source mesh. Defaults to the MeshFilter mesh on this GameObject.")]
        public Mesh mesh;

        [Tooltip("Maximum hull vertices. Fewer is faster; 16-32 is plenty for most shapes.")]
        [Range(4, 64)] public int maxVertices = 32;

        protected override B3ShapeId CreateNativeShape(B3BodyId bodyId, ref B3ShapeDef def)
        {
            Mesh source = ResolveMesh();
            if (source == null)
            {
                Debug.LogError("[Box3D] Box3DConvexCollider has no mesh. Assign one or add a MeshFilter.", this);
                return default;
            }

            if (!source.isReadable && Application.isPlaying)
            {
                Debug.LogError($"[Box3D] Mesh '{source.name}' is not readable. Enable Read/Write in import settings.", this);
                return default;
            }

            GetBodyLocalPose(out Vector3 localPosition, out Quaternion localRotation, out Vector3 scale);

            Vector3[] vertices = source.vertices;
            var points = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                points[i] = localPosition + localRotation * Vector3.Scale(vertices[i], scale);
            }

            IntPtr hull = B3Api.b3CreateHull(points, points.Length, maxVertices);
            if (hull == IntPtr.Zero)
            {
                Debug.LogError($"[Box3D] Failed to build convex hull from mesh '{source.name}'. The mesh may be degenerate.", this);
                return default;
            }

            B3ShapeId shapeId = B3Api.b3CreateHullShape(bodyId, ref def, hull);
            B3Api.b3DestroyHull(hull);
            return shapeId;
        }

        Mesh ResolveMesh()
        {
            if (mesh != null)
            {
                return mesh;
            }

            var filter = GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        void OnDrawGizmosSelected()
        {
            Mesh source = ResolveMesh();
            if (source == null)
            {
                return;
            }

            Gizmos.color = isTrigger ? new Color(1.0f, 0.7f, 0.0f, 0.6f) : new Color(0.55f, 0.95f, 0.55f, 0.6f);
            Gizmos.DrawWireMesh(source, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
