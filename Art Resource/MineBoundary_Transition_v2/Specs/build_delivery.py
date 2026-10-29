"""Export image_gen art; construct read-only Map01 fitting and static review composites.
Requires Pillow. Does not write any Unity asset or original reference.
"""
from pathlib import Path
import sys,json,math,re,csv,hashlib,zipfile
from collections import Counter
sys.path.insert(0,str(Path(__file__).parent/'.tools'))
from PIL import Image,ImageDraw,ImageFont,ImageChops

ROOT=Path(__file__).resolve().parents[1];PROJECT=ROOT.parents[1]
SRC=ROOT/'Sources';PRE=ROOT/'Previews';SPEC=ROOT/'Specs';TEX=ROOT/'Textures'
for p in [PRE,TEX/'Corners',TEX/'EndCaps',TEX/'Straights',TEX/'WallRoots',TEX/'Map01Fits']:p.mkdir(parents=True,exist_ok=True)
NN=Image.Resampling.NEAREST
v1=PROJECT/'Art Resource/MineBoundary_Transition_v1/Textures'
A=Image.open(v1/'Transition_A_ContactDust_48x128.png').convert('RGBA')
B=Image.open(v1/'Transition_B_RockToe_48x128.png').convert('RGBA')
palette_colors=list(dict.fromkeys(p[:3] for im in [A,B] for p in im.get_flattened_data() if p[3]))
pal=Image.new('P',(1,1));flat=[c for p in palette_colors for c in p];pal.putpalette((flat+flat[:3]*256)[:768])
manifest=[];byname={}
def qalpha(a,maximum=144):return max(0,min(maximum,round(a/24)*24))
def tidy(im):
 im.putdata([p if p[3] else (0,0,0,0) for p in im.get_flattened_data()]);return im
def export(name,im,anchor,role,source,group):
 im=tidy(im.convert('RGBA'));path=TEX/group/(name+'.png');im.save(path);byname[name]=im
 manifest.append(dict(file=path.relative_to(ROOT).as_posix(),name=name,width=im.width,height=im.height,ppu=64,
  anchor_top_left_px=list(anchor),pivot=[anchor[0]/im.width,1-anchor[1]/im.height],scale=[1,1,1],rotation_z=0,
  sorting_layer='Default (existing)',sorting_order=-25,role_ko=role,source=source))
 return im
def colorize(im):
 a=im.getchannel('A');out=im.convert('RGB').quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA');out.putalpha(a);return out
def projected_source(name,size,source_anchor,target_anchor,scale):
 im=Image.open(SRC/('Generated_'+name+'.png')).convert('RGBA')
 # One nearest-neighbour sample per native pixel; no blur/antialiased resampling.
 coeff=(1/scale,0,source_anchor[0]-target_anchor[0]/scale,0,1/scale,source_anchor[1]-target_anchor[1]/scale)
 im=im.transform(size,Image.Transform.AFFINE,coeff,resample=NN)
 return colorize(im)
def finish(im,wall_test,kind='corner'):
 out=Image.new('RGBA',im.size)
 for y in range(im.height):
  for x in range(im.width):
   p=im.getpixel((x,y));edge=min(x,y,im.width-1-x,im.height-1-y)
   if p[3]<85 or edge<2:continue
   maximum=216 if wall_test(x+.5,y+.5) else (120 if kind=='corner' else 144)
   a=qalpha(p[3]/255*maximum*min(1,edge/3),maximum)
   if a:out.putpixel((x,y),p[:3]+(a,))
 return out

# Six source motifs; cardinal versions are exact 90-degree transforms, not extra painted variants.
conv=finish(projected_source('convex',(64,64),(700,570),(32,32),.055),lambda x,y:x<32 and y>32)
conc=finish(projected_source('concave',(64,64),(450,940),(32,32),.038),lambda x,y:not(x>32 and y<32))
for k,d in enumerate(['SW','SE','NE','NW']):
 export('CornerConvex_Wall'+d+'_64x64',conv.rotate(k*90), (32,32),'볼록 코너. Wall 뒤 방향은 암벽이 차지하는 사분면','Generated_convex', 'Corners')
