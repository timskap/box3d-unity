// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>
    /// Base class for Box3D colliders. A collider becomes a shape on the closest
    /// Box3DBody up the hierarchy. Without a body it becomes a static shape, like
    /// Unity colliders without a Rigidbody. Transform scale is baked into the
    /// geometry at creation; call Rebuild() after changing scale or size fields
    /// at runtime.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public abstract class Box3DCollider : MonoBehaviour
    {
        [Tooltip("Mass density in kg/m^3 (water = 1000). Determines the body mass.")]
        public float density = 1000.0f;

        [Tooltip("Coulomb friction coefficient, usually 0..1.")]
        [Min(0.0f)] public float friction = 0.6f;

        [Tooltip("Bounciness, usually 0..1.")]
        [Min(0.0f)] public float restitution;

        [Tooltip("Rolling resistance 0..1. Spheres and capsules only.")]
        [Min(0.0f)] public float rollingResistance;

        [Tooltip("Triggers (sensors) detect overlaps but have no collision response.")]
        public bool isTrigger;

        [Header("Events")]
        [Tooltip("Raise collision enter/exit events for this shape (dynamic/kinematic bodies).")]
        public bool contactEvents = true;

        [Tooltip("Raise events for impacts faster than the world hit event threshold.")]
        public bool hitEvents;

        [Header("Filtering")]
        [Tooltip("Category bits of this shape. Usually a single bit.")]
        public ulong categoryBits = 1;

        [Tooltip("Categories this shape collides with.")]
        public ulong maskBits = ulong.MaxValue;

        [Tooltip("Non-zero group: negative never collides with same group, positive always collides.")]
        public int groupIndex;

        /// <summary>Collision began this step (contact events must be enabled).</summary>
        public event Action<Box3DContact> CollisionEntered;

        /// <summary>Collision ended this step.</summary>
        public event Action<Box3DContact> CollisionExited;

        /// <summary>High speed impact (hit events must be enabled).</summary>
        public event Action<Box3DHit> HitOccurred;

        /// <summary>Sensor overlap began (this collider is the sensor or the visitor).</summary>
        public event Action<Box3DTrigger> TriggerEntered;

        /// <summary>Sensor overlap ended.</summary>
        public event Action<Box3DTrigger> TriggerExited;

        /// <summary>Native shape id. Null when not created.</summary>
        public B3ShapeId ShapeId => _shapeId;

        /// <summary>The body this collider is attached to. Null for detached static colliders.</summary>
        public Box3DBody Body => _body;

        /// <summary>The world owning this collider's shape.</summary>
        public Box3DWorld World => _world;

        /// <summary>True while the native shape exists.</summary>
        public bool IsCreated => !_shapeId.IsNull && B3Api.b3Shape_IsValid(_shapeId);

        B3ShapeId _shapeId;
        Box3DBody _body;
        Box3DWorld _world;
        B3BodyId _ownedStaticBody; // used when there is no Box3DBody in the parents
        bool _builtIsTrigger;

        IBox3DCollisionHandler[] _collisionHandlers = Array.Empty<IBox3DCollisionHandler>();
        IBox3DTriggerHandler[] _triggerHandlers = Array.Empty<IBox3DTriggerHandler>();
        IBox3DHitHandler[] _hitHandlers = Array.Empty<IBox3DHitHandler>();

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        protected virtual void OnEnable()
        {
            CreateShape();
        }

        protected virtual void OnDisable()
        {
            DestroyShape();
        }

        /// <summary>Destroy and recreate the native shape. Call after changing geometry fields or transform scale.</summary>
        [ContextMenu("Rebuild Shape")]
        public void Rebuild()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            DestroyShape();
            CreateShape();
        }

        void CreateShape()
        {
            if (IsCreated)
            {
                return;
            }

            // Find the owning body, if any.
            _body = GetComponentInParent<Box3DBody>();
            B3BodyId bodyId;

            if (_body != null)
            {
                if (!_body.EnsureCreated())
                {
                    return;
                }

                _world = _body.World;
                bodyId = _body.BodyId;
            }
            else
            {
                // Detached collider: create a private static body at our transform.
                _world = Box3DWorld.Main != null ? Box3DWorld.Main : FindWorld();
                if (_world == null || !_world.EnsureCreated())
                {
                    Debug.LogError("[Box3D] No Box3DWorld in the scene. Add one before using Box3D components.", this);
                    enabled = false;
                    return;
                }

                B3BodyDef def = B3Api.b3DefaultBodyDef();
                def.position = transform.position;
                def.rotation = transform.rotation;
                _ownedStaticBody = B3Api.b3CreateBody(_world.WorldId, ref def);
                bodyId = _ownedStaticBody;
            }

            B3ShapeDef shapeDef = B3Api.b3DefaultShapeDef();
            shapeDef.density = density;
            shapeDef.baseMaterial.friction = friction;
            shapeDef.baseMaterial.restitution = restitution;
            shapeDef.baseMaterial.rollingResistance = rollingResistance;
            shapeDef.filter.categoryBits = categoryBits;
            shapeDef.filter.maskBits = maskBits;
            shapeDef.filter.groupIndex = groupIndex;
            shapeDef.isSensor = (byte)(isTrigger ? 1 : 0);
            shapeDef.enableSensorEvents = (byte)(isTrigger ? 1 : 0);
            shapeDef.enableContactEvents = (byte)(contactEvents && !isTrigger ? 1 : 0);
            shapeDef.enableHitEvents = (byte)(hitEvents && !isTrigger ? 1 : 0);

            _shapeId = CreateNativeShape(bodyId, ref shapeDef);
            if (_shapeId.IsNull)
            {
                Debug.LogError($"[Box3D] Failed to create shape for {GetType().Name}.", this);
                ReleaseOwnedBody();
                return;
            }

            _builtIsTrigger = isTrigger;
            _world.RegisterCollider(_shapeId, this);
            _body?.AttachCollider(this);
            RefreshHandlers();
        }

        void DestroyShape()
        {
            if (!_shapeId.IsNull)
            {
                _world?.UnregisterCollider(_shapeId);
                _body?.DetachCollider(this);

                if (B3Api.b3Shape_IsValid(_shapeId))
                {
                    B3Api.b3DestroyShape(_shapeId, true);
                }

                _shapeId = default;
                OnShapeDestroyed();
            }

            ReleaseOwnedBody();
            _body = null;
        }

        void ReleaseOwnedBody()
        {
            if (!_ownedStaticBody.IsNull)
            {
                if (B3Api.b3Body_IsValid(_ownedStaticBody))
                {
                    B3Api.b3DestroyBody(_ownedStaticBody);
                }

                _ownedStaticBody = default;
            }
        }

        /// <summary>Called by the body when the native body (and its shapes) was destroyed.</summary>
        internal void NotifyBodyDestroyed()
        {
            if (!_shapeId.IsNull)
            {
                _world?.UnregisterCollider(_shapeId);
                _shapeId = default;
                OnShapeDestroyed();
            }

            _body = null;
        }

        /// <summary>Called by the world when the whole native world was destroyed.</summary>
        internal void NotifyWorldDestroyed()
        {
            _shapeId = default;
            _ownedStaticBody = default;
            _body = null;
            _world = null;
            OnShapeDestroyed();
        }

        static Box3DWorld FindWorld()
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<Box3DWorld>();
#else
            return UnityEngine.Object.FindObjectOfType<Box3DWorld>();
