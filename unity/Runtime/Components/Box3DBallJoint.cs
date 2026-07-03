// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Ball-and-socket (spherical) joint: bodies share a point and rotate freely around it,
    /// optionally limited by a cone and twist range. Good for ragdolls and chains.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Ball Joint")]
    public sealed class Box3DBallJoint : Box3DJoint
    {
        [Tooltip("Cone/twist axis in this GameObject's local space.")]
        public Vector3 axis = Vector3.forward;

        [Header("Cone Limit")]
        public bool useConeLimit;

        [Tooltip("Half angle of the allowed cone in degrees.")]
        [Range(0.0f, 180.0f)] public float coneAngle = 45.0f;

        [Header("Twist Limit")]
        public bool useTwistLimit;

        [Range(-178.0f, 178.0f)] public float minTwist = -30.0f;
        [Range(-178.0f, 178.0f)] public float maxTwist = 30.0f;

        [Header("Spring")]
        [Tooltip("Spring that drives the joint back to its rest rotation.")]
        public bool useSpring;

        [Min(0.0f)] public float springHertz = 5.0f;
        [Min(0.0f)] public float springDampingRatio = 0.7f;

        [Header("Motor")]
        public bool useMotor;

        [Tooltip("Target angular velocity in degrees per second (frame A space).")]
        public Vector3 motorVelocity;

        [Min(0.0f)] public float maxMotorTorque = 100.0f;

        /// <summary>Current cone angle in degrees.</summary>
        public float currentConeAngle => IsCreated ? B3Api.b3SphericalJoint_GetConeAngle(JointId) * Mathf.Rad2Deg : 0.0f;

        /// <summary>Current twist angle in degrees.</summary>
        public float currentTwistAngle => IsCreated ? B3Api.b3SphericalJoint_GetTwistAngle(JointId) * Mathf.Rad2Deg : 0.0f;

        protected override Quaternion WorldFrameRotation()
        {
            // The cone limit is centered on the frame A z axis.
            return BasisWithZ(transform.TransformDirection(axis));
        }

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3SphericalJointDef def = B3Api.b3DefaultSphericalJointDef();
            FillBase(ref def.@base);

            def.enableConeLimit = (byte)(useConeLimit ? 1 : 0);
            def.coneAngle = coneAngle * Mathf.Deg2Rad;
            def.enableTwistLimit = (byte)(useTwistLimit ? 1 : 0);
            def.lowerTwistAngle = minTwist * Mathf.Deg2Rad;
            def.upperTwistAngle = maxTwist * Mathf.Deg2Rad;
            def.enableSpring = (byte)(useSpring ? 1 : 0);
            def.hertz = springHertz;
            def.dampingRatio = springDampingRatio;
            def.enableMotor = (byte)(useMotor ? 1 : 0);
            def.motorVelocity = motorVelocity * Mathf.Deg2Rad;
            def.maxMotorTorque = maxMotorTorque;

            return B3Api.b3CreateSphericalJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3SphericalJoint_EnableConeLimit(JointId, useConeLimit);
            B3Api.b3SphericalJoint_SetConeLimit(JointId, coneAngle * Mathf.Deg2Rad);
            B3Api.b3SphericalJoint_EnableTwistLimit(JointId, useTwistLimit);
            B3Api.b3SphericalJoint_SetTwistLimits(JointId, minTwist * Mathf.Deg2Rad, maxTwist * Mathf.Deg2Rad);
            B3Api.b3SphericalJoint_EnableSpring(JointId, useSpring);
            B3Api.b3SphericalJoint_SetSpringHertz(JointId, springHertz);
            B3Api.b3SphericalJoint_SetSpringDampingRatio(JointId, springDampingRatio);
            B3Api.b3SphericalJoint_EnableMotor(JointId, useMotor);
            B3Api.b3SphericalJoint_SetMotorVelocity(JointId, motorVelocity * Mathf.Deg2Rad);
            B3Api.b3SphericalJoint_SetMaxMotorTorque(JointId, maxMotorTorque);
            B3Api.b3Joint_WakeBodies(JointId);
        }

        void OnValidate()
        {
            maxTwist = Mathf.Max(minTwist, maxTwist);
            if (Application.isPlaying)
            {
                ApplyProperties();
            }
        }
    }
}
