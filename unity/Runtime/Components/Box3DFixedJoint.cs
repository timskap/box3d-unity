// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Fixed (weld) joint: rigidly locks this body to the connected body. Optional
    /// linear/angular springs soften the weld for breakable or wobbly structures.
    /// </summary>
    [AddComponentMenu("Box3D/Joints/Box3D Fixed Joint")]
    public sealed class Box3DFixedJoint : Box3DJoint
    {
        [Header("Softness")]
        [Tooltip("Linear stiffness in Hz. 0 = fully rigid.")]
        [Min(0.0f)] public float linearHertz;

        [Tooltip("Angular stiffness in Hz. 0 = fully rigid.")]
        [Min(0.0f)] public float angularHertz;

        [Tooltip("Linear damping ratio. 1 = critical damping.")]
        [Min(0.0f)] public float linearDampingRatio = 1.0f;

        [Tooltip("Angular damping ratio. 1 = critical damping.")]
        [Min(0.0f)] public float angularDampingRatio = 1.0f;

        protected override B3JointId CreateNativeJoint(B3WorldId worldId)
        {
            B3WeldJointDef def = B3Api.b3DefaultWeldJointDef();
            FillBase(ref def.@base);

            def.linearHertz = linearHertz;
            def.angularHertz = angularHertz;
            def.linearDampingRatio = linearDampingRatio;
            def.angularDampingRatio = angularDampingRatio;

            return B3Api.b3CreateWeldJoint(worldId, ref def);
        }

        /// <summary>Push runtime-editable fields to the native joint.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3WeldJoint_SetLinearHertz(JointId, linearHertz);
            B3Api.b3WeldJoint_SetAngularHertz(JointId, angularHertz);
            B3Api.b3WeldJoint_SetLinearDampingRatio(JointId, linearDampingRatio);
            B3Api.b3WeldJoint_SetAngularDampingRatio(JointId, angularDampingRatio);
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
