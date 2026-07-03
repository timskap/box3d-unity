// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using UnityEngine;

namespace Box3D.Native
{
    /// <summary>
    /// Validates that the C# struct mirrors match the native ABI. The expected sizes
    /// were captured from the C compiler (sizeof) for the single precision build.
    /// Runs once; logs errors and returns false on any mismatch.
    /// </summary>
    public static class B3LayoutCheck
    {
        static bool _checked;
        static bool _ok;

        public static bool Validate()
        {
            if (_checked)
            {
                return _ok;
            }

            _checked = true;
            _ok = true;

            Check<B3WorldId>(4);
            Check<B3BodyId>(8);
            Check<B3ShapeId>(8);
            Check<B3JointId>(8);
            Check<B3ContactId>(12);
            Check<B3Transform>(28);
            Check<B3WorldDef>(144);
            Check<B3BodyDef>(104);
            Check<B3Filter>(24);
            Check<B3SurfaceMaterial>(40);
            Check<B3ShapeDef>(112);
            Check<B3JointDef>(112);
            Check<B3DistanceJointDef>(160);
            Check<B3MotorJointDef>(168);
            Check<B3FilterJointDef>(112);
            Check<B3ParallelJointDef>(128);
            Check<B3PrismaticJointDef>(152);
            Check<B3RevoluteJointDef>(152);
            Check<B3SphericalJointDef>(184);
            Check<B3WeldJointDef>(128);
            Check<B3WheelJointDef>(184);
            Check<B3SensorBeginTouchEvent>(16);
            Check<B3SensorEndTouchEvent>(16);
            Check<B3SensorEvents>(24);
            Check<B3ContactBeginTouchEvent>(28);
            Check<B3ContactEndTouchEvent>(28);
            Check<B3ContactHitEvent>(72);
            Check<B3ContactEvents>(40);
            Check<B3BodyMoveEvent>(48);
            Check<B3BodyEvents>(16);
            Check<B3JointEvent>(16);
            Check<B3JointEvents>(16);
            Check<B3QueryFilter>(32);
            Check<B3RayResult>(64);
            Check<B3ShapeProxy>(16);
            Check<B3CastOutput>(48);
            Check<B3BodyCastResult>(56);
            Check<B3MassData>(52);
            Check<B3Sphere>(16);
            Check<B3Capsule>(28);
            Check<B3MeshDef>(40);
            Check<B3ExplosionDef>(32);
            Check<B3MotionLocks>(6);
            Check<B3AABB>(24);
            Check<B3Capacity>(20);

            if (_ok && B3Api.b3IsDoublePrecision())
            {
                Debug.LogError("[Box3D] The native library was built with BOX3D_DOUBLE_PRECISION which is not " +
                               "supported by these bindings. Rebuild the native library in single precision.");
                _ok = false;
            }

            return _ok;
        }

        static void Check<T>(int expected) where T : struct
        {
            int actual = Marshal.SizeOf<T>();
            if (actual != expected)
            {
                Debug.LogError($"[Box3D] ABI mismatch: {typeof(T).Name} is {actual} bytes in C#, native expects {expected}. " +
                               "Physics is disabled to avoid memory corruption.");
                _ok = false;
            }
        }
    }
}
