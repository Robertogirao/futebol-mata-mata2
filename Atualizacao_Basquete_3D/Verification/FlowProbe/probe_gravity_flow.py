"""Auxiliary rigid-body flow probe in MuJoCo, not Unity/PhysX validation."""
from pathlib import Path
import json, math, sys
import numpy as np
import mujoco

prefixes=('Open duel deck','Front exit guard','Side front guard','Side rear guard','Rear deck guard','Front exit funnel','Open drop chute','Drop guide side','Entry deflector','Basket collection')
HERE=Path(__file__).resolve().parent
def vec(values):return ' '.join(str(float(v)) for v in values)
def run(snapshot,positions,radius=.18,seconds=12,exit_height=2.8,collector=False):
    assets=[];geoms=[]
    for i,s in enumerate(snapshot['shapes']):
        if not s['name'].startswith(prefixes):continue
        q=s['rotation'];attrs=f'pos="{vec(s["position"])}" quat="{vec([q[3],q[0],q[1],q[2]])}"'
        if s['kind']=='box':
            half=np.array(s['size'])*np.array(s['scale'])/2
            geoms.append(f'<geom type="box" size="{vec(half)}" {attrs}/>')
        elif s['kind']=='mesh':
            vertices=np.array(s['vertices'])*np.array(s['scale'])
            faces=' '.join(str(v) for v in s['triangles'])
            assets.append(f'<mesh name="mesh{i}" vertex="{vec(vertices.flatten())}" face="{faces}"/>')
            geoms.append(f'<geom type="mesh" mesh="mesh{i}" {attrs}/>')
    balls=[]
    for i,p in enumerate(positions):balls.append(f'<body name="ball{i}" pos="{vec(p)}"><freejoint/><geom type="sphere" size="{radius}" mass=".45"/></body>')
    xml='<mujoco><compiler angle="radian"/><option timestep=".002" gravity="0 -9.81 0" iterations="100"/><default><geom friction=".08 0 0" solref=".005 1" solimp=".99 .99 .001" condim="3"/></default><asset>'+''.join(assets)+'</asset><worldbody>'+''.join(geoms+balls)+'</worldbody></mujoco>'
    model=mujoco.MjModel.from_xml_string(xml);data=mujoco.MjData(model)
    ids=[mujoco.mj_name2id(model,mujoco.mjtObj.mjOBJ_BODY,f'ball{i}') for i in range(len(positions))]
    exits={}
    for step in range(round(seconds/.002)):
        mujoco.mj_step(model,data)
        for i,bid in enumerate(ids):
            p=data.xpos[bid]
            if i not in exits and p[1]<exit_height:
                correct=abs(p[0]-5.4)<.8 and abs(p[2]+2.3)<.8 if collector else abs(p[0])<1.6 and -2.5<p[2]<.1
                exits[i]=dict(time=round(data.time,3),position=[round(float(v),3) for v in p],feed=bool(correct))
    return dict(feed=sum(e['feed'] for e in exits.values()),outside=sum(not e['feed'] for e in exits.values()),remaining=[dict(ball=i,position=[round(float(v),3) for v in data.xpos[bid]]) for i,bid in enumerate(ids) if i not in exits],exits=exits)

reports={}
for version in ['v4','v5']:
    snapshot=json.loads((HERE/f'flow-geometry-{version}.json').read_text())
    y=lambda z:4.45+math.tan(math.radians(snapshot['slope']))*(z-1.4)+.20
    cases={'left_corner':[[-2.85,y(.4),.4]],'right_corner':[[2.85,y(.4),.4]],'two_at_descent':[[.03,4.7,-.70],[.43,4.7,-.70]],'eight_front':[[x,y(z),z] for z in [.3,.8] for x in [-.75,-.25,.25,.75]]}
    reports[version]={name:run(snapshot,positions) for name,positions in cases.items()}
    reports[version]['two_at_collector']=run(snapshot,[[5.2,1.7,-2.3],[5.6,1.7,-2.3]],exit_height=.1,collector=True)
    reports[version]['eight_front_large']=run(snapshot,[[x,4.45+math.tan(math.radians(snapshot['slope']))*(z-1.4)+.245,z] for z in [.3,.8] for x in [-.75,-.25,.25,.75]],radius=.22)
    for name,result in reports[version].items():print(version,name,json.dumps(result))
(HERE/'gravity-flow-results.json').write_text(json.dumps(reports,indent=2))
if '--assert' in sys.argv:
    for result in reports['v5'].values():assert not result['remaining'] and not result['outside'],result
