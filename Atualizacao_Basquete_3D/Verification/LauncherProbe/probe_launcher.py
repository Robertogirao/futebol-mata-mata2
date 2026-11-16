"""Moving-wheel contact probe in MuJoCo. It does not execute Unity/PhysX."""
from pathlib import Path
import json,math,sys
import numpy as np
import mujoco
HERE=Path(__file__).resolve().parent
def vec(a):return ' '.join(str(float(x)) for x in a)
def build(snapshot):
    assets=[];geoms=[];wheel=snapshot['launcher']
    allowed=('Open duel deck','Front exit guard','Side rear guard','Rear deck guard','Open drop chute','Drop guide side','Entry deflector','Impact feed deflector')
    for i,s in enumerate(snapshot['shapes']):
        if not s['name'].startswith(allowed):continue
        q=s['rotation'];pose=f'pos="{vec(s["position"])}" quat="{vec([q[3],*q[:3]])}"'
        if s['kind']=='box':geoms.append(f'<geom type="box" size="{vec(np.array(s["size"])*s["scale"]/2)}" {pose}/>')
        elif s['kind']=='mesh':
            assets.append(f'<mesh name="s{i}" vertex="{vec((np.array(s["vertices"])*s["scale"]).flatten())}" face="'+ ' '.join(str(v) for v in s['triangles'])+'"/>')
            geoms.append(f'<geom type="mesh" mesh="s{i}" {pose}/>')
    pieces=[]
    for i,s in enumerate(wheel['pieces']):
        q=s['rotation'];pieces.append(f'<geom name="p{i}" type="box" size="{vec(np.array(s["size"])/2)}" pos="{vec(s["position"])}" quat="{vec([q[3],*q[:3]])}" mass=".1"/>')
    q=wheel['rotation']
    motor=f'<body name="wheel" pos="{vec(wheel["position"])}" quat="{vec([q[3],*q[:3]])}"><joint name="motor" type="hinge" axis="1 0 0" armature="1000"/>'+''.join(pieces)+'</body>'
    ball=f'<body name="ball" pos="0 4.7 -.5"><freejoint name="balljoint"/><geom name="ballgeom" type="sphere" size="{wheel["ballRadius"]}" mass=".45"/></body>'
    xml='<mujoco><compiler angle="radian"/><option timestep=".002" gravity="0 -9.81 0" iterations="100"/><default><geom friction=".08 0 0" solref=".005 1" solimp=".99 .99 .001" condim="3"/></default><asset>'+''.join(assets)+'</asset><worldbody>'+''.join(geoms)+motor+ball+'</worldbody></mujoco>'
    return mujoco.MjModel.from_xml_string(xml)
def run(snapshot):
    model=build(snapshot);data=mujoco.MjData(model);wheel=snapshot['launcher']
    jid=mujoco.mj_name2id(model,mujoco.mjtObj.mjOBJ_JOINT,'motor');bjid=mujoco.mj_name2id(model,mujoco.mjtObj.mjOBJ_JOINT,'balljoint')
    qj=model.jnt_qposadr[jid];vj=model.jnt_dofadr[jid];qb=model.jnt_qposadr[bjid];vb=model.jnt_dofadr[bjid]
    bid=mujoco.mj_name2id(model,mujoco.mjtObj.mjOBJ_BODY,'ball');bg=mujoco.mj_name2id(model,mujoco.mjtObj.mjOBJ_GEOM,'ballgeom')
    wg={mujoco.mj_name2id(model,mujoco.mjtObj.mjOBJ_GEOM,f'p{i}') for i in range(len(wheel['pieces']))}
    target=np.array(wheel['target']);results=[];omega=-wheel['rotationSign']*wheel['rpm']*2*math.pi/60
    for x in [-.75,-.25,.25,.75]:
      for phase in range(8):
        mujoco.mj_resetData(model,data);data.qpos[qb:qb+3]=[x,4.7,-.5]
        touched=False;last=-1;release=None
        for step in range(1500):
            data.qpos[qj]=phase*math.pi/16+omega*data.time;data.qvel[vj]=omega
            mujoco.mj_step(model,data)
            contact=any((c.geom1==bg and c.geom2 in wg) or (c.geom2==bg and c.geom1 in wg) for c in data.contact)
            if contact:touched=True;last=data.time
            if touched and data.time-last>.04:
                p=data.xpos[bid].copy();v=data.qvel[vb:vb+3].copy();heading=target-p;heading[1]=0;heading/=np.linalg.norm(heading)
                forward=float(v@heading);peak=float(p[1]+max(0,v[1])**2/(2*9.81));disc=v[1]**2-2*9.81*(target[1]-p[1]);offset=None
                if v[1]>0 and disc>=0:
                    t=(v[1]+math.sqrt(disc))/9.81;at=p+v*t;offset=float(np.linalg.norm((at-target)[[0,2]]))
                release=dict(position=p.tolist(),velocity=v.tolist(),forward=forward,up=float(v[1]),peak=peak,ideal_offset=offset);break
        results.append(dict(x=x,phase=phase,touched=touched,release=release))
    counts=dict(trials=len(results),contacts=sum(r['touched'] for r in results),up_forward=sum(r['release'] is not None and r['release']['forward']>.8 and r['release']['up']>1.5 for r in results),rim_height=sum(r['release'] is not None and r['release']['forward']>.8 and r['release']['peak']>=target[1]+wheel['ballRadius'] for r in results),near_rim=sum(r['release'] is not None and r['release']['ideal_offset'] is not None and r['release']['ideal_offset']<1.5 for r in results))
    return dict(summary={k:int(v) for k,v in counts.items()},trials=results)
reports={}
for version in ['v5','v6']:
    snapshot=json.loads((HERE/f'launcher-geometry-{version}.json').read_text());reports[version]=run(snapshot);print(version,json.dumps(reports[version]['summary']),flush=True)
(HERE/'launcher-results.json').write_text(json.dumps(reports,indent=2))
if '--assert' in sys.argv:
    assert reports['v6']['summary']['contacts']>=24,reports['v6']['summary']
    assert reports['v6']['summary']['up_forward']>reports['v5']['summary']['up_forward'],reports
    assert reports['v6']['summary']['up_forward']>=12,reports['v6']['summary']
    assert reports['v6']['summary']['rim_height']>=4 and reports['v6']['summary']['near_rim']>0,reports['v6']['summary']
