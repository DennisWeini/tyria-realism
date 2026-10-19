# Third-party components

The root MIT license covers original Tyria code only. No NVIDIA models, game textures, raw user logs, downloaded runtime archives or third-party binaries are included.

* DLSSNR-AMD: original code MIT; [upstream](https://github.com/mochizuki0323/DLSSNR-AMD). The upstream package retains its own dependency notices and model requirements. Our source snapshot pins Windows v0.0.6-win-preview-alpha hotfix, not the moving latest release.
* ReShade: BSD-3-Clause; [upstream](https://github.com/crosire/reshade). Installed separately or by the unchanged upstream neural installer.
* RenoDX: MIT; [NotVoosh GW2 source](https://github.com/NotVoosh/renodx/tree/2dd89823ff0503f003d6b9c134ac04a64c48577e/src/games/guildwars2). Experimental source patch and upstream license included. No compiled add-on included; no claim that the modified SDR branch is upstream-supported.
* Elora's Personal Presets: MIT; [v5.0.0 source](https://github.com/AlteredM1nd/gw2-reshade-eloras-personal-presets/tree/95eb5dcf53808c8eb7651a76e473b9bbf77afb05). Included INIs are reduced adaptations: MartysMods/Zenteon and duplicate AO omitted; Quality uses NeoSSAO intensity 0.25. Original license retained under `licenses`.
* Optional source downloads: BloomingHDR by BlueSkyDefender (Elora documentation lists CC BY-SA 4.0); LocalContrast by Jakub Maksymilian Fober (CC BY-SA 3.0); NeoSSAO by Barbatos Bachiko (MIT, embedded credits); Reinhard by Marty McFly, port by Insomnia (no standalone file license established in this audit). These shader files are not redistributed here; source notices and local download receipts are retained. Downloading does not remove license obligations. This is not a blanket endorsement of an upstream bundle's licensing claims.
* AMD AFMF/RSR are driver features; no AMD driver is distributed or modified. NVIDIA, AMD, Guild Wars 2 and ArenaNet trademarks remain with their owners. No affiliation, endorsement, account safety certification or permission to redistribute proprietary components is implied.

Do not publish the private development workspace wholesale. It contains runtime downloads, model caches and personal testing paths that are deliberately excluded from this repository and release builder.

