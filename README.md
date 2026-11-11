# Chasm RGDS+ — v1.0.0

Standalone native Chasm port for the Anbernic RGDS+ (RK3568, ARM64, 1 GB RAM).
No PortMaster is required. Uses the stock Linux firmware's SDL2, Wayland and
ALSA with a bundled ARM64 .NET 8 runtime, FNA, FNA3D and FAudio.

**You must own the official Linux version of Chasm. No game files or game
artwork are supplied here.** Supported input: Chasm 1.111, GOG build 91697.
Other editions and versions are not supported by this release.

## Install

1. Download `Chasm_RGDSPlus_v1.0.0.zip` from this repository's Releases page.
2. Extract it. Copy the contents of APPS into the SD card's existing APPS
   folder (the same location used by other RGDS+ Apps).
3. On your computer, copy the **contents** of your official Linux Chasm game
   folder into `APPS/Chasm_RGDSPlus/original/` on the SD card. That folder
   must directly contain `Chasm.exe`, `bitkid.common.dll`, `FNA.dll`,
   `mscorlib.dll`, `config.ini`, `Content/` and the other original files.
   Do not put an extra nested game folder or only the GOG installer there.
4. Start Chasm from the Apps menu. First-launch preparation is automatic;
   follow the on-screen physical-button prompts once.

If you have the GOG Linux `.sh` installer on Windows, 7-Zip can open this
installer's embedded archive. Its game folder is `data/noarch/game/`.
Copy that folder's contents to the destination above. Alternatively use the
files from an existing official Linux installation. No Windows game build.

Content is moved from original/Content into game/Content on first launch,
inside this port's own folder, to avoid keeping two large SD-card copies.
Keep original/Chasm.exe and original/bitkid.common.dll for future preparation.
Allow at least 1.5 GB free (2 GB recommended) for the runtime, game, saves and logs.

## Controls

| Physical control | Default action |
|---|---|
| D-pad / left stick | Move |
| A | Jump |
| X / B | Left-hand / right-hand action |
| Y | Backdash |
| L1 / R1 | Previous / next selection |
| START | Character menu / menus |
| SELECT | Map |
| START + SELECT, held 1.5 seconds | Exit |
| Volume + / − | System volume curve |

L2/R2 are calibrated but have no extra action in the game's default layout.
Button mappings are learned from the labels on the physical handheld.

The gain curve is `0.35 × (level / 10)²`, applied before the opening logo
sound. Entry volume is restored on exit. The lower-screen backlight is
switched off; its power setting is also changed where firmware supports it.
The previous screen settings and performance governors are restored on exit.

## Saves and updates

Saves and options live under `APPS/Chasm_RGDSPlus/savedata/`. Keep that whole
folder when updating. Merge the next release's APPS folder and replace its
files. To repeat controller setup, delete only
`savedata/config/chasm-controls-v1.json`.

When upgrading from an earlier Alpha, preserve savedata and copy your
original Linux game files into original/ once, as described above. The
public final package never supplies the commercial assemblies.

## Status and support

v1.0.0 is the first stable port release based on successful RGDS+ gameplay,
save/load and clean-exit sessions, physical volume-button logs, and owner
confirmation that the opening-volume issue is fixed. Local ARM64-emulated
checks cover audio startup, music loading and the volume curve. This is a
community port, not an official release from Chasm's developer. Other
firmware builds and devices are unverified. Earlier sessions contained
occasional ALSA underrun messages; universal frame-rate or audio guarantees
are not claimed.

If reporting an issue, attach `Chasm_RGDSPlus/Chasm_TestLogs.zip`, with a
short description and firmware version. Review logs before posting publicly.
Do not attach saves, commercial game files or the original folder.

See THIRD_PARTY_NOTICES.md for component licenses. Integration source is
MIT-licensed; each bundled third-party component retains its own license.
