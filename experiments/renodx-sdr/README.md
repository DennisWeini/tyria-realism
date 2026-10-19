# GW2 RenoDX SDR source experiment

Base: NotVoosh/renodx commit `2dd89823ff0503f003d6b9c134ac04a64c48577e`.

Apply `guildwars2-sdr.patch` to a clean checkout of that exact commit. Initialize its pinned submodules including ReShade's ImGui dependency. Build only `guildwars2` with a C++20-compatible Windows toolchain, Windows SDK FXC, DXC and Slang as specified upstream. This local experiment built with MSVC19.44, Windows SDK10.0.26100.0, DXC1.9.2602.17 and Slang2026.14.1.

The patch changes the output to SDR BGRA8/BT.709, retains FP16 intermediates and fixes reference whites at100 rather than HDR display values. The retained HDR brightness controls are disabled. It is not an upstream-supported general SDR conversion and has no full colorimetric validation. Original GW2 shaders and user files are not included here.

Observed:116 shader compilations, limited GW2 replacements and saturation test affecting the world while HUD stayed colored. Combination with the AMD neural add-on ran in a separate small harness, not a complete in-game acceptance matrix. The experimental binary is deliberately not installed or distributed by the public toolkit. Do not run the private session-specific rollback scripts on another user's game.

