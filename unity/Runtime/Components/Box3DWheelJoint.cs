// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Wheel joint for vehicles. Put this on the wheel body and connect it to the
    /// chassis. The wheel spins about its spin axis, travels along the chassis
    /// suspension axis, and can optionally steer around the suspension axis.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Wheel Joint")]
    [HelpURL("https://github.com/timskap/box3d-unity/blob/main/unity/README.md#box3dwheeljoint")]
    public sealed class Box3DWheelJoint : Box3DJoint
    {
        [Tooltip("Wheel spin axis in this GameObject's local space (usually the lateral axis).")]
        public Vector3 spinAxis = Vector3.right;

        [Tooltip("Suspension travel axis in world terms at setup (usually up).")]
        public Vector3 suspensionAxis = Vector3.up;

        [Header("Suspension")]
        public bool useSuspension = true;

        [Tooltip("Suspension stiffness in Hz. Cars are usually 1-4 Hz.")]
        [Min(0.0f)] public float suspensionHertz = 2.0f;

        [Min(0.0f)] public float suspensionDampingRatio = 0.7f;

        public bool useSuspensionLimits = true;

        [Tooltip("Lowest suspension travel in meters (relative to setup pose).")]
        public float minSuspension = -0.25f;

        [Tooltip("Highest suspension travel in meters.")]
        public float maxSuspension = 0.25f;

        [Header("Drive Motor")]
        public bool useMotor;

        [Tooltip("Target spin speed in degrees per second.")]
        public float motorSpeed;

        [Tooltip("Maximum drive torque in N*m.")]
        [Min(0.0f)] public float maxMotorTorque = 50.0f;

        [Header("Steering")]
        public bool useSteering;

        [Tooltip("Steering target angle in degrees.")]
        public float steeringAngle;

        [Min(0.0f)] public float steeringHertz = 10.0f;
        [Min(0.0f)] public float steeringDampingRatio = 1.0f;

        [Tooltip("Maximum steering torque in N*m.")]
        [Min(0.0f)] public float maxSteeringTorque = 200.0f;

        public bool useSteeringLimits = true;

        [Range(-90.0f, 90.0f)] public float minSteeringAngle = -35.0f;
        [Range(-90.0f, 90.0f)] public float maxSteeringAngle = 35.0f;

        /// <summary>Current wheel spin speed in degrees per second.</summary>
        public float spinSpeed => IsCreated ? B3Api.b3WheelJoint_GetSpinSpeed(JointId) * Mathf.Rad2Deg : 0.0f;

        /// <summary>Current steering angle in degrees.</summary>
        public float currentSteeringAngle => IsCreated ? B3Api.b3WheelJoint_GetSteeringAngle(JointId) * Mathf.Rad2Deg : 0.0f;

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3WheelJointDef def = B3Api.b3DefaultWheelJointDef();
            FillBase(ref def.@base);

            // Frame A (chassis): x axis = suspension/steering axis.
            // Frame B (wheel): z axis = spin axis. Both frames sit at the anchor.
            Vector3 worldAnchor = WorldAnchor;
            Quaternion frameARotation = BasisWithX(transform.TransformDirection(suspensionAxis));
            Quaternion frameBRotation = BasisWithZ(transform.TransformDirection(spinAxis));
            def.@base.localFrameA = WorldToBodyFrame(connectedBody, worldAnchor, frameARotation);
            def.@base.localFrameB = WorldToBodyFrame(OwnBody, worldAnchor, frameBRotation);

            def.enableSuspensionSpring = (byte)(useSuspension ? 1 : 0);
            def.suspensionHertz = suspensionHertz;
            def.suspensionDampingRatio = suspensionDampingRatio;
            def.enableSuspensionLimit = (byte)(useSuspensionLimits ? 1 : 0);
            def.lowerSuspensionLimit = Mathf.Min(minSuspension, maxSuspension);
            def.upperSuspensionLimit = Mathf.Max(minSuspension, maxSuspension);
            def.enableSpinMotor = (byte)(useMotor ? 1 : 0);
            def.spinSpeed = motorSpeed * Mathf.Deg2Rad;
            def.maxSpinTorque = maxMotorTorque;
            def.enableSteering = (byte)(useSteering ? 1 : 0);
            def.targetSteeringAngle = steeringAngle * Mathf.Deg2Rad;
            def.steeringHertz = steeringHertz;
            def.steeringDampingRatio = steeringDampingRatio;
            def.maxSteeringTorque = maxSteeringTorque;
            def.enableSteeringLimit = (byte)(useSteeringLimits ? 1 : 0);
            def.lowerSteeringLimit = Mathf.Min(minSteeringAngle, maxSteeringAngle) * Mathf.Deg2Rad;
            def.upperSteeringLimit = Mathf.Max(minSteeringAngle, maxSteeringAngle) * Mathf.Deg2Rad;

            return B3Api.b3CreateWheelJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint. Call after changing motor speed or steering.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3WheelJoint_EnableSuspension(JointId, useSuspension);
            B3Api.b3WheelJoint_SetSuspensionHertz(JointId, suspensionHertz);
            B3Api.b3WheelJoint_SetSuspensionDampingRatio(JointId, suspensionDampingRatio);
            B3Api.b3WheelJoint_EnableSuspensionLimit(JointId, useSuspensionLimits);
            B3Api.b3WheelJoint_SetSuspensionLimits(JointId, Mathf.Min(minSuspension, maxSuspension), Mathf.Max(minSuspension, maxSuspension));
            B3Api.b3WheelJoint_EnableSpinMotor(JointId, useMotor);
            B3Api.b3WheelJoint_SetSpinMotorSpeed(JointId, motorSpeed * Mathf.Deg2Rad);
            B3Api.b3WheelJoint_SetMaxSpinTorque(JointId, maxMotorTorque);
            B3Api.b3WheelJoint_EnableSteering(JointId, useSteering);
            B3Api.b3WheelJoint_SetTargetSteeringAngle(JointId, steeringAngle * Mathf.Deg2Rad);
            B3Api.b3WheelJoint_SetSteeringHertz(JointId, steeringHertz);
            B3Api.b3WheelJoint_SetSteeringDampingRatio(JointId, steeringDampingRatio);
            B3Api.b3WheelJoint_SetMaxSteeringTorque(JointId, maxSteeringTorque);
            B3Api.b3WheelJoint_EnableSteeringLimit(JointId, useSteeringLimits);
            B3Api.b3WheelJoint_SetSteeringLimits(JointId,
                Mathf.Min(minSteeringAngle, maxSteeringAngle) * Mathf.Deg2Rad,
                Mathf.Max(minSteeringAngle, maxSteeringAngle) * Mathf.Deg2Rad);
            B3Api.b3Joint_WakeBodies(JointId);
        }

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyProperties();
            }
        }

        protected override void DrawJointGizmos(Vector3 worldAnchor)
        {
            // Spin axis in cyan, suspension travel in green.
            DrawAxisGizmo(worldAnchor, spinAxis, new Color(0.3f, 0.8f, 1.0f, 0.9f), 0.4f);

            Vector3 dir = transform.TransformDirection(suspensionAxis);
            if (dir.sqrMagnitude < 1e-10f)
            {
                return;
            }

            dir.Normalize();
            Gizmos.color = new Color(0.4f, 1.0f, 0.4f, 0.9f);
            if (useSuspensionLimits)
            {
                Vector3 a = worldAnchor + dir * Mathf.Min(minSuspension, maxSuspension);
                Vector3 b = worldAnchor + dir * Mathf.Max(minSuspension, maxSuspension);
                Gizmos.DrawLine(a, b);
                Gizmos.DrawWireCube(a, Vector3.one * 0.04f);
                Gizmos.DrawWireCube(b, Vector3.one * 0.04f);
            }
            else
            {
                Gizmos.DrawLine(worldAnchor - dir * 0.4f, worldAnchor + dir * 0.4f);
            }
        }
    }
}
