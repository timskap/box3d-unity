# Changelog

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
