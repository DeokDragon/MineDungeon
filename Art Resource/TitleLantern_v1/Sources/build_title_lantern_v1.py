from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,GifImagePlugin
import numpy as np,json,hashlib,zipfile,shutil,math
W=Path('C:/Users/User/.codex/visualizations/2026/10/22/01a16344-f87e-7b93-9d18-cb1d88939ccf')
R=Path('D:/Git/MineDungeon2');O=R/'Art Resource/TitleLantern_v1';OUT=O
for sub in ['Flames/Frames_Hanging','Flames/Frames_Barrel','Glow','Glass','SurfaceLighting','Previews','Specs','Sources']:(O/sub).mkdir(parents=True,exist_ok=True)
source=Path('C:/Users/User/Downloads/TitleScene_background.png');bg=Image.open(source).convert('RGBA');assert bg.size==(2752,1536)
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
D=Path('C:/Users/User/.codex/generated_images/01a16344-f87e-7b93-9d18-cb1d88939ccf')
sources={'flames':D/'exec-ed55aff5-1ae0-4cc9-b760-66bd565f02c5.png','glow':D/'exec-b0594390-fb14-4f1a-a3ba-fedae5140e57.png','lit_reference':D/'exec-fdcf344a-d2ea-4422-944d-13c74093534d.png'}
manifest=[]
def save(im,path,role,**extra):
 im.save(O/path);im2=Image.open(O/path);assert im2.size==im.size and im2.mode=='RGBA'
 ar=np.array(im2);manifest.append(dict(file=path,size=list(im.size),mode='RGBA',alpha_range=[int(ar[:,:,3].min()),int(ar[:,:,3].max())],role=role,**extra));return im
raw=Image.open(sources['flames']).convert('RGBA');rw,rh=raw.size;rawparts=[]
for i in range(6):
 cell=raw.crop((round(i*rw/6),0,round((i+1)*rw/6),rh));ar=np.array(cell);ar[:,:,3]=np.where(ar[:,:,3]>=160,255,0);ar[ar[:,:,3]==0,:3]=0;cell=Image.fromarray(ar);bb=cell.getbbox();assert bb;rawparts.append(cell.crop(bb))
