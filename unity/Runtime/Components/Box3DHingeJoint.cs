// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Hinge (revolute) joint: allows rotation about a single axis through the anchor.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Hinge Joint")]
    [HelpURL("https://github.com/timskap/box3d-unity/blob/main/unity/README.md#box3dhingejoint")]
    public sealed class Box3DHingeJoint : Box3DJoint
    {
        [Tooltip("Hinge axis in this GameObject's local space.")]
        public Vector3 axis = Vector3.right;

        [Header("Limits")]
        public bool useLimits;

        [Tooltip("Lower angle limit in degrees (min -178).")]
        [Range(-178.0f, 178.0f)] public float minAngle = -45.0f;

        [Tooltip("Upper angle limit in degrees (max 178).")]
        [Range(-178.0f, 178.0f)] public float maxAngle = 45.0f;

        [Header("Motor")]
        public bool useMotor;

        [Tooltip("Target angular speed in degrees per second.")]
        public float motorSpeed = 90.0f;

        [Tooltip("Maximum motor torque in N*m.")]
        [Min(0.0f)] public float maxMotorTorque = 100.0f;

        [Header("Spring")]
        public bool useSpring;

        [Tooltip("Spring target angle in degrees.")]
        public float springTargetAngle;

        [Tooltip("Spring stiffness in Hz. Keep below a quarter of the physics rate.")]
        [Min(0.0f)] public float springHertz = 5.0f;

        [Tooltip("Spring damping ratio. 1 = critical damping.")]
        [Min(0.0f)] public float springDampingRatio = 0.7f;

        /// <summary>Current hinge angle in degrees.</summary>
        public float angle => IsCreated ? B3Api.b3RevoluteJoint_GetAngle(JointId) * Mathf.Rad2Deg : 0.0f;

        /// <summary>Current motor torque in N*m.</summary>
        public float motorTorque => IsCreated ? B3Api.b3RevoluteJoint_GetMotorTorque(JointId) : 0.0f;

        protected override Quaternion WorldFrameRotation()
        {
            // Revolute joints rotate about the frame z axis.
            return BasisWithZ(transform.TransformDirection(axis));
        }

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3RevoluteJointDef def = B3Api.b3DefaultRevoluteJointDef();
            FillBase(ref def.@base);

            def.enableLimit = (byte)(useLimits ? 1 : 0);
            def.lowerAngle = minAngle * Mathf.Deg2Rad;
            def.upperAngle = maxAngle * Mathf.Deg2Rad;
            def.enableMotor = (byte)(useMotor ? 1 : 0);
            def.motorSpeed = motorSpeed * Mathf.Deg2Rad;
            def.maxMotorTorque = maxMotorTorque;
            def.enableSpring = (byte)(useSpring ? 1 : 0);
            def.targetAngle = springTargetAngle * Mathf.Deg2Rad;
            def.hertz = springHertz;
            def.dampingRatio = springDampingRatio;

            return B3Api.b3CreateRevoluteJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3RevoluteJoint_EnableLimit(JointId, useLimits);
            B3Api.b3RevoluteJoint_SetLimits(JointId, minAngle * Mathf.Deg2Rad, maxAngle * Mathf.Deg2Rad);
            B3Api.b3RevoluteJoint_EnableMotor(JointId, useMotor);
            B3Api.b3RevoluteJoint_SetMotorSpeed(JointId, motorSpeed * Mathf.Deg2Rad);
            B3Api.b3RevoluteJoint_SetMaxMotorTorque(JointId, maxMotorTorque);
            B3Api.b3RevoluteJoint_EnableSpring(JointId, useSpring);
            B3Api.b3RevoluteJoint_SetTargetAngle(JointId, springTargetAngle * Mathf.Deg2Rad);
            B3Api.b3RevoluteJoint_SetSpringHertz(JointId, springHertz);
            B3Api.b3RevoluteJoint_SetSpringDampingRatio(JointId, springDampingRatio);
            B3Api.b3Joint_WakeBodies(JointId);
        }

        void OnValidate()
        {
            maxAngle = Mathf.Max(minAngle, maxAngle);
            if (Application.isPlaying)
            {
                ApplyProperties();
            }
        }

        protected override void DrawJointGizmos(Vector3 worldAnchor)
        {
            DrawAxisGizmo(worldAnchor, axis, new Color(0.3f, 0.8f, 1.0f, 0.9f));
            if (useLimits)
            {
                DrawLimitArcGizmo(worldAnchor, transform.TransformDirection(axis), minAngle, maxAngle,
                    new Color(0.3f, 0.8f, 1.0f, 0.5f));
            }
        }
    }
}
