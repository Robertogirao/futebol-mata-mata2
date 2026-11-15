#!/usr/bin/env python3
"""Read-only audit of the four football scene mode flags."""
from pathlib import Path
import re
import sys

source = Path(sys.argv[1]).resolve()
metadata = (source / 'Assets/FutebolDiscos/Scripts/ProjetoFutebolDiscos.cs.meta').read_text(encoding='utf-8-sig')
guid = re.search(r'^guid:\s*(\w+)', metadata, re.MULTILINE).group(1)
scenes = {
    'Liga_Discos.unity': False,
    'MataMata_Discos.unity': False,
    'Capitaes/Liga_Capitaes.unity': True,
    'Capitaes/MataMata_Capitaes.unity': True,
}
for name, expected in scenes.items():
    path = source / 'Assets/Scenes/FutebolDiscos' / name
    text = path.read_text(encoding='utf-8-sig')
    components = [b for b in text.split('--- !u!114') if 'guid: ' + guid in b]
    if len(components) != 1:
        raise SystemExit('Expected exactly one football configuration component in ' + str(path))
    flag = re.search(r'^\s*usarCapitaesConvidados:\s*(\d+)\s*$', components[0], re.MULTILINE)
    actual = flag is not None and flag.group(1) != '0'
    if actual != expected:
        raise SystemExit('Incorrect guest/special mode flag: ' + str(path))
    print('PASS scene mode:', name, 'guest/special=' + str(actual).lower(), '(omitted uses default false)' if flag is None else '')
print('PASS 4 read-only scene mode assertions. No scene files were written.')
