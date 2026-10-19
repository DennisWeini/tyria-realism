# Neural model provenance and choices

Visual-only presets do not need NVIDIA files. Neural rendering needs compatible model weights. We neither upload NVIDIA DLLs nor extracted model weights in this repository, Pages site, Nexus file or GitHub release.

For the NVIDIA generation-specific paths, see `NVIDIA-VARIANTS.md`. RTX 50 can use unchanged NVIDIA-signed runtimes together with a separately obtained consumer. RTX 20/30/40 currently require a GPU-modified proprietary NR DLL; the upstream Feeder points to the RenoDX Discord, but this project found no stable public artifact or redistribution grant and therefore does not mirror or auto-download it.

**Default:** supply an unchanged DLL from a game/source you are entitled to use. The installer accepts recognized 310.8.0 / 310.9.16 hashes and checks Authenticode. Matching a hash/signature does not itself grant a license or establish that extraction/use is permitted under your source's terms.

**Optional third-party source:** [RankFTW/rhi-repo, DLSS NR 310.8.0](https://github.com/RankFTW/rhi-repo/releases/tag/dlssnr-310.8.0). `Setup.cmd` exposes this as a separate choice. It is not an official NVIDIA endpoint. The RHI release page does not document a specific NVIDIA grant for this distribution/use. External hosting is not a solution to that question. If the file is unavailable or changes, retrieval stops: no automatic mirror, latest version or signature bypass.

**Official SDK check (2026-10-19):** the public main file trees of [NVIDIA/DLSS](https://github.com/NVIDIA/DLSS) and [NVIDIA-RTX/Streamline](https://github.com/NVIDIA-RTX/Streamline) did not contain the matching `nvngx_dlssnr.dll`. Ordinary `nvngx_dlss.dll` and `nvngx_dlssd.dll` implement other features and are not substitutes. This check does not prove that NVIDIA has no private, partner or later distribution.

Historical local prototype: a RHI archive was downloaded and its signature/hash validated; the extracted original model was used. A separately supplied Crimson Desert DLL failed the prior hash/signature check and was not used. No NVIDIA runtime code is patched by this project. Model extraction is delegated to the pinned upstream DLSSNR-AMD installer, whose terms and implementation must also be reviewed.

Accepted archive SHA256 (310.8.0): `388c0a7912e15ec911b9c9e11a692142b11fe387ddf2b637d8c358138fffb3ac`.
Accepted extracted DLL SHA256: `e16bcf15e16e13f527491cdf7845b2fe6521a738d8f7c9c721866a8496e1fc8e`.

The optional downloader was tested against a fresh GitHub download, a local known archive, a valid cache and a corrupt archive. It does not install into the game by itself. Neural is an experimental independent AMD implementation; it is not NVIDIA-supported DLSS integration.
