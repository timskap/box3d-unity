// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>Capsule collision shape (a segment with radius).</summary>
    [AddComponentMenu("Box3D/Box3D Capsule Collider")]
    public sealed class Box3DCapsuleCollider : Box3DCollider
    {
        public enum Axis
        {
            X = 0,
            Y = 1,
            Z = 2,
        }

        [Tooltip("Local center of the capsule.")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Radius before transform scale.")]
        [Min(0.001f)] public float radius = 0.5f;

        [Tooltip("Total height including the hemispherical caps, before transform scale.")]
        [Min(0.002f)] public float height = 2.0f;

        [Tooltip("Local axis the capsule extends along.")]
        public Axis direction = Axis.Y;

        protected override B3ShapeId CreateNativeShape(B3BodyId bodyId, ref B3ShapeDef def)
        {
            GetBodyLocalPose(out Vector3 localPosition, out Quaternion localRotation, out Vector3 scale);
            ComputeEndpoints(scale, out Vector3 p1, out Vector3 p2, out float worldRadius);

            var capsule = new B3Capsule
            {
                center1 = localPosition + localRotation * p1,
                center2 = localPosition + localRotation * p2,
                radius = worldRadius,
            };

            return B3Api.b3CreateCapsuleShape(bodyId, ref def, ref capsule);
        }

        void ComputeEndpoints(Vector3 scale, out Vector3 p1, out Vector3 p2, out float worldRadius)
        {
            Vector3 axis = direction switch
            {
                Axis.X => Vector3.right,
                Axis.Z => Vector3.forward,
                _ => Vector3.up,
            };

            float axisScale = direction switch
            {
                Axis.X => Mathf.Abs(scale.x),
                Axis.Z => Mathf.Abs(scale.z),
                _ => Mathf.Abs(scale.y),
            };

            worldRadius = direction switch
            {
                Axis.X => radius * MaxAbs(scale.y, scale.z),
                Axis.Z => radius * MaxAbs(scale.x, scale.y),
                _ => radius * MaxAbs(scale.x, scale.z),
            };
            worldRadius = Mathf.Max(worldRadius, 0.001f);

            // Segment half length. Box3D requires a minimum segment length, so a capsule
            // never fully degenerates to a sphere.
            float halfSegment = Mathf.Max(0.5f * height * axisScale - worldRadius, 0.003f);

            Vector3 scaledCenter = Vector3.Scale(center, scale);
            p1 = scaledCenter - axis * halfSegment;
            p2 = scaledCenter + axis * halfSegment;
        }

        void OnDrawGizmosSelected()
        {
            ComputeEndpoints(transform.lossyScale, out Vector3 p1, out Vector3 p2, out float worldRadius);
            Vector3 w1 = transform.position + transform.rotation * p1;
            Vector3 w2 = transform.position + transform.rotation * p2;

            Gizmos.color = isTrigger ? new Color(1.0f, 0.7f, 0.0f, 0.8f) : new Color(0.55f, 0.95f, 0.55f, 0.8f);
            Gizmos.DrawWireSphere(w1, worldRadius);
            Gizmos.DrawWireSphere(w2, worldRadius);

            Vector3 axisDir = (w2 - w1).sqrMagnitude > 1e-12f ? (w2 - w1).normalized : Vector3.up;
            Vector3 side = Vector3.Cross(axisDir, Mathf.Abs(axisDir.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
            Vector3 forward = Vector3.Cross(axisDir, side);
            Gizmos.DrawLine(w1 + side * worldRadius, w2 + side * worldRadius);
            Gizmos.DrawLine(w1 - side * worldRadius, w2 - side * worldRadius);
            Gizmos.DrawLine(w1 + forward * worldRadius, w2 + forward * worldRadius);
            Gizmos.DrawLine(w1 - forward * worldRadius, w2 - forward * worldRadius);
        }
    }
}
