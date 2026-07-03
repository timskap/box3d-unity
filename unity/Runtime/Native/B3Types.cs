// SPDX-License-Identifier: MIT
// C# mirrors of the Box3D public C API (include/box3d).
// Every struct here is blittable and laid out to match the native ABI exactly
// (single precision build). Native `bool` fields are mirrored as `byte`.
// Layouts are verified at runtime by B3LayoutCheck.

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Box3D.Native
{
    // ------------------------------------------------------------------
    // Ids (opaque handles). Null when index1 == 0.
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3WorldId : IEquatable<B3WorldId>
    {
        public ushort index1;
        public ushort generation;

        public bool IsNull => index1 == 0;
        public bool Equals(B3WorldId other) => index1 == other.index1 && generation == other.generation;
        public override int GetHashCode() => (index1 << 16) | generation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3BodyId : IEquatable<B3BodyId>
    {
        public int index1;
        public ushort world0;
        public ushort generation;

        public bool IsNull => index1 == 0;
        public ulong Key => ((ulong)(uint)index1 << 32) | ((ulong)world0 << 16) | generation;
        public bool Equals(B3BodyId other) => index1 == other.index1 && world0 == other.world0 && generation == other.generation;
        public override int GetHashCode() => Key.GetHashCode();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ShapeId : IEquatable<B3ShapeId>
    {
        public int index1;
        public ushort world0;
        public ushort generation;

        public bool IsNull => index1 == 0;
        public ulong Key => ((ulong)(uint)index1 << 32) | ((ulong)world0 << 16) | generation;
        public bool Equals(B3ShapeId other) => index1 == other.index1 && world0 == other.world0 && generation == other.generation;
        public override int GetHashCode() => Key.GetHashCode();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3JointId : IEquatable<B3JointId>
    {
        public int index1;
        public ushort world0;
        public ushort generation;

        public bool IsNull => index1 == 0;
        public ulong Key => ((ulong)(uint)index1 << 32) | ((ulong)world0 << 16) | generation;
        public bool Equals(B3JointId other) => index1 == other.index1 && world0 == other.world0 && generation == other.generation;
        public override int GetHashCode() => Key.GetHashCode();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ContactId
    {
        public int index1;
        public ushort world0;
        public short padding;
        public uint generation;

        public bool IsNull => index1 == 0;
    }

    // ------------------------------------------------------------------
    // Math. b3Vec3 == UnityEngine.Vector3 and b3Quat == UnityEngine.Quaternion
    // byte-for-byte, so Unity types are used directly in all signatures.
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Transform
    {
        public Vector3 p;
        public Quaternion q;

        public static B3Transform Identity => new B3Transform { p = Vector3.zero, q = Quaternion.identity };

        public B3Transform(Vector3 position, Quaternion rotation)
        {
            p = position;
            q = rotation;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Matrix3
    {
        public Vector3 cx, cy, cz;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3AABB
    {
        public Vector3 lowerBound;
        public Vector3 upperBound;
    }

    // ------------------------------------------------------------------
    // Enums
    // ------------------------------------------------------------------

    public enum B3BodyType : int
    {
        Static = 0,
        Kinematic = 1,
        Dynamic = 2,
    }

    public enum B3ShapeType : int
    {
        Capsule = 0,
        Compound = 1,
        HeightField = 2,
        Hull = 3,
        Mesh = 4,
        Sphere = 5,
    }

    public enum B3JointType : int
    {
        Parallel = 0,
        Distance = 1,
        Filter = 2,
        Motor = 3,
        Prismatic = 4,
        Revolute = 5,
        Spherical = 6,
        Weld = 7,
        Wheel = 8,
    }

    // ------------------------------------------------------------------
    // World
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Capacity
    {
        public int staticShapeCount;
        public int dynamicShapeCount;
        public int staticBodyCount;
        public int dynamicBodyCount;
        public int contactCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3WorldDef
    {
        public Vector3 gravity;
        public float restitutionThreshold;
        public float hitEventThreshold;
        public float contactHertz;
        public float contactDampingRatio;
        public float contactSpeed;
        public float maximumLinearSpeed;
        public IntPtr frictionCallback;
        public IntPtr restitutionCallback;
        public byte enableSleep;
        public byte enableContinuous;
        public uint workerCount;
        public IntPtr enqueueTask;
        public IntPtr finishTask;
        public IntPtr userTaskContext;
        public IntPtr userData;
        public IntPtr createDebugShape;
        public IntPtr destroyDebugShape;
        public IntPtr userDebugShapeContext;
        public B3Capacity capacity;
        public int internalValue;
    }

    // ------------------------------------------------------------------
    // Body
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3MotionLocks
    {
        public byte linearX;
        public byte linearY;
        public byte linearZ;
        public byte angularX;
        public byte angularY;
        public byte angularZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3BodyDef
    {
        public B3BodyType type;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public float linearDamping;
        public float angularDamping;
        public float gravityScale;
        public float sleepThreshold;
        public IntPtr name;
        public IntPtr userData;
        public B3MotionLocks motionLocks;
        public byte enableSleep;
        public byte isAwake;
        public byte isBullet;
        public byte isEnabled;
        public byte allowFastRotation;
        public byte enableContactRecycling;
        public int internalValue;
    }

    // ------------------------------------------------------------------
    // Shape
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Filter
    {
        public ulong categoryBits;
        public ulong maskBits;
        public int groupIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3SurfaceMaterial
    {
        public float friction;
        public float restitution;
        public float rollingResistance;
        public Vector3 tangentVelocity;
        public ulong userMaterialId;
        public uint customColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ShapeDef
    {
        public IntPtr userData;
        public IntPtr materials;
        public int materialCount;
        public B3SurfaceMaterial baseMaterial;
        public float density;
        public float explosionScale;
        public B3Filter filter;
        public byte enableCustomFiltering;
        public byte isSensor;
        public byte enableSensorEvents;
        public byte enableContactEvents;
        public byte enableHitEvents;
        public byte enablePreSolveEvents;
        public byte invokeContactCreation;
        public byte updateBodyMass;
        public int internalValue;
    }

    // ------------------------------------------------------------------
    // Geometry
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3MassData
    {
        public float mass;
        public Vector3 center;
        public B3Matrix3 inertia;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Sphere
    {
        public Vector3 center;
        public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Capsule
    {
        public Vector3 center1;
        public Vector3 center2;
        public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3MeshDef
    {
        public IntPtr vertices;        // b3Vec3*
        public IntPtr indices;         // int32*
        public IntPtr materialIndices; // uint8*, optional
        public float weldTolerance;
        public int vertexCount;
        public int triangleCount;
        public byte weldVertices;
        public byte useMedianSplit;
        public byte identifyEdges;
    }

    // ------------------------------------------------------------------
    // Joints
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3JointDef
    {
        public IntPtr userData;
        public B3BodyId bodyIdA;
        public B3BodyId bodyIdB;
        public B3Transform localFrameA;
        public B3Transform localFrameB;
        public float forceThreshold;
        public float torqueThreshold;
        public float constraintHertz;
        public float constraintDampingRatio;
        public float drawScale;
        public byte collideConnected;
        public int internalValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3DistanceJointDef
    {
        public B3JointDef @base;
        public float length;
        public byte enableSpring;
        public float lowerSpringForce;
        public float upperSpringForce;
        public float hertz;
        public float dampingRatio;
        public byte enableLimit;
        public float minLength;
        public float maxLength;
        public byte enableMotor;
        public float maxMotorForce;
        public float motorSpeed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3MotorJointDef
    {
        public B3JointDef @base;
        public Vector3 linearVelocity;
        public float maxVelocityForce;
        public Vector3 angularVelocity;
        public float maxVelocityTorque;
        public float linearHertz;
        public float linearDampingRatio;
        public float maxSpringForce;
        public float angularHertz;
        public float angularDampingRatio;
        public float maxSpringTorque;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3FilterJointDef
    {
        public B3JointDef @base;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ParallelJointDef
    {
        public B3JointDef @base;
        public float hertz;
        public float dampingRatio;
        public float maxTorque;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3PrismaticJointDef
    {
        public B3JointDef @base;
        public byte enableSpring;
        public float hertz;
        public float dampingRatio;
        public float targetTranslation;
        public byte enableLimit;
        public float lowerTranslation;
        public float upperTranslation;
        public byte enableMotor;
        public float maxMotorForce;
        public float motorSpeed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3RevoluteJointDef
    {
        public B3JointDef @base;
        public float targetAngle;
        public byte enableSpring;
        public float hertz;
        public float dampingRatio;
        public byte enableLimit;
        public float lowerAngle;
        public float upperAngle;
        public byte enableMotor;
        public float maxMotorTorque;
        public float motorSpeed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3SphericalJointDef
    {
        public B3JointDef @base;
        public byte enableSpring;
        public float hertz;
        public float dampingRatio;
        public Quaternion targetRotation;
        public byte enableConeLimit;
        public float coneAngle;
        public byte enableTwistLimit;
        public float lowerTwistAngle;
        public float upperTwistAngle;
        public byte enableMotor;
        public float maxMotorTorque;
        public Vector3 motorVelocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3WeldJointDef
    {
        public B3JointDef @base;
        public float linearHertz;
        public float angularHertz;
        public float linearDampingRatio;
        public float angularDampingRatio;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3WheelJointDef
    {
        public B3JointDef @base;
        public byte enableSuspensionSpring;
        public float suspensionHertz;
        public float suspensionDampingRatio;
        public byte enableSuspensionLimit;
        public float lowerSuspensionLimit;
        public float upperSuspensionLimit;
        public byte enableSpinMotor;
        public float maxSpinTorque;
        public float spinSpeed;
        public byte enableSteering;
        public float steeringHertz;
        public float steeringDampingRatio;
        public float targetSteeringAngle;
        public float maxSteeringTorque;
        public byte enableSteeringLimit;
        public float lowerSteeringLimit;
        public float upperSteeringLimit;
    }

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3SensorBeginTouchEvent
    {
        public B3ShapeId sensorShapeId;
        public B3ShapeId visitorShapeId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3SensorEndTouchEvent
    {
        public B3ShapeId sensorShapeId;
        public B3ShapeId visitorShapeId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3SensorEvents
    {
        public IntPtr beginEvents; // B3SensorBeginTouchEvent*
        public IntPtr endEvents;   // B3SensorEndTouchEvent*
        public int beginCount;
        public int endCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ContactBeginTouchEvent
    {
        public B3ShapeId shapeIdA;
        public B3ShapeId shapeIdB;
        public B3ContactId contactId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ContactEndTouchEvent
    {
        public B3ShapeId shapeIdA;
        public B3ShapeId shapeIdB;
        public B3ContactId contactId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ContactHitEvent
    {
        public B3ShapeId shapeIdA;
        public B3ShapeId shapeIdB;
        public B3ContactId contactId;
        public Vector3 point;
        public Vector3 normal;
        public float approachSpeed;
        public ulong userMaterialIdA;
        public ulong userMaterialIdB;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ContactEvents
    {
        public IntPtr beginEvents; // B3ContactBeginTouchEvent*
        public IntPtr endEvents;   // B3ContactEndTouchEvent*
        public IntPtr hitEvents;   // B3ContactHitEvent*
        public int beginCount;
        public int endCount;
        public int hitCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3BodyMoveEvent
    {
        public IntPtr userData;
        public B3Transform transform;
        public B3BodyId bodyId;
        public byte fellAsleep;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3BodyEvents
    {
        public IntPtr moveEvents; // B3BodyMoveEvent*
        public int moveCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3JointEvent
    {
        public B3JointId jointId;
        public IntPtr userData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3JointEvents
    {
        public IntPtr jointEvents; // B3JointEvent*
        public int count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ContactData
    {
        public B3ContactId contactId;
        public B3ShapeId shapeIdA;
        public B3ShapeId shapeIdB;
        public IntPtr manifolds; // const b3Manifold*
        public int manifoldCount;
    }

    // ------------------------------------------------------------------
    // Queries
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3QueryFilter
    {
        public ulong categoryBits;
        public ulong maskBits;
        public ulong id;
        public IntPtr name;

        public static B3QueryFilter Default => new B3QueryFilter
        {
            categoryBits = ulong.MaxValue,
            maskBits = ulong.MaxValue,
            id = 0,
            name = IntPtr.Zero,
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3RayResult
    {
        public B3ShapeId shapeId;
        public Vector3 point;
        public Vector3 normal;
        public ulong userMaterialId;
        public float fraction;
        public int triangleIndex;
        public int childIndex;
        public int nodeVisits;
        public int leafVisits;
        public byte hit;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ShapeProxy
    {
        public IntPtr points; // const b3Vec3*
        public int count;
        public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3CastOutput
    {
        public Vector3 normal;
        public Vector3 point;
        public float fraction;
        public int iterations;
        public int triangleIndex;
        public int childIndex;
        public int materialIndex;
        public byte hit;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3BodyCastResult
    {
        public B3ShapeId shapeId;
        public Vector3 point;
        public Vector3 normal;
        public float fraction;
        public int triangleIndex;
        public ulong userMaterialId;
        public int iterations;
        public byte hit;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3TreeStats
    {
        public int nodeVisits;
        public int leafVisits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct B3ExplosionDef
    {
        public ulong maskBits;
        public Vector3 position;
        public float radius;
        public float falloff;
        public float impulsePerArea;
    }

    // ------------------------------------------------------------------
    // Misc
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct B3Version
    {
        public int major;
        public int minor;
        public int revision;
    }
}
