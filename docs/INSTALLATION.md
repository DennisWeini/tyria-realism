# Installation and rollback

This preview keeps optional third-party components separate. Close GW2 before changing an installation. Existing unmanaged ReShade/proxy files are a reason to stop and review, not to overwrite them.

## AMD neural rendering (RX 9070 XT tested)

1. Download a versioned toolkit ZIP from GitHub Releases or the matching Nexus file. Extract it completely. Keep this folder for uninstalling.
2. Run `Setup.cmd`. Select your GW2 executable and the desired dependency source. The default own-DLL path accepts only recognized unchanged files with valid NVIDIA Authenticode. The optional RHI route downloads from a third party, not NVIDIA; read `MODEL-SOURCES.md`.
3. The installer downloads one pinned DLSSNR-AMD package, checks its SHA256, calls its installer and applies a preset. No model is bundled. Do not disable antivirus or replace protected game files.
4. Start GW2 in a safe place. Allow network compilation to finish. In neural profiles, F6 toggles NR and F5 captures pairs; Home opens ReShade. These keys do not turn NR on in profiles without its feeder.
5. Close GW2 before changing profiles. Use `Set-NeuralPreset.ps1 -Preset Efficient|Balanced|Quality|Cinematic -GameExe <path>`; names describe settings, not promised FPS or quality.

The neural setup has been tested in earlier package revisions. The new source-selection path has download/validation tests, not a complete fresh-install acceptance matrix. Keep backups.

## Visual-only / Elora-inspired profiles

These do not need a NVIDIA model. Install ReShade with add-on support from its official source yourself, or use an existing reviewed setup. Do not install another loader over an existing one blindly.

Run `scripts/Install-VisualComparison.ps1 -Profile Lite|Balanced|Quality -GameExe <path-to-Gw2-64.exe>`. It downloads four unchanged, pinned shader sources from the referenced Elora repository, verifies their hashes and adds named test profiles. No original Elora binary/add-on is bundled. Existing active presets are backed up; RenoDX's settings are not automatically reset by this portable importer. If using our RenoDX experiment, first select its Vanilla/neutral grade and disable its bloom/adaptation/blur to avoid duplicate grading. Neural should be off for the first comparison.

Lite is the default for older AMD, NVIDIA and Intel candidates and uses only Reinhard plus LocalContrast. Balanced adds BloomingHDR. Quality additionally uses NeoSSAO and requires the correct current depth buffer. This is a reduced adaptation, not the full original Elora preset. REST UI protection is not included; examine HUD and nameplates. BloomingHDR is a multi-pass effect, not a guaranteed cheap effect.

`TyriaMaterials.fx` is an optional original five-sample detail shader. It does not create new textures. Avoid stacking it with other sharpening until compared.

## NVIDIA RTX 50 neural preparation (not yet live tested)

Read `NVIDIA-VARIANTS.md` first. Install ReShade with add-on support from the official source and keep its standard shader folder. Put your own valid NVIDIA-signed `nvngx_dlssnr.dll`, `nvngx_dlss.dll` and exactly one supported neural consumer in a separate folder. Close GW2, run `Setup.cmd`, choose the RTX 50 option and select that folder.

The installer refuses non-RTX-50 hardware, unsigned/modified NVIDIA runtimes, mixed neural consumers, known bridge conflicts and missing ReShade prerequisites. It then retrieves pinned DLSS5-Feeder and LumeniteFX sources from their official GitHub projects, verifies both hashes and creates a removal receipt. No proprietary runtime or neural consumer is downloaded by Tyria Realism.

After starting GW2 with MSAA off, verify the ReShade order and inspect `dlss5-feed.log` for feature 18, model creation and continuing neural frames. Until that evidence exists on the target computer, the result is only a prepared installation.

## Uninstall

1. Close GW2.
2. For the visual comparison, run `Install-VisualComparison.ps1 -GameExe <path> -Restore` first. It restores the previous config and removes only unchanged files owned by that import; changed files are retained for review.
3. For an AMD neural installation, run `scripts/Uninstall-DlssNrAmd.ps1` from the same toolkit folder used to install. The downloaded upstream installer uses its installation manifest to restore/remove managed files. For RTX 50, use `scripts/Uninstall-NvidiaRtx50.ps1`; it removes only unchanged files in its allowlisted receipt and restores collisions/ReShade.ini from backup.
4. Only then remove the toolkit/Vortex deployment. Vortex alone does not undo downloaded or generated runtime files.
5. Keep `.tyria-realism-backups` until the original game setup has been verified. Game updates or other mods can require manual reconciliation.

Do not run the older session-specific experimental rollback scripts on an unrelated machine. The RenoDX SDR patch is developer material, not automatically installed by this toolkit.

## Vortex

The release includes a `_TyriaRealism` folder intended to be placed beside `Gw2-64.exe`. Vortex game discovery/support and deployment method depend on your installed extension; automatic support is not claimed for every Vortex setup. Enable/deploy the folder, then run `Setup.cmd` inside it. A manual extracted installation is supported by executable selection. Always uninstall the runtime before disabling/removing the toolkit in Vortex.
