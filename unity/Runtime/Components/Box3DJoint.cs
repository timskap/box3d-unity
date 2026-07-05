// SPDX-License-Identifier: MIT

using System;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Base class for Box3D joints. A joint connects the Box3DBody on this GameObject
    /// (body B) to another body (body A), or to the static world when Connected Body
    /// is null. Joint frames are computed once at creation from the current poses.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    [RequireComponent(typeof(Box3DBody))]
    [HelpURL("https://github.com/timskap/box3d-unity/blob/main/unity/README.md#joints")]
    public abstract class Box3DJoint : MonoBehaviour
    {
        [Tooltip("The body this joint connects to. None = anchored to the world.")]
        public Box3DBody connectedBody;

        [Tooltip("Anchor point in this GameObject's local space.")]
        public Vector3 anchor = Vector3.zero;

        [Tooltip("Should the two connected bodies collide with each other?")]
        public bool collideConnected;

        [Header("Breaking")]
        [Tooltip("The joint breaks when its reaction force exceeds this value (N). Infinity = unbreakable.")]
        public float breakForce = Mathf.Infinity;

        [Tooltip("The joint breaks when its reaction torque exceeds this value (N*m). Infinity = unbreakable.")]
        public float breakTorque = Mathf.Infinity;

        /// <summary>Raised right before the joint destroys itself due to break force/torque.</summary>
        public event Action<Box3DJoint> Broke;

        /// <summary>Native joint id.</summary>
        public B3JointId JointId => _jointId;

        /// <summary>True while the native joint exists.</summary>
        public bool IsCreated => !_jointId.IsNull && B3Api.b3Joint_IsValid(_jointId);

        /// <summary>Current constraint force (N).</summary>
        public Vector3 reactionForce => IsCreated ? B3Api.b3Joint_GetConstraintForce(_jointId) : Vector3.zero;

        /// <summary>Current constraint torque (N*m).</summary>
        public Vector3 reactionTorque => IsCreated ? B3Api.b3Joint_GetConstraintTorque(_jointId) : Vector3.zero;

        protected Box3DBody OwnBody { get; private set; }
        protected Box3DWorld World { get; private set; }

        B3JointId _jointId;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        void OnEnable()
        {
            TryCreate();
        }

        void Start()
        {
            // Second chance: bodies enabled later in the same frame are ready by now.
            TryCreate();
        }

        void OnDisable()
        {
            DestroyJoint(true);
        }

        void TryCreate()
        {
            if (IsCreated || !isActiveAndEnabled)
            {
                return;
            }

            OwnBody = GetComponent<Box3DBody>();
            if (OwnBody == null || !OwnBody.EnsureCreated())
            {
                return;
            }

            if (connectedBody != null && !connectedBody.EnsureCreated())
            {
                return;
            }

            if (connectedBody == OwnBody)
            {
                Debug.LogError("[Box3D] A joint cannot connect a body to itself.", this);
                return;
            }

            World = OwnBody.World;

            _jointId = CreateNativeJoint(World.WorldId);
            if (_jointId.IsNull)
            {
                Debug.LogError($"[Box3D] Failed to create {GetType().Name}.", this);
                return;
            }

            World.RegisterJoint(_jointId, this);
        }

        void DestroyJoint(bool wakeBodies)
        {
            if (_jointId.IsNull)
            {
                return;
            }

            World?.UnregisterJoint(_jointId);
            if (B3Api.b3Joint_IsValid(_jointId))
            {
                B3Api.b3DestroyJoint(_jointId, wakeBodies);
            }

            _jointId = default;
        }

        internal void NotifyWorldDestroyed()
        {
            _jointId = default;
            World = null;
        }

        /// <summary>Called when a required body becomes available after this joint failed to create.</summary>
        internal void RetryCreate()
        {
            if (!IsCreated && isActiveAndEnabled)
            {
                TryCreate();
            }
        }

        internal void HandleForceThresholdExceeded()
        {
            if (float.IsInfinity(breakForce) && float.IsInfinity(breakTorque))
            {
                return;
            }

            Broke?.Invoke(this);
            DestroyJoint(true);
            Destroy(this);
        }

        /// <summary>Create the typed native joint. Implemented per joint type.</summary>
        protected abstract B3JointId CreateNativeJoint(B3WorldId worldId);

        // ------------------------------------------------------------------
        // Frame helpers
        // ------------------------------------------------------------------

        /// <summary>Body A of the native joint: the connected body or the world's ground body.</summary>
        protected B3BodyId BodyIdA => connectedBody != null ? connectedBody.BodyId : World.GroundBodyId;

        /// <summary>Body B of the native joint: the body on this GameObject.</summary>
        protected B3BodyId BodyIdB => OwnBody.BodyId;

        /// <summary>World-space anchor point.</summary>
        protected Vector3 WorldAnchor => transform.TransformPoint(anchor);

        /// <summary>
        /// World rotation of the joint frame. Subclasses override to align the frame
        /// with their axis convention (revolute rotates about local z, prismatic slides
        /// along local x, and so on).
        /// </summary>
        protected virtual Quaternion WorldFrameRotation()
        {
            return transform.rotation;
        }

        /// <summary>Fill the shared part of a joint definition: bodies, frames, thresholds.</summary>
        protected void FillBase(ref B3JointDef def)
        {
            Vector3 worldAnchor = WorldAnchor;
            Quaternion worldRotation = WorldFrameRotation();

            def.bodyIdA = BodyIdA;
            def.bodyIdB = BodyIdB;
            def.localFrameA = WorldToBodyFrame(connectedBody, worldAnchor, worldRotation);
            def.localFrameB = WorldToBodyFrame(OwnBody, worldAnchor, worldRotation);
            def.collideConnected = (byte)(collideConnected ? 1 : 0);

            if (!float.IsInfinity(breakForce))
            {
                def.forceThreshold = breakForce;
            }

            if (!float.IsInfinity(breakTorque))
            {
                def.torqueThreshold = breakTorque;
            }
        }

        /// <summary>Convert a world pose into a body's local joint frame. Null body = world ground (identity pose).</summary>
        protected static B3Transform WorldToBodyFrame(Box3DBody body, Vector3 worldPoint, Quaternion worldRotation)
        {
            if (body == null)
            {
                return new B3Transform(worldPoint, worldRotation);
            }

            Transform t = body.transform;
            Quaternion inv = Quaternion.Inverse(t.rotation);
            return new B3Transform(inv * (worldPoint - t.position), inv * worldRotation);
        }

        /// <summary>Build a rotation whose local z axis equals the given world axis.</summary>
        protected static Quaternion BasisWithZ(Vector3 worldAxis)
        {
            Vector3 z = worldAxis.sqrMagnitude > 1e-10f ? worldAxis.normalized : Vector3.forward;
            Vector3 up = Mathf.Abs(z.y) < 0.99f ? Vector3.up : Vector3.right;
            return Quaternion.LookRotation(z, up);
        }

        /// <summary>Build a rotation whose local x axis equals the given world axis.</summary>
        protected static Quaternion BasisWithX(Vector3 worldAxis)
        {
            Vector3 x = worldAxis.sqrMagnitude > 1e-10f ? worldAxis.normalized : Vector3.right;
            Vector3 forward = Vector3.Cross(x, Mathf.Abs(x.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
            Vector3 up = Vector3.Cross(forward, x);
            return Quaternion.LookRotation(forward, up);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.6f, 0.1f, 0.9f);
            Vector3 worldAnchor = transform.TransformPoint(anchor);
            Gizmos.DrawWireSphere(worldAnchor, 0.05f);
            if (connectedBody != null)
            {
                Gizmos.DrawLine(worldAnchor, connectedBody.transform.position);
            }

            DrawJointGizmos(worldAnchor);
        }

        /// <summary>Extra gizmos per joint type (axes, limits), drawn while selected.</summary>
        protected virtual void DrawJointGizmos(Vector3 worldAnchor)
        {
        }

        /// <summary>Draw a joint axis as a line through the anchor.</summary>
        protected void DrawAxisGizmo(Vector3 worldAnchor, Vector3 localAxis, Color color, float halfLength = 0.5f)
        {
            Vector3 dir = transform.TransformDirection(localAxis);
            if (dir.sqrMagnitude < 1e-10f)
            {
                return;
            }

            dir.Normalize();
            Gizmos.color = color;
            Gizmos.DrawLine(worldAnchor - dir * halfLength, worldAnchor + dir * halfLength);
        }

        /// <summary>Draw an angular limit arc around a world axis through the anchor.</summary>
        protected static void DrawLimitArcGizmo(Vector3 worldAnchor, Vector3 worldAxis, float minDegrees,
            float maxDegrees, Color color, float radius = 0.25f)
        {
            if (worldAxis.sqrMagnitude < 1e-10f)
            {
                return;
            }

            Vector3 axis = worldAxis.normalized;
            Vector3 reference = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.99f ? Vector3.up : Vector3.right).normalized;

            Gizmos.color = color;
            const int segments = 24;
            Vector3 previous = worldAnchor + Quaternion.AngleAxis(minDegrees, axis) * reference * radius;
            Gizmos.DrawLine(worldAnchor, previous);
            for (int i = 1; i <= segments; ++i)
            {
                float angle = Mathf.Lerp(minDegrees, maxDegrees, i / (float)segments);
                Vector3 current = worldAnchor + Quaternion.AngleAxis(angle, axis) * reference * radius;
                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            Gizmos.DrawLine(worldAnchor, previous);
        }
    }
}
