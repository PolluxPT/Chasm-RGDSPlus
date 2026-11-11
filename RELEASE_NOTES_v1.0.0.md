# Chasm RGDS+ v1.0.0

First stable standalone release for stock Linux on the Anbernic RGDS+.

- Native ARM64 .NET/FNA integration; no PortMaster required.
- First-run physical-button calibration and START+SELECT exit.
- Quadratic volume curve, physical volume buttons and exit restoration.
- Lower-screen backlight shutdown and restoration.
- Native FAudio compatibility for the device-format query path.
- Removed repeated diagnostic SD writes that caused avoidable slowdown.
- Opening-logo audio follows the volume curve before the first game frame.
- Commercial game data excluded. Each owner supplies Linux Chasm 1.111,
  GOG build 91697; first launch adapts those files automatically on-device.

Download Chasm_RGDSPlus_v1.0.0.zip below, then follow README.md.
Supported device: RGDS+ RK3568 ARM64, 1 GB RAM, stock firmware.
Other firmware/device combinations are not certified. Some earlier logs
contained ALSA underruns; report reproducible audible issues if encountered.
