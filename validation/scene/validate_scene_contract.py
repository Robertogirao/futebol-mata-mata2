from pathlib import Path
import importlib.util
import re
import json
import sys

project = Path(__file__).resolve().parents[2] / 'Projeto'
spec = importlib.util.spec_from_file_location('refs', str(Path(__file__).resolve().parent / 'unity_asset_refs.py'))
refs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refs)
errors = []
checks = 0

def check(value, message):
    global checks
    checks += 1
    if not value:
        errors.append(message)

def guid(file):
    return re.search(r'^guid: ([a-f0-9]{32})$', Path(str(file) + '.meta').read_text(), re.M)[1]

all_teams = list((project / 'Assets/FichasDeTimes').rglob('*.asset'))
teams = {guid(team): team for team in all_teams}
scenes = list((project / 'Assets/Scenes').rglob('*.unity'))
check(len(scenes) == 2, 'Only the two new competition scenes remain')
expected = {
    '6ec746ce093b4c1ca9153bf49b6f124f', '19702e25c9b44f81b6b96c2be36b6d82',
    'a10effdcb13b8254c92e906f53a3e0cf', 'b2e94b1fdfe40b344ab6220604989ed9',
    '4287aa7349134cec9868fcd4add2dd60', '28f7409f34fafad49a16daa16d64d055',
    '2cf2595752e6cff40a8954d7d570f60e', '93c8f9a5da2a46968420890cd2722b82',
    '98be1f90d558b8c468a88cc1fabcc618', '8725e7d635ffac444b408215c08bc68e'
}
for file in scenes:
    rows = refs.read_records(file)
    by_id = {row[0]: row for row in rows}
    managers = [row for row in rows if row[2] == 'GameObject' and row[3].get('m_Name') == 'FutebolDiscos_Manager']
    check(len(managers) == 1, str(file) + ': single manager')
    parts = [by_id[part['component']['fileID']] for part in managers[0][3]['m_Component']]
    scripts = [row for row in parts if row[2] == 'MonoBehaviour']
    check(len(scripts) == 10, 'Ten manager behaviours')
    check({row[3]['m_Script']['guid'] for row in scripts} == expected, 'Exact component types')
    check(all(row[3]['m_Enabled'] == 1 for row in scripts), 'All components enabled before Play')
    root = next(row[3] for row in scripts if row[3]['m_Script']['guid'] == '6ec746ce093b4c1ca9153bf49b6f124f')
    tm = next(row[3] for row in scripts if row[3]['m_Script']['guid'] == 'a10effdcb13b8254c92e906f53a3e0cf')
    knockout = file.stem.startswith('Mata')
    count = 16 if knockout else 8
    check(root['formato'] == int(knockout), 'Format matches scene')
    check(root['quantidadeTimes'] == count, 'Default team count')
    check(root['quantidadeRodadas'] == 7, 'Seven default league rounds')
    check(len(root['participantes']) == count, 'Default roster count')
    ids = [item['guid'] for item in root['participantes']]
    check(len(ids) == len(set(ids)), 'No duplicate team refs')
    check(all(g in teams for g in ids), 'Real TeamData in roster')
    check(tm['iniciarAutomaticamente'] == 0, 'TM manual root bootstrap')
    check({item['guid'] for item in tm['todosOsTimesDisponiveis']} == set(teams), 'All 68 team assets in catalogue')
    check(root['semente'] == 0 and root['sortearConfrontos'] == 1, 'Fresh seasons auto random')
    check(root['tamanhoDisco'] == 1 and root['tamanhoBola'] == .42, 'Default safe actor sizes')
    check(root['discosPorTime'] == 3, 'Three discs per team')
    camera = next(row[3] for row in rows if row[2] == 'Camera')
    check(camera['orthographic'] == 1 and camera['orthographic size'] == 8.3, 'Orthographic frame 8.3')
    transform = next(row[3] for row in rows if row[2] == 'Transform' and row[3]['m_GameObject']['fileID'] == camera['m_GameObject']['fileID'])
    check(transform['m_LocalPosition'] == {'x': 0, 'y': 0, 'z': -10}, 'Camera at 0,0,-10')
    check(not any('Goalkeeper' in row[3].get('m_EditorClassIdentifier', '') for row in scripts), 'No keeper script')

settings = (project / 'ProjectSettings/ProjectSettings.asset').read_text()
check('productName: Futebol de Discos' in settings, 'Isolated product save root')
check('Standalone: com.robertogirao.futeboldiscos' in settings, 'Correct bundle ID')
build = (project / 'ProjectSettings/EditorBuildSettings.asset').read_text()
check(build.count('path:') == 2, 'Two scenes in build')
check(all(str(scene.relative_to(project)) in build for scene in scenes), 'New build scenes')
material = (project / 'Assets/FutebolDiscos/Resources/DiscosSprite.mat').read_text()
check('guid: 13c02b14c4d048fa9653293d54f6e0e1' in material, 'URP2D SpriteUnlit material retained in Resources')
report = {
    'scope': 'Static Unity scene/content contract; not Unity import/Play',
    'checks': checks, 'errors': errors, 'rosterTeamCount': len(all_teams)
}
Path(__file__).with_name('scene-contract-report.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
sys.exit(bool(errors))
