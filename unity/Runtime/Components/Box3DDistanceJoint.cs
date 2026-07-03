// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Distance joint: keeps a point on this body at a distance from a point on the
    /// connected body. Rigid by default; enable the spring for rope/bungee behavior.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Distance Joint")]
    public sealed class Box3DDistanceJoint : Box3DJoint
    {
        [Tooltip("Anchor on the connected body, in its local space (world space when connected to the world).")]
        public Vector3 connectedAnchor = Vector3.zero;

        [Tooltip("Compute the rest length from the anchor positions at creation.")]
        public bool autoConfigureLength = true;

        [Tooltip("Rest length in meters (used when auto configure is off).")]
        [Min(0.001f)] public float length = 1.0f;

        [Header("Spring")]
        [Tooltip("When enabled the joint stretches like a spring; otherwise it is a rigid rod.")]
        public bool useSpring;

        [Min(0.0f)] public float springHertz = 5.0f;
        [Min(0.0f)] public float springDampingRatio = 0.7f;

        [Header("Limits (spring mode only)")]
        public bool useLimits;

        [Min(0.0f)] public float minLength = 0.5f;
        [Min(0.0f)] public float maxLength = 2.0f;

        /// <summary>Current distance between the anchors, in meters.</summary>
        public float currentLength => IsCreated ? B3Api.b3DistanceJoint_GetCurrentLength(JointId) : 0.0f;

        /// <summary>World position of the connected anchor.</summary>
        public Vector3 ConnectedWorldAnchor => connectedBody != null
            ? connectedBody.transform.TransformPoint(connectedAnchor)
            : connectedAnchor;

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3DistanceJointDef def = B3Api.b3DefaultDistanceJointDef();
            FillBase(ref def.@base);

            // Distance joints use two distinct anchor points; override the base frames.
            Vector3 worldAnchorB = WorldAnchor;
            Vector3 worldAnchorA = ConnectedWorldAnchor;
            def.@base.localFrameA = WorldToBodyFrame(connectedBody, worldAnchorA, Quaternion.identity);
            def.@base.localFrameB = WorldToBodyFrame(OwnBody, worldAnchorB, Quaternion.identity);

            float restLength = autoConfigureLength ? Vector3.Distance(worldAnchorA, worldAnchorB) : length;
            def.length = Mathf.Max(restLength, 0.001f);
            length = def.length;

            def.enableSpring = (byte)(useSpring ? 1 : 0);
            def.hertz = springHertz;
            def.dampingRatio = springDampingRatio;
            def.enableLimit = (byte)(useLimits ? 1 : 0);
            def.minLength = Mathf.Min(minLength, maxLength);
            def.maxLength = Mathf.Max(minLength, maxLength);

            return B3Api.b3CreateDistanceJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3DistanceJoint_SetLength(JointId, Mathf.Max(length, 0.001f));
            B3Api.b3DistanceJoint_EnableSpring(JointId, useSpring);
            B3Api.b3DistanceJoint_SetSpringHertz(JointId, springHertz);
            B3Api.b3DistanceJoint_SetSpringDampingRatio(JointId, springDampingRatio);
            B3Api.b3DistanceJoint_EnableLimit(JointId, useLimits);
            B3Api.b3DistanceJoint_SetLengthRange(JointId, Mathf.Min(minLength, maxLength), Mathf.Max(minLength, maxLength));
            B3Api.b3Joint_WakeBodies(JointId);
        }

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyProperties();
            }
        }
    }
}
