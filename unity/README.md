# Box3D for Unity

Unity integration for [Box3D](https://box2d.org), Erin Catto's 3D physics engine.
The native engine runs through P/Invoke; on top of it this package provides a familiar
component workflow: a world, rigid bodies, colliders, joints, trigger/collision events,
and scene queries.

## Installation

1. Copy (or symlink) this `unity` folder into your Unity project's `Packages/` folder,
   for example `MyProject/Packages/com.box3d.unity`. Unity picks it up as an embedded
   package and generates `.meta` files on first import.
   - Alternatively: `Window > Package Manager > + > Add package from disk...` and select
     this folder's `package.json`.
2. The package ships with a prebuilt macOS universal binary
   (`Runtime/Plugins/macOS/libbox3d.dylib`, arm64 + x86_64). For Windows/Linux run the
   scripts in `Native~/` (requires CMake), which build from the repository root and copy
   the binary into `Runtime/Plugins/<Platform>/`.
3. Requires Unity 2021.3 or newer.

## Quick start

1. Add a **Box3D World** component to an empty GameObject.
2. Give any GameObject a **Box3D Box/Sphere/Capsule/Convex Collider** — without a body it
   is static, like a Unity collider without a Rigidbody.
3. Add a **Box3D Body** to make it dynamic. Colliders on the body's children are
   compounded into the same body, like Unity.
4. Press play.

```csharp
using Box3D;
using UnityEngine;

public class Example : MonoBehaviour, IBox3DCollisionHandler
{
    void Start()
    {
        var body = GetComponent<Box3DBody>();
        body.AddImpulse(Vector3.up * 5f);

        if (Box3DPhysics.Raycast(transform.position, Vector3.forward, 100f, out Box3DRaycastHit hit))
            Debug.Log($"hit {hit.collider.name} at {hit.point}");
    }

    public void OnBox3DCollisionEnter(Box3DContact contact) =>
        Debug.Log($"{contact.collider.name} touched {contact.otherCollider?.name}");

    public void OnBox3DCollisionExit(Box3DContact contact) { }
}
```

## Components

| Component | Purpose |
| --- | --- |
| `Box3DWorld` | Owns the simulation. Steps in `FixedUpdate`, dispatches events. Gravity, sub-steps, worker threads, sleeping, continuous collision, contact tuning. |
| `Box3DBody` | Rigid body (Static / Kinematic / Dynamic). Velocities, forces, impulses, damping, gravity scale, motion locks (freeze axes), bullet CCD, sleep control, interpolation, `MovePosition` / `MoveRotation` for kinematics. |
| `Box3DSphereCollider` | Sphere shape. |
| `Box3DCapsuleCollider` | Capsule shape with X/Y/Z direction. |
| `Box3DBoxCollider` | Box shape (8-vertex convex hull). |
| `Box3DConvexCollider` | Convex hull from a Mesh, simplified to a vertex budget. For dynamic bodies. |
| `Box3DMeshCollider` | Triangle mesh for static level geometry (native data shared and refcounted). |
| `Box3DHingeJoint` | Revolute joint: limits, motor, rotational spring. |
| `Box3DBallJoint` | Spherical joint: cone limit, twist limits, spring, 3D motor. |
| `Box3DDistanceJoint` | Rigid rod or spring between two anchor points; limits and motor. |
| `Box3DPrismaticJoint` | Slider: limits, motor, positional spring. |
| `Box3DFixedJoint` | Weld with optional linear/angular softness. |
| `Box3DWheelJoint` | Vehicle wheel: suspension, drive motor, steering. |
| `Box3DMotorJoint` | Velocity/pose controller with force limits. |
| `Box3DParallelJoint` | Keeps an axis aligned (keep characters upright). |

All joints sit on the moving body and connect to `connectedBody` (or the static world when
null). `breakForce` / `breakTorque` destroy the joint when exceeded and raise `Broke`.

### Collisions and triggers

Per collider: `friction`, `restitution`, `rollingResistance`, `density`, 64-bit category /
mask bits and group index for filtering, `isTrigger` for sensors.

Events arrive two ways:

- C# events: `collider.CollisionEntered/CollisionExited/HitOccurred/TriggerEntered/TriggerExited`.
- Interfaces on the collider's (or body's) GameObject: `IBox3DCollisionHandler`,
  `IBox3DTriggerHandler`, `IBox3DHitHandler`.

Contact events are on by default; **hit events** (fast impacts with point/normal/speed) are
opt-in per collider.

### Queries

`Box3DPhysics.Raycast / RaycastAll / SphereCast / OverlapSphere / OverlapAABB / Explode / IgnoreCollision`.

## Notes and limitations

- **Scale** is baked into shapes at creation. After changing `transform.lossyScale` or
  collider size fields at runtime, call `collider.Rebuild()`. Shear is not supported.
- **Mesh colliders** need `Read/Write` enabled on the mesh import settings and only
  collide as static geometry (a Box3D property). Use convex colliders on dynamic bodies.
- **Kinematic bodies**: move them with `MovePosition`/`MoveRotation` or by setting the
  transform; the body follows with proper velocities.
- Moving a dynamic body's transform directly teleports it (expensive; prefer forces).
- **Threads**: the world runs Box3D's internal scheduler. `workerCount = 0` picks
  half your cores (max 8).
- The native library must be a **single precision** build (default). The C# layer
  validates struct layout and precision at startup and disables itself on mismatch.
- iOS needs a static library build and `DllImport("__Internal")` (already handled by
  the bindings define) — not shipped yet.

## Rebuilding the native library

```
Native~/build_macos.sh      # universal dylib -> Runtime/Plugins/macOS
Native~/build_windows.bat   # box3d.dll       -> Runtime/Plugins/Windows
Native~/build_linux.sh      # libbox3d.so     -> Runtime/Plugins/Linux
```

After adding a new platform binary, select it in Unity and set its platform in the
plugin importer (Editor + Standalone for the matching OS/CPU).

## Sample

Package Manager > Box3D for Unity > Samples > **Demo** imports `Box3DDemo.cs`.
Drop it on an empty GameObject in an empty scene and press play: box stack, bouncy
balls, pendulum, hinged door, trigger zone. Left click shoots balls, right click
raycasts, Space explodes.

## License

MIT, same as Box3D. Box3D is developed by Erin Catto.
