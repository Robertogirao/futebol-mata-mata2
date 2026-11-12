from pathlib import Path
import re,uuid,json,yaml
P=Path('/workspace/work/pokemon-marble-race/Projeto')
A=P/'Assets'
scene_dir=A/'Scenes/MarbleRace';scene_dir.mkdir(parents=True,exist_ok=True)
editor_dir=A/'MarbleRace/Editor';editor_dir.mkdir(parents=True,exist_ok=True)
# Fixed identities make the creation script safe to re-run without changing references.
SCENE_GUID='2f36b54bf17342639499fbab8fe77168'
THEME_GUID='7b880d4b3a074ecba3d48629c8285577'
EDITOR_GUID='38f64881c94040f5a7c3f3aa0787a253'
EDITOR_FOLDER_GUID='b648a9f4f11c4a0da80c3e6eb36928fd'
SCENE_FOLDER_GUID='ef72d9ce3ce447e2abb21d4b765e5180'
MANAGER_GUID='a6bfe1fc140649d5a13c3703f3c19da1'
def importer(g,kind='DefaultImporter'):
 return f'fileFormatVersion: 2\nguid: {g}\n{kind}:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
def folder_meta(path,g):
 p=Path(str(path)+'.meta')
 if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+g+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
folder_meta(scene_dir,SCENE_FOLDER_GUID);folder_meta(editor_dir,EDITOR_FOLDER_GUID)
(editor_dir/'MarbleRaceManagerEditor.cs.meta').write_text('fileFormatVersion: 2\nguid: '+EDITOR_GUID+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
theme=(A/'Pokemon/Temas/Tema_Pokemon_Liga.asset').read_text()
theme=theme.replace('m_Name: Tema_Pokemon_Liga','m_Name: Tema_MarbleRace').replace('competicaoId: pokemon_liga','competicaoId: pokemon_marble_race').replace('nomeCompeticao: POKEMON LEAGUE','nomeCompeticao: POKÉMON MARBLE RACE')
for field in ('fundoAbertura','fundoChaveamento','centroHelice','campo','gol','helice2','goleiro','materialGol','materialHelice','materialGoleiro'):
 theme=re.sub(r'^  '+field+r':.*$',f'  {field}: {{fileID: 0}}',theme,flags=re.M)
(A/'MarbleRace/Tema_MarbleRace.asset').write_text(theme)
(A/'MarbleRace/Tema_MarbleRace.asset.meta').write_text(importer(THEME_GUID,'NativeFormatImporter').replace('  userData:', '  mainObjectFileID: 11400000\n  userData:'))
old=(A/'Scenes/Ligas/Liga_Pokemon.unity').read_text()
headers=list(re.finditer(r'^--- !u!(\d+) &(-?\d+).*$',old,re.M));records={}
for i,m in enumerate(headers):
 end=headers[i+1].start() if i+1<len(headers) else len(old)
 records[int(m.group(2))]=old[m.start():end]
chosen=[1,2,3,4,519420028,519420032,519420031,519420029,519420030,619394800,619394802,619394801,1584016045,1584016048,1584016047,1584016046]
raw='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(records[n] for n in chosen)
raw=raw.replace('orthographic size: 6\n','orthographic size: 8.3\n').replace('m_BackGroundColor: {r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}','m_BackGroundColor: {r: 0.015, g: 0.028, b: 0.065, a: 1}')
def guid(path):
 return re.search(r'^guid: ([0-9a-f]{32})',Path(str(path)+'.meta').read_text(),re.M).group(1)
ids=[1,4,7,25,54,74,92,133]
participants=[]
for n in ids:
 match=list((A/'Pokemon/Participantes').glob('Pokemon_'+str(n).zfill(4)+'_*.asset'))
 assert len(match)==1,(n,match)
 participants.append(guid(match[0]))
raw+='''--- !u!1 &2026111201
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2026111202}
  - component: {fileID: 2026111203}
  m_Layer: 0
  m_Name: MarbleRace_Manager
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2026111202
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2026111201}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &2026111203
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2026111201}
  m_Enabled: 1
  m_EditorHideFlags: 0
'''
raw+='  m_Script: {fileID: 11500000, guid: '+MANAGER_GUID+', type: 3}\n'
raw+='''  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::PokemonMarbleRace.MarbleRaceManager
  nomeCampeonato: Pokémon Marble Race
  campeonatoId: pokemon_marble_race
  temporadaId: temporada_01
'''
raw+='  tema: {fileID: 11400000, guid: '+THEME_GUID+', type: 2}\n'
raw+='  fundo: {fileID: 0}\n'
for field,asset in [('spriteBola',A/'Pokemon/Arte/Bola_Base_Pokemon.png'),('spriteSnorlax',A/'Pokemon/Arte/Snorlax_Goleiro.png'),('spriteRocket',A/'Pokemon/Resources/EquipeRocket_Lancador.png')]:
 raw+='  '+field+': {fileID: 21300000, guid: '+guid(asset)+', type: 3}\n'
raw+='''  formato: 1
  quantidadeParticipantes: 8
  quantidadeCorridas: 3
  semente: 0
  participantes:
'''
for g in participants:raw+='  - {fileID: 11400000, guid: '+g+', type: 2}\n'
raw+='''  geracoesPermitidas: []
  tiposPermitidos: []
  duracaoAlvo: 45
  duracaoMaxima: 60
  duracaoAbertura: 4
  duracaoResultado: 7
  intervaloEntreCorridas: 2
  umaCorridaPorPlay: 0
  poderesPorTipo: 1
  tamanhoMarble: 0.55
  situacao: Pronto para configurar e iniciar a competição.
  ultimoErro: 
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 519420032}
  - {fileID: 619394802}
  - {fileID: 1584016048}
  - {fileID: 2026111202}
'''
(scene_dir/'MarbleRace_Pokemon.unity').write_text(raw)
(scene_dir/'MarbleRace_Pokemon.unity.meta').write_text(importer(SCENE_GUID))
settings=P/'ProjectSettings/EditorBuildSettings.asset'
s=settings.read_text();s=re.sub(r'  m_Scenes:\n.*?(?=  m_configObjects:)', '  m_Scenes:\n  - enabled: 1\n    path: Assets/Scenes/MarbleRace/MarbleRace_Pokemon.unity\n    guid: '+SCENE_GUID+'\n',s,flags=re.S);settings.write_text(s)
settings=P/'ProjectSettings/ProjectSettings.asset';s=settings.read_text();s=re.sub(r'^  productName:.*$', '  productName: Pokemon Marble Race',s,flags=re.M);s=re.sub(r'^(    Standalone: )com.DefaultCompany.2D-URP$',r'\1com.robertogirao.pokemonmarblerace',s,flags=re.M);settings.write_text(s)
Path('/workspace/work/pokemon-marble-race/validation/scene/identities.json').write_text(json.dumps({'scene_guid':SCENE_GUID,'theme_guid':THEME_GUID,'editor_guid':EDITOR_GUID,'manager_guid':MANAGER_GUID,'participants':ids,'participant_guids':participants},indent=2)+'\n')
print('Created race scene, editor metadata, dedicated theme and isolated build/product settings.')
