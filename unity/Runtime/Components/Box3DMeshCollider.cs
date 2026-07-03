// SPDX-License-Identifier: MIT

using System;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Triangle mesh collision shape for static level geometry. Box3D only generates
    /// mesh contacts on static bodies, so use this without a Box3DBody (or on a static one).
    /// Native mesh data is shared between colliders that use the same mesh, scale, and offset.
    /// </summary>
    [AddComponentMenu("Box3D/Box3D Mesh Collider")]
    public sealed class Box3DMeshCollider : Box3DCollider
    {
        [Tooltip("Source mesh. Defaults to the MeshFilter mesh on this GameObject.")]
        public Mesh mesh;

        [Tooltip("Weld nearby vertices so edge collision is smooth across triangles.")]
        public bool weldVertices = true;

        IntPtr _meshData;

        protected override B3ShapeId CreateNativeShape(B3BodyId bodyId, ref B3ShapeDef def)
        {
            Mesh source = ResolveMesh();
            if (source == null)
            {
                Debug.LogError("[Box3D] Box3DMeshCollider has no mesh. Assign one or add a MeshFilter.", this);
                return default;
            }

            if (Body != null && Body.bodyType == Box3DBodyType.Dynamic)
            {
                Debug.LogWarning("[Box3D] Mesh colliders only generate contacts on static bodies. " +
                                 "Use Box3DConvexCollider for dynamic bodies.", this);
            }

            GetBodyLocalPose(out Vector3 localPosition, out Quaternion localRotation, out Vector3 scale);

            _meshData = B3MeshCache.Acquire(source, scale, localPosition, localRotation, weldVertices);
            if (_meshData == IntPtr.Zero)
            {
                return default;
            }

            // Scale and pose are baked into the cached vertices.
            return B3Api.b3CreateMeshShape(bodyId, ref def, _meshData, Vector3.one);
        }

        protected override void OnShapeDestroyed()
        {
            if (_meshData != IntPtr.Zero)
            {
                B3MeshCache.Release(_meshData);
                _meshData = IntPtr.Zero;
            }
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

            Gizmos.color = isTrigger ? new Color(1.0f, 0.7f, 0.0f, 0.5f) : new Color(0.4f, 0.75f, 0.95f, 0.5f);
            Gizmos.DrawWireMesh(source, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
