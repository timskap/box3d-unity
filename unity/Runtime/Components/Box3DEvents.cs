// SPDX-License-Identifier: MIT

using Box3D.Native;
using UnityEngine;

namespace Box3D
{
    /// <summary>Contact begin/end information delivered to both colliders of a touching pair.</summary>
    public struct Box3DContact
    {
        /// <summary>The collider receiving the callback.</summary>
        public Box3DCollider collider;

        /// <summary>The other collider. May be null if it was destroyed this step.</summary>
        public Box3DCollider otherCollider;

        /// <summary>Native contact id. Advanced: pass to B3Api contact functions while valid.</summary>
        public B3ContactId contactId;
    }

    /// <summary>High speed impact information (shapes with hit events enabled).</summary>
    public struct Box3DHit
    {
        public Box3DCollider collider;
        public Box3DCollider otherCollider;

        /// <summary>Approximate world contact point at the start of the step.</summary>
        public Vector3 point;

        /// <summary>Normal pointing from this collider toward the other collider.</summary>
        public Vector3 normal;

        /// <summary>Approach speed in m/s. Always positive.</summary>
        public float approachSpeed;
    }

    /// <summary>Sensor overlap information.</summary>
    public struct Box3DTrigger
    {
        /// <summary>The trigger (sensor) collider.</summary>
        public Box3DCollider sensor;

        /// <summary>The collider that entered or left the sensor. May be null if destroyed.</summary>
        public Box3DCollider visitor;
    }

    /// <summary>
    /// Implement on any component that lives on the same GameObject as a Box3DCollider
    /// (or its Box3DBody) to receive collision callbacks.
    /// </summary>
    public interface IBox3DCollisionHandler
    {
        void OnBox3DCollisionEnter(Box3DContact contact);
        void OnBox3DCollisionExit(Box3DContact contact);
    }

    /// <summary>Implement to receive trigger (sensor) callbacks. Fired on both the sensor and the visitor side.</summary>
    public interface IBox3DTriggerHandler
    {
        void OnBox3DTriggerEnter(Box3DTrigger trigger);
        void OnBox3DTriggerExit(Box3DTrigger trigger);
    }

    /// <summary>Implement to receive high-speed impact callbacks (requires hit events on the collider).</summary>
    public interface IBox3DHitHandler
    {
        void OnBox3DHit(Box3DHit hit);
    }
}
