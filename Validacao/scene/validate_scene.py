from pathlib import Path
import re,json,sys,importlib.util,yaml
P=Path(sys.argv[1]) if len(sys.argv)>1 else Path('/workspace/work/pokemon-marble-race/Projeto')
OUT=Path(__file__).parent
helper=Path(__file__).with_name('unity_asset_refs.py')
spec=importlib.util.spec_from_file_location('unity_refs',helper);mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
guidmap={};duplicates=[]
for meta in (P/'Assets').rglob('*.meta'):
 m=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(errors='ignore'),re.M)
 if m:
  if m.group(1) in guidmap:duplicates.append((m.group(1),str(meta)))
  guidmap[m.group(1)]=meta.with_suffix('')
assert not duplicates,duplicates
missing_cs_metas=[str(f.relative_to(P)) for f in (P/'Assets').rglob('*.cs') if not Path(str(f)+'.meta').is_file()]
assert not missing_cs_metas,missing_cs_metas
scene=P/'Assets/Scenes/MarbleRace/MarbleRace_Pokemon.unity'
report=mod.audit(scene,guidmap);assert not report['errors'],report['errors']
records=mod.read_records(scene)
fields={ident:(typ,data) for ident,_,typ,data in records}
gos={ident:data for ident,_,typ,data in records if typ=='GameObject'}
assert {d['m_Name'] for d in gos.values()}=={'Main Camera','Global Light 2D','EventSystem','MarbleRace_Manager'}
assert all(d['m_IsActive']==1 for d in gos.values())
manager=fields[2026111203][1]
assert manager['m_Script']['guid']=='a6bfe1fc140649d5a13c3703f3c19da1'
assert manager['m_Enabled']==1
assert manager['quantidadeParticipantes']==8 and manager['quantidadeCorridas']==3
assert manager['formato']==1 and manager['semente']==0
assert manager['duracaoAlvo']==45 and manager['duracaoMaxima']==60
assert manager['poderesPorTipo']==1 and manager['umaCorridaPorPlay']==0
assert manager['tamanhoMarble']==0.55
assert len(manager['participantes'])==8
ids=[]
for item in manager['participantes']:
 p=guidmap[item['guid']]
 data=yaml.safe_load(re.sub(r'^%.*\n|^--- !u!.*\n','',p.read_text(),flags=re.M))['MonoBehaviour']
 ids.append(data['numeroPokedex'])
assert ids==[1,4,7,25,54,74,92,133],ids
for field in ('tema','spriteBola','spriteSnorlax','spriteRocket'):
 assert manager[field]['guid'] in guidmap and guidmap[manager[field]['guid']].is_file(),field
assert manager['fundo']['fileID']==0
assert fields[519420031][1]['orthographic']==1 and fields[519420031][1]['orthographic size']==8.3
assert fields[519420032][1]['m_LocalPosition']=={'x':0,'y':0,'z':-10}
assert len(fields[9223372036854775807][1]['m_Roots'])==4
settings=(P/'ProjectSettings/EditorBuildSettings.asset').read_text()
assert settings.count('  - enabled: 1')==1 and 'Assets/Scenes/MarbleRace/MarbleRace_Pokemon.unity' in settings
settings=(P/'ProjectSettings/ProjectSettings.asset').read_text()
assert 'productName: Pokemon Marble Race' in settings and 'Standalone: com.robertogirao.pokemonmarblerace' in settings
material=(P/'Assets/MarbleRace/Resources/MarbleRaceSprite.mat').read_text()
assert 'guid: 13c02b14c4d048fa9653293d54f6e0e1' in material
report.update({'unique_source_guids':True,'missing_cs_metas':missing_cs_metas,'root_count':4,'legacy_gameplay_components':False,'participant_ids':ids,'default_races':3,'default_target_seconds':45,'default_max_seconds':60,'camera_orthographic_size':8.3,'build_only_race':True,'product_name':'Pokemon Marble Race','resources_unlit_material':True,'manager_source_present':manager['m_Script']['guid'] in guidmap})
(OUT/'scene-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='external_package_or_builtin_guids'},ensure_ascii=False,indent=2))
