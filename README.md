# Box3D for Unity

> [!IMPORTANT]
> **This repository is archived and no longer maintained.** Future development
> continues at **[Suvitruf/box3d-unity](https://github.com/Suvitruf/box3d-unity)**.

Unity integration for **[Box3D](https://box2d.org)**, Erin Catto's 3D physics engine
for games. This repository contains the Box3D engine source together with a Unity
package (`unity/`) that exposes it through a familiar component workflow — world,
rigid bodies, colliders, joints, collision/trigger events, and scene queries — backed
by the native library via P/Invoke.

## Repository layout

| Path | Contents |
| --- | --- |
| `unity/` | **The Unity package** (`com.box3d.unity`): components, native bindings, prebuilt binaries for Windows/macOS/Linux, and editor tooling. |
| `src/`, `include/` | Box3D engine source (portable C17). |
| `samples/` | Native samples app (sokol + imgui). |
| `test/`, `benchmark/` | Engine unit tests and benchmarks. |
| `docs/` | Engine user manual (Doxygen). |

## Using the Unity package

Quick version:

1. Copy (or symlink) the `unity/` folder into your project's `Packages/` folder
   (e.g. `MyProject/Packages/com.box3d.unity`), or use
   `Window > Package Manager > + > Add package from disk...` and pick `unity/package.json`.
2. In a scene: `GameObject > Box3D > Physics World`, then
   `GameObject > Box3D > Dynamic Box`. Press play.

```csharp
using Box3D;
using UnityEngine;

public class Kick : MonoBehaviour
{
    void Start() => GetComponent<Box3DBody>().AddImpulse(Vector3.up * 5f);
}
```

Prebuilt native binaries for all desktop platforms are included, so no native build
step is needed. Requires Unity 2021.3+.

**Full documentation: [`unity/README.md`](unity/README.md)** — installation, quick
start, editor workflow, a reference for every component, events, filtering, queries,
troubleshooting, and how to rebuild the native library.

### Unity feature overview

- `Box3DWorld` — owns and steps the simulation, live-tunable in play mode.
- `Box3DBody` — Static / Kinematic / Dynamic rigid bodies with damping, gravity
  scale, interpolation, CCD, sleep control, and per-axis freeze constraints.
- Colliders — sphere, capsule, box, convex hull, and static triangle mesh, with
  auto-fit to render bounds, per-shape materials, and 64-bit collision filtering.
- Joints — hinge, ball, distance, prismatic, fixed, wheel, motor, and parallel,
  with limits, motors, springs, breaking, and gizmo visualization.
- Events — collision enter/exit, high-speed hits, and trigger enter/exit via C#
  events or handler interfaces.
- Queries — raycast, raycast-all, sphere cast, overlap sphere/AABB, explosions.

## The Box3D engine

Box3D features continuous collision detection, a robust *Soft Step* rigid body
solver, convex hulls / capsules / spheres / triangle meshes / height fields,
sensors, joints with limits/motors/springs, extensive multithreading and SIMD,
and cross-platform determinism. It is written in portable C17 and developed by
[Erin Catto](https://github.com/erincatto).

[![Introducing Box3D](https://img.youtube.com/vi/jr_Fzl2XwKU/maxresdefault.jpg)](https://www.youtube.com/watch?v=jr_Fzl2XwKU)

### Building the engine

- Install [CMake](https://cmake.org/) and [git](https://git-scm.com/).
- With CMake presets (recommended):
  - Windows: `cmake --preset windows` then `cmake --build --preset windows-release`
  - Linux: `cmake --preset linux-release` then `cmake --build --preset linux-release`
  - macOS: `cmake --preset macos` then `cmake --build --preset macos-release`
- Visual Studio: run `build_vs2026.bat`, open `build/box3d.slnx`.
- Linux shell: run `build.sh`.
- Xcode: `cmake -G Xcode ..` in a `build` folder, open `box3d.xcodeproj`.

Run the samples app from the repository root:

- Windows: `.\build\bin\Release\samples.exe`
- Linux: `./build/bin/samples`
- macOS: `./build/bin/Release/samples`

To produce the shared library the Unity package loads, configure with
`-DBUILD_SHARED_LIBS=ON` — see
[Rebuilding the native library](unity/README.md#rebuilding-the-native-library).

### Using the engine from C/C++

The core library has no dependencies beyond the C runtime (and `libm` on Unix).
Linking it gives you the `box3d::box3d` target:

```cmake
include(FetchContent)
FetchContent_Declare(box3d
  GIT_REPOSITORY https://github.com/erincatto/box3d.git
  GIT_TAG v0.1.0)
FetchContent_MakeAvailable(box3d)

target_link_libraries(my_app PRIVATE box3d::box3d)
```

A vendored copy or submodule works with `add_subdirectory`, and an installed copy
(`cmake --install`) with `find_package(box3d 0.1 REQUIRED)`. See
[`docs/hello.md`](docs/hello.md) for a minimal first program.

### Compatibility

- The library builds and runs on Windows, Linux, and macOS.
- C17 compiler for the library; C++20 for the native samples.
- SSE2 / Neon SIMD is used for performance and can be disabled with
  `BOX3D_DISABLE_SIMD`.

## Documentation and community

- Unity package manual: [`unity/README.md`](unity/README.md)
- Engine user manual: [`docs/`](docs/) (Doxygen; enable the `BOX3D_DOCS` CMake
  option and build the `doc` target)
- [Discord](https://discord.gg/NKYgCBP) ·
  [GitHub Discussions](https://github.com/erincatto/box3d/discussions)

## License

Box3D is developed by Erin Catto and uses the [MIT license](LICENSE). The Unity
integration in `unity/` is MIT as well.
