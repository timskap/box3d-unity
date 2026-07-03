// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Prismatic (slider) joint: this body slides along an axis fixed on the connected
    /// body, with no relative rotation. Pistons, elevators, sliding doors.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Prismatic Joint")]
    public sealed class Box3DPrismaticJoint : Box3DJoint
    {
        [Tooltip("Slide axis in this GameObject's local space.")]
        public Vector3 axis = Vector3.up;

        [Header("Limits")]
        public bool useLimits;

        [Tooltip("Lower translation limit in meters.")]
        public float minTranslation = -1.0f;

        [Tooltip("Upper translation limit in meters.")]
        public float maxTranslation = 1.0f;

        [Header("Motor")]
        public bool useMotor;

        [Tooltip("Target speed in meters per second.")]
        public float motorSpeed = 1.0f;

        [Tooltip("Maximum motor force in N.")]
        [Min(0.0f)] public float maxMotorForce = 100.0f;

        [Header("Spring")]
        public bool useSpring;

        [Tooltip("Translation the spring drives toward, in meters.")]
        public float springTargetTranslation;

        [Min(0.0f)] public float springHertz = 5.0f;
        [Min(0.0f)] public float springDampingRatio = 0.7f;

        /// <summary>Current translation along the axis in meters.</summary>
        public float translation => IsCreated ? B3Api.b3PrismaticJoint_GetTranslation(JointId) : 0.0f;

        /// <summary>Current translation speed in m/s.</summary>
        public float speed => IsCreated ? B3Api.b3PrismaticJoint_GetSpeed(JointId) : 0.0f;

        protected override Quaternion WorldFrameRotation()
        {
            // Prismatic joints slide along the frame A x axis.
            return BasisWithX(transform.TransformDirection(axis));
        }

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3PrismaticJointDef def = B3Api.b3DefaultPrismaticJointDef();
            FillBase(ref def.@base);

            def.enableLimit = (byte)(useLimits ? 1 : 0);
            def.lowerTranslation = Mathf.Min(minTranslation, maxTranslation);
            def.upperTranslation = Mathf.Max(minTranslation, maxTranslation);
            def.enableMotor = (byte)(useMotor ? 1 : 0);
            def.motorSpeed = motorSpeed;
            def.maxMotorForce = maxMotorForce;
            def.enableSpring = (byte)(useSpring ? 1 : 0);
            def.targetTranslation = springTargetTranslation;
            def.hertz = springHertz;
            def.dampingRatio = springDampingRatio;

            return B3Api.b3CreatePrismaticJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3PrismaticJoint_EnableLimit(JointId, useLimits);
            B3Api.b3PrismaticJoint_SetLimits(JointId, Mathf.Min(minTranslation, maxTranslation), Mathf.Max(minTranslation, maxTranslation));
            B3Api.b3PrismaticJoint_EnableMotor(JointId, useMotor);
            B3Api.b3PrismaticJoint_SetMotorSpeed(JointId, motorSpeed);
            B3Api.b3PrismaticJoint_SetMaxMotorForce(JointId, maxMotorForce);
            B3Api.b3PrismaticJoint_EnableSpring(JointId, useSpring);
            B3Api.b3PrismaticJoint_SetTargetTranslation(JointId, springTargetTranslation);
            B3Api.b3PrismaticJoint_SetSpringHertz(JointId, springHertz);
            B3Api.b3PrismaticJoint_SetSpringDampingRatio(JointId, springDampingRatio);
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
