// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>Body simulation type, mirroring b3BodyType.</summary>
    public enum Box3DBodyType
    {
        /// <summary>Zero mass, never moves under simulation. Cheapest.</summary>
        Static = B3BodyType.Static,

        /// <summary>Moved by user (transform or velocity), pushes dynamic bodies, infinite mass.</summary>
        Kinematic = B3BodyType.Kinematic,

        /// <summary>Fully simulated rigid body.</summary>
        Dynamic = B3BodyType.Dynamic,
    }

    /// <summary>How the rendered transform follows the fixed-rate physics pose.</summary>
    public enum Box3DInterpolation
    {
        None,
        Interpolate,
    }

    /// <summary>
    /// A Box3D rigid body. The equivalent of Unity's Rigidbody: attach colliders to this
    /// GameObject or its children and they become shapes on this body.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Box3D/Box3D Body")]
    public sealed class Box3DBody : MonoBehaviour
    {
        [Tooltip("Static bodies never move. Kinematic bodies follow the transform and push dynamic bodies. Dynamic bodies are fully simulated.")]
        [SerializeField] Box3DBodyType _bodyType = Box3DBodyType.Dynamic;

        [Tooltip("Linear velocity damping per second.")]
        public float linearDamping;

        [Tooltip("Angular velocity damping per second.")]
        public float angularDamping = 0.05f;

        [Tooltip("Multiplier on world gravity for this body.")]
        public float gravityScale = 1.0f;

        [Tooltip("Smooths rendering between fixed physics steps (dynamic bodies only).")]
        public Box3DInterpolation interpolation = Box3DInterpolation.None;

        [Header("Sleep")]
        [Tooltip("Allow this body to sleep when at rest.")]
        public bool enableSleep = true;

        [Tooltip("Speed below which the body can fall asleep, m/s.")]
        public float sleepThreshold = 0.05f;

        [Header("Advanced")]
        [Tooltip("Continuous collision detection for fast bodies. Use sparingly.")]
        public bool isBullet;

        [Tooltip("Bypass rotational speed limits. Only for round objects like wheels.")]
        public bool allowFastRotation;

        [Tooltip("Reuse contacts when barely moving. Faster, but can cause ghost collisions on characters.")]
        public bool enableContactRecycling = true;

        [Tooltip("Prevent translation on world axes.")]
        public bool freezePositionX, freezePositionY, freezePositionZ;

        [Tooltip("Prevent rotation around world axes.")]
        public bool freezeRotationX, freezeRotationY, freezeRotationZ;

        /// <summary>Raised when the body is put to sleep by the simulation.</summary>
        public event Action FellAsleep;

        /// <summary>Native body id.</summary>
        public B3BodyId BodyId => _bodyId;

        /// <summary>World that owns this body.</summary>
        public Box3DWorld World => _world;

        /// <summary>True while the native body exists.</summary>
        public bool IsCreated => !_bodyId.IsNull && B3Api.b3Body_IsValid(_bodyId);

        B3BodyId _bodyId;
        Box3DWorld _world;
        Transform _transform;

        // Last pose this component wrote to the Transform. Any difference means the
        // user moved the transform and physics must follow (teleport / kinematic target).
        Vector3 _lastWrittenPosition;
        Quaternion _lastWrittenRotation;

        // Interpolation history.
        Vector3 _prevPosition, _currPosition;
        Quaternion _prevRotation, _currRotation;

        bool _hasKinematicTarget;
        Vector3 _kinematicTargetPosition;
        Quaternion _kinematicTargetRotation;

        readonly List<Box3DCollider> _attachedColliders = new List<Box3DCollider>(4);

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>Body type. Changing at runtime is supported but expensive.</summary>
        public Box3DBodyType bodyType
        {
            get => _bodyType;
            set
            {
                if (_bodyType == value)
                {
                    return;
                }

                _bodyType = value;
                if (IsCreated)
                {
                    B3Api.b3Body_SetType(_bodyId, (B3BodyType)value);
                }
            }
        }

        /// <summary>Linear velocity of the center of mass in m/s.</summary>
        public Vector3 linearVelocity
        {
            get => IsCreated ? B3Api.b3Body_GetLinearVelocity(_bodyId) : Vector3.zero;
            set
            {
                if (IsCreated)
                {
                    B3Api.b3Body_SetLinearVelocity(_bodyId, value);
                }
            }
        }

        /// <summary>Angular velocity in radians per second.</summary>
        public Vector3 angularVelocity
        {
            get => IsCreated ? B3Api.b3Body_GetAngularVelocity(_bodyId) : Vector3.zero;
            set
            {
                if (IsCreated)
                {
                    B3Api.b3Body_SetAngularVelocity(_bodyId, value);
                }
            }
        }

        /// <summary>Mass in kilograms, computed from shapes and densities.</summary>
        public float mass => IsCreated ? B3Api.b3Body_GetMass(_bodyId) : 0.0f;

        /// <summary>World space center of mass.</summary>
        public Vector3 worldCenterOfMass => IsCreated ? B3Api.b3Body_GetWorldCenterOfMass(_bodyId) : transform.position;

        /// <summary>Is the body awake and simulating?</summary>
        public bool IsAwake => IsCreated && B3Api.b3Body_IsAwake(_bodyId);

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        void OnEnable()
        {
            EnsureCreated();
        }

        void OnDisable()
        {
            DestroyNativeBody();
        }

        /// <summary>Create the native body if needed. Colliders call this to guarantee ordering.</summary>
        public bool EnsureCreated()
        {
            if (IsCreated)
            {
                return true;
            }

            if (!isActiveAndEnabled)
            {
                return false;
            }

            _world = Box3DWorld.Main != null ? Box3DWorld.Main : FindWorld();
            if (_world == null || !_world.EnsureCreated())
            {
                Debug.LogError("[Box3D] No Box3DWorld in the scene. Add one before using Box3D components.", this);
                enabled = false;
                return false;
            }

            _transform = transform;

            B3BodyDef def = B3Api.b3DefaultBodyDef();
            def.type = (B3BodyType)_bodyType;
            def.position = _transform.position;
            def.rotation = _transform.rotation;
            def.linearDamping = linearDamping;
            def.angularDamping = angularDamping;
            def.gravityScale = gravityScale;
            def.sleepThreshold = sleepThreshold;
            def.enableSleep = (byte)(enableSleep ? 1 : 0);
            def.isBullet = (byte)(isBullet ? 1 : 0);
            def.allowFastRotation = (byte)(allowFastRotation ? 1 : 0);
            def.enableContactRecycling = (byte)(enableContactRecycling ? 1 : 0);
            def.motionLocks = new B3MotionLocks
            {
                linearX = (byte)(freezePositionX ? 1 : 0),
                linearY = (byte)(freezePositionY ? 1 : 0),
                linearZ = (byte)(freezePositionZ ? 1 : 0),
                angularX = (byte)(freezeRotationX ? 1 : 0),
                angularY = (byte)(freezeRotationY ? 1 : 0),
                angularZ = (byte)(freezeRotationZ ? 1 : 0),
            };

            _bodyId = B3Api.b3CreateBody(_world.WorldId, ref def);
            if (_bodyId.IsNull)
            {
                Debug.LogError("[Box3D] Failed to create body.", this);
                return false;
            }

            _lastWrittenPosition = _transform.position;
            _lastWrittenRotation = _transform.rotation;
            _prevPosition = _currPosition = _lastWrittenPosition;
            _prevRotation = _currRotation = _lastWrittenRotation;
            _hasKinematicTarget = false;

            _world.RegisterBody(this);

            // Colliders that enabled before this body (or attached as detached static
            // shapes because no body existed yet) re-attach to this body now.
            Box3DCollider[] colliders = GetComponentsInChildren<Box3DCollider>();
            for (int i = 0; i < colliders.Length; ++i)
            {
                Box3DCollider collider = colliders[i];
                if (collider.isActiveAndEnabled && (!collider.IsCreated || collider.Body == null))
                {
                    collider.Rebuild();
                }
            }

            // Joints on this GameObject that could not create earlier get another chance.
            Box3DJoint[] joints = GetComponents<Box3DJoint>();
            for (int i = 0; i < joints.Length; ++i)
            {
                joints[i].RetryCreate();
            }

            return true;
        }

        static Box3DWorld FindWorld()
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<Box3DWorld>();
#else
            return UnityEngine.Object.FindObjectOfType<Box3DWorld>();
#endif
        }

        void DestroyNativeBody()
        {
            if (_bodyId.IsNull)
            {
                return;
            }

            // Native body destruction destroys attached shapes; tell colliders to drop their handles.
            for (int i = _attachedColliders.Count - 1; i >= 0; --i)
            {
                _attachedColliders[i].NotifyBodyDestroyed();
            }

            _attachedColliders.Clear();

            if (_world != null)
            {
                _world.UnregisterBody(this);
                if (B3Api.b3Body_IsValid(_bodyId))
                {
                    B3Api.b3DestroyBody(_bodyId);
                }
            }

            _bodyId = default;
        }

        internal void NotifyWorldDestroyed()
        {
            _attachedColliders.Clear();
            _bodyId = default;
            _world = null;
        }

        internal void AttachCollider(Box3DCollider collider)
        {
            if (!_attachedColliders.Contains(collider))
            {
                _attachedColliders.Add(collider);
            }
        }

        internal void DetachCollider(Box3DCollider collider)
        {
            _attachedColliders.Remove(collider);
        }

        // ------------------------------------------------------------------
        // Transform synchronization
        // ------------------------------------------------------------------

        internal void PrePhysicsStep(float dt)
        {
            if (!IsCreated)
            {
                return;
            }

            if (_bodyType == Box3DBodyType.Kinematic)
            {
                if (_hasKinematicTarget)
                {
                    B3Api.b3Body_SetTargetTransform(_bodyId, new B3Transform(_kinematicTargetPosition, _kinematicTargetRotation), dt, true);
                    _hasKinematicTarget = false;
                    return;
                }

                if (TransformMovedExternally())
                {
                    // Follow the transform with a velocity so pushes look correct.
                    B3Api.b3Body_SetTargetTransform(_bodyId, new B3Transform(_transform.position, _transform.rotation), dt, true);
                    _lastWrittenPosition = _transform.position;
                    _lastWrittenRotation = _transform.rotation;
                }

                return;
            }

            if (TransformMovedExternally())
            {
                // The user moved the transform of a dynamic/static body: teleport.
                B3Api.b3Body_SetTransform(_bodyId, _transform.position, _transform.rotation);
                _lastWrittenPosition = _transform.position;
                _lastWrittenRotation = _transform.rotation;
                _prevPosition = _currPosition = _transform.position;
                _prevRotation = _currRotation = _transform.rotation;
            }
        }

        bool TransformMovedExternally()
        {
            Vector3 p = _transform.position;
            if ((p - _lastWrittenPosition).sqrMagnitude > 1e-10f)
            {
                return true;
            }

            Quaternion q = _transform.rotation;
            return Mathf.Abs(Quaternion.Dot(q, _lastWrittenRotation)) < 1.0f - 1e-6f;
        }

        internal void ApplyPhysicsPose(Vector3 position, Quaternion rotation, bool fellAsleep)
        {
            _prevPosition = _currPosition;
            _prevRotation = _currRotation;
            _currPosition = position;
            _currRotation = rotation;

            if (interpolation == Box3DInterpolation.None || _bodyType != Box3DBodyType.Dynamic)
            {
                _transform.SetPositionAndRotation(position, rotation);
                _lastWrittenPosition = position;
                _lastWrittenRotation = rotation;
            }

            if (fellAsleep)
            {
                FellAsleep?.Invoke();
            }
        }

        internal void InterpolationUpdate(float alpha)
        {
            if (interpolation != Box3DInterpolation.Interpolate || _bodyType != Box3DBodyType.Dynamic || !IsCreated)
            {
                return;
            }

            Vector3 p = Vector3.LerpUnclamped(_prevPosition, _currPosition, alpha);
            Quaternion q = Quaternion.SlerpUnclamped(_prevRotation, _currRotation, alpha);
            _transform.SetPositionAndRotation(p, q);
            _lastWrittenPosition = p;
            _lastWrittenRotation = q;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>Move a kinematic body to a pose over the next step, generating proper velocities.</summary>
        public void MovePosition(Vector3 position)
        {
            _kinematicTargetPosition = position;
            _kinematicTargetRotation = _hasKinematicTarget ? _kinematicTargetRotation : _transform.rotation;
            _hasKinematicTarget = true;
        }

        /// <summary>Rotate a kinematic body to a rotation over the next step, generating proper velocities.</summary>
        public void MoveRotation(Quaternion rotation)
        {
            _kinematicTargetRotation = rotation;
            _kinematicTargetPosition = _hasKinematicTarget ? _kinematicTargetPosition : _transform.position;
            _hasKinematicTarget = true;
        }

        /// <summary>Instantly move the body (and transform). Expensive; prefer creating bodies in place.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (IsCreated)
            {
                B3Api.b3Body_SetTransform(_bodyId, position, rotation);
            }

            _transform.SetPositionAndRotation(position, rotation);
            _lastWrittenPosition = position;
            _lastWrittenRotation = rotation;
            _prevPosition = _currPosition = position;
            _prevRotation = _currRotation = rotation;
        }

        /// <summary>Apply a continuous force (N) at the center of mass.</summary>
        public void AddForce(Vector3 force, bool wake = true)
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyForceToCenter(_bodyId, force, wake);
            }
        }

        /// <summary>Apply a continuous force (N) at a world position.</summary>
        public void AddForceAtPosition(Vector3 force, Vector3 position, bool wake = true)
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyForce(_bodyId, force, position, wake);
            }
        }

        /// <summary>Apply a torque (N*m).</summary>
        public void AddTorque(Vector3 torque, bool wake = true)
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyTorque(_bodyId, torque, wake);
            }
        }

        /// <summary>Apply an instant impulse (N*s) at the center of mass.</summary>
        public void AddImpulse(Vector3 impulse, bool wake = true)
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyLinearImpulseToCenter(_bodyId, impulse, wake);
            }
        }

        /// <summary>Apply an instant impulse (N*s) at a world position.</summary>
        public void AddImpulseAtPosition(Vector3 impulse, Vector3 position, bool wake = true)
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyLinearImpulse(_bodyId, impulse, position, wake);
            }
        }

        /// <summary>Apply an instant angular impulse (kg*m^2/s).</summary>
        public void AddAngularImpulse(Vector3 impulse, bool wake = true)
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyAngularImpulse(_bodyId, impulse, wake);
            }
        }

        /// <summary>Velocity of a world point attached to this body.</summary>
        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            return IsCreated ? B3Api.b3Body_GetWorldPointVelocity(_bodyId, worldPoint) : Vector3.zero;
        }

        /// <summary>Wake the body (and its island).</summary>
        public void WakeUp()
        {
            if (IsCreated)
            {
                B3Api.b3Body_SetAwake(_bodyId, true);
            }
        }

        /// <summary>Put the body (and its island) to sleep.</summary>
        public void Sleep()
        {
            if (IsCreated)
            {
                B3Api.b3Body_SetAwake(_bodyId, false);
            }
        }

        /// <summary>Recompute mass from attached shapes (after changing densities or shapes).</summary>
        public void RecomputeMass()
        {
            if (IsCreated)
            {
                B3Api.b3Body_ApplyMassFromShapes(_bodyId);
            }
        }

        /// <summary>Push runtime-editable inspector values to the native body.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3Api.b3Body_SetLinearDamping(_bodyId, linearDamping);
            B3Api.b3Body_SetAngularDamping(_bodyId, angularDamping);
            B3Api.b3Body_SetGravityScale(_bodyId, gravityScale);
            B3Api.b3Body_SetSleepThreshold(_bodyId, sleepThreshold);
            B3Api.b3Body_EnableSleep(_bodyId, enableSleep);
            B3Api.b3Body_SetBullet(_bodyId, isBullet);
            B3Api.b3Body_EnableContactRecycling(_bodyId, enableContactRecycling);
            B3Api.b3Body_SetMotionLocks(_bodyId, new B3MotionLocks
            {
                linearX = (byte)(freezePositionX ? 1 : 0),
                linearY = (byte)(freezePositionY ? 1 : 0),
                linearZ = (byte)(freezePositionZ ? 1 : 0),
                angularX = (byte)(freezeRotationX ? 1 : 0),
                angularY = (byte)(freezeRotationY ? 1 : 0),
                angularZ = (byte)(freezeRotationZ ? 1 : 0),
            });
        }

        void OnValidate()
        {
            if (Application.isPlaying && IsCreated)
            {
                ApplyProperties();
            }
        }
    }
}