for k,d in enumerate(['NE','NW','SW','SE']):
 export('CornerConcave_Floor'+d+'_64x64',conc.rotate(k*90), (32,32),'오목 코너. Floor 뒤 방향은 바닥이 차지하는 사분면','Generated_concave', 'Corners')

cap=finish(projected_source('cap',(48,64),(470,245),(16,12),.045),lambda x,y:x<16,kind='cap')
export('EndCap_WallW_Up_48x64',cap,(16,12),'암벽 서쪽/바닥 동쪽인 세로 구간의 위 끝','Generated_cap','EndCaps')
export('EndCap_WallW_Down_48x64',cap.transpose(Image.Transpose.FLIP_TOP_BOTTOM),(16,52),'암벽 서쪽/바닥 동쪽인 세로 구간의 아래 끝','Generated_cap reflected','EndCaps')
export('EndCap_WallE_Up_48x64',cap.transpose(Image.Transpose.FLIP_LEFT_RIGHT),(32,12),'암벽 동쪽/바닥 서쪽 세로 구간의 위 끝','Generated_cap reflected','EndCaps')
export('EndCap_WallE_Down_48x64',cap.transpose(Image.Transpose.ROTATE_180),(32,52),'암벽 동쪽/바닥 서쪽 세로 구간의 아래 끝','Generated_cap rotated','EndCaps')
short=cap.crop((0,0,48,32));short=finish(short,lambda x,y:x<16,kind='cap')
export('EndCap_ShortJoin_WallW_48x32',short,(16,12),'짧은 0.5유닛 끝 연결. 별도 사각 벽 아님','Generated_cap native crop','EndCaps')
for n in [1,2]:
 im=Image.open(SRC/f'Generated_straight{n}.png').convert('RGBA');bb=im.getchannel('A').point(lambda a:255 if a>=90 else 0).getbbox()
 crop=im.crop(bb);scale=min(28/crop.width,116/crop.height);crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),NN)
 native=Image.new('RGBA',(48,128));native.alpha_composite(crop,(4,6))
 native=finish(colorize(native),lambda x,y:False,kind='straight')
 # Hairline wall-facing contact is kept at x=4; faded ends are independent of the original A.
 export(f'Straight_ContactVar0{n}_48x128',native,(4,64),'긴 직선의 보조 변형. A를 교체하거나 같은 자리에 중첩 금지',f'Generated_straight{n}','Straights')
im=Image.open(SRC/'Generated_root.png').convert('RGBA').crop((225,95,661,1030))
scale=min(36/im.width,88/im.height);im=im.resize((round(im.width*scale),round(im.height*scale)),NN)
root=Image.new('RGBA',(48,96));root.alpha_composite(im,(-2,8));root=colorize(root)
for y in range(96):
 for x in range(48):
  p=root.getpixel((x,y));fade=min(1,max(0,(x-3)/7),max(0,(34-x)/7),max(0,(y-2)/6),max(0,(93-y)/6))
  # Do not add a second pebble outline: retain broad facet colors, suppress dark perimeter ink.
  luminance=.2126*p[0]+.7152*p[1]+.0722*p[2]
  a=qalpha(p[3]/255*96*fade,96) if p[3]>=90 and x<32 and luminance>=50 else 0
  root.putpixel((x,y),p[:3]+(a,) if a else (0,0,0,0))
export('WallRoot_FacetBlend_48x96',root,(32,48),'벽 안쪽 대형 암석 면을 잇는 국소 패치. 연속 띠 배치 금지','Generated_root','WallRoots')

snapshot=json.loads((SPEC/'PrefabReadOnlySnapshot.json').read_text())['data']['result']
geometry=json.loads(snapshot['geometry'][0]);floor_rects=geometry['floorAreas'];W,H=28*64,36*64
floor=Image.new('L',(W,H),0);fd=ImageDraw.Draw(floor)
for r in floor_rects:
 x0=round(r['x']*64);x1=round((r['x']+r['width'])*64);y0=round((36-r['y']-r['height'])*64);y1=round((36-r['y'])*64)
 fd.rectangle((x0,y0,x1-1,y1-1),fill=255)
def walk(x,y):return any(r['x']<=x<r['x']+r['width'] and r['y']<=y<r['y']+r['height'] for r in floor_rects)
def nums(s):return [float(x) for x in re.findall(r'-?\d+(?:\.\d+)?',s)]