# Common size/grounded base; the generated tip/inner-flame shapes are preserved.
scale=30/max(im.height for im in rawparts);flames={};flame_specs={}
for name,wh,basept,factor in [('Hanging',(32,48),(16,42),1),('Barrel',(24,32),(12,27),16/30)]:
 fs=[]
 for i,im in enumerate(rawparts):
  sz=(max(1,round(im.width*scale*factor)),max(1,round(im.height*scale*factor)))
  small=im.resize(sz,Image.Resampling.NEAREST)
  # Clean finite flame palette, preserving binary alpha from the source.
  ar=np.array(small);aa=ar[:,:,3].copy();q=small.convert('RGB').quantize(colors=7,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB');ar[:,:,:3]=np.array(q);ar[:,:,3]=aa;ar[aa==0,:3]=0
  small=Image.fromarray(ar);f=Image.new('RGBA',wh);f.paste(small,(basept[0]-sz[0]//2,basept[1]-sz[1]));fs.append(f)
  save(f,f'Flames/Frames_{name}/Flame_F{i+1:02}.png',f'{name} flame frame {i+1}')
 sheet=Image.new('RGBA',(wh[0]*6,wh[1]))
 for i,f in enumerate(fs):sheet.paste(f,(wh[0]*i,0))
 path=f'Flames/TitleLantern_{name}_Flame_6f_{wh[0]}x{wh[1]}_v1.png'
 save(sheet,path,'flame strip',frames=6,cell=list(wh),pivot_normalized=[basept[0]/wh[0],1-basept[1]/wh[1]])
 assert len({hashlib.sha256(f.tobytes()).hexdigest() for f in fs})==6
 for f in fs:
  ar=np.array(f);assert set(np.unique(ar[:,:,3]))=={0,255};assert not ar[0,:,3].any() and not ar[-1,:,3].any() and not ar[:,0,3].any() and not ar[:,-1,3].any()
 flames[name]=fs;flame_specs[name]=dict(cell=list(wh),file=path,source_anchor_top_left=list(basept),pivot_normalized=[basept[0]/wh[0],1-basept[1]/wh[1]])
halo=Image.open(sources['glow']).convert('RGBA').resize((96,96),Image.Resampling.NEAREST).resize((384,384),Image.Resampling.NEAREST);ar=np.array(halo);ar[:,:,3]=np.rint(ar[:,:,3]*.18).astype('uint8');ar[ar[:,:,3]<2]=0
halo=save(Image.fromarray(ar),'Glow/TitleLantern_WarmHalo_384x384_v1.png','shared subtle halo; position each lamp independently',pivot=[.5,.5])

anchors={'Hanging':(2344,800),'Barrel':(2476,1078)}
flameanchors={'Hanging':(2338,820),'Barrel':(2476,1090)}
glass_specs={'Hanging':dict(size=(64,80),origin=(2312,760),polys=[[(2324,777),(2350,776),(2351,822),(2324,820)],[(2358,775),(2370,772),(2370,822),(2358,824)]]),'Barrel':dict(size=(40,48),origin=(2456,1054),polys=[[(2468,1063),(2485,1063),(2485,1089),(2468,1089)]])}
glass={}
for key,sp in glass_specs.items():
 im=Image.new('RGBA',sp['size']);d=ImageDraw.Draw(im)
 for j,poly in enumerate(sp['polys']):d.polygon([(x-sp['origin'][0],y-sp['origin'][1]) for x,y in poly],fill=(246,158,58,72 if j==0 else 44))
 glass[key]=save(im,f'Glass/TitleLantern_{key}_GlassLight_v1.png','warm glass only, metal bars excluded')

# Extract illumination guidance only. Never copy the generated scene/object pixels.
cropbox=(1728,512,2752,1536);original=np.array(bg.crop(cropbox))[:,:,:3].astype(float)
lit=np.array(Image.open(sources['lit_reference']).convert('RGB').resize((1024,1024),Image.Resampling.NEAREST)).astype(float)
positive=np.maximum(lit-original,0).mean(2)
guide=Image.fromarray(np.clip(positive*2,0,255).astype('uint8')).resize((128,128),Image.Resampling.BOX).resize((1024,1024),Image.Resampling.NEAREST)
guide=np.array(guide).astype(float)/255
Y,X=np.indices((1024,1024));lum=original.mean(2);facet=.60+.40*np.clip(lum/85,0,1)
def poly(points):
 m=Image.new('L',(1024,1024));ImageDraw.Draw(m).polygon(points,fill=255);return np.array(m)/255
wood=poly([(610,0),(690,0),(690,659),(641,659),(619,624)])
wood=np.maximum(wood,poly([(700,80),(803,70),(825,489),(694,533)]))
wood=np.maximum(wood,poly([(684,575),(741,557),(799,576),(820,614),(806,741),(761,757),(680,735),(662,684)]))
rock=poly([(807,0),(1023,0),(1023,780),(879,801),(830,691),(835,533),(776,463),(774,315)])
floor=poly([(259,660),(615,553),(656,654),(774,741),(873,758),(903,865),(639,922),(246,865),(150,776)])
case_mask=poly([(590,252),(597,244),(609,233),(622,233),(633,244),(649,248),(647,318),(642,328),(626,337),(601,333),(589,324),(585,312)])>0
case_mask|=poly([(731,539),(739,533),(744,526),(754,526),(761,535),(767,540),(765,584),(734,585)])>0
edgefade=np.minimum.reduce([np.clip(X/36,0,1),np.clip((1023-X)/36,0,1),np.clip(Y/36,0,1),np.clip((1023-Y)/36,0,1)])
def field(cx,cy,rx,ry):return np.exp(-2.3*(((X-cx)/rx)**2+((Y-cy)/ry)**2))
surfaces={};surface_layers={}
for key,cx,cy in [('Hanging',616,288),('Barrel',748,566)]:
 rad=field(cx,cy,380 if key=='Hanging' else 290,470 if key=='Hanging' else 320)
 pool=field(520 if key=='Hanging' else 705,730 if key=='Hanging' else 790,370 if key=='Hanging' else 270,165)
 channels={'Wood':wood*rad*.30,'Rock':rock*rad*.23,'Floor':floor*pool*(.11 if key=='Hanging' else .16)}
 total=np.zeros((1024,1024))
 for mat,m in channels.items():
  strength=m*facet*(.70+.30*guide)*edgefade;alpha=np.rint(strength*255).astype('uint8');alpha[alpha<2]=0
  # Re-quantize opacity on a4px grid, retaining exact transparent support.
  alpha=np.array(Image.fromarray(alpha).resize((256,256),Image.Resampling.BOX).resize((1024,1024),Image.Resampling.NEAREST));alpha[m==0]=0
  # Preserve the existing metal lantern cage. Glass/flame are separate resources.
  alpha[case_mask]=0;alpha[0,:]=0;alpha[-1,:]=0;alpha[:,0]=0;alpha[:,-1]=0
  ar=np.zeros((1024,1024,4),np.uint8);ar[:,:,:3]=[249,176,81];ar[:,:,3]=alpha;ar[alpha==0,:3]=0
  layer=save(Image.fromarray(ar),f'SurfaceLighting/TitleLantern_{key}_{mat}_Light_v1.png',f'{key}: {mat} illumination only',source_crop=list(cropbox),pivot=[.5,.5]);surface_layers[(key,mat)]=layer
  total=1-(1-total)*(1-alpha/255)
 ar=np.zeros((1024,1024,4),np.uint8);ar[:,:,:3]=[249,176,81];ar[:,:,3]=np.rint(total*255).astype('uint8');ar[ar[:,:,3]==0,:3]=0
 surfaces[key]=save(Image.fromarray(ar),f'SurfaceLighting/TitleLantern_{key}_SurfaceLight_Combined_v1.png',f'{key}: optional combined wood+rock+floor; do not stack with components',source_crop=list(cropbox),pivot=[.5,.5])

def tintalpha(im,v):
 ar=np.array(im);ar[:,:,3]=np.rint(ar[:,:,3]*v).astype('uint8');return Image.fromarray(ar)
def brightness(key,t):
 return .94+.06*math.sin(2*math.pi*(t/(2.6 if key=='Hanging' else 2.9))+(0 if key=='Hanging' else 1.1))
def composite(t=0,active=True):
 out=bg.copy()
 if not active:return out
 for key in ['Hanging','Barrel']:
  b=brightness(key,t);out.alpha_composite(tintalpha(surfaces[key],b),(1728,512))
  h=halo if key=='Hanging' else halo.resize((256,256),Image.Resampling.NEAREST)
  cx,cy=anchors[key];out.alpha_composite(tintalpha(h,b),(cx-h.width//2,cy-h.height//2))
  sp=glass_specs[key];out.alpha_composite(tintalpha(glass[key],b),sp['origin'])
  fs=flames[key];dt=.14 if key=='Hanging' else .16;ix=int((t+(0 if key=='Hanging' else .32))/dt)%6
  f=fs[ix];basept=flame_specs[key]['source_anchor_top_left'];px,py=flameanchors[key];out.alpha_composite(f,(px-basept[0],py-basept[1]))
 return out
on=composite();on.save(O/'Previews/Title_Lanterns_ON_FullResolution.png');bg.save(O/'Previews/Title_Lanterns_OFF_Reference.png')
ft=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',23)
compare=Image.new('RGB',(1920,576),(16,21,24));compare.paste(bg.convert('RGB').resize((960,536),Image.Resampling.NEAREST),(0,40));compare.paste(on.convert('RGB').resize((960,536),Image.Resampling.NEAREST),(960,40));d=ImageDraw.Draw(compare);d.text((16,4),'OFF · 원본 유지',font=ft,fill=(223,215,191));d.text((976,4),'ON · 분리 조명/불꽃 시험 합성',font=ft,fill=(223,215,191));compare.save(O/'Previews/Title_OFF_ON_Comparison.png')
detail=Image.new('RGBA',(960,416),(18,23,26,255));d=ImageDraw.Draw(detail)
for j,key in enumerate(['Hanging','Barrel']):
 cx,cy=anchors[key];box=(cx-120,cy-90,cx+120,cy+90)
 for i,im in enumerate([bg,on]):detail.alpha_composite(im.crop(box).resize((480,360),Image.Resampling.NEAREST),(i*480,28 if j==0 else 0)) if False else None
# Two actual native crop comparisons, each at2x; original remains unmodified.
for key in ['Hanging','Barrel']:
 cx,cy=anchors[key];box=(cx-100,cy-100,cx+100,cy+100);c=Image.new('RGBA',(800,432),(18,23,26,255));d=ImageDraw.Draw(c)
 for i,im in enumerate([bg,on]):c.alpha_composite(im.crop(box).resize((400,400),Image.Resampling.NEAREST),(i*400,32))
 d.text((8,2),key+' OFF / ON (2x)',font=ft,fill=(230,220,193));c.save(O/'Previews'/f'{key}_Glass_Alignment_2x.png')

window_checks={}
for key in ['Hanging','Barrel']:
 window=Image.new('L',bg.size);dd=ImageDraw.Draw(window)
 for polygon in glass_specs[key]['polys']:dd.polygon(polygon,fill=255)
 mask=np.array(window)>0;basept=flame_specs[key]['source_anchor_top_left'];px,py=flameanchors[key]
 for f in flames[key]:
  yy,xx=np.where(np.array(f)[:,:,3]>0);assert mask[yy+py-basept[1],xx+px-basept[0]].all(),key
 window_checks[key]='all six flame masks inside measured existing glass polygons'

code=(W/'build_p1_attacks.py').read_text(encoding='utf-8');exec(code[code.index('def background(f):'):code.index('results={};gifchecks=[]')],globals())
gifchecks=[]
for key in ['Hanging','Barrel']:
 dt=140 if key=='Hanging' else 160
 gifchecks.append(gif(flames[key],[dt]*6,O/'Previews'/f'{key}_Flame_6f_4x.gif',4))
frames=[composite(i*.1).resize((1376,768),Image.Resampling.NEAREST) for i in range(52)]
gifchecks.append(gif(frames,[100]*52,O/'Previews/Title_Lanterns_Lit_5p2s_Preview.gif'))
for key in ['Hanging','Barrel']:
 cx,cy=anchors[key];fs=[composite(i*.1).crop((cx-90,cy-90,cx+90,cy+90)).resize((360,360),Image.Resampling.NEAREST) for i in range(29)]
 gifchecks.append(gif(fs,[100]*29,O/'Previews'/f'{key}_LightAndFlame_Closeup.gif'))
assert hashlib.sha256(source.read_bytes()).hexdigest()==source_hash
assert all(0<np.array(f)[:,:,3].sum() for f in flames['Hanging']+flames['Barrel'])
assert all(np.array(surfaces[k])[:,:,3].max()<128 for k in surfaces)
spec={'source_background':str(source),'source_size':[2752,1536],'source_sha256':source_hash,'coordinate_system':'PNG top-left origin x right y down; all placement in original background pixels; no stretched background','crop_region':list(cropbox),'surface_crop_center':[2240,1024],'flame_specs':flame_specs,'lantern_centers':{k:list(v) for k,v in anchors.items()},'flame_base_anchors':{k:list(v) for k,v in flameanchors.items()},'glass_specs':glass_specs,'trial_timing':{'flame_Hanging_frame_ms':140,'flame_Hanging_cycle_ms':840,'flame_Barrel_frame_ms':160,'flame_Barrel_cycle_ms':960,'Barrel_flame_offset_ms':320,'light_Hanging_cycle_s':2.6,'light_Barrel_cycle_s':2.9,'Barrel_light_phase_radians':1.1,'light_alpha_multiplier_min':.88,'light_alpha_multiplier_max':1.0},'draw_order':['unchanged background','surface illumination (combined OR separate components)','subtle halo','glass light','flame'],'blend':'normal straight RGBA alpha, not additive','assets':manifest,'gif_checks':gifchecks,'checks':{'source_file_unchanged':True,'all_runtime_PNG_RGBA_and_dimensions_readback':True,'flame_frames_six_unique_each':True,'flames_always_visible':True,'flame_binary_alpha_no_canvas_clipping':True,'flame_inside_existing_windows':window_checks,'layers_independent_background_not_baked':True,'preview_restores_original_geometry':True,'static_OFF_ON_and_lantern_closeups_visually_reviewed':True,'actual_realtime_GIF_observation':False,'Unity_Play':'out of scope; not performed'},'generation_sources':{k:str(p) for k,p in sources.items()},'surface_method':'Only coarse positive-light guidance extracted from generated lighting edit; pure warm-color alpha layers authored over original surface masks; generated RGB scene is never composited into final background'}
(O/'Specs/Placement_and_Validation.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'output':str(O),'runtime_assets_including_individual_frames':len(manifest),'source_unchanged':True,'gif_count':len(gifchecks)},ensure_ascii=False))

readme='''# 메인 타이틀 랜턴 불꽃·조명 v1

## 기준과 제작 범위

`C:/Users/User/Downloads/TitleScene_background.png` 원본을 검사했다. 실제 크기는 **2752×1536px RGBA**다. 채팅에 축소 표시된2048px 이미지를 좌표 기준으로 쓰지 않았다. 기둥의 큰 랜턴과 통 위의 작은 랜턴을 각각 측정했으며 배경 구도·랜턴 케이스·기둥·통·레일을 이동하거나 덮어쓰지 않았다.

내장 image_gen으로 불꽃6프레임과 빛 번짐 소스를 제작하고, 원본 부분 영역의 조명 편집을 참고했다. 생성된 조명 배경의 물체 RGB는 최종 배경에 합성하지 않았다. 그 이미지에서 거친 밝기 변화만 참고해 **원본 표면 마스크 위에 따뜻한 색의 투명 조명 레이어**를 만들었다. 따라서 물체가 이중으로 나타나거나 생성본의 모양으로 교체되지 않는다. 세부 제작 프롬프트는 `Sources/GenerationPrompts.md`에 있다.

이번 결과물은 아트만이다. Unity Import·코드·씬·프리팹·Light2D·밝기 애니메이션을 구현하지 않았다. 기존 배경 원본과 프로젝트 리소스는 변경하지 않았다.

## 게임용 PNG

|파일/폴더|셀·프레임|실제 크기|용도|
|---|---|---|---|
|Flames/TitleLantern_Hanging_Flame_6f_32x48_v1.png|32×48 /6|192×48|기둥 랜턴 불꽃|
|Flames/TitleLantern_Barrel_Flame_6f_24x32_v1.png|24×32 /6|144×32|통 위 랜턴 불꽃|
|Flames/Frames_Hanging/Flame_F01~06.png|각32×48|개별6개|기둥 불꽃 분리 프레임|
|Flames/Frames_Barrel/Flame_F01~06.png|각24×32|개별6개|통 위 불꽃 분리 프레임|
|Glow/TitleLantern_WarmHalo_384x384_v1.png|단일|384×384|공유 가능한 약한 빛 번짐|
|Glass/TitleLantern_Hanging_GlassLight_v1.png|단일|64×80|기둥 랜턴 유리창 안쪽 빛|
|Glass/TitleLantern_Barrel_GlassLight_v1.png|단일|40×48|통 위 랜턴 유리창 안쪽 빛|
|SurfaceLighting/TitleLantern_Hanging_Wood/Rock/Floor_Light_v1.png|단일3개|각1024×1024|기둥 랜턴의 재질별 반사광|
|SurfaceLighting/TitleLantern_Barrel_Wood/Rock/Floor_Light_v1.png|단일3개|각1024×1024|통 위 랜턴의 재질별 반사광|
|SurfaceLighting/TitleLantern_Hanging_SurfaceLight_Combined_v1.png|단일|1024×1024|기둥 반사광3종을 합친 선택형|
|SurfaceLighting/TitleLantern_Barrel_SurfaceLight_Combined_v1.png|단일|1024×1024|통 위 반사광3종을 합친 선택형|

총25개 PNG이며 개별 프레임12개와 선택형 결합본2개를 포함한 수량이다. 모든 게임용 PNG는 **RGBA·실제 투명 배경**이다. 불꽃은 알파0/255의 선명한 픽셀이며6프레임 모두 불이 켜져 있다. 빛 번짐·유리·반사광은 의도적인 반투명 알파다. 배경·랜턴 본체·텍스트를 게임용 레이어에 넣지 않았다.

**Combined는 조명 성분끼리만 합친 투명 PNG**다. 배경과 합친 파일이 아니다. 적용할 때 각 랜턴의 Combined1개 또는 Wood/Rock/Floor3개 중 하나의 방식을 선택한다. 둘을 동시에 쓰면 빛이 중복된다. 두 랜턴의 조명은 독립적으로 재생/밝기 조절할 수 있다.

## 원본 픽셀 기준 배치

좌표는 원본2752×1536의 좌상단(0,0), 오른쪽+X, 아래쪽+Y다. 아래 수치는 배경에 대한 시각 정렬용이며 게임 월드 좌표가 아니다.

|레이어|배경 안의 기준 위치|파일 Pivot/주의|
|---|---|---|
|기둥 불꽃|불꽃 밑변 앵커(2338,820)|셀 내 앵커(16,42), Unity Pivot(0.5,0.125)|
|통 위 불꽃|불꽃 밑변 앵커(2476,1090)|셀 내 앵커(12,27), Unity Pivot(0.5,0.15625)|
|기둥 유리 빛|중앙(2344,800)|중앙Pivot, 좌상단(2312,760)|
|통 위 유리 빛|중앙(2476,1078)|중앙Pivot, 좌상단(2456,1054)|
|기둥 빛 번짐|중앙(2344,800)|중앙Pivot,384×384 표시 시험|
|통 위 빛 번짐|중앙(2476,1078)|같은PNG를256×256으로 표시 시험|
|모든 표면 조명 레이어|중앙(2240,1024)|중앙Pivot,1024×1024, 좌상단(1728,512)|

표면 PNG들은 같은1024×1024 영역을 공유한다. 자동으로 내용만 Tight Crop하면 정렬이 깨지므로 FullRect로 유지한다. Unity에서 위/아래 좌표를 뒤집지 않도록 주의한다. 중앙정렬된2752×1536 UI 부모를 쓸 경우 이미지 좌표(x,y)는 로컬(x-1376,768-y)로 변환할 수 있다. 예를 들어 기둥 유리/번짐은(968,-32), 통 위 유리/번짐은(1100,-310), 표면 조명은(864,-256)이다. 불꽃은 중앙Pivot이 아닌 밑변 앵커 위치로 놓는다.

배경과 모든 레이어를 같은 부모 아래 두고 **같은 비율로** 조정한다. 화면에 맞추느라 배경만 가로/세로로 다르게 늘이지 않는다. Fit 기준 배율은 s=min(표시가로/2752,표시세로/1536), Cover를 쓸 경우에는 max와 실제 크롭 오프셋을 후속Codex가 함께 확인한다. 기존 타이틀 배경을 어떤 UI/카메라 방식으로 표시하는지는 이번에 조사·변경하지 않았다.

## 불꽃과 밝기 변화 시험값

- 기둥 불꽃: 프레임당140ms,6프레임,0.84초 루프.
- 통 위 불꽃: 프레임당160ms,6프레임,0.96초 루프. 시작 위상은0.32초 뒤쪽으로 제안한다.
- 기둥 조명:2.6초 주기, 알파배수0.94+0.06×sin(2πt/2.6).
- 통 위 조명:2.9초 주기, 알파배수0.94+0.06×sin(2πt/2.9+1.1).
- 조명 알파배수 범위는0.88~1.00으로 빛이 꺼지거나 강하게 번쩍이지 않는다. 각 랜턴의 번짐·유리·표면 조명에 같은 배수를 적용하는 시험안이다. 불꽃 자체를0으로 페이드하지 않는다.

시간과 밝기는 최종 확정값이 아니다. 실제 타이틀 텍스트/버튼이 겹친 화면에서 너무 밝지 않은지 후속 검토한다. 두 랜턴을 동일 위상으로 시작하지 않으며, 불꽃 프레임 주기와 주변 밝기 주기도 구분한다.

## Import·블렌드·겹침

Point / Compression None / Mipmaps Off / sRGB On / FullRect 권장. UI Sprite PPU는 기존Canvas 설정을 따른다. 환경 오브젝트의PPU64를 타이틀UI에 임의로 강제하지 않는다. 불꽃은Multiple, 수동 그리드32×48 또는24×32로 분리한다.

모든 조명은 **일반 Straight RGBA Alpha Blend**로 미리보기했다. Additive/Screen용으로 만든 PNG가 아니므로 밝기가 달라지는 재질로 바로 바꾸지 않는다. Image.color는 흰색에서 시작하고 조명만 알파배수를 제어한다. 파일 알파에 이미 약한 밝기가 담겨 있으므로 중복 발광/새 광원은 필요하지 않다.

겹침 순서: 원본 배경→표면 조명→약한 빛 번짐→유리 안쪽 빛→불꽃. 기존 랜턴의 금속 케이스를 재생성하거나 덮는 별도 케이스 스프라이트는 없다. 표면 조명에는 기존 케이스 실루엣을 제외했고, 불꽃은 기존 유리창 내부에 맞췄다. 유리와 작은 번짐은 금속에 아주 약한 따뜻한 반사를 보이게 할 수 있다.

## 미리보기

- `Title_OFF_ON_Comparison.png`: 원본꺼짐/분리레이어켜짐 비교.
- `Title_Lanterns_ON_FullResolution.png`: 원본2752×1536 위에 레이어를 합성한 검토용 정지 이미지.
- `Title_Lanterns_OFF_Reference.png`: 원본의 검토용 복사본.
- `Hanging_Glass_Alignment_2x.png`, `Barrel_Glass_Alignment_2x.png`: 랜턴 위치 확대 비교.
- `Hanging_Flame_6f_4x.gif`, `Barrel_Flame_6f_4x.gif`: 불꽃 단독6프레임.
- `Title_Lanterns_Lit_5p2s_Preview.gif`:1376×768 검토용,5.2초,10fps.
- `Hanging_LightAndFlame_Closeup.gif`, `Barrel_LightAndFlame_Closeup.gif`: 각 랜턴의 불꽃·주변조명 확대검토,2.9초,10fps.

**배경과 합쳐진 파일은 Previews 안의 검토용으로만 제공**한다. 게임에는 Flames/Glow/Glass/SurfaceLighting의 투명파일과 원래배경을 별도로 사용한다. GIF는 독립적인0.84/0.96/2.6/2.9초 주기를 짧게 보여주는 검토영상이다. GIF 파일의 반복길이가 모든 주기의 공배수는 아니므로 끝→시작의 밝기 초기화는 실제 Unity의 연속 주기와 동일하지 않다. GIF 팔레트256색 특성으로 배경 색이 미세하게 단순화된다. 게임용PNG에는 그 GIF 변환을 적용하지 않았다.

## 실제 검증과 미검증

- 원본의 실제2752×1536/RGBA 규격과 작업 전후SHA256 동일성을 확인했다.
- 게임용25개 PNG를 다시 열어 크기·RGBA·알파를 검사했다. 모든불꽃은 선명한0/255 알파이며 시트당6개가 서로 다른 프레임이다.
- 두 랜턴 모두6프레임의 불투명 불꽃 픽셀 전체가 측정한 원본 유리창 폴리곤 안에 들어가는지 검사했다. 셀 가장자리 잘림도 없다.
- 조명은 독립된 반투명파일이다. 표면 레이어 외부 배경은 완전 투명이고, 원본 랜턴 본체를 복제하지 않는다. 생성된 조명 배경으로 원본을 교체하지 않았다.
- 전체OFF/ON과 두랜턴의 확대합성을 정적으로 검토했다. 기둥 랜턴 주변의 사각형 마스크 자국을 보정했고, 원본 암벽/목재/통/바닥 형태를 유지했다.
- GIF5개의 저장 프레임 수·표시 시간·디코딩 픽셀을 검사했다. **실시간으로 움직이는 GIF를 직접 관찰한 자연스러움 검증은 미실시**다.
- 실제Unity Import/Play, 타이틀UI 배치, 최종밝기·블렌드·화면비·텍스트시인성은 미검증이며 후속Codex가 확인한다.

정확한파일목록·배치·시험시간·알파범위·검증기록은 `Specs/Placement_and_Validation.json`에 있다. 이번에는Unity코드·씬·프리팹·원본을 수정하거나Git커밋/푸시를 하지 않았다.
'''
(O/'README_KO.md').write_text(readme,encoding='utf-8')
prompts='''# Generation prompts — built-in image_gen

## Flame strip, transparent
Production game art sprite sheet: SIX tiny candle-like oil lantern flames in one horizontal row, six equally spaced identical-size square cells, genuine transparent RGBA background. Only flame shapes, NO lantern, NO wick object, NO text, NO grid, NO smoke, NO sparks, NO surrounding glow. Each compact upright teardrop flame has same base alignment and same stable small size, six gently changing tips leaning left, center, right, center for a seamless slow burning loop. Always lit; never fades away. Dark burnt orange silhouette, muted amber-orange body, warm gold inner flame, tiny cream-yellow core at base; no white explosion. Sharp deliberate clustered pixel art, 4-5 flat color bands, native visual design ~12 pixels wide and24 high, upscaled clean blocks. No antialiasing, no blurry edges. All six flames entirely inside their equally sized cells, base centered at same vertical position. Minimal restrained real oil lamp fire for dark mining title background.

## Halo, transparent
Standalone reusable game lighting overlay PNG, square canvas, genuine transparent RGBA background. A single very restrained warm amber light halo centered precisely in canvas, brightest at tiny center, spreads to transparent outer edges, no lamp, no flame, no object, no scene, no labels. Soft atmospheric opacity falloff but expressed in subtle stepped pixel-art bands and clustered low-opacity pixels to match a dark painterly pixel-art mine background. Muted warm orange gold, not saturated neon, no lens flare, no rays, no rings, no sharp circular outline. Smooth overall illumination impression while preserving pixel blocks. Outer15percent completely transparent, center moderate semi-transparent alpha rather than opaque yellow blob. Intended as normal alpha-blended subtle lantern light, not a magical aura.

## Lighting-reference edit only
Input reference: original background crop(1728,512)-(2752,1536),1024 square. This edited RGB guide is NOT a game asset or replacement background.
LIGHTING EDIT ONLY of this exact1024x1024 mine title background crop. Preserve every object's shape, every timber/rock/barrel/rail and all exact positions, camera, texture, dark overall atmosphere. The hanging lantern center is at (620,288), and the much smaller barrel lantern center is (750,554). Gently light these two existing lanterns warm muted amber. Add small golden illumination through their existing glass, a restrained soft warm bounce on the right timber post, right rock facets, barrel top and adjacent floor. Hanging lantern slightly stronger than small barrel one. Do not add/change/remove any objects or fire outside glass, don't move lanterns, do not paint rays or sparks, don't make the tunnel bright. Keep left tunnel and distant rock dark. This is a lighting reference edit for extracting a separate diffuse material illumination overlay. Pixel-painterly texture and edges must remain exactly aligned, same square crop, no text.

## Export
Nearest sampling and finite palette for flame frames. One generated flame strip supplies two glass-fitted sizes. Halo opacity reduced for normal alpha blend. Only coarse positive-light guidance extracted from the lighting reference, applied to original surface masks as warm RGB/alpha. No generated scene RGB is used in final previews. Original source geometry remains the base.
'''
(O/'Sources/GenerationPrompts.md').write_text(prompts,encoding='utf-8')
shutil.copyfile(sources['flames'],O/'Sources/Flames_Generated_Source.png');shutil.copyfile(sources['glow'],O/'Sources/Halo_Generated_Source.png')
shutil.copyfile(Path(__file__),O/'Sources/build_title_lantern_v1.py')
zpath=O.parent/'TitleLantern_v1.zip'
with zipfile.ZipFile(zpath,'w',zipfile.ZIP_DEFLATED) as z:
 for p in sorted(O.rglob('*')):
  if p.is_file():z.write(p,Path(O.name)/p.relative_to(O))
with zipfile.ZipFile(zpath) as z:assert z.testzip() is None
print(json.dumps({'ZIP':str(zpath),'bytes':zpath.stat().st_size,'window_checks':window_checks},ensure_ascii=False))
