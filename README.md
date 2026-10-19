# Tyria Realism

**0.9.0-preview.1 · Experimental preview; individual components tested, full new-install matrix incomplete**

A reversible graphics toolkit for Guild Wars 2: SDR/ReShade looks, an optional experimental AMD neural path and documented compatibility.

[Nexus mod](https://www.nexusmods.com/guildwars2/mods/7) · [Project page](https://dennisweini.github.io/tyria-realism/) · [Downloads](https://github.com/DennisWeini/tyria-realism/releases) · [Report a problem](https://github.com/DennisWeini/tyria-realism/issues)

## Start here

Download a versioned release, extract it completely and run **Setup.cmd** with GW2 closed. Visual-only setup needs an existing reviewed ReShade installation. Neural setup requires a compatible AMD card and model source; it can cost significant GPU time and often makes only a subtle difference. It is optional.

Read [installation and uninstall](docs/INSTALLATION.md), [hardware/test matrix](docs/COMPATIBILITY.md) and [model source choices](docs/MODEL-SOURCES.md) before installing. No NVIDIA DLLs, model weights, game assets or third-party binaries are bundled. Source downloads remain pinned and hash checked.

## What is available

- Original TyriaMaterials detail shader; no replacement textures.
- Elora-inspired Balanced/Quality comparison INIs with optional verified shader-source retrieval. This is a reduced adaptation, not Elora's full preset.
- Optional DLSSNR-AMD integration, own-DLL selection and an explicitly selected RHI third-party source. No native NVIDIA support is claimed.
- Source of the Windows toolkit and a developer-only RenoDX SDR patch. The experimental add-on is not installed by Setup.cmd.
- Guarded setup/uninstall helpers, update information and public test status.

## Tested vs expected

AMD Radeon RX 9070 XT, 16 GB: Windows 11 Pro; Ryzen 7 5800X; DX11; 2560x1440 SDR; driver 32.0.32015.2008.

Real GW2 inference observed. Improvement is scene dependent and was often subtle; it is not a texture overhaul.

Other RDNA4 cards are candidates, not tested targets. Older AMD/NVIDIA/Intel GPUs may run ordinary ReShade effects; this pinned neural backend is not supported on them. HDR and other operating systems are not validated here. See the full matrix; no minimum-VRAM or universal FPS claim is made from one card.

## Latest data and updates

[data/project.json](data/project.json) is the source for this README, the compatibility page, the project site and [Nexus description text](docs/NEXUS-DESCRIPTION.txt). Run `node tools/generate-docs.mjs` after editing it. CI checks/builds on every push; version tags generate a new toolkit release. Updates never silently replace the user's game installation or switch unreviewed dependencies to latest. `scripts/Check-Updates.ps1 -IncludePreview` only reports releases.

Nexus descriptions and files are snapshots: they link to current GitHub data, but Nexus does not execute our page JavaScript. No automatic authenticated Nexus upload is implemented. Upload the generated description and matching versioned ZIP when releasing there; do not claim the old Nexus file contains newer changes.

## Deutsch

Experimenteller Stand. Erst GW2 beenden, ZIP entpacken und Setup.cmd starten. Neural ist optional und kann GPU-Zeit kosten, ohne sichtbar besser auszusehen. Nur RX 9070 XT/Windows11/DX11/1440p SDR wurde hier praktisch geprueft. Vor Entfernen des Toolkits zuerst die passende Rueckfallfunktion ausfuehren.

## Development

Windows, .NET 9 SDK for optional native tooling: `dotnet run --project src/TyriaLab -c Release -- selftest` (23 checks passed locally). `node tools/audit.mjs` checks publication boundaries; `scripts/Build-Release.ps1` creates an allowlisted script-only ZIP. Full fresh-game installation and latest-stack quality/performance acceptance remain incomplete.

[Third-party notices](THIRD-PARTY-NOTICES.md) · [MIT license for original code](LICENSE)

