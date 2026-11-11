# Private binary compatibility patch for the exact supplied ARM64 FAudio build.
# Native platform code uses SDL_GetDefaultAudioInfo / SDL_GetAudioDeviceSpec.
# Route both format probes to the existing zero-spec fallback in that function.
# This retains device enumeration, SDL_OpenAudioDevice and actual audio output.
import hashlib,pathlib,sys
p=pathlib.Path(sys.argv[1]);b=bytearray(p.read_bytes())
assert hashlib.sha256(b).hexdigest()=="dfa6126f87e38b8f1ee0803c6e36bd0ec86cc96b49825af728c5946c10297411", "Unsupported native build"
assert b[0x242cc:0x242d0]==bytes.fromhex("8592ff97")
assert b[0x243d8:0x243dc]==bytes.fromhex("e2230091")
b[0x242cc:0x242d0]=(0x12800000).to_bytes(4,"little") # return -1; existing default-query failure branch zeroes spec
b[0x243d8:0x243dc]=b[0x242bc:0x242c0] # add x20,sp,8: address of SDL_AudioSpec
b[0x243dc:0x243e0]=(0x14000000|((0x244c4-0x243dc)//4)).to_bytes(4,"little") # shared zero-spec path
pathlib.Path(sys.argv[2]).write_bytes(b)