# Reconstruct only the ALPHA coverage of the already installed A65/B4 visuals, never change them.
old_a=Image.new('L',(W,H),0)
old_b=Image.new('L',(W,H),0)
masks=json.loads((SPEC/'MaskReadOnlySnapshot.json').read_text())['data']['result']
for sr in snapshot['sprites']:
 is_a=sr['name'].startswith('A_')
 px,py,_=nums(sr['pos']);angle=round(nums(sr['rot'])[2])%360
 tile=(A if is_a else B).rotate(angle,expand=True);alpha=tile.getchannel('A');x=round(px*64-tile.width/2);y=round((36-py)*64-tile.height/2)
 layer=Image.new('L',(W,H));layer.paste(alpha,(x,y))
 if sr['name'].startswith('A_Short_'):
  m=masks[int(sr['name'][-2:])];clip=Image.new('L',(W,H));cd=ImageDraw.Draw(clip)
  cd.rectangle((round((m['cx']-m['sx']/2)*64),round((36-m['cy']-m['sy']/2)*64),round((m['cx']+m['sx']/2)*64)-1,round((36-m['cy']+m['sy']/2)*64)-1),fill=255)
  layer=ImageChops.multiply(layer,clip)
 # Standard source-over coverage of the unchanged existing A pieces.
 if is_a:old_a=ImageChops.invert(ImageChops.multiply(ImageChops.invert(old_a),ImageChops.invert(layer)))
 else:old_b=ImageChops.invert(ImageChops.multiply(ImageChops.invert(old_b),ImageChops.invert(layer)))

# Existing small rim coverage, sampled from the exact read-only static mesh UVs.
rim=Image.new('L',(W,H),0)
rimdata=json.loads((SPEC/'RimReadOnlySnapshot.json').read_text())['data']['result']['meshes']
for mesh in rimdata:
 tex=Image.open(PROJECT/mesh['texture']).convert('RGBA');ta=tex.getchannel('A')
 for i in range(0,len(mesh['verts']),4):
  vs=mesh['verts'][i:i+4];uv=mesh['uv'][i:i+4]
  x0,x1=round(vs[0]['x']*64),round(vs[1]['x']*64);y0,y1=round((36-vs[2]['y'])*64),round((36-vs[0]['y'])*64)
  if x1<=x0 or y1<=y0:continue
  for yy in range(y0,y1):
   t=(yy+.5-y0)/(y1-y0);v=uv[2]['y']*(1-t)+uv[0]['y']*t
   for xx in range(x0,x1):
    s=(xx+.5-x0)/(x1-x0);u=uv[0]['x']*(1-s)+uv[1]['x']*s
    tx=min(tex.width-1,int((u%1)*tex.width));ty=min(tex.height-1,int((1-v%1)*tex.height))
    a=ta.getpixel((tx,ty))
    if a>rim.getpixel((xx,yy)):rim.putpixel((xx,yy),a)

vertices=[('LeftTip',11.75,16),('MiddleLower',16.25,15),('MiddleUpper',16.25,16),
 ('StepLowerA',20,18),('StepLowerB',21,18),('StepInnerA',21,19),('StepOuterA',21.5,19),('StepInnerB',21,20.5),('StepOuterB',21.5,20.5)]
