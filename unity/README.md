# Box3D for Unity

Unity integration for [Box3D](https://box2d.org), Erin Catto's 3D physics engine.
The native engine runs through P/Invoke; on top of it this package provides a familiar
component workflow: a world, rigid bodies, colliders, joints, trigger/collision events,
and scene queries.

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Editor workflow](#editor-workflow)
- [Components](#components)
  - [Box3DWorld](#box3dworld)
  - [Box3DBody](#box3dbody)
  - [Colliders](#colliders)
  - [Joints](#joints)
- [Collision and trigger events](#collision-and-trigger-events)
- [Collision filtering](#collision-filtering)
- [Scene queries](#scene-queries)
- [Notes and limitations](#notes-and-limitations)
- [Troubleshooting](#troubleshooting)
- [Rebuilding the native library](#rebuilding-the-native-library)
- [License](#license)

## Requirements

- Unity **2021.3** or newer.
- A desktop platform. Prebuilt native binaries are included:

  | Platform | Binary | Notes |
  | --- | --- | --- |
  | macOS | `Runtime/Plugins/macOS/libbox3d.dylib` | Universal (arm64 + x86_64) |
  | Windows | `Runtime/Plugins/Windows/box3d.dll` | x86_64, self-contained |
  | Linux | `Runtime/Plugins/Linux/libbox3d.so` | x86_64, glibc 2.31+ (Ubuntu 20.04 or newer) |

  The shipped `.meta` files restrict each binary to its own platform (Editor +
  Standalone), so builds only include the matching library. iOS/Android are not
  shipped yet (see [Notes and limitations](#notes-and-limitations)).

## Installation

Option A — embedded package (recommended while iterating):

1. Copy (or symlink) this `unity` folder into your Unity project's `Packages/` folder,
   for example `MyProject/Packages/com.box3d.unity`.
2. Unity picks it up as an embedded package automatically on the next import.

Option B — local package reference:

1. `Window > Package Manager > + > Add package from disk...`
2. Select this folder's `package.json`.

## Quick start

The fastest way: **GameObject > Box3D > Physics World**, then
**GameObject > Box3D > Dynamic Box** (or Sphere/Capsule) and press play. The menu
creates a world when the scene has none and the objects come with a render mesh,
body, and fitted collider.

Manually:

1. Add a **Box3D World** component to an empty GameObject.
2. Give any GameObject a **Box3D Box/Sphere/Capsule/Convex Collider** — without a body it
   is static, like a Unity collider without a Rigidbody. New primitive colliders
   auto-fit themselves to the render mesh on the same GameObject.
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

## Editor workflow

The package is built to feel like Unity's own physics components:

- **Creation menu** — `GameObject > Box3D` creates a Physics World, dynamic
  box/sphere/capsule primitives, and a static ground, wired up and ready to simulate.
- **Auto-fit colliders** — adding a Box/Sphere/Capsule collider (or choosing *Reset*)
  fits it to the render mesh on the GameObject. Refit any time via the component's
  **Fit to Render Bounds** context-menu item or inspector button.
- **Live editing in play mode** — every inspector field can be tweaked while playing:
  material and filter fields are pushed to the native shape, geometry fields
  (size, radius, height, trigger flag, mesh) rebuild it, world tuning and joint
  limits/motors/springs apply immediately.
- **Gizmos** — selected colliders draw their exact native geometry (orange when a
  trigger), joints draw their anchor, axis, angular limit arcs, cone limits, and
  suspension/slide travel ranges.
- **Inspector diagnostics** — the inspector warns in place about missing or
  non-readable meshes, mesh colliders under dynamic bodies, non-uniform scale on
  round shapes, duplicate worlds, and a missing world in the scene.
- **Runtime panels** — in play mode the world shows awake-body stats and bodies show
  mass, velocities, and sleep state with Wake/Sleep/Recompute Mass buttons.
- **Context menus** — colliders: *Rebuild Shape*, *Fit to Render Bounds*; bodies:
  *Wake Up*, *Sleep*, *Recompute Mass*.
- **Help links** — the `?` icon on every Box3D component opens its section in this
  document.

## Components

### Box3DWorld

Owns the simulation. Steps during `FixedUpdate` and dispatches events afterwards.
One world per scene; bodies, colliders, and joints attach to the first enabled world
(`Box3DWorld.Main`).

Inspector: gravity, solver sub-steps, worker threads (0 = automatic), sleeping,
continuous collision, and contact tuning (restitution/hit thresholds, contact
stiffness/damping/speed, maximum body speed). All values can be changed live in
play mode; `ApplyProperties()` does the same from code.

Key script API: `Stepped` event, `AwakeBodyCount`, `SetGravity`, `Explode`,
`IgnoreCollision`, `FindCollider`, `FindBody`.

### Box3DBody

The equivalent of Unity's `Rigidbody`. Attach colliders to the same GameObject or its
children and they become shapes on this body.

- **Body types** — *Static* (never moves), *Kinematic* (moved by you, pushes dynamic
  bodies), *Dynamic* (fully simulated). Changing type at runtime is supported.
- **Damping, gravity scale, interpolation** — interpolation smooths rendering between
  fixed steps for dynamic bodies.
- **Sleep** — per-body enable + threshold; `FellAsleep` event.
- **Advanced** — bullet CCD for fast movers, fast-rotation override for wheels,
  contact recycling, and freeze position/rotation per world axis (drawn as compact
  constraint rows, like Rigidbody).

Key script API: `linearVelocity`, `angularVelocity`, `mass`, `AddForce`,
`AddForceAtPosition`, `AddTorque`, `AddImpulse`, `AddImpulseAtPosition`,
`AddAngularImpulse`, `GetPointVelocity`, `MovePosition` / `MoveRotation` (kinematic),
`Teleport`, `WakeUp` / `Sleep`, `RecomputeMass`, `ApplyProperties`.

### Colliders

All colliders share: `density` (drives body mass), `friction`, `restitution`,
`rollingResistance`, `isTrigger`, contact/hit event toggles, and
[filtering fields](#collision-filtering). A collider without a `Box3DBody` up the
hierarchy is static world geometry.

Transform scale is baked into the shape at creation. Colliders rebuild automatically
when you edit their fields in the inspector during play mode; after changing
`transform.lossyScale` or fields **from code** at runtime, call `Rebuild()`.

#### Box3DBoxCollider

Box shape (8-vertex convex hull). `center` + `size`, auto-fits to the render mesh
when added.

#### Box3DSphereCollider

Sphere shape. `center` + `radius`. With non-uniform scale the largest scale axis is
used, so the shape stays round. Auto-fits when added.

#### Box3DCapsuleCollider

Capsule with X/Y/Z direction, `center`, `radius`, and full `height` (including caps).
Auto-fit picks the longest render-bounds axis as the capsule direction.

#### Box3DConvexCollider

Convex hull built from a Mesh (assigned, or the MeshFilter's), simplified to a vertex
budget (`maxVertices`). The right choice for dynamic bodies with non-primitive shapes.
The mesh must have Read/Write enabled to work in players.

#### Box3DMeshCollider

Triangle mesh for static level geometry. Box3D only generates mesh contacts on static
bodies — use a convex collider on dynamic bodies (the inspector warns about this).
Native mesh data is shared and ref-counted between colliders using the same mesh,
scale, and offset. Optional vertex welding smooths edge collisions across triangles.

### Joints

All joints sit on the moving body's GameObject (they require a `Box3DBody`) and
connect to `connectedBody`, or to the static world when it is null. The `anchor` is in
this GameObject's local space. `breakForce` / `breakTorque` destroy the joint when
exceeded and raise the `Broke` event. `reactionForce` / `reactionTorque` expose the
current constraint load. Joint frames are captured once at creation from the current
poses; selected joints draw their anchor, axes, and limits as gizmos.

#### Box3DHingeJoint

Revolute joint: rotation about one axis. Angle limits (drawn as an arc), motor,
rotational spring. `angle`, `motorTorque` getters.

#### Box3DBallJoint

Ball-and-socket: free rotation around the anchor with optional cone limit (drawn as a
cone), twist limits, spring, and 3D motor. Ragdolls and chains.

#### Box3DDistanceJoint

Keeps a point on this body at a distance from a point on the connected body
(`connectedAnchor`). Rigid rod by default; enable the spring for rope/bungee behavior,
with optional min/max length limits. Rest length can be auto-configured from the
starting pose.

#### Box3DPrismaticJoint

Slider: this body translates along one axis with no relative rotation. Limits (drawn
as a travel range), motor, positional spring. Pistons, elevators, sliding doors.

#### Box3DFixedJoint

Weld. Optional linear/angular softness (Hz + damping ratio) for wobbly or breakable
structures.

#### Box3DWheelJoint

Vehicle wheel: put it on the wheel body, connect to the chassis. Suspension spring
with travel limits, drive motor, and steering with angle limits. Gizmos show the spin
axis and the suspension travel.

#### Box3DMotorJoint

Velocity and/or pose controller with force/torque limits that stays responsive to
collisions. Animated platforms, grabbed objects, top-down character friction.

#### Box3DParallelJoint

A spring that keeps one of this body's axes aligned with a world axis (or an axis on
the connected body). The classic use is keeping characters and vehicles upright.

## Collision and trigger events

Events arrive two ways:

- **C# events** on the collider:
  `CollisionEntered` / `CollisionExited` / `HitOccurred` / `TriggerEntered` / `TriggerExited`.
- **Interfaces** on any component of the collider's (or its body's) GameObject:
  `IBox3DCollisionHandler`, `IBox3DTriggerHandler`, `IBox3DHitHandler`.

Contact events are on by default. **Hit events** (fast impacts with point, normal, and
approach speed) are opt-in per collider and gated by the world's hit event threshold.
Trigger (sensor) events fire on both the sensor and the visitor.

## Collision filtering

Per collider, 64-bit filtering in the spirit of Box2D:

- `categoryBits` — what this shape *is* (usually a single bit).
- `maskBits` — what this shape *collides with*.
- `groupIndex` — non-zero override: negative = never collide with the same group,
  positive = always collide.

Two shapes collide when `(a.category & b.mask) != 0` **and** `(b.category & a.mask) != 0`,
unless a group index overrides it. Queries accept the same category/mask pair. To make
two specific bodies ignore each other, use `Box3DPhysics.IgnoreCollision(bodyA, bodyB)`.

## Scene queries

Static API in the spirit of `UnityEngine.Physics`, running against `Box3DWorld.Main`:

```csharp
Box3DPhysics.Raycast(origin, direction, maxDistance, out Box3DRaycastHit hit);
Box3DPhysics.RaycastAll(origin, direction, maxDistance, results);
Box3DPhysics.SphereCast(origin, radius, direction, maxDistance, out hit);
Box3DPhysics.OverlapSphere(center, radius, results);
Box3DPhysics.OverlapAABB(center, size, results);
Box3DPhysics.Explode(position, radius, impulsePerArea);
Box3DPhysics.IgnoreCollision(bodyA, bodyB);
```

`Box3DRaycastHit` carries the collider, point, normal, distance, fraction, and
triangle index (for mesh shapes). Queries run synchronously on the caller's thread
and must not be nested inside query callbacks.

## Notes and limitations

- **Scale** is baked into shapes at creation. Inspector edits rebuild the shape
  automatically in play mode; after changing scale or fields from code at runtime,
  call `collider.Rebuild()`. Shear is not supported.
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

## Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `[Box3D] No Box3DWorld in the scene` | Add a world: `GameObject > Box3D > Physics World`. |
| Collider ignores scale or size changes made from code | Geometry is baked at creation — call `collider.Rebuild()`. |
| `Mesh '...' is not readable` | Enable **Read/Write** in the mesh import settings. |
| Dynamic body passes through a mesh collider | Mesh colliders are static-only; use `Box3DConvexCollider` on dynamic bodies. |
| Fast body tunnels through walls | Enable `isBullet` on the body and keep `enableContinuous` on in the world. |
| Bodies jitter at rest on a character | Try disabling `enableContactRecycling` on the body. |
| `[Box3D] ABI mismatch` or double-precision error on startup | The native binary doesn't match the C# bindings — rebuild it in single precision from this repo (see below). |

## Rebuilding the native library

The binaries in `Runtime/Plugins/` are built from this repository's engine source with
CMake. On each target OS:

```bash
cmake -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON \
      -DBOX3D_SAMPLES=OFF -DBOX3D_UNIT_TESTS=OFF
cmake --build build --config Release
```

Copy the resulting `libbox3d.dylib` / `box3d.dll` / `libbox3d.so` over the matching
file in `Runtime/Plugins/<platform>/`. Keep the default single-precision configuration —
the C# layer refuses double-precision builds.

## License

MIT, same as Box3D. Box3D is developed by Erin Catto.
