# Compatibility — 2026-10-19

Experimental preview; individual components tested, full new-install matrix incomplete

| Hardware / environment | Visual effects | Neural path | Evidence level |
|---|---|---|---|
| RX 9070 XT | Tested in selected scenes | Tested experimentally | tested, limited scope |
| Other Radeon RX 9000 / RDNA4 | Expected with compatible DX11/ReShade driver | Backend target family; untested by this project | candidate, not guaranteed |
| Radeon RX 7000 and older | Possible for non-neural effects; untested here | Not supported by this pinned RDNA4 backend | visual-only candidate |
| NVIDIA GeForce / Intel Arc | Possible for non-neural effects; untested here | Not supported by this AMD backend; no alternative implemented | visual-only candidate |
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

### Installer and tooling

Windows 11 x64; Isolated folders and current source build.

23 native self-checks passed; tested archive/hash/signature validation and rejection of corrupt dependency archives. This is not a fresh end-to-end test of every new release option.

## Limits

- No native NVIDIA DLSS 5 integration, DLSS Super Resolution or FSR4 is implemented.
- No new GW2 texture pack, ray tracing, geometry or BRDF replacement is included.
- Engine motion vectors are not proven. The latest combination test used neutral motion/depth guides; camera-motion artifacts need testing.
- REST is not included or validated with the RenoDX experiment. Global effects may modify the HUD.
- Do not combine multiple bloom, AO or sharpening stacks without a controlled comparison.
- No ArenaNet endorsement or account/anti-cheat approval is claimed. Use is at the player's discretion.

