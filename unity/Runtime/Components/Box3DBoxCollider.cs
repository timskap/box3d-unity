// SPDX-License-Identifier: MIT

using System;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>Box collision shape (implemented as an 8-vertex convex hull).</summary>
    [AddComponentMenu("Box3D/Box3D Box Collider")]
    public sealed class Box3DBoxCollider : Box3DCollider
    {
        [Tooltip("Local center of the box.")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Full extents before transform scale.")]
        public Vector3 size = Vector3.one;

        static readonly Vector3[] CornerSigns =
        {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1),
        };

        protected override B3ShapeId CreateNativeShape(B3BodyId bodyId, ref B3ShapeDef def)
        {
            GetBodyLocalPose(out Vector3 localPosition, out Quaternion localRotation, out Vector3 scale);

            var points = new Vector3[8];
            Vector3 half = 0.5f * size;
            for (int i = 0; i < 8; ++i)
            {
                Vector3 corner = center + Vector3.Scale(half, CornerSigns[i]);
                points[i] = localPosition + localRotation * Vector3.Scale(corner, scale);
            }

            IntPtr hull = B3Api.b3CreateHull(points, 8, 8);
            if (hull == IntPtr.Zero)
            {
                Debug.LogError("[Box3D] Failed to build box hull. Is the size degenerate?", this);
                return default;
            }

            // The world clones hulls into an internal database, so ours can be freed immediately.
            B3ShapeId shapeId = B3Api.b3CreateHullShape(bodyId, ref def, hull);
            B3Api.b3DestroyHull(hull);
            return shapeId;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = isTrigger ? new Color(1.0f, 0.7f, 0.0f, 0.8f) : new Color(0.55f, 0.95f, 0.55f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
