# Changelog

## [0.1.0] - 2026-07-03

Initial release.

- Native Box3D bindings (world, bodies, shapes, joints, queries, events) with
  runtime ABI validation.
- Components: `Box3DWorld`, `Box3DBody`, sphere/capsule/box/convex/mesh colliders,
  hinge/ball/distance/prismatic/fixed/wheel/motor/parallel joints.
- Collision, trigger, and hit events via C# events and handler interfaces.
- Scene queries: raycast, raycast-all, sphere cast, overlap sphere/AABB, explosion.
- Prebuilt macOS universal binary; build scripts for Windows and Linux.
- Demo sample.
