// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Box3D.Native
{
    /// <summary>
    /// Builds and caches native b3MeshData from Unity meshes.
    /// Native mesh shapes hold a reference to this data for their lifetime, so entries
    /// are refcounted: Acquire when a shape is created, Release when it is destroyed.
    /// Scale and the collider's body-local pose are baked into the vertices, so they
    /// are part of the cache key.
    /// </summary>
    public static class B3MeshCache
    {
        struct Entry
        {
            public IntPtr meshData;
            public int refCount;
        }

        struct Key : IEquatable<Key>
        {
            public int meshId;
            public Vector3 scale;
            public Vector3 position;
            public Quaternion rotation;
            public bool weld;

            public bool Equals(Key other)
            {
                return meshId == other.meshId && scale == other.scale && position == other.position &&
                       rotation == other.rotation && weld == other.weld;
            }

            public override int GetHashCode()
            {
                return meshId ^ scale.GetHashCode() ^ position.GetHashCode() ^ (weld ? 1 : 0);
            }
        }

        static readonly Dictionary<Key, Entry> Cache = new Dictionary<Key, Entry>();
        static readonly Dictionary<IntPtr, Key> Reverse = new Dictionary<IntPtr, Key>();

        /// <summary>Get (or build) native mesh data for a Unity mesh with the given bake transform.</summary>
        public static IntPtr Acquire(Mesh mesh, Vector3 scale, Vector3 position, Quaternion rotation, bool weldVertices)
        {
            if (mesh == null)
            {
                return IntPtr.Zero;
            }

            var key = new Key
            {
                meshId = mesh.GetInstanceID(),
                scale = scale,
                position = position,
                rotation = rotation,
                weld = weldVertices,
            };

            if (Cache.TryGetValue(key, out Entry entry))
            {
                entry.refCount++;
                Cache[key] = entry;
                return entry.meshData;
            }

            IntPtr data = Build(mesh, scale, position, rotation, weldVertices);
            if (data == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            Cache[key] = new Entry { meshData = data, refCount = 1 };
            Reverse[data] = key;
            return data;
        }

        /// <summary>Release a reference obtained from Acquire. Frees the native data on last release.</summary>
        public static void Release(IntPtr meshData)
        {
            if (meshData == IntPtr.Zero)
            {
                return;
            }

            if (!Reverse.TryGetValue(meshData, out Key key) || !Cache.TryGetValue(key, out Entry entry))
            {
                // Unknown pointer (should not happen) - free directly to avoid a leak.
                B3Api.b3DestroyMesh(meshData);
                return;
            }

            entry.refCount--;
            if (entry.refCount <= 0)
            {
                Cache.Remove(key);
                Reverse.Remove(meshData);
                B3Api.b3DestroyMesh(meshData);
            }
            else
            {
                Cache[key] = entry;
            }
        }

        static unsafe IntPtr Build(Mesh mesh, Vector3 scale, Vector3 position, Quaternion rotation, bool weldVertices)
        {
            if (!mesh.isReadable && Application.isPlaying)
            {
                Debug.LogError($"[Box3D] Mesh '{mesh.name}' is not readable. Enable Read/Write in the mesh import settings.", mesh);
                return IntPtr.Zero;
            }

            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;
            if (vertices.Length < 3 || indices.Length < 3)
            {
                Debug.LogError($"[Box3D] Mesh '{mesh.name}' has no triangles.", mesh);
                return IntPtr.Zero;
            }

            bool identityPose = position == Vector3.zero && rotation == Quaternion.identity;
            if (scale != Vector3.one || !identityPose)
            {
                for (int i = 0; i < vertices.Length; ++i)
                {
                    vertices[i] = position + rotation * Vector3.Scale(vertices[i], scale);
                }
            }

            // Negative scale components flip the winding; fix it so collision normals stay outward.
            if (scale.x * scale.y * scale.z < 0.0f)
            {
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                }
            }

            fixed (Vector3* pVerts = vertices)
            fixed (int* pIndices = indices)
            {
                var def = new B3MeshDef
                {
                    vertices = (IntPtr)pVerts,
                    indices = (IntPtr)pIndices,
                    materialIndices = IntPtr.Zero,
                    weldTolerance = 0.001f,
                    vertexCount = vertices.Length,
                    triangleCount = indices.Length / 3,
                    weldVertices = (byte)(weldVertices ? 1 : 0),
                    useMedianSplit = 0,
                    identifyEdges = 1,
                };

                return B3Api.b3CreateMesh(ref def, IntPtr.Zero, 0);
            }
        }
    }
}
