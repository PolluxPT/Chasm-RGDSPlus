# Maintainer build notes

End users do not compile anything; use the public release ZIP.

With .NET 8 SDK: build Source/Host/Host.csproj and Source/Patcher/Patcher.csproj
in Release configuration. Copy Host DLL, deps.json and runtimeconfig.json
into release APPS/Chasm_RGDSPlus/game. Copy Patcher output DLLs and JSON
files into runtime/patcher. Dependencies/FNA.dll is the baseline framework;
copy it to runtime/patcher/FNA.Base.dll. Local game preparation invokes the
patcher with the owner's original game path, output folder, Host DLL and
FNA.Base.dll. No game code is embedded in either tool.

Source/native_audio_compat.py takes Dependencies/FAudio.Original.so and an
output filename; it reproduces the shipped libFAudio.so.0 compatibility fix.
Source/upstream-fna.tar.gz contains the frozen framework/native source
snapshot inherited from the tested Bastion RGDS+ runtime. Component licenses
are in APPS/Chasm_RGDSPlus/licenses. The .NET 8.0.15 ARM64 runtime is bundled
with its notices in the release asset. Firmware SDL2 is not bundled.

Runtime-dependent public files must be checksum-manifested after rebuilding.
Never generate the manifest from a populated private game installation.
Do not enable CHASM_BUILD_TRACE for production builds.
