# Changelog

## [0.2.0] - 2026-07-05

Editor experience and documentation.

- `GameObject > Box3D` creation menu: Physics World, dynamic box/sphere/capsule,
  and static ground, ready to simulate (a world is created when the scene has none).
- Box/sphere/capsule colliders auto-fit to the render mesh when added, with a
  "Fit to Render Bounds" context-menu item and inspector button.
- Live editing in play mode: collider geometry edits (size, radius, mesh, trigger
  flag) rebuild the native shape, world tuning fields apply immediately
  (`Box3DWorld.ApplyProperties`), and inspector body-type changes take effect.
- Custom inspectors: compact Rigidbody-style freeze-constraint rows and runtime
  stats/actions on `Box3DBody`, live world stats and duplicate-world warning on
  `Box3DWorld`, setup diagnostics on colliders (missing/non-readable mesh, mesh
  collider under a dynamic body, non-uniform scale on round shapes, missing world).
- Joint gizmos: axes, hinge limit arcs, ball cone limits, prismatic/wheel travel
  ranges, distance joint anchors.
- Context-menu actions: Rebuild Shape on colliders; Wake Up, Sleep, Recompute Mass
  on bodies.
- Help links (`?` icon) on every component pointing at the package manual.
- Documentation: rewritten package manual and repository README; removed stale
  references to non-existent folders; fixed the package sample declaration.

## [0.1.0] - 2026-07-03

Initial release.

- Native Box3D bindings (world, bodies, shapes, joints, queries, events) with
  runtime ABI validation.
- Components: `Box3DWorld`, `Box3DBody`, sphere/capsule/box/convex/mesh colliders,
  hinge/ball/distance/prismatic/fixed/wheel/motor/parallel joints.
- Collision, trigger, and hit events via C# events and handler interfaces.
- Scene queries: raycast, raycast-all, sphere cast, overlap sphere/AABB, explosion.
- Prebuilt native binaries for macOS (universal), Windows (x86_64), and
  Linux (x86_64, glibc 2.31+), with platform-restricted plugin import settings.
- Native build scripts per platform plus a macOS cross-build script
  (MinGW for Windows, Docker for Linux).
- Demo sample.
