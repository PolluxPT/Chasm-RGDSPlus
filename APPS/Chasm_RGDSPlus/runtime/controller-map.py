#!/usr/bin/env python3
"""Export the validated per-controller profile before the game initializes SDL.
SDL_GAMECONTROLLERCONFIG has higher SDL priority than SDL_GameControllerAddMapping.
"""
import json,pathlib,re,sys
ACTIONS=('a','b','x','y','leftshoulder','rightshoulder','back','start','lefttrigger','righttrigger','dpup','dpdown','dpleft','dpright')

def mapping(profile):
    if not isinstance(profile,dict) or type(profile.get('Version')) is not int or profile['Version']!=1:
        raise ValueError('Unsupported controller profile version')
    guid=profile.get('Guid')
    if not isinstance(guid,str) or re.fullmatch('[0-9a-f]{32}',guid) is None:
        raise ValueError('Invalid controller GUID')
    for key in ('ButtonCount','AxisCount','HatCount'):
        if type(profile.get(key)) is not int or not 0<=profile[key]<=256:
            raise ValueError('Invalid controller counts')
    bindings=profile.get('Bindings')
    if not isinstance(bindings,dict) or set(bindings)!=set(ACTIONS):
        raise ValueError('Incomplete controller bindings')
    seen=set()
    for action in ACTIONS:
        value=bindings[action]
        if not isinstance(value,str) or value in seen:
            raise ValueError('Duplicate or invalid controller binding')
        seen.add(value)
        button=re.fullmatch(r'b([0-9]+)',value)
        hat=re.fullmatch(r'h([0-9]+)\.([1248])',value)
        if button:
            valid=int(button[1])<profile['ButtonCount']
        elif hat:
            valid=int(hat[1])<profile['HatCount']
        else:valid=False
        if not valid:raise ValueError('Out-of-range controller binding: '+action)
    axes='leftx:a0,lefty:a1,' if profile['AxisCount']>=2 else ''
    return guid+',RGDS calibrated,'+''.join(k+':'+bindings[k]+',' for k in ACTIONS)+axes+'platform:Linux,'

def main():
    try:
        path=pathlib.Path(sys.argv[1])
        if path.stat().st_size>32768:raise ValueError('Controller profile is too large')
        print(mapping(json.loads(path.read_text())))
    except (OSError,ValueError,IndexError) as ex:
        print('Controller export failed: '+str(ex),file=sys.stderr);return 3
    return 0
if __name__=='__main__':sys.exit(main())
