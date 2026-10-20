# Tyria Realism

**0.10.0-preview.1 · Experimental preview; individual components tested, full new-install matrix incomplete**

A reversible graphics toolkit for Guild Wars 2: low-cost SDR/ReShade looks, an experimental AMD RDNA4 neural path and a guarded NVIDIA RTX 50 feeder preparation.

[Nexus mod](https://www.nexusmods.com/guildwars2/mods/7) · [Project page](https://dennisweini.github.io/tyria-realism/) · [Downloads](https://github.com/DennisWeini/tyria-realism/releases) · [Report a problem](https://github.com/DennisWeini/tyria-realism/issues)

> **Download notice:** Nexus has hidden the mod for manual moderation review. Do not treat the Nexus file as available or attempt to bypass that review. This repository provides the requested source code, build steps and checksums for inspection.

## Start here

Download a versioned release, extract it completely and run **Setup.cmd** with GW2 closed. **RX 9070 XT users should choose AMD RX 9000 neural.** After the neural setup works, run Setup again only if you want to add one visual profile: Universal Lite, Balanced or Quality. Visual setup needs an existing reviewed ReShade installation. Neural setup is optional, hardware-specific and may cost substantial GPU time.

Read [installation and rollback](docs/INSTALLATION.md), [hardware matrix](docs/COMPATIBILITY.md), [NVIDIA variants](docs/NVIDIA-VARIANTS.md) and [model sources](docs/MODEL-SOURCES.md). No NVIDIA DLLs, model weights, game assets or third-party binaries are bundled.

## Available variants

- Universal Lite uses Reinhard and LocalContrast for a low-cost visual change on AMD, NVIDIA and Intel candidates.
- Balanced adds BloomingHDR; Quality adds depth-based NeoSSAO. These are reduced Elora-inspired profiles.
- DLSSNR-AMD is the experimental RDNA4 route; only RX 9070 XT has been tested by this project.
- RTX 50 has a guarded DLSS5-Feeder preparation requiring signed user-supplied NVIDIA runtimes and one separately obtained consumer. It has not been live-tested by this project.
- RTX 20/30/40 neural rendering currently needs an external GPU-modified proprietary DLL. It is documented as manual research only and is not downloaded or supported.

## Evidence boundary

AMD Radeon RX 9070 XT, 16 GB: Windows 11 Pro; Ryzen 7 5800X; DX11; 2560x1440 SDR; driver 32.0.32015.2008. Real GW2 inference observed. Improvement is scene dependent and was often subtle; it is not a texture overhaul.

The NVIDIA installer and upstream packages passed static and isolated validation, but no NVIDIA/GW2 neural run was performed. Setup completion alone is not proof of inference; the Feeder log must show feature 18, model creation and continuing neural frames.

## Updates and development

[data/project.json](data/project.json) generates this README, compatibility page, project site and Nexus-ready description. CI checks every push and version tags build releases. Updates never silently replace the game installation.

Windows/.NET 9 developer check: `dotnet run --project src/TyriaLab -c Release -- selftest`. Publication boundary: `node tools/audit.mjs`. Release: `scripts/Build-Release.ps1`.

[Third-party notices](THIRD-PARTY-NOTICES.md) · [MIT license for original code](LICENSE)

