# NVIDIA variants and external DLLs

## RTX 50 series: guarded experimental preparation

`Setup.cmd` can prepare the documented DLSS5-Feeder route for a detected RTX 50-series GPU. It downloads the pinned official [DLSS5-Feeder 1.19.0](https://github.com/jlrouzies-fr/DLSS5-Feeder/releases/tag/v1.19.0) and pinned [LumeniteFX](https://github.com/umar-afzaal/LumeniteFX) sources, verifies their archive hashes, and installs only the files needed by the DX11/ReShade path.

You must provide these files yourself in one folder:

- unchanged `nvngx_dlssnr.dll` and `nvngx_dlss.dll` from a source you are entitled to use; both must have valid NVIDIA Authenticode signatures;
- exactly one neural consumer: a `renodx-dlss5*.addon64` file, or the three Deep Fried Chicken files named by the installer.

Those consumers are separately distributed third-party projects. Tyria Realism does not certify or redistribute them. The installer records their hashes, rejects mixed consumers and known bridge conflicts, backs up collisions and can undo only the files recorded in its receipt. It also refuses to run when an RTX 50 GPU, GW2, or an existing ReShade add-on installation cannot be identified.

After installation, a successful setup is still not proof that neural rendering ran. Start GW2 with MSAA disabled, confirm that Lumenite Kernel precedes DLSS5 Feed in ReShade, then inspect `dlss5-feed.log`. It must show feature 18/model creation and continuing neural frames. This project has not yet performed that live test on NVIDIA hardware.

## RTX 20, 30 and 40 series

The upstream Feeder documentation says these generations need a GPU-modified `nvngx_dlssnr.dll` and currently directs users to the DLSS5 channel of the [RenoDX Discord](https://discord.com/invite/renodx). We found no stable public GitHub release, NVIDIA download or clear redistribution grant for that modified proprietary file.

Tyria Realism therefore does not bundle, mirror or automatically download it. The RTX 50 installer also rejects it because a modified DLL normally cannot satisfy the unchanged NVIDIA-signature requirement. Advanced users can research that external route independently, but it is not a supported Tyria Realism installation and must not be described as verified DLSS 5 for GW2.

## What this route actually does

Guild Wars 2 does not expose a native DLSS integration. DLSS5-Feeder uses ReShade depth and LumeniteFX optical-flow motion estimates to construct an external DLAA-like neural-rendering contract. That can change reconstruction/lighting detail, but it does not add higher-resolution texture assets, native engine motion vectors, DLSS Super Resolution or frame generation.

## Pinned public-source integrity

- DLSS5-Feeder 1.19.0 ZIP SHA256: `9CC19E28F8FB4E216EA61621FDF6443E207E338EC639979AB335503C75F48F36`
- LumeniteFX commit: `25f40a2ac70bc90bd3dc53bb2fc9d5fa50536b8b`
- LumeniteFX archive SHA256: `06D40E49B54B7E2FCAE61B1F489B25A0C1779796160955760A3739B28CD103CC`

The Feeder add-on was inspected statically as a 64-bit PE. Its dependency strings reference `nvngx_dlssnr.dll`, neural-rendering feature 18, supported consumer add-ons and `dlss5-feed.log`. Static inspection establishes the package boundary; it does not prove runtime quality, safety of an externally supplied consumer or successful GW2 inference.
