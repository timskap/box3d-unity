// SPDX-License-Identifier: MIT
// P/Invoke bindings for the Box3D native library.

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Box3D.Native
{
    // Callback delegates. All native callbacks are cdecl.

    /// <summary>Ray/shape cast callback. Return -1 to ignore, 0 to terminate, fraction to clip, 1 to continue.</summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float B3CastResultFcn(B3ShapeId shapeId, Vector3 point, Vector3 normal, float fraction,
        ulong userMaterialId, int triangleIndex, int childIndex, IntPtr context);

    /// <summary>Overlap callback. Return 0 to terminate the query, 1 to continue.</summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte B3OverlapResultFcn(B3ShapeId shapeId, IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void B3LogFcn(IntPtr message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int B3AssertFcn(IntPtr condition, IntPtr fileName, int lineNumber);

    /// <summary>
    /// Raw bindings to the Box3D C API. Prefer the component layer
    /// (Box3DWorld, Box3DBody, colliders, joints) unless you need direct access.
    /// </summary>
    public static class B3Api
    {
#if UNITY_IOS && !UNITY_EDITOR
        public const string Lib = "__Internal";
#else
        public const string Lib = "box3d";
#endif

        // ---------------- base ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3Version b3GetVersion();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3IsDoublePrecision();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SetLogFcn(B3LogFcn logFcn);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SetAssertFcn(B3AssertFcn assertFcn);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int b3GetByteCount();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3GetLengthUnitsPerMeter();

        // ---------------- defaults ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3WorldDef b3DefaultWorldDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3BodyDef b3DefaultBodyDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3Filter b3DefaultFilter();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3QueryFilter b3DefaultQueryFilter();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3SurfaceMaterial b3DefaultSurfaceMaterial();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeDef b3DefaultShapeDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ExplosionDef b3DefaultExplosionDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3DistanceJointDef b3DefaultDistanceJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3MotorJointDef b3DefaultMotorJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3FilterJointDef b3DefaultFilterJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ParallelJointDef b3DefaultParallelJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3PrismaticJointDef b3DefaultPrismaticJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3RevoluteJointDef b3DefaultRevoluteJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3SphericalJointDef b3DefaultSphericalJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3WeldJointDef b3DefaultWeldJointDef();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3WheelJointDef b3DefaultWheelJointDef();

        // ---------------- world ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3WorldId b3CreateWorld(ref B3WorldDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DestroyWorld(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3World_IsValid(B3WorldId id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_Step(B3WorldId worldId, float timeStep, int subStepCount);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3BodyEvents b3World_GetBodyEvents(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3SensorEvents b3World_GetSensorEvents(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ContactEvents b3World_GetContactEvents(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointEvents b3World_GetJointEvents(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3TreeStats b3World_OverlapAABB(B3WorldId worldId, B3AABB aabb, B3QueryFilter filter,
            B3OverlapResultFcn fcn, IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3TreeStats b3World_OverlapShape(B3WorldId worldId, Vector3 origin, ref B3ShapeProxy proxy,
            B3QueryFilter filter, B3OverlapResultFcn fcn, IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3TreeStats b3World_CastRay(B3WorldId worldId, Vector3 origin, Vector3 translation,
            B3QueryFilter filter, B3CastResultFcn fcn, IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3RayResult b3World_CastRayClosest(B3WorldId worldId, Vector3 origin, Vector3 translation,
            B3QueryFilter filter);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3TreeStats b3World_CastShape(B3WorldId worldId, Vector3 origin, ref B3ShapeProxy proxy,
            Vector3 translation, B3QueryFilter filter, B3CastResultFcn fcn, IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_EnableSleeping(B3WorldId worldId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_EnableContinuous(B3WorldId worldId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_SetRestitutionThreshold(B3WorldId worldId, float value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_SetHitEventThreshold(B3WorldId worldId, float value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_SetGravity(B3WorldId worldId, Vector3 gravity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3World_GetGravity(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_Explode(B3WorldId worldId, ref B3ExplosionDef explosionDef);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_SetContactTuning(B3WorldId worldId, float hertz, float dampingRatio, float contactSpeed);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_SetMaximumLinearSpeed(B3WorldId worldId, float maximumLinearSpeed);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int b3World_GetAwakeBodyCount(B3WorldId worldId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3World_SetWorkerCount(B3WorldId worldId, int count);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int b3World_GetWorkerCount(B3WorldId worldId);

        // ---------------- body ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3BodyId b3CreateBody(B3WorldId worldId, ref B3BodyDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DestroyBody(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3Body_IsValid(B3BodyId id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3BodyType b3Body_GetType(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetType(B3BodyId bodyId, B3BodyType type);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void b3Body_SetName(B3BodyId bodyId, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetPosition(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Quaternion b3Body_GetRotation(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3Transform b3Body_GetTransform(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetTransform(B3BodyId bodyId, Vector3 position, Quaternion rotation);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetLocalPoint(B3BodyId bodyId, Vector3 worldPoint);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetWorldPoint(B3BodyId bodyId, Vector3 localPoint);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetLinearVelocity(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetAngularVelocity(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetLinearVelocity(B3BodyId bodyId, Vector3 linearVelocity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetAngularVelocity(B3BodyId bodyId, Vector3 angularVelocity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetTargetTransform(B3BodyId bodyId, B3Transform target, float timeStep,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetLocalPointVelocity(B3BodyId bodyId, Vector3 localPoint);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetWorldPointVelocity(B3BodyId bodyId, Vector3 worldPoint);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyForce(B3BodyId bodyId, Vector3 force, Vector3 point,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyForceToCenter(B3BodyId bodyId, Vector3 force,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyTorque(B3BodyId bodyId, Vector3 torque,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyLinearImpulse(B3BodyId bodyId, Vector3 impulse, Vector3 point,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyLinearImpulseToCenter(B3BodyId bodyId, Vector3 impulse,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyAngularImpulse(B3BodyId bodyId, Vector3 impulse,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3Body_GetMass(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3MassData b3Body_GetMassData(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetMassData(B3BodyId bodyId, B3MassData massData);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_ApplyMassFromShapes(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetLocalCenterOfMass(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Body_GetWorldCenterOfMass(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetLinearDamping(B3BodyId bodyId, float linearDamping);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetAngularDamping(B3BodyId bodyId, float angularDamping);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetGravityScale(B3BodyId bodyId, float gravityScale);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3Body_GetGravityScale(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3Body_IsAwake(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetAwake(B3BodyId bodyId, [MarshalAs(UnmanagedType.I1)] bool awake);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_EnableSleep(B3BodyId bodyId, [MarshalAs(UnmanagedType.I1)] bool enableSleep);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetSleepThreshold(B3BodyId bodyId, float sleepThreshold);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3Body_IsEnabled(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_Disable(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_Enable(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetMotionLocks(B3BodyId bodyId, B3MotionLocks locks);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3MotionLocks b3Body_GetMotionLocks(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_SetBullet(B3BodyId bodyId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_EnableContactRecycling(B3BodyId bodyId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Body_EnableHitEvents(B3BodyId bodyId, [MarshalAs(UnmanagedType.I1)] bool enableHitEvents);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int b3Body_GetShapeCount(B3BodyId bodyId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3AABB b3Body_ComputeAABB(B3BodyId bodyId);

        // ---------------- shapes ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeId b3CreateSphereShape(B3BodyId bodyId, ref B3ShapeDef def, ref B3Sphere sphere);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeId b3CreateCapsuleShape(B3BodyId bodyId, ref B3ShapeDef def, ref B3Capsule capsule);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeId b3CreateHullShape(B3BodyId bodyId, ref B3ShapeDef def, IntPtr hullData);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeId b3CreateTransformedHullShape(B3BodyId bodyId, ref B3ShapeDef def, IntPtr hullData,
            B3Transform transform, Vector3 scale);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeId b3CreateMeshShape(B3BodyId bodyId, ref B3ShapeDef def, IntPtr meshData, Vector3 scale);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DestroyShape(B3ShapeId shapeId, [MarshalAs(UnmanagedType.I1)] bool updateBodyMass);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3Shape_IsValid(B3ShapeId id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3ShapeType b3Shape_GetType(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3BodyId b3Shape_GetBody(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3Shape_IsSensor(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_SetDensity(B3ShapeId shapeId, float density,
            [MarshalAs(UnmanagedType.I1)] bool updateBodyMass);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3Shape_GetDensity(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_SetFriction(B3ShapeId shapeId, float friction);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3Shape_GetFriction(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_SetRestitution(B3ShapeId shapeId, float restitution);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3Shape_GetRestitution(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_SetSurfaceMaterial(B3ShapeId shapeId, B3SurfaceMaterial surfaceMaterial);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3SurfaceMaterial b3Shape_GetSurfaceMaterial(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3Filter b3Shape_GetFilter(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_SetFilter(B3ShapeId shapeId, B3Filter filter,
            [MarshalAs(UnmanagedType.I1)] bool invokeContacts);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_EnableSensorEvents(B3ShapeId shapeId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_EnableContactEvents(B3ShapeId shapeId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_EnableHitEvents(B3ShapeId shapeId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3AABB b3Shape_GetAABB(B3ShapeId shapeId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Shape_GetClosestPoint(B3ShapeId shapeId, Vector3 target);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Shape_ApplyWind(B3ShapeId shapeId, Vector3 wind, float drag, float lift, float maxSpeed,
            [MarshalAs(UnmanagedType.I1)] bool wake);

        // ---------------- geometry helpers ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr b3CreateHull([In] Vector3[] points, int pointCount, int maxVertexCount);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr b3CreateCylinder(float height, float radius, float yOffset, int sides);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr b3CreateCone(float height, float radius1, float radius2, int slices);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DestroyHull(IntPtr hull);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr b3CreateMesh(ref B3MeshDef def, IntPtr degenerateTriangleIndices, int degenerateCapacity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DestroyMesh(IntPtr mesh);

        // ---------------- joints (common) ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DestroyJoint(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool wakeAttached);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool b3Joint_IsValid(B3JointId id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointType b3Joint_GetType(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Joint_SetLocalFrameA(B3JointId jointId, B3Transform localFrame);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3Transform b3Joint_GetLocalFrameA(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Joint_SetLocalFrameB(B3JointId jointId, B3Transform localFrame);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3Transform b3Joint_GetLocalFrameB(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Joint_SetCollideConnected(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool shouldCollide);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3Joint_WakeBodies(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Joint_GetConstraintForce(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 b3Joint_GetConstraintTorque(B3JointId jointId);

        // ---------------- joint creation ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateDistanceJoint(B3WorldId worldId, ref B3DistanceJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateMotorJoint(B3WorldId worldId, ref B3MotorJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateFilterJoint(B3WorldId worldId, ref B3FilterJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateParallelJoint(B3WorldId worldId, ref B3ParallelJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreatePrismaticJoint(B3WorldId worldId, ref B3PrismaticJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateRevoluteJoint(B3WorldId worldId, ref B3RevoluteJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateSphericalJoint(B3WorldId worldId, ref B3SphericalJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateWeldJoint(B3WorldId worldId, ref B3WeldJointDef def);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern B3JointId b3CreateWheelJoint(B3WorldId worldId, ref B3WheelJointDef def);

        // ---------------- revolute joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_EnableSpring(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableSpring);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_SetSpringHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_SetSpringDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_SetTargetAngle(B3JointId jointId, float targetRadians);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3RevoluteJoint_GetAngle(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_EnableLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableLimit);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_SetLimits(B3JointId jointId, float lowerRadians, float upperRadians);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_EnableMotor(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableMotor);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_SetMotorSpeed(B3JointId jointId, float motorSpeed);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3RevoluteJoint_GetMotorTorque(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3RevoluteJoint_SetMaxMotorTorque(B3JointId jointId, float torque);

        // ---------------- distance joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_SetLength(B3JointId jointId, float length);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3DistanceJoint_GetLength(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3DistanceJoint_GetCurrentLength(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_EnableSpring(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableSpring);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_SetSpringHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_SetSpringDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_EnableLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableLimit);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_SetLengthRange(B3JointId jointId, float minLength, float maxLength);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_EnableMotor(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableMotor);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_SetMotorSpeed(B3JointId jointId, float motorSpeed);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3DistanceJoint_SetMaxMotorForce(B3JointId jointId, float force);

        // ---------------- prismatic joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_EnableSpring(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableSpring);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_SetSpringHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_SetSpringDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_SetTargetTranslation(B3JointId jointId, float targetTranslation);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_EnableLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableLimit);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_SetLimits(B3JointId jointId, float lower, float upper);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_EnableMotor(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableMotor);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_SetMotorSpeed(B3JointId jointId, float motorSpeed);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3PrismaticJoint_SetMaxMotorForce(B3JointId jointId, float force);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3PrismaticJoint_GetTranslation(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3PrismaticJoint_GetSpeed(B3JointId jointId);

        // ---------------- spherical joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_EnableConeLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableLimit);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetConeLimit(B3JointId jointId, float angleRadians);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3SphericalJoint_GetConeAngle(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_EnableTwistLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableLimit);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetTwistLimits(B3JointId jointId, float lowerRadians, float upperRadians);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3SphericalJoint_GetTwistAngle(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_EnableSpring(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableSpring);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetSpringHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetSpringDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetTargetRotation(B3JointId jointId, Quaternion targetRotation);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_EnableMotor(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool enableMotor);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetMotorVelocity(B3JointId jointId, Vector3 motorVelocity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3SphericalJoint_SetMaxMotorTorque(B3JointId jointId, float torque);

        // ---------------- weld joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WeldJoint_SetLinearHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WeldJoint_SetLinearDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WeldJoint_SetAngularHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WeldJoint_SetAngularDampingRatio(B3JointId jointId, float dampingRatio);

        // ---------------- motor joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetLinearVelocity(B3JointId jointId, Vector3 velocity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetAngularVelocity(B3JointId jointId, Vector3 velocity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetMaxVelocityForce(B3JointId jointId, float maxForce);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetMaxVelocityTorque(B3JointId jointId, float maxTorque);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetLinearHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetLinearDampingRatio(B3JointId jointId, float damping);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetAngularHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetAngularDampingRatio(B3JointId jointId, float damping);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetMaxSpringForce(B3JointId jointId, float maxForce);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3MotorJoint_SetMaxSpringTorque(B3JointId jointId, float maxTorque);

        // ---------------- parallel joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3ParallelJoint_SetSpringHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3ParallelJoint_SetSpringDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3ParallelJoint_SetMaxTorque(B3JointId jointId, float force);

        // ---------------- wheel joint ----------------

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_EnableSuspension(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSuspensionHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSuspensionDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_EnableSuspensionLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSuspensionLimits(B3JointId jointId, float lower, float upper);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_EnableSpinMotor(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSpinMotorSpeed(B3JointId jointId, float speed);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetMaxSpinTorque(B3JointId jointId, float torque);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3WheelJoint_GetSpinSpeed(B3JointId jointId);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_EnableSteering(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSteeringHertz(B3JointId jointId, float hertz);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSteeringDampingRatio(B3JointId jointId, float dampingRatio);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetMaxSteeringTorque(B3JointId jointId, float torque);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_EnableSteeringLimit(B3JointId jointId, [MarshalAs(UnmanagedType.I1)] bool flag);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetSteeringLimits(B3JointId jointId, float lowerRadians, float upperRadians);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void b3WheelJoint_SetTargetSteeringAngle(B3JointId jointId, float radians);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern float b3WheelJoint_GetSteeringAngle(B3JointId jointId);
    }
}