quadrants=[('NE',.01,.01),('NW',-.01,.01),('SW',-.01,-.01),('SE',.01,-.01)]
new_dust=Image.new('RGBA',(W,H));new_roots=Image.new('RGBA',(W,H));ownership=Image.new('L',(W,H));placements=[]
for index,(label,vx,vy) in enumerate(vertices):
 floor_quads=[q for q,dx,dy in quadrants if walk(vx+dx,vy+dy)]
 if len(floor_quads)==3:
  wallq=next(q for q,dx,dy in quadrants if q not in floor_quads);name='CornerConvex_Wall'+wallq+'_64x64'
 elif len(floor_quads)==1:name='CornerConcave_Floor'+floor_quads[0]+'_64x64'
 else:raise ValueError(f'Non-corner geometry at {label}: {floor_quads}')
 im=byname[name].copy();x0=round(vx*64)-32;y0=round((36-vy)*64)-32
 dust=Image.new('RGBA',(64,64));roots=Image.new('RGBA',(64,64));cut=0
 for y in range(64):
  for x in range(64):
   p=im.getpixel((x,y));xx=x0+x;yy=y0+y
   if not p[3]:continue
   if rim.getpixel((xx,yy))>0 or old_b.getpixel((xx,yy))>0 or ownership.getpixel((xx,yy)):
    im.putpixel((x,y),(0,0,0,0));cut+=1;continue
   is_floor=floor.getpixel((xx,yy))>0
   if is_floor and old_a.getpixel((xx,yy))>0:
    im.putpixel((x,y),(0,0,0,0));cut+=1;continue
   # Reclassify with actual geometry; outside-wall part never carries rock-level opacity.
   a=min(p[3],120) if is_floor else p[3]
   p=p[:3]+(a,);im.putpixel((x,y),p);ownership.putpixel((xx,yy),255)
   (dust if is_floor else roots).putpixel((x,y),p)
 fitname=f'Map01_J{index:02d}_{label}_64x64'
 export(fitname,im,(32,32),'Map01 정확한 현 배치 전용. A/Rim 중복부를 신규 PNG에서만 제외',name,'Map01Fits')
 new_dust.alpha_composite(dust,(x0,y0));new_roots.alpha_composite(roots,(x0,y0))
 placements.append(dict(id=index,label=label,vertex=[vx,vy],sprite=fitname,base_sprite=name,position=[vx,vy,0],pivot=[.5,.5],rotation_z=0,scale=[1,1,1],sorting_order=-25,removed_overlap_pixels=cut,floor_quadrants=floor_quads))

# Four local wall-side stitches. They are not a replacement wall or a continuous new border.
for j,(label,vx,vy,wallside) in enumerate([
 ('LeftTipWall',11.75,15.55,'W'),('MiddleWall',16.25,15.5,'E'),
 ('LongWall',21,21.55,'E'),('StepWall',21.5,19.75,'E')]):
 im=root.copy() if wallside=='W' else root.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
 ax=32 if wallside=='W' else 16;ay=48
 x0=round(vx*64)-ax;y0=round((36-vy)*64)-ay
 for y in range(im.height):
  for x in range(im.width):
   xx=x0+x;yy=y0+y;p=im.getpixel((x,y))
   if floor.getpixel((xx,yy)) or rim.getpixel((xx,yy)) or old_b.getpixel((xx,yy)) or ownership.getpixel((xx,yy)):
    im.putpixel((x,y),(0,0,0,0))
   elif p[3]:ownership.putpixel((xx,yy),255)
 name=f'Map01_R{j:02d}_{label}_48x96'
 export(name,im,(ax,ay),'Map01 국소 벽 안쪽 결합. 실제 벽/Rim/B 영역에 맞춘 전용본','WallRoot_FacetBlend_48x96','Map01Fits')
 new_roots.alpha_composite(im,(x0,y0))
 placements.append(dict(id=9+j,label=label,vertex=[vx,vy],sprite=name,base_sprite='WallRoot_FacetBlend_48x96',position=[vx,vy,0],pivot=[ax/48,.5],rotation_z=0,scale=[1,1,1],sorting_order=-25,wall_side=wallside))

actual=Image.open(SRC/'game-after.png').convert('RGBA')
assert actual.size==(1920,1080)
def project_to_capture(world_im):
 # Orthographic camera documented in the actual capture record, no new live camera assumed.
 crop=world_im.crop((round(10.56*64),round((36-22.74)*64),round(26.56*64),round((36-13.74)*64)))
 return crop.resize(actual.size,NN)
