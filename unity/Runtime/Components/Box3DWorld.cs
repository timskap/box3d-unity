// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using AOT;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// A Box3D simulation world. Add one to your scene; bodies, colliders, and joints
    /// attach to the first enabled world automatically. The world steps during
    /// FixedUpdate and pumps contact/sensor/move events back to components.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Box3D/Box3D World")]
    [HelpURL("https://github.com/timskap/box3d-unity/blob/main/unity/README.md#box3dworld")]
    public sealed class Box3DWorld : MonoBehaviour
    {
        [Tooltip("World gravity in m/s^2. Box3D has no fixed up axis; Unity convention is -Y.")]
        public Vector3 gravity = new Vector3(0.0f, -10.0f, 0.0f);

        [Tooltip("Solver sub-steps per fixed update. 4 is a good default; more increases accuracy.")]
        [Range(1, 16)]
        public int subStepCount = 4;

        [Tooltip("Worker threads. 0 = automatic (half the cores, capped at 8). 1 = single threaded. Uses Box3D's internal scheduler.")]
        [Range(0, 32)]
        public int workerCount;

        [Tooltip("Allow bodies to sleep for performance.")]
        public bool enableSleep = true;

        [Tooltip("Continuous collision keeps fast bodies from tunneling through static geometry.")]
        public bool enableContinuous = true;

        [Header("Tuning (engine defaults)")]
        [Tooltip("Collisions faster than this bounce (restitution). m/s.")]
        [Min(0.0f)] public float restitutionThreshold = 1.0f;

        [Tooltip("Collisions faster than this can raise hit events. m/s.")]
        [Min(0.0f)] public float hitEventThreshold = 1.0f;

        [Tooltip("Contact stiffness in cycles per second.")]
        [Min(0.0f)] public float contactHertz = 30.0f;

        [Tooltip("Contact bounciness damping. Higher is less energetic overlap recovery.")]
        [Min(0.0f)] public float contactDampingRatio = 10.0f;

        [Tooltip("Maximum overlap resolution speed in m/s.")]
        [Min(0.0f)] public float contactSpeed = 3.0f;

        [Tooltip("Maximum linear speed of any body in m/s.")]
        [Min(0.001f)] public float maximumLinearSpeed = 400.0f;

        /// <summary>The first enabled world in the scene, if any.</summary>
        public static Box3DWorld Main { get; private set; }

        /// <summary>Raised after every physics step, before events are dispatched to colliders.</summary>
        public event Action<Box3DWorld> Stepped;

        /// <summary>Native world id. Null until created.</summary>
        public B3WorldId WorldId => _worldId;

        /// <summary>True once the native world exists.</summary>
        public bool IsCreated => _created && B3Api.b3World_IsValid(_worldId);

        /// <summary>Number of awake bodies (diagnostics).</summary>
        public int AwakeBodyCount => IsCreated ? B3Api.b3World_GetAwakeBodyCount(_worldId) : 0;

        B3WorldId _worldId;
        bool _created;
        B3BodyId _groundBodyId;

        readonly Dictionary<ulong, Box3DBody> _bodies = new Dictionary<ulong, Box3DBody>(256);
        readonly Dictionary<ulong, Box3DCollider> _colliders = new Dictionary<ulong, Box3DCollider>(256);
        readonly Dictionary<ulong, Box3DJoint> _joints = new Dictionary<ulong, Box3DJoint>(64);
        readonly List<Box3DBody> _bodyList = new List<Box3DBody>(256);

        static bool _nativeHooksInstalled;
        static readonly B3LogFcn LogHook = OnNativeLog;
        static readonly B3AssertFcn AssertHook = OnNativeAssert;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        void Awake()
        {
            EnsureCreated();
        }

        void OnEnable()
        {
            if (Main == null)
            {
                Main = this;
            }
        }

        void OnDisable()
        {
            if (Main == this)
            {
                Main = null;
            }
        }

        void OnDestroy()
        {
            if (!_created)
            {
                return;
            }

            // Destroy the native world first: it may still reference user-owned data
            // (mesh colliders). Wrappers release that data in NotifyWorldDestroyed.
            if (B3Api.b3World_IsValid(_worldId))
            {
                B3Api.b3DestroyWorld(_worldId);
            }

            foreach (Box3DJoint joint in _joints.Values)
            {
                joint.NotifyWorldDestroyed();
            }

            foreach (Box3DCollider collider in _colliders.Values)
            {
                collider.NotifyWorldDestroyed();
            }

            foreach (Box3DBody body in _bodyList)
            {
                body.NotifyWorldDestroyed();
            }

            _joints.Clear();
            _colliders.Clear();
            _bodies.Clear();
            _bodyList.Clear();

            _created = false;
            _groundBodyId = default;
        }

        /// <summary>Create the native world if it does not exist yet. Safe to call repeatedly.</summary>
        public bool EnsureCreated()
        {
            if (_created)
            {
                return true;
            }

            if (!B3LayoutCheck.Validate())
            {
                enabled = false;
                return false;
            }

            InstallNativeHooks();

            B3WorldDef def = B3Api.b3DefaultWorldDef();
            def.gravity = gravity;
            def.enableSleep = (byte)(enableSleep ? 1 : 0);
            def.enableContinuous = (byte)(enableContinuous ? 1 : 0);
            def.restitutionThreshold = restitutionThreshold;
            def.hitEventThreshold = hitEventThreshold;
            def.contactHertz = contactHertz;
            def.contactDampingRatio = contactDampingRatio;
            def.contactSpeed = contactSpeed;
            def.maximumLinearSpeed = maximumLinearSpeed;
            def.workerCount = (uint)ResolveWorkerCount();

            _worldId = B3Api.b3CreateWorld(ref def);
            _created = !_worldId.IsNull;

            if (!_created)
            {
                Debug.LogError("[Box3D] Failed to create world.", this);
                enabled = false;
            }
            else if (Main == null)
            {
                Main = this;
            }

            return _created;
        }

        int ResolveWorkerCount()
        {
            if (workerCount > 0)
            {
                return Mathf.Min(workerCount, 32);
            }

            return Mathf.Clamp(SystemInfo.processorCount / 2, 1, 8);
        }

        static void InstallNativeHooks()
        {
            if (_nativeHooksInstalled)
            {
                return;
            }

            _nativeHooksInstalled = true;
            B3Api.b3SetLogFcn(LogHook);
            B3Api.b3SetAssertFcn(AssertHook);
        }

        [MonoPInvokeCallback(typeof(B3LogFcn))]
        static void OnNativeLog(IntPtr message)
        {
            Debug.LogWarning("[Box3D] " + PtrToString(message));
        }

        [MonoPInvokeCallback(typeof(B3AssertFcn))]
        static int OnNativeAssert(IntPtr condition, IntPtr fileName, int lineNumber)
        {
            Debug.LogError($"[Box3D] Assertion failed: {PtrToString(condition)} at {PtrToString(fileName)}:{lineNumber}");
            return 0; // skip the native debugger break
        }

        static string PtrToString(IntPtr ptr)
        {
            return ptr == IntPtr.Zero ? string.Empty : System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
        }

        // ------------------------------------------------------------------
        // Stepping
        // ------------------------------------------------------------------

        void FixedUpdate()
        {
            if (!IsCreated)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < _bodyList.Count; ++i)
            {
                _bodyList[i].PrePhysicsStep(dt);
            }

            B3Api.b3World_Step(_worldId, dt, subStepCount);

            PumpMoveEvents();
            Stepped?.Invoke(this);
            PumpContactEvents();
            PumpSensorEvents();
            PumpJointEvents();
        }

        void Update()
        {
            if (_bodyList.Count == 0)
            {
                return;
            }

            float alpha = Time.fixedDeltaTime > 0.0f
                ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime)
                : 1.0f;

            for (int i = 0; i < _bodyList.Count; ++i)
            {
                _bodyList[i].InterpolationUpdate(alpha);
            }
        }

        unsafe void PumpMoveEvents()
        {
            B3BodyEvents events = B3Api.b3World_GetBodyEvents(_worldId);
            var moves = (B3BodyMoveEvent*)events.moveEvents;
            for (int i = 0; i < events.moveCount; ++i)
            {
                ref B3BodyMoveEvent e = ref moves[i];
                if (_bodies.TryGetValue(e.bodyId.Key, out Box3DBody body))
                {
                    body.ApplyPhysicsPose(e.transform.p, e.transform.q, e.fellAsleep != 0);
                }
            }
        }

        unsafe void PumpContactEvents()
        {
            B3ContactEvents events = B3Api.b3World_GetContactEvents(_worldId);

            var begins = (B3ContactBeginTouchEvent*)events.beginEvents;
            for (int i = 0; i < events.beginCount; ++i)
            {
                ref B3ContactBeginTouchEvent e = ref begins[i];
                Box3DCollider a = FindCollider(e.shapeIdA);
                Box3DCollider b = FindCollider(e.shapeIdB);
                if (a != null)
                {
                    a.DispatchCollisionEnter(new Box3DContact { collider = a, otherCollider = b, contactId = e.contactId });
                }

                if (b != null)
                {
                    b.DispatchCollisionEnter(new Box3DContact { collider = b, otherCollider = a, contactId = e.contactId });
                }
            }

            var ends = (B3ContactEndTouchEvent*)events.endEvents;
            for (int i = 0; i < events.endCount; ++i)
            {
                ref B3ContactEndTouchEvent e = ref ends[i];
                Box3DCollider a = FindCollider(e.shapeIdA);
                Box3DCollider b = FindCollider(e.shapeIdB);
                if (a != null)
                {
                    a.DispatchCollisionExit(new Box3DContact { collider = a, otherCollider = b, contactId = e.contactId });
                }

                if (b != null)
                {
                    b.DispatchCollisionExit(new Box3DContact { collider = b, otherCollider = a, contactId = e.contactId });
                }
            }

            var hits = (B3ContactHitEvent*)events.hitEvents;
            for (int i = 0; i < events.hitCount; ++i)
            {
                ref B3ContactHitEvent e = ref hits[i];
                Box3DCollider a = FindCollider(e.shapeIdA);
                Box3DCollider b = FindCollider(e.shapeIdB);
                if (a != null)
                {
                    a.DispatchHit(new Box3DHit
                    {
                        collider = a,
                        otherCollider = b,
                        point = e.point,
                        normal = e.normal,
                        approachSpeed = e.approachSpeed,
                    });
                }

                if (b != null)
                {
                    b.DispatchHit(new Box3DHit
                    {
                        collider = b,
                        otherCollider = a,
                        point = e.point,
                        normal = -e.normal,
                        approachSpeed = e.approachSpeed,
                    });
                }
            }
        }

        unsafe void PumpSensorEvents()
        {
            B3SensorEvents events = B3Api.b3World_GetSensorEvents(_worldId);

            var begins = (B3SensorBeginTouchEvent*)events.beginEvents;
            for (int i = 0; i < events.beginCount; ++i)
            {
                ref B3SensorBeginTouchEvent e = ref begins[i];
                Box3DCollider sensor = FindCollider(e.sensorShapeId);
                Box3DCollider visitor = FindCollider(e.visitorShapeId);
                var args = new Box3DTrigger { sensor = sensor, visitor = visitor };
                if (sensor != null)
                {
                    sensor.DispatchTriggerEnter(args);
                }

                if (visitor != null)
                {
                    visitor.DispatchTriggerEnter(args);
                }
            }

            var ends = (B3SensorEndTouchEvent*)events.endEvents;
            for (int i = 0; i < events.endCount; ++i)
            {
                ref B3SensorEndTouchEvent e = ref ends[i];
                Box3DCollider sensor = FindCollider(e.sensorShapeId);
                Box3DCollider visitor = FindCollider(e.visitorShapeId);
                var args = new Box3DTrigger { sensor = sensor, visitor = visitor };
                if (sensor != null)
                {
                    sensor.DispatchTriggerExit(args);
                }

                if (visitor != null)
                {
                    visitor.DispatchTriggerExit(args);
                }
            }
        }

        unsafe void PumpJointEvents()
        {
            B3JointEvents events = B3Api.b3World_GetJointEvents(_worldId);
            var jointEvents = (B3JointEvent*)events.jointEvents;
            for (int i = 0; i < events.count; ++i)
            {
                ref B3JointEvent e = ref jointEvents[i];
                if (_joints.TryGetValue(e.jointId.Key, out Box3DJoint joint))
                {
                    joint.HandleForceThresholdExceeded();
                }
            }
        }

        // ------------------------------------------------------------------
        // Registration (used by components)
        // ------------------------------------------------------------------

        internal void RegisterBody(Box3DBody body)
        {
            _bodies[body.BodyId.Key] = body;
            _bodyList.Add(body);
        }

        internal void UnregisterBody(Box3DBody body)
        {
            _bodies.Remove(body.BodyId.Key);
            _bodyList.Remove(body);
        }

        internal void RegisterCollider(B3ShapeId shapeId, Box3DCollider collider)
        {
            _colliders[shapeId.Key] = collider;
        }

        internal void UnregisterCollider(B3ShapeId shapeId)
        {
            _colliders.Remove(shapeId.Key);
        }

        internal void RegisterJoint(B3JointId jointId, Box3DJoint joint)
        {
            _joints[jointId.Key] = joint;
        }

        internal void UnregisterJoint(B3JointId jointId)
        {
            _joints.Remove(jointId.Key);
        }

        /// <summary>Resolve a native shape id to its collider component, or null.</summary>
        public Box3DCollider FindCollider(B3ShapeId shapeId)
        {
            return _colliders.TryGetValue(shapeId.Key, out Box3DCollider collider) ? collider : null;
        }

        /// <summary>Resolve a native body id to its body component, or null.</summary>
        public Box3DBody FindBody(B3BodyId bodyId)
        {
            return _bodies.TryGetValue(bodyId.Key, out Box3DBody body) ? body : null;
        }

        /// <summary>
        /// A hidden static body at the origin used to anchor joints that have no connected body.
        /// </summary>
        public B3BodyId GroundBodyId
        {
            get
            {
                if (_groundBodyId.IsNull && EnsureCreated())
                {
                    B3BodyDef def = B3Api.b3DefaultBodyDef();
                    _groundBodyId = B3Api.b3CreateBody(_worldId, ref def);
                }

                return _groundBodyId;
            }
        }

        // ------------------------------------------------------------------
        // World utilities
        // ------------------------------------------------------------------

        /// <summary>Change gravity at runtime.</summary>
        public void SetGravity(Vector3 value)
        {
            gravity = value;
            if (IsCreated)
            {
                B3Api.b3World_SetGravity(_worldId, value);
            }
        }

        /// <summary>Push runtime-editable inspector values to the native world.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3World_SetGravity(_worldId, gravity);
            B3Api.b3World_EnableSleeping(_worldId, enableSleep);
            B3Api.b3World_EnableContinuous(_worldId, enableContinuous);
            B3Api.b3World_SetRestitutionThreshold(_worldId, restitutionThreshold);
            B3Api.b3World_SetHitEventThreshold(_worldId, hitEventThreshold);
            B3Api.b3World_SetContactTuning(_worldId, contactHertz, contactDampingRatio, contactSpeed);
            B3Api.b3World_SetMaximumLinearSpeed(_worldId, maximumLinearSpeed);
            B3Api.b3World_SetWorkerCount(_worldId, ResolveWorkerCount());
        }

        void OnValidate()
        {
            if (Application.isPlaying && IsCreated)
            {
                ApplyProperties();
            }
        }

        /// <summary>Apply a radial explosion impulse to nearby dynamic bodies.</summary>
        public void Explode(Vector3 position, float radius, float impulsePerArea, float falloff = 0.0f, ulong maskBits = ulong.MaxValue)
        {
            if (!IsCreated)
            {
                return;
            }

            var def = new B3ExplosionDef
            {
                maskBits = maskBits,
                position = position,
                radius = radius,
                falloff = falloff,
                impulsePerArea = impulsePerArea,
            };
            B3Api.b3World_Explode(_worldId, ref def);
        }

        /// <summary>
        /// Permanently disable collision between two bodies (creates a native filter joint).
        /// </summary>
        public B3JointId IgnoreCollision(Box3DBody bodyA, Box3DBody bodyB)
        {
            if (!IsCreated || bodyA == null || bodyB == null || !bodyA.IsCreated || !bodyB.IsCreated)
            {
                return default;
            }

            B3FilterJointDef def = B3Api.b3DefaultFilterJointDef();
            def.@base.bodyIdA = bodyA.BodyId;
            def.@base.bodyIdB = bodyB.BodyId;
            return B3Api.b3CreateFilterJoint(_worldId, ref def);
        }
    }
}
