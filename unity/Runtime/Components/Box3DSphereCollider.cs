// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>Sphere collision shape.</summary>
    [AddComponentMenu("Box3D/Box3D Sphere Collider")]
    [HelpURL("https://github.com/timskap/box3d-unity/blob/main/unity/README.md#box3dspherecollider")]
    public sealed class Box3DSphereCollider : Box3DCollider
    {
        [Tooltip("Local center of the sphere.")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Radius before transform scale.")]
        [Min(0.001f)] public float radius = 0.5f;

        Vector3 _builtCenter;
        float _builtRadius;

        protected override bool GeometryOutOfDate =>
            base.GeometryOutOfDate || center != _builtCenter || radius != _builtRadius;

        void Reset()
        {
            FitToRenderBounds();
        }

        /// <summary>Size the sphere from the render mesh on this GameObject, like Unity's SphereCollider.</summary>
        [ContextMenu("Fit to Render Bounds")]
        public void FitToRenderBounds()
        {
            if (!TryGetLocalRenderBounds(out Bounds bounds))
            {
                return;
            }

            center = bounds.center;
            Vector3 extents = bounds.extents;
            radius = Mathf.Max(extents.x, extents.y, extents.z, 0.001f);

            if (Application.isPlaying && IsCreated)
            {
                Rebuild();
            }
        }

        protected override B3ShapeId CreateNativeShape(B3BodyId bodyId, ref B3ShapeDef def)
        {
            _builtCenter = center;
            _builtRadius = radius;
            GetBodyLocalPose(out Vector3 localPosition, out Quaternion localRotation, out Vector3 scale);

            float worldRadius = radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var sphere = new B3Sphere
            {
                center = localPosition + localRotation * Vector3.Scale(center, scale),
                radius = Mathf.Max(worldRadius, 0.001f),
            };

            return B3Api.b3CreateSphereShape(bodyId, ref def, ref sphere);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 scale = transform.lossyScale;
            float worldRadius = radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Gizmos.color = isTrigger ? new Color(1.0f, 0.7f, 0.0f, 0.8f) : new Color(0.55f, 0.95f, 0.55f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + transform.rotation * Vector3.Scale(center, scale), worldRadius);
        }
    }
}
