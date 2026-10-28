# Compatibility — 2026-10-20

Experimental preview; individual components tested, full new-install matrix incomplete

| Hardware / environment | Visual effects | Neural path | Evidence level |
|---|---|---|---|
| RX 9070 XT | Tested in selected scenes | Tested experimentally | tested, limited scope |
| Other Radeon RX 9000 / RDNA4 | Expected with compatible DX11/ReShade driver | Backend target family; untested by this project | candidate, not guaranteed |
| Radeon RX 6000 / RX 7000 | Universal Lite profile available; hardware not tested here | No proven GW2 DX11 route in this package; RDNA4 backend does not target these cards | visual-only candidate |
| NVIDIA GeForce RTX 50 | Universal Lite profile available; hardware not tested here | Guarded DLSS5-Feeder preparation implemented; live GW2 test still required | implemented, untested on target GPU |
| NVIDIA GeForce RTX 20 / 30 / 40 | Universal Lite profile available; hardware not tested here | Requires a third-party GPU-modified proprietary NR DLL; manual research only, not downloaded or supported | visual-only supported path |
| Intel Arc / other GPUs | Universal Lite profile available; untested here | No neural route implemented | visual-only candidate |
| Windows 10, HDR, Linux/Proton, other resolutions | Not validated by this project | No support claim for this Windows package | untested |

## Recorded tests

### Neural rendering

AMD Radeon RX 9070 XT, 16 GB; Windows 11 Pro; Ryzen 7 5800X; DX11; 2560x1440 SDR; driver 32.0.32015.2008.

Real GW2 inference observed. Improvement is scene dependent and was often subtle; it is not a texture overhaul.

### RenoDX SDR experiment

RX 9070 XT; GW2 DX11, 1440p SDR.

116 shaders compile; game shader replacements and world-only grading demonstrated. Experimental source patch; no ready-made binary bundled.

### RenoDX + neural combination

RX 9070 XT; Separate DX11 1280x720 harness.

Both loaded and more than 500 neural frames completed. Joint GW2 quality, motion stability and AFMF acceptance remain incomplete.

### AFMF

RX 9070 XT; Separate earlier GW2 DX11 runs, native and earlier Tyria setup.

AMD AFMF frames detected. An AMD Software UI restart resolved one inactive status. Latest look/combined stack has no full AFMF validation.

### Elora-inspired comparison

RX 9070 XT; Four shaders in a separate DX11 harness; user screenshots from GW2.

Runtime shader compilation passed. New Balanced/Quality look has no controlled GW2 FPS or full depth/HUD acceptance yet.

### NVIDIA RTX 50 feeder

Static analysis and isolated validation only; DLSS5-Feeder 1.19.0 plus pinned LumeniteFX source; no NVIDIA test GPU available.

Official package hashes, PE architecture and dependency strings were checked. Installer requires RTX 50, signed user-supplied NVIDIA runtimes and exactly one external consumer. No GW2 neural frame was claimed.

### Installer and tooling

Windows 11 x64; Isolated folders and current source build.

23 native self-checks passed; tested archive/hash/signature validation and rejection of corrupt dependency archives. This is not a fresh end-to-end test of every new release option.

## Limits

- RTX 50 setup prepares a DLSS5-Feeder contract but has no project live test on NVIDIA hardware. It is not DLSS Super Resolution, FSR4 or a native GW2 engine integration.
- RTX 20/30/40 neural rendering depends on an externally modified proprietary NVIDIA DLL whose safety, license and redistribution chain this project cannot establish.
- No new GW2 texture pack, ray tracing, geometry or BRDF replacement is included.
- Engine motion vectors are not proven. The latest combination test used neutral motion/depth guides; camera-motion artifacts need testing.
- REST is not included or validated with the RenoDX experiment. Global effects may modify the HUD.
- Do not combine multiple bloom, AO or sharpening stacks without a controlled comparison.
- No ArenaNet endorsement or account/anti-cheat approval is claimed. Use is at the player's discretion.
