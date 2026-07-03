// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using AOT;
using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>Result of a Box3D scene query.</summary>
    public struct Box3DRaycastHit
    {
        /// <summary>The collider that was hit. Null if the shape has no managed collider.</summary>
        public Box3DCollider collider;

        /// <summary>World hit point.</summary>
        public Vector3 point;

        /// <summary>World surface normal at the hit point.</summary>
        public Vector3 normal;

        /// <summary>Distance from the ray origin in meters.</summary>
        public float distance;

        /// <summary>Fraction along the cast [0..1].</summary>
        public float fraction;

        /// <summary>Triangle index for mesh shapes, -1 otherwise.</summary>
        public int triangleIndex;
    }

    /// <summary>
    /// Static scene queries against the main Box3DWorld, in the spirit of UnityEngine.Physics.
    /// All queries run synchronously on the caller's thread and must not be nested
    /// (do not issue a query from inside a query callback).
    /// </summary>
    public static class Box3DPhysics
    {
        /// <summary>The world queries run against (Box3DWorld.Main).</summary>
        public static Box3DWorld World => Box3DWorld.Main;

        // Static query state; queries are main-thread and non-reentrant.
        static Box3DWorld _activeWorld;
        static List<Box3DRaycastHit> _hitResults;
        static List<Box3DCollider> _overlapResults;
        static Vector3 _castOrigin;
        static Vector3 _castTranslation;

        static readonly B3CastResultFcn CollectHitsCallback = CollectHits;
        static readonly B3OverlapResultFcn CollectOverlapCallback = CollectOverlap;

        // ------------------------------------------------------------------
        // Raycast
        // ------------------------------------------------------------------

        /// <summary>Cast a ray and return the closest hit.</summary>
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out Box3DRaycastHit hit,
            ulong categoryBits = ulong.MaxValue, ulong maskBits = ulong.MaxValue)
        {
            return Raycast(World, origin, direction, maxDistance, out hit, categoryBits, maskBits);
        }

        /// <summary>Cast a ray in a specific world and return the closest hit.</summary>
        public static bool Raycast(Box3DWorld world, Vector3 origin, Vector3 direction, float maxDistance,
            out Box3DRaycastHit hit, ulong categoryBits = ulong.MaxValue, ulong maskBits = ulong.MaxValue)
        {
            hit = default;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            Vector3 translation = direction.normalized * maxDistance;
            B3QueryFilter filter = MakeFilter(categoryBits, maskBits);
            B3RayResult result = B3Api.b3World_CastRayClosest(world.WorldId, origin, translation, filter);
            if (result.hit == 0)
            {
                return false;
            }

            hit = new Box3DRaycastHit
            {
                collider = world.FindCollider(result.shapeId),
                point = result.point,
                normal = result.normal,
                fraction = result.fraction,
                distance = result.fraction * maxDistance,
                triangleIndex = result.triangleIndex,
            };
            return true;
        }

        /// <summary>Collect every shape along a ray. Results are unordered. Returns the hit count.</summary>
        public static int RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, List<Box3DRaycastHit> results,
            ulong categoryBits = ulong.MaxValue, ulong maskBits = ulong.MaxValue)
        {
            results.Clear();
            Box3DWorld world = World;
            if (world == null || !world.IsCreated)
            {
                return 0;
            }

            Vector3 translation = direction.normalized * maxDistance;

            _activeWorld = world;
            _hitResults = results;
            _castOrigin = origin;
            _castTranslation = translation;

            B3Api.b3World_CastRay(world.WorldId, origin, translation, MakeFilter(categoryBits, maskBits),
                CollectHitsCallback, IntPtr.Zero);

            _activeWorld = null;
            _hitResults = null;
            return results.Count;
        }

        [MonoPInvokeCallback(typeof(B3CastResultFcn))]
        static float CollectHits(B3ShapeId shapeId, Vector3 point, Vector3 normal, float fraction,
            ulong userMaterialId, int triangleIndex, int childIndex, IntPtr context)
        {
            _hitResults.Add(new Box3DRaycastHit
            {
                collider = _activeWorld != null ? _activeWorld.FindCollider(shapeId) : null,
                point = point,
                normal = normal,
                fraction = fraction,
                distance = fraction * _castTranslation.magnitude,
                triangleIndex = triangleIndex,
            });

            return 1.0f; // keep going, collect everything
        }

        // ------------------------------------------------------------------
        // Shape casts
        // ------------------------------------------------------------------

        /// <summary>Sweep a sphere along a direction and return the closest hit.</summary>
        public static unsafe bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance,
            out Box3DRaycastHit hit, ulong categoryBits = ulong.MaxValue, ulong maskBits = ulong.MaxValue)
        {
            hit = default;
            Box3DWorld world = World;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            Vector3 translation = direction.normalized * maxDistance;

            _activeWorld = world;
            _hitResults = _scratchHits;
            _scratchHits.Clear();
            _castOrigin = origin;
            _castTranslation = translation;

            Vector3 localPoint = Vector3.zero;
            var proxy = new B3ShapeProxy { points = (IntPtr)(&localPoint), count = 1, radius = radius };
            B3Api.b3World_CastShape(world.WorldId, origin, ref proxy, translation, MakeFilter(categoryBits, maskBits),
                ClosestHitCallback, IntPtr.Zero);

            _activeWorld = null;
            _hitResults = null;

            if (_scratchHits.Count == 0)
            {
                return false;
            }

            hit = _scratchHits[_scratchHits.Count - 1];
            return true;
        }

        static readonly List<Box3DRaycastHit> _scratchHits = new List<Box3DRaycastHit>(1);
        static readonly B3CastResultFcn ClosestHitCallback = ClosestHit;

        [MonoPInvokeCallback(typeof(B3CastResultFcn))]
        static float ClosestHit(B3ShapeId shapeId, Vector3 point, Vector3 normal, float fraction,
            ulong userMaterialId, int triangleIndex, int childIndex, IntPtr context)
        {
            _hitResults.Add(new Box3DRaycastHit
            {
                collider = _activeWorld != null ? _activeWorld.FindCollider(shapeId) : null,
                point = point,
                normal = normal,
                fraction = fraction,
                distance = fraction * _castTranslation.magnitude,
                triangleIndex = triangleIndex,
            });

            // Clip the cast so only closer hits are reported after this one.
            return fraction;
        }

        // ------------------------------------------------------------------
        // Overlaps
        // ------------------------------------------------------------------

        /// <summary>Collect all colliders overlapping a sphere. Returns the count.</summary>
        public static unsafe int OverlapSphere(Vector3 center, float radius, List<Box3DCollider> results,
            ulong categoryBits = ulong.MaxValue, ulong maskBits = ulong.MaxValue)
        {
            results.Clear();
            Box3DWorld world = World;
            if (world == null || !world.IsCreated)
            {
                return 0;
            }

            _activeWorld = world;
            _overlapResults = results;

            Vector3 localPoint = Vector3.zero;
            var proxy = new B3ShapeProxy { points = (IntPtr)(&localPoint), count = 1, radius = radius };
            B3Api.b3World_OverlapShape(world.WorldId, center, ref proxy, MakeFilter(categoryBits, maskBits),
                CollectOverlapCallback, IntPtr.Zero);

            _activeWorld = null;
            _overlapResults = null;
            return results.Count;
        }

        /// <summary>Collect all colliders whose bounds overlap a world-space box. Returns the count.</summary>
        public static int OverlapAABB(Vector3 center, Vector3 size, List<Box3DCollider> results,
            ulong categoryBits = ulong.MaxValue, ulong maskBits = ulong.MaxValue)
        {
            results.Clear();
            Box3DWorld world = World;
            if (world == null || !world.IsCreated)
            {
                return 0;
            }

            _activeWorld = world;
            _overlapResults = results;

            var aabb = new B3AABB { lowerBound = center - 0.5f * size, upperBound = center + 0.5f * size };
            B3Api.b3World_OverlapAABB(world.WorldId, aabb, MakeFilter(categoryBits, maskBits),
                CollectOverlapCallback, IntPtr.Zero);

            _activeWorld = null;
            _overlapResults = null;
            return results.Count;
        }

        [MonoPInvokeCallback(typeof(B3OverlapResultFcn))]
        static byte CollectOverlap(B3ShapeId shapeId, IntPtr context)
        {
            Box3DCollider collider = _activeWorld != null ? _activeWorld.FindCollider(shapeId) : null;
            if (collider != null)
            {
                _overlapResults.Add(collider);
            }

            return 1; // continue
        }

        // ------------------------------------------------------------------
        // Misc
        // ------------------------------------------------------------------

        /// <summary>Apply a radial explosion impulse in the main world.</summary>
        public static void Explode(Vector3 position, float radius, float impulsePerArea, float falloff = 0.0f)
        {
            World?.Explode(position, radius, impulsePerArea, falloff);
        }

        /// <summary>Permanently disable collision between two bodies.</summary>
        public static void IgnoreCollision(Box3DBody bodyA, Box3DBody bodyB)
        {
            World?.IgnoreCollision(bodyA, bodyB);
        }

        static B3QueryFilter MakeFilter(ulong categoryBits, ulong maskBits)
        {
            B3QueryFilter filter = B3QueryFilter.Default;
            filter.categoryBits = categoryBits;
            filter.maskBits = maskBits;
            return filter;
        }
    }
}