#endif
        }

        /// <summary>Create the native geometry. Implemented by concrete collider types.</summary>
        protected abstract B3ShapeId CreateNativeShape(B3BodyId bodyId, ref B3ShapeDef def);

        /// <summary>Optional cleanup of geometry resources held by the concrete collider.</summary>
        protected virtual void OnShapeDestroyed()
        {
        }

        // ------------------------------------------------------------------
        // Geometry helpers for subclasses
        // ------------------------------------------------------------------

        /// <summary>
        /// Pose of this collider in the owning body's local space, plus the world scale
        /// to bake into geometry.
        /// </summary>
        protected void GetBodyLocalPose(out Vector3 localPosition, out Quaternion localRotation, out Vector3 scale)
        {
            scale = transform.lossyScale;

            Transform bodyTransform = _body != null ? _body.transform : transform;
            if (bodyTransform == transform)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                return;
            }

            Quaternion invBodyRotation = Quaternion.Inverse(bodyTransform.rotation);
            localPosition = invBodyRotation * (transform.position - bodyTransform.position);
            localRotation = invBodyRotation * transform.rotation;
        }

        /// <summary>Largest absolute scale component; used for radii.</summary>
        protected static float MaxAbs(float a, float b)
        {
            return Mathf.Max(Mathf.Abs(a), Mathf.Abs(b));
        }

        /// <summary>
        /// Local-space render bounds of this GameObject (MeshFilter or SkinnedMeshRenderer),
        /// used to auto-fit primitive colliders.
        /// </summary>
        protected bool TryGetLocalRenderBounds(out Bounds bounds)
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                bounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            var skinned = GetComponent<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                bounds = skinned.localBounds;
                return true;
            }

            bounds = default;
            return false;
        }

        // ------------------------------------------------------------------
        // Runtime property updates
        // ------------------------------------------------------------------

        /// <summary>Push material and filter fields to the live native shape.</summary>
        public void ApplyProperties()
        {
            if (!IsCreated)
            {
                return;
            }

            B3SurfaceMaterial material = B3Api.b3Shape_GetSurfaceMaterial(_shapeId);
            material.friction = friction;
            material.restitution = restitution;
            material.rollingResistance = rollingResistance;
            B3Api.b3Shape_SetSurfaceMaterial(_shapeId, material);
            B3Api.b3Shape_SetDensity(_shapeId, density, true);

            B3Filter filter = B3Api.b3Shape_GetFilter(_shapeId);
            if (filter.categoryBits != categoryBits || filter.maskBits != maskBits || filter.groupIndex != groupIndex)
            {
                filter.categoryBits = categoryBits;
                filter.maskBits = maskBits;
                filter.groupIndex = groupIndex;
                B3Api.b3Shape_SetFilter(_shapeId, filter, true);
            }

            B3Api.b3Shape_EnableContactEvents(_shapeId, contactEvents && !isTrigger);
            B3Api.b3Shape_EnableHitEvents(_shapeId, hitEvents && !isTrigger);
        }

        /// <summary>
        /// True when serialized geometry fields no longer match the built native shape.
        /// Subclasses extend this so inspector edits rebuild the shape live in play mode.
        /// </summary>
        protected virtual bool GeometryOutOfDate => isTrigger != _builtIsTrigger;

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying || !IsCreated)
            {
                return;
            }

            if (GeometryOutOfDate)
            {
                Rebuild();
            }
            else
            {
                ApplyProperties();
            }
        }

        /// <summary>Re-scan this GameObject (and the body's) for event handler components.</summary>
        public void RefreshHandlers()
        {
            var collisionHandlers = new List<IBox3DCollisionHandler>();
            var triggerHandlers = new List<IBox3DTriggerHandler>();
            var hitHandlers = new List<IBox3DHitHandler>();

            GetComponents(collisionHandlers);
            GetComponents(triggerHandlers);
            GetComponents(hitHandlers);

            if (_body != null && _body.gameObject != gameObject)
            {
                collisionHandlers.AddRange(_body.GetComponents<IBox3DCollisionHandler>());
                triggerHandlers.AddRange(_body.GetComponents<IBox3DTriggerHandler>());
                hitHandlers.AddRange(_body.GetComponents<IBox3DHitHandler>());
            }

            _collisionHandlers = collisionHandlers.ToArray();
            _triggerHandlers = triggerHandlers.ToArray();
            _hitHandlers = hitHandlers.ToArray();
        }

        // ------------------------------------------------------------------
        // Event dispatch (called by Box3DWorld)
        // ------------------------------------------------------------------

        internal void DispatchCollisionEnter(Box3DContact contact)
        {
            CollisionEntered?.Invoke(contact);
            for (int i = 0; i < _collisionHandlers.Length; ++i)
            {
                try
                {
                    _collisionHandlers[i].OnBox3DCollisionEnter(contact);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        internal void DispatchCollisionExit(Box3DContact contact)
        {
            CollisionExited?.Invoke(contact);
            for (int i = 0; i < _collisionHandlers.Length; ++i)
            {
                try
                {
                    _collisionHandlers[i].OnBox3DCollisionExit(contact);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        internal void DispatchHit(Box3DHit hit)
        {
            HitOccurred?.Invoke(hit);
            for (int i = 0; i < _hitHandlers.Length; ++i)
            {
                try
                {
                    _hitHandlers[i].OnBox3DHit(hit);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        internal void DispatchTriggerEnter(Box3DTrigger trigger)
        {
            TriggerEntered?.Invoke(trigger);
            for (int i = 0; i < _triggerHandlers.Length; ++i)
            {
                try
                {
                    _triggerHandlers[i].OnBox3DTriggerEnter(trigger);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        internal void DispatchTriggerExit(Box3DTrigger trigger)
        {
            TriggerExited?.Invoke(trigger);
            for (int i = 0; i < _triggerHandlers.Length; ++i)
            {
                try
                {
                    _triggerHandlers[i].OnBox3DTriggerExit(trigger);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }
    }
}
