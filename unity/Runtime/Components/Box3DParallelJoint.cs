// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Parallel joint: a spring that keeps this body's axis aligned with a world axis
    /// (or an axis on the connected body). The classic use is keeping characters and
    /// vehicles upright while still letting them tilt and recover.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Parallel Joint")]
    [HelpURL("https://github.com/timskap/box3d-unity/blob/main/unity/README.md#box3dparalleljoint")]
    public sealed class Box3DParallelJoint : Box3DJoint
    {
        [Tooltip("Axis of this body to keep aligned, in local space.")]
        public Vector3 axis = Vector3.up;

        [Tooltip("Spring stiffness in Hz.")]
        [Min(0.0f)] public float springHertz = 5.0f;

        [Tooltip("Spring damping ratio. 1 = critical damping.")]
        [Min(0.0f)] public float springDampingRatio = 1.0f;

        [Tooltip("Maximum restoring torque in N*m.")]
        [Min(0.0f)] public float maxTorque = 100.0f;

        protected override Quaternion WorldFrameRotation()
        {
            // The joint aligns the z axes of the two frames.
            return BasisWithZ(transform.TransformDirection(axis));
        }

        protected override void DrawJointGizmos(Vector3 worldAnchor)
        {
            DrawAxisGizmo(worldAnchor, axis, new Color(0.3f, 0.8f, 1.0f, 0.9f));
        }

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3ParallelJointDef def = B3Api.b3DefaultParallelJointDef();
            FillBase(ref def.@base);

            def.hertz = springHertz;
            def.dampingRatio = springDampingRatio;
            def.maxTorque = maxTorque;

            return B3Api.b3CreateParallelJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3ParallelJoint_SetSpringHertz(JointId, springHertz);
            B3Api.b3ParallelJoint_SetSpringDampingRatio(JointId, springDampingRatio);
            B3Api.b3ParallelJoint_SetMaxTorque(JointId, maxTorque);
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