screen_dust=project_to_capture(new_dust);screen_roots=project_to_capture(new_roots)
stage2=Image.alpha_composite(actual,screen_dust);stage3=Image.alpha_composite(stage2,screen_roots)
stage2.save(PRE/'Map01_CornerEndCap_Expected.png');stage3.save(PRE/'Map01_WallBlend_Expected.png')
actual.save(PRE/'Map01_CurrentActual.png')
try:font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',15);titlefont=ImageFont.truetype('C:/Windows/Fonts/consolab.ttf',20)
except OSError:font=ImageFont.load_default();titlefont=font
def triptych(crop,name,zoom=1):
 imgs=[p.crop(crop) for p in [actual,stage2,stage3]];w,h=imgs[0].size
 out=Image.new('RGBA',(w*zoom*3,h*zoom+64),(25,29,31,255));d=ImageDraw.Draw(out)
 d.text((10,5),'CURRENT = ACTUAL CAPTURE | MIDDLE/RIGHT = STATIC EXPECTED COMPOSITES',font=font,fill=(240,210,140))
 for i,(im,label) in enumerate(zip(imgs,['CURRENT / A65+B4','+ CORNER / END JOINS','+ WALL ROOT BLEND'])):
  d.text((i*w*zoom+8,32),label,font=font,fill='white');out.alpha_composite(im.resize((w*zoom,h*zoom),NN),(i*w*zoom,64))
 out.save(PRE/(name+'.png'))
triptych((65,735,245,960),'Region1_LeftTip_Compare_3x',3)
triptych((590,710,820,995),'Region2_MiddleEnds_Compare_3x',3)
triptych((1060,220,1440,885),'Region3_RightSteps_Compare_2x',2)
triptych((1200,70,1370,500),'Region4_LongWall_Compare_2x',2)
# One overview keeps full-resolution comparisons available as separate files.
overview=Image.new('RGBA',(1536,340),(25,29,31,255));od=ImageDraw.Draw(overview)
for i,(im,label) in enumerate(zip([actual,stage2,stage3],['ACTUAL / CURRENT','EXPECTED / CORNERS','EXPECTED / WALL ROOTS'])):
 od.text((i*512+10,12),label,font=titlefont,fill='white');overview.alpha_composite(im.resize((512,288),NN),(i*512,45))
overview.save(PRE/'Map01_Overview_Current_Expected.png')

# At native 64PPU, contact/rim ownership is exactly exclusive in the fitted additions.
rim_overlap=sum(1 for p,q in zip(new_roots.getchannel('A').get_flattened_data(),rim.get_flattened_data()) if p and q)
dust_overlap=sum(1 for p,q in zip(new_dust.getchannel('A').get_flattened_data(),old_a.get_flattened_data()) if p and q)
combined=Image.alpha_composite(new_dust,new_roots)
outside_changes=sum(1 for a,b,m in zip(actual.get_flattened_data(),stage3.get_flattened_data(),ImageChops.lighter(screen_dust.getchannel('A'),screen_roots.getchannel('A')).get_flattened_data()) if m==0 and a!=b)

