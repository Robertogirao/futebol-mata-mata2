#!/usr/bin/env python3
"""Compile actual game sources with the existing Unity/TMP facade, no NuGet restore."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

HERE = Path(__file__).resolve().parent
REPOSITORY = HERE.parents[1]
SOURCE = Path(os.environ.get('TEST_SOURCE', str(REPOSITORY / 'Projeto'))).resolve()
HARNESSES = sys.argv[1:] or ['FeatureHarness', 'RuntimeHarness', 'GoalResetHarness',
                            'CaptainHarness', 'CornerRestartHarness']
DOTNET = os.environ.get('DOTNET') or shutil.which('dotnet')
if not DOTNET:
    raise SystemExit('Install a .NET SDK (8 or newer), or set DOTNET to its dotnet executable.')
DOTNET = Path(DOTNET).resolve()
DOTNET_ROOT = Path(os.environ.get('DOTNET_ROOT', str(DOTNET.parent)))

def version_key(path):
    return tuple(int(x) if x.isdigit() else 0 for x in path.name.split('.'))

sdks = sorted((DOTNET_ROOT / 'sdk').glob('*'), key=version_key)
packs = sorted((DOTNET_ROOT / 'packs/Microsoft.NETCore.App.Ref').glob('*'), key=version_key)
runtimes = sorted((DOTNET_ROOT / 'shared/Microsoft.NETCore.App').glob('*'), key=version_key)
if not sdks or not packs or not runtimes:
    raise SystemExit('The selected dotnet installation must include its SDK, reference pack, and runtime.')
compiler = sdks[-1] / 'Roslyn/bincore/csc.dll'
framework = sorted((packs[-1] / 'ref').glob('net*'))[-1]
references = ['-r:' + str(p) for p in sorted(framework.glob('*.dll'))]
sources = [str(p) for directory in ('FutebolDiscos/Scripts', 'Scripts')
           for p in sorted((SOURCE / 'Assets' / directory).glob('*.cs'))]
if not sources:
    raise SystemExit('No game sources found under ' + str(SOURCE))

with tempfile.TemporaryDirectory(prefix='captain-special-') as temp:
    out = Path(temp)
    def compile_sources(arguments):
        response = out / 'compiler.rsp'
        response.write_text('\n'.join('"' + a + '"' for a in arguments), encoding='utf-8')
        subprocess.run([str(DOTNET), str(compiler), '@' + str(response)], check=True)

    library = out / 'AllRuntime.dll'
    compile_sources(['-nologo', '-target:library', '-nostdlib+', '-out:' + str(library)]
                    + references + [str(HERE.parent / 'runtime/UnityContract.cs')] + sources)
    print('Compiled %d actual game runtime sources.' % len(sources), flush=True)
    for name in HARNESSES:
        harness = HERE / (name + '.cs') if name == 'FeatureHarness' else HERE.parent / 'runtime' / (name + '.cs')
        if not harness.is_file():
            raise SystemExit('Unknown harness: ' + name)
        assembly = out / (name + '.dll')
        compile_sources(['-nologo', '-target:exe', '-nostdlib+', '-out:' + str(assembly), '-r:' + str(library)]
                        + references + [str(harness)])
        (out / (name + '.runtimeconfig.json')).write_text(json.dumps({
            'runtimeOptions': {'tfm': framework.name,
                               'framework': {'name': 'Microsoft.NETCore.App', 'version': runtimes[-1].name}}}), encoding='utf-8')
        subprocess.run([str(DOTNET), str(assembly)], check=True, cwd=out)
