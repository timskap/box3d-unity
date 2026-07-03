// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Motor joint: drives a body's relative velocity and/or pose with force and torque
    /// limits, while staying responsive to collisions. Useful for animated platforms,
    /// grabbed objects, and top-down character friction.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Motor Joint")]
    public sealed class Box3DMotorJoint : Box3DJoint
    {
        [Header("Velocity Control")]
        [Tooltip("Target relative linear velocity in m/s.")]
        public Vector3 targetLinearVelocity;

        [Tooltip("Maximum force used for velocity control (N). 0 disables it.")]
        [Min(0.0f)] public float maxVelocityForce;

        [Tooltip("Target relative angular velocity in degrees per second.")]
        public Vector3 targetAngularVelocity;

        [Tooltip("Maximum torque used for velocity control (N*m). 0 disables it.")]
        [Min(0.0f)] public float maxVelocityTorque;

        [Header("Position Spring")]
        [Tooltip("Linear spring stiffness in Hz that holds the setup pose. 0 disables it.")]
        [Min(0.0f)] public float linearHertz;

        [Min(0.0f)] public float linearDampingRatio = 1.0f;

        [Tooltip("Maximum linear spring force (N).")]
        [Min(0.0f)] public float maxSpringForce = float.MaxValue;

        [Tooltip("Angular spring stiffness in Hz that holds the setup rotation. 0 disables it.")]
        [Min(0.0f)] public float angularHertz;

        [Min(0.0f)] public float angularDampingRatio = 1.0f;

        [Tooltip("Maximum angular spring torque (N*m).")]
        [Min(0.0f)] public float maxSpringTorque = float.MaxValue;

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3MotorJointDef def = B3Api.b3DefaultMotorJointDef();
            FillBase(ref def.@base);

            def.linearVelocity = targetLinearVelocity;
            def.maxVelocityForce = maxVelocityForce;
            def.angularVelocity = targetAngularVelocity * Mathf.Deg2Rad;
            def.maxVelocityTorque = maxVelocityTorque;
            def.linearHertz = linearHertz;
            def.linearDampingRatio = linearDampingRatio;
            def.maxSpringForce = maxSpringForce;
            def.angularHertz = angularHertz;
            def.angularDampingRatio = angularDampingRatio;
            def.maxSpringTorque = maxSpringTorque;

            return B3Api.b3CreateMotorJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint. Call every frame if driving velocities.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3MotorJoint_SetLinearVelocity(JointId, targetLinearVelocity);
            B3Api.b3MotorJoint_SetMaxVelocityForce(JointId, maxVelocityForce);
            B3Api.b3MotorJoint_SetAngularVelocity(JointId, targetAngularVelocity * Mathf.Deg2Rad);
            B3Api.b3MotorJoint_SetMaxVelocityTorque(JointId, maxVelocityTorque);
            B3Api.b3MotorJoint_SetLinearHertz(JointId, linearHertz);
            B3Api.b3MotorJoint_SetLinearDampingRatio(JointId, linearDampingRatio);
            B3Api.b3MotorJoint_SetMaxSpringForce(JointId, maxSpringForce);
            B3Api.b3MotorJoint_SetAngularHertz(JointId, angularHertz);
            B3Api.b3MotorJoint_SetAngularDampingRatio(JointId, angularDampingRatio);
            B3Api.b3MotorJoint_SetMaxSpringTorque(JointId, maxSpringTorque);
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