def checkboard(size):
 im=Image.new('RGBA',size,(51,54,53,255));d=ImageDraw.Draw(im)
 for y in range(0,size[1],8):
  for x in range(0,size[0],8):
   if (x//8+y//8)%2:d.rectangle((x,y,x+7,y+7),fill=(61,64,61,255))
 return im
cols=5;cw=260;ch=222;rows=math.ceil(len(manifest)/cols)
sheet=Image.new('RGBA',(cols*cw,rows*ch+55),(25,29,31,255));sd=ImageDraw.Draw(sheet)
sd.text((12,12),'MINE BOUNDARY v2 / REVIEW ONLY / CHECKERBOARD IS NOT IN PNGs',font=titlefont,fill='white')
for i,a in enumerate(manifest):
 x=(i%cols)*cw;y=55+(i//cols)*ch;tile=checkboard((250,170));im=byname[a['name']];s=min(3,230//im.width,160//im.height)
 im=im.resize((im.width*s,im.height*s),NN);tile.alpha_composite(im,((250-im.width)//2,(170-im.height)//2));sheet.alpha_composite(tile,(x+5,y))
 sd.text((x+7,y+174),a['name'][:31],font=font,fill=(220,220,207));sd.text((x+7,y+193),f'{a["width"]}x{a["height"]}  anchor {a["anchor_top_left_px"]}',font=font,fill=(165,175,167))
sheet.save(PRE/'AllAssets_ContactSheet.png')

# Long straight: old A kept unchanged. New variants occupy only empty original alpha samples.
floor_tile=Image.open(PROJECT/'Art Resource/MineEnvironment_v1/Floors/Floor_RockGray_64x64.png').convert('RGBA')
wall_tile=Image.open(PROJECT/'Art Resource/MineEnvironment_v1/Walls/Wall_RockMass_64x64.png').convert('RGBA')
long=Image.new('RGBA',(128,640));baseold=Image.new('RGBA',long.size)
for yy in range(0,640,64):
 long.paste(wall_tile,(0,yy));long.paste(floor_tile,(64,yy))
for i in range(5):baseold.alpha_composite(A,(60,i*128))
before=Image.alpha_composite(long,baseold);after=before.copy();add=Image.new('RGBA',long.size)
for i in range(5):
 tile=byname[f'Straight_ContactVar0{1+i%2}_48x128'].copy()
 for y in range(128):
  for x in range(48):
   if baseold.getpixel((60+x,i*128+y))[3]:tile.putpixel((x,y),(0,0,0,0))
 add.alpha_composite(tile,(60,i*128))
for yy in [75,355]:
 t=root.copy()
 for y in range(t.height):
  for x in range(t.width):
   if 32+x>=64:t.putpixel((x,y),(0,0,0,0))
 add.alpha_composite(t,(32,yy))
after.alpha_composite(add)
lp=Image.new('RGBA',(1280,600),(25,29,31,255));ld=ImageDraw.Draw(lp)
ld.text((12,8),'LONG RUN / 10 UNITS / OLD A KEPT / VAR01+VAR02 IN EMPTY COVERAGE ONLY',font=titlefont,fill='white')
for i,(p,label) in enumerate([(before,'CURRENT A REPEAT'),(after,'A + TWO SPARSE SUPPLEMENTS')]):
 p=p.rotate(90,expand=True).resize((1280,256),NN);ld.text((10,38+i*278),label,font=font,fill='white');lp.alpha_composite(p,(0,60+i*278))
lp.save(PRE/'LongRun_Repeat_Example_2x.png')

# SpriteMask-equivalent CPU clipping, not an in-Unity import test.
masktests=[];mp=Image.new('RGBA',(1080,500),(25,29,31,255));md=ImageDraw.Draw(mp)
md.text((12,8),'MASK CLIP SIMULATION / 0.5, 1.0, 1.5 UNITS / NO UNITY APPLY',font=titlefont,fill='white')
for k,length in enumerate([32,64,96]):
 im=byname['Straight_ContactVar01_48x128'].copy();m=Image.new('L',im.size);d=ImageDraw.Draw(m);top=(128-length)//2;d.rectangle((0,top,47,top+length-1),fill=255)
 im.putalpha(ImageChops.multiply(im.getchannel('A'),m));outside=sum(1 for a,b in zip(im.getchannel('A').get_flattened_data(),m.get_flattened_data()) if a and not b)
 masktests.append(dict(length_px=length,length_units=length/64,outside_mask_nonzero=outside))
 for j,angle in enumerate([0,90]):
  p=im.rotate(angle,expand=True);bg=checkboard((320,190));s=min(3,300//p.width,170//p.height);p=p.resize((p.width*s,p.height*s),NN);bg.alpha_composite(p,((320-p.width)//2,(190-p.height)//2));mp.alpha_composite(bg,(k*360+15,60+j*215))
 md.text((k*360+15,35),f'{length/64:g} unit / crop only / scale 1',font=font,fill='white')
mp.save(PRE/'SpriteMask_ClipSimulation.png')

# Nonoverlapping cap/body ownership, with the attachment line held fixed at x=32.
jp=Image.new('RGBA',(960,640),(25,29,31,255));jd=ImageDraw.Draw(jp)
jd.text((12,10),'ENDCAP + A PROFILE / ISOLATED COMPATIBILITY TEST / CPU SIMULATION',font=titlefont,fill='white')
joint_tests=[]
for k,length in enumerate([32,64,96]):
 bg=checkboard((96,180));top=36;bottom=top+length
 layers=[]
 for name,pos,ymin,ymax in [
  ('EndCap_WallW_Up_48x64',(16,top-12),0,top+16),
  ('EXISTING_A_REFERENCE',(28,(top+bottom)//2-64),top+16,bottom-16),
  ('EndCap_WallW_Down_48x64',(16,bottom-52),bottom-16,180)]:
  layer=Image.new('RGBA',bg.size);layer.alpha_composite(A if name=='EXISTING_A_REFERENCE' else byname[name],pos)
  for y in range(180):
   if y<ymin or y>=ymax:
    for x in range(96):layer.putpixel((x,y),(0,0,0,0))
  layers.append(layer);bg.alpha_composite(layer)
 overlap=sum(1 for px in zip(*(im.getchannel('A').get_flattened_data() for im in layers)) if sum(a>0 for a in px)>1)
 joint_tests.append(dict(length_units=length/64,body_endcap_overlap_pixels=overlap))
 jp.alpha_composite(bg.resize((288,540),NN),(k*320+16,75));jd.text((k*320+16,42),f'{length/64:g} unit / cap-body-cap',font=font,fill='white')
jp.save(PRE/'EndCap_ShortBody_JoinSimulation.png')

# Direction and anchor card. The cross marks vertex, not collider placement.
card=Image.new('RGBA',(1024,560),(25,29,31,255));cd=ImageDraw.Draw(card)
cd.text((12,10),'DIRECTIONS / WALL quadrant for CONVEX, FLOOR quadrant for CONCAVE',font=titlefont,fill='white')
for i,a in enumerate(manifest[:8]):
 x=(i%4)*256;y=60+(i//4)*250;bg=checkboard((240,208));im=byname[a['name']].resize((192,192),NN);bg.alpha_composite(im,(24,8));bd=ImageDraw.Draw(bg);bd.line((116,104,124,104),fill=(180,197,192),width=1);bd.line((120,100,120,108),fill=(180,197,192),width=1);card.alpha_composite(bg,(x+8,y));cd.text((x+9,y+210),a['name'].replace('_64x64',''),font=font,fill='white')
card.save(PRE/'Direction_Anchors.png')

with (SPEC/'AssetManifest.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(manifest[0]));w.writeheader();w.writerows(manifest)
(SPEC/'AssetManifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
(SPEC/'Map01_PlacementPlan.json').write_text(json.dumps(placements,ensure_ascii=False,indent=2),encoding='utf8')
audit=dict(asset_count=len(manifest),original_motifs=6,straight_variations=2,ppu=64,palette_rgb_count=len(palette_colors),
 old_A_count=65,old_B_count=4,colliders_snapshot=snapshot['colliders'],rim_new_overlap_pixels=rim_overlap,
 existing_A_new_dust_overlap_pixels=dust_overlap,actual_capture_outside_overlay_changes=outside_changes,mask_clip_tests=masktests,
 unity_applied=False,unity_spritemask_tested=False,play_tested=False,files=[])
audit['existing_B_new_overlay_overlap_pixels']=sum(1 for p,q in zip(combined.getchannel('A').get_flattened_data(),old_b.get_flattened_data()) if p and q)
audit['exclusive_endcap_body_tests']=joint_tests
for a in manifest:
 p=ROOT/a['file'];im=Image.open(p);pixels=list(im.get_flattened_data());levels=sorted(set(p[3] for p in pixels));border=[]
 for x in range(im.width):border.extend([im.getpixel((x,0)),im.getpixel((x,im.height-1))])
 for y in range(im.height):border.extend([im.getpixel((0,y)),im.getpixel((im.width-1,y))])
 palette_ok=all(p[:3] in palette_colors for p in pixels if p[3])
 audit['files'].append(dict(file=a['file'],size=list(im.size),mode=im.mode,bbox=im.getchannel('A').getbbox(),alpha_levels=levels,
 alpha_step_24=all(n%24==0 for n in levels),palette_subset=palette_ok,outer_edge_transparent=all(p[3]==0 for p in border),
 sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
assert all(f['mode']=='RGBA' and f['alpha_step_24'] and f['palette_subset'] and f['outer_edge_transparent'] and f['bbox'] for f in audit['files'])
assert not rim_overlap and not dust_overlap and not outside_changes
protected=json.loads((SPEC/'ProtectedFiles_Before.json').read_text(encoding='utf8'))
changes=[p for p,h in protected.items() if not (PROJECT/p).exists() or hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()!=h]
audit['protected_files_checked']=len(protected);audit['protected_changed']=changes
assert not changes,changes
(SPEC/'Validation.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in audit.items() if k!='files'},ensure_ascii=False))
