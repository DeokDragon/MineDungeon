from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,GifImagePlugin
import numpy as np,math,json,hashlib,shutil,zipfile
W=Path('C:/Users/User/.codex/visualizations/2026/10/22/01a16344-f87e-7b93-9d18-cb1d88939ccf');R=Path('D:/Git/MineDungeon2');O=R/'Art Resource/TitleMinerSlime_v1';OUT=O
for sub in ['Sprites','Frames','Layers','Previews','Specs','Sources']:(O/sub).mkdir(parents=True,exist_ok=True)
root=R/'Assets/Art/Player/MinerSlime';idlepath=root/'Player_MinerSlime_Idle_4f_64x64_v1.png';toolpath=root/'Pickaxe_Basic_v1.png';socketpath=root/'Player_MinerSlime_JellySocket_8x8_v1.png'
bgpath=Path('C:/Users/User/Downloads/TitleScene_background.png');litpath=R/'Art Resource/TitleLantern_v1/Previews/Title_Lanterns_ON_FullResolution.png'
protected={}
for p in list(root.glob('*'))+list((R/'Art Resource/TitleLantern_v1').rglob('*'))+[bgpath,R/'Docs/Systems/PlayerSystem.md',R/'Docs/Systems/ArtDirection.md']:
 if p.is_file():protected[str(p)]=hashlib.sha256(p.read_bytes()).hexdigest()
game=Image.open(idlepath).convert('RGBA').crop((0,0,64,64));weapon=Image.open(toolpath).convert('RGBA');socket=Image.open(socketpath).convert('RGBA')
gen=Path('C:/Users/User/.codex/generated_images/01a16344-f87e-7b93-9d18-cb1d88939ccf/exec-750b3b81-8639-4ab3-a714-80117e7b6441.png')
im=Image.open(gen).convert('RGBA');a=np.array(im);a[:,:,3]=np.where(a[:,:,3]>=160,255,0);a[a[:,:,3]==0,:3]=0;im=Image.fromarray(a).crop(Image.fromarray(a).getbbox());wh=(round(im.width/im.height*56),56)
rear=Image.new('RGBA',(64,64));rear.paste(im.resize(wh,Image.Resampling.NEAREST),((64-wh[0])//2,4))
a=np.array(rear);Y,X=np.indices((64,64));opaque=a[:,:,3]>0;body=opaque&(Y>=np.rint(29-.10*X));hat=opaque&~body
g=np.array(game);gy,gx=np.indices((64,64));gp=g[:,:,3]>0
# Use existing gameplay body/helmet palettes separately to avoid golden jelly highlights.
for mask,gmask,count in [(body,gp&(gy>=25),32),(hat,gp&(gy<25),32)]:
 colors=g[gmask,:3];pal=Image.fromarray(colors.reshape(1,len(colors),3)).quantize(colors=count,method=Image.Quantize.MEDIANCUT)
 q=rear.convert('RGB').quantize(palette=pal,dither=Image.Dither.NONE).convert('RGB');a[mask,:3]=np.array(q)[mask]
a[a[:,:,3]==0,:3]=0;rear=Image.fromarray(a)
manifest=[]
def save(im,path,role):
 im.save(O/path);q=Image.open(O/path);assert q.size==im.size and q.mode=='RGBA';ar=np.array(q);manifest.append(dict(file=path,size=list(im.size),alpha_values=np.unique(ar[:,:,3]).tolist(),role=role));return im
save(rear,'Sources/TitleMinerSlime_RearBody_Working_64x64.png','derived editable title body, not gameplay replacement')
def bodypose(upper,wide,hatdy):
 ar=np.zeros_like(a);dest={}
 for y,x in zip(*np.where(body)):
  top=29-.1*x;t=float(np.clip((y-top)/(59-top),0,1));tx=int(x+round(wide*((x-32)/28)*math.sin(math.pi*t)));ty=int(y+round(upper*(1-t)));ar[ty,tx]=a[y,x];dest[(x,y)]=(tx,ty)
 p=Image.fromarray(ar);d=ImageDraw.Draw(p)
 for (x,y),(tx,ty) in dest.items():
  for neighbor in [(x+1,y),(x,y+1)]:
   if neighbor in dest:
    nx,ny=dest[neighbor]
    if max(abs(nx-tx),abs(ny-ty))>1:d.line([(tx,ty),(nx,ny)],fill=tuple(a[y,x]),width=1)
 ar=np.array(p);hy,hx=np.where(hat);ar[hy+hatdy,hx]=a[hat]
 # Fill only the original contiguous neck, preserving the helmet-body attachment.
 for x in range(64):
  oldhat=np.where(hat[:,x])[0];oldbody=np.where(body[:,x])[0]
  if len(oldhat) and len(oldbody) and oldbody.min()-oldhat.max()<=2:
   hbot=int(oldhat.max()+hatdy);by=[ty for (sx,sy),(tx,ty) in dest.items() if sx==x and sy==oldbody.min()]
   if by and 0<by[0]-hbot<=3:ar[hbot+1:by[0],x]=a[oldbody.min(),x]
 # Rigid contact rows: same alpha and RGB on the original curved sole.
 sole=opaque&(Y>=58);ar[sole]=a[sole];ar[ar[:,:,3]==0,:3]=0
 return Image.fromarray(ar)
def placed_weapon(pivot,angle):
 th=math.radians(-angle);c,s=math.cos(th),math.sin(th);M=np.array([[c,-s],[s,c]]);iv=M.T;p=np.array([14,24]);q=np.array(pivot);offset=p-iv@q
 out=weapon.transform((128,128),Image.Transform.AFFINE,(iv[0,0],iv[0,1],offset[0],iv[1,0],iv[1,1],offset[1]),Image.Resampling.NEAREST)
 sy,sx=np.where(np.array(weapon)[:,:,3]>0);dest=M@(np.stack([sx,sy])-p[:,None])+q[:,None]
 assert dest.min()>1 and dest.max()<127,(dest.min(),dest.max())
 return out
plans=[(0,0,0,55,0),(1,1,1,54,1),(2,1,1,53,1),(-1,0,0,54,0),(0,0,-1,56,-1),(0,0,0,56,0)]
names=['基本静止','小さく圧縮','下部に張り','復元開始','帽子と道具の遅れ','安定']
frames=[];bodyframes=[];toolframes=[];records=[]
for i,(upper,wide,hatdy,angle,tool_y) in enumerate(plans):
 b=bodypose(upper,wide,hatdy);bb=np.array(b);row=44+round(upper*.4);edge=int(np.where(bb[row,:,3]>0)[0].max());grip=(28+edge+1,32+row+tool_y)
 bcanvas=Image.new('RGBA',(128,128));bcanvas.alpha_composite(b,(28,32));tools=Image.new('RGBA',(128,128));tools.alpha_composite(socket,(grip[0]-2,grip[1]-4));tools.alpha_composite(placed_weapon(grip,angle));f=bcanvas.copy();f.alpha_composite(tools)
 frames.append(f);bodyframes.append(bcanvas);toolframes.append(tools);save(f,f'Frames/TitleMinerSlime_Idle_F{i+1:02}.png','title-only combined frame')
 rec=dict(frame=i+1,body_upper_dy=upper,body_local_width=wide,helmet_dy=hatdy,weapon_angle_screen_ccw=angle,weapon_grip_pixels=list(grip),bbox=list(f.getbbox()))
 records.append(rec)
save(frames[0],'Sprites/TitleMinerSlime_Base_128x128_v1.png','title base pose, identical Idle F1')
def sheet(fs):
 im=Image.new('RGBA',(128*len(fs),128))
 for i,f in enumerate(fs):im.paste(f,(128*i,0))
 return im
save(sheet(frames),'Sprites/TitleMinerSlime_Idle_6f_128x128_v1.png','main title Idle,6x128x128')
save(sheet(bodyframes),'Layers/TitleMinerSlime_BodyOnly_Idle_6f_128x128_v1.png','optional body layer; do not stack with combined main sheet')
save(sheet(toolframes),'Layers/TitleMinerSlime_ToolAndSocket_Idle_6f_128x128_v1.png','optional exact-source tool/socket layer; do not stack with main sheet')
shadow=Image.new('RGBA',(64,16));d=ImageDraw.Draw(shadow);d.ellipse((3,4,60,12),fill=(8,16,17,20));d.ellipse((9,6,54,11),fill=(8,16,17,34));save(shadow,'Layers/TitleMinerSlime_ContactShadow_64x16_v1.png','optional separate contact shadow, preview only visual support')
times=[240]*6
code=(W/'build_p1_attacks.py').read_text(encoding='utf-8');exec(code[code.index('def background(f):'):code.index('results={};gifchecks=[]')],globals())
gifs=[gif(frames,times,O/'Previews/TitleMinerSlime_Idle_Native128.gif'),gif(frames,times,O/'Previews/TitleMinerSlime_Idle_4x.gif',4)]
floorY=91;sourcebase=np.array(bodyframes[0]);contact=sourcebase[floorY-1:floorY+1,:,:].copy()
for f in bodyframes:
 assert np.array_equal(np.array(f)[floorY-1:floorY+1],contact)
for f in frames:
 ar=np.array(f);assert set(np.unique(ar[:,:,3]))=={0,255};assert min(f.getbbox()[:2])>0 and max(f.getbbox()[2:])<128
assert len({hashlib.sha256(f.tobytes()).hexdigest() for f in frames})==6
attachment_checks=[]
for b,t in zip(bodyframes,toolframes):
 overlap=np.sum((np.array(b)[:,:,3]>0)&(np.array(t)[:,:,3]>0));assert overlap>0
 attachment_checks.append(int(overlap))
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in protected.items())

lit=Image.open(litpath).convert('RGBA');assert lit.size==(2752,1536)
candidates=[dict(name='A',anchor=[2180,1310],scale=3.0,recommended=True),dict(name='B',anchor=[2240,1210],scale=2.75,recommended=False),dict(name='C',anchor=[2070,1395],scale=3.5,recommended=False)]
footpivot=(60,92)
def composite(f,c):
 out=lit.copy();k=c['scale'];anchor=c['anchor'];s=shadow.resize((round(64*k),round(16*k)),Image.Resampling.NEAREST);out.alpha_composite(s,(round(anchor[0]-32*k),round(anchor[1]-5*k)))
 sprite=f.resize((round(128*k),round(128*k)),Image.Resampling.NEAREST);left=round(anchor[0]-footpivot[0]*k);top=round(anchor[1]-footpivot[1]*k);out.alpha_composite(sprite,(left,top))
 # Left half reserved for menu/logo in this art-layout test; actual UI not inspected.
 assert np.array(sprite)[:,:,3].any();assert left+round(f.getbbox()[0]*k)>1376
 return out
fontpath='C:/Windows/Fonts/malgun.ttf'
def font(sz):return ImageFont.truetype(fontpath,sz)
overview=Image.new('RGB',(2064,430),(18,24,27));d=ImageDraw.Draw(overview)
for i,c in enumerate(candidates):
 image=composite(frames[0],c);image.save(O/'Previews'/f'Title_Placement_{c["name"]}_2752x1536.png');small=image.convert('RGB').resize((688,384),Image.Resampling.NEAREST);overview.paste(small,(i*688,42));d.text((i*688+10,6),f'{c["name"]}: anchor{tuple(c["anchor"])} x{c["scale"]}'+(' / 추천' if c['recommended'] else ''),font=font(19),fill=(229,222,199))
overview.save(O/'Previews/Three_Placements_Comparison.png')
recommend=candidates[0];recommended=[composite(f,recommend).resize((1376,768),Image.Resampling.NEAREST) for f in frames];gifs.append(gif(recommended,times,O/'Previews/Title_Placement_A_Idle_1p44s.gif'))
# Native design comparison: existing gameplay body and new title pose at equal pixel scale.
compare=Image.new('RGBA',(768,440),(25,31,33,255));d=ImageDraw.Draw(compare)
front=Image.new('RGBA',(128,128));front.alpha_composite(game,(28,32));front.alpha_composite(socket,(82,71));front.alpha_composite(placed_weapon((85,73),55))
for i,(label,f) in enumerate([('기존 게임플레이 / 정면',front),('타이틀 / 후측면 준비 자세',frames[0])]):
 compare.alpha_composite(f.resize((384,384),Image.Resampling.NEAREST),(i*384,38));d.text((i*384+12,6),label,font=font(18),fill=(224,219,195))
compare.save(O/'Previews/Gameplay_Title_Design_Comparison_3x.png')
contactsheet=Image.new('RGBA',(768,156),(25,31,33,255));d=ImageDraw.Draw(contactsheet)
for i,f in enumerate(frames):contactsheet.alpha_composite(f,(128*i,28));d.text((128*i+6,5),f'F{i+1}: {times[i]}ms',font=font(13),fill=(222,215,190))
contactsheet.save(O/'Previews/Idle_AllFrames_Native.png');contactsheet.resize((3072,624),Image.Resampling.NEAREST).save(O/'Previews/Idle_AllFrames_4x.png')
spec={'source_gameplay_idle':str(idlepath),'source_pickaxe':str(toolpath),'source_socket':str(socketpath),'generated_rear_reference':str(gen),'main_cell':[128,128],'main_sheet':[768,128],'frames':6,'durations_ms':times,'cycle_ms':1440,'body_source_logical_size':[64,64],'source_character_height_trial':56,'body_placement_in_cell':[28,32],'foot_pivot_pixels_top_left':list(footpivot),'foot_pivot_unity':[60/128,1-92/128],'PPU_trial':64,'source_native_pixel_scale':1,'contact_rows_fixed_in_main_cell':[90,91],'records':records,'placement_candidates':candidates,'background_size':[2752,1536],'background_reference':str(litpath),'coordinate_system':'background PNG top-left x right y down; anchors are character ground pivot; UI local=(x-1376,768-y) before common background scale','checks':{'RGBA_dimensions_alpha_readback':True,'six_frames_unique':True,'same_sole_rows_exact':True,'socket_tool_body_overlap_pixels':attachment_checks,'native_original_pickaxe_and_socket_pixels_reused_nearest_rotation_only':True,'source_files_hash_unchanged':True,'static_visual_review':'native/enlarged6-frame contact sheet, gameplay-vs-title comparison and3scene placements inspected','actual_realtime_GIF_observation':False,'Unity_Play':'not performed / out of scope','actual_title_logo_menu_layout':'not inspected; left half reserved in preview test'},'assets':manifest,'GIF':gifs,'protected_sha256':protected}
(O/'Specs/Placement_and_Validation.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'output':str(O),'poses':records,'candidates':candidates,'native_rear_size':wh},ensure_ascii=False))

readme='''# 타이틀 광부 슬라임 아트 v1

## 외형 기준과 제작 방식

현재 Unity에 적용된 `Assets/Art/Player/MinerSlime/Player_MinerSlime_Idle_4f_64x64_v1.png` 첫 프레임, Move, `Pickaxe_Basic_v1.png` 및8×8 젤리소켓을 확인했다. 과거 Art Resource의 플레이어 제작폴더는 현재 경로에서 확인되지 않아 실제 적용된Assets 원본을 사용했다. 기존 정면Idle은 입구를 바라보는 구도와 맞지 않으므로 그대로 확대해 재사용하지 않았다.

타이틀 몸체는 내장 image_gen으로 **오른쪽을 향한3/4 후측면**을 제작한 뒤, 기존64px 플레이어의 논리적 픽셀크기에 맞춰64×64 작업본으로 정리했다. 동일 후측면 작업본을 부위별 픽셀 변형해6프레임을 만들었으며 프레임마다 캐릭터를 다시 생성하지 않았다. 헬멧과몸체는 게임 원본에서 얻은 각각의 제한 팔레트로 색을 맞췄다.

둥근 청회색/청록 몸체, 황동색 헬멧, 어두운 밴드, 램프1개를 유지한다. 고개를 돌려 얼굴 일부만 보이는 자세이며 완전히 등을 보이는 정후면은 아니다. 기존에 없던 뒤쪽 면과 투영은 타이틀용으로 해석한 부분이므로 **원본과픽셀단위로동일한외형이라는뜻은아니다. 최종 동일캐릭터 인상은 사용자 미술검토 대상**이다.

곡괭이는 실제 게임의56×48 독립PNG를 최근접 회전/위치조정해 그대로 재사용했다. 금속머리·나무손잡이를 새로 디자인하지 않았다. 기존8×8 젤리소켓을 몸체 오른쪽의 짧은 부착부로 사용했으며 팔·손가락·다리·긴촉수·배낭·장식은 추가하지 않았다. 공격준비나 휘두르는 자세가 아닌 탐험 대기 자세다.

## 게임용 파일

|파일|실제 규격|용도|
|---|---|---|
|Sprites/TitleMinerSlime_Base_128x128_v1.png|128×128 RGBA|기본포즈, Idle F1과정확히동일|
|Sprites/TitleMinerSlime_Idle_6f_128x128_v1.png|768×128 RGBA|가로6셀,각128×128|
|Frames/TitleMinerSlime_Idle_F01~06.png|각128×128 RGBA|개별6프레임|
|Layers/TitleMinerSlime_BodyOnly_Idle_6f_128x128_v1.png|768×128 RGBA|선택형 몸체/헬멧 레이어|
|Layers/TitleMinerSlime_ToolAndSocket_Idle_6f_128x128_v1.png|768×128 RGBA|선택형 곡괭이/소켓 레이어|
|Layers/TitleMinerSlime_ContactShadow_64x16_v1.png|64×16 RGBA|선택형 별도 접지그림자|

기본 PNG/메인시트에는 몸체·헬멧·소켓·곡괭이만 있으며 배경이나 바닥그림자는 포함하지 않았다. 메인시트 하나를 쓰거나 BodyOnly+ToolAndSocket2개를 동기화해 쓰는 방식 중 하나를 선택한다. 둘을 중복해서 겹치지 않는다. 선택형 그림자는 합성미리보기의 접지보조이며 캐릭터와 분리되어 있다.

본체/곡괭이의 원본 픽셀배율은1이다. 타이틀 몸체 작업본은64×64, 가시높이56px 시험규격으로 구성했고128×128은 곡괭이와 움직임을 위한 투명패딩이다.64×64 전체셀에 도구까지 억지로 넣거나 잘라내지 않았다. `Sources/TitleMinerSlime_RearBody_Working_64x64.png`는 편집원본으로, 게임플레이 캐릭터 교체파일이 아니다.

## Pivot·접지·Import

- 셀128×128, 가로6개. 기본 스프라이트와 각프레임의 기준은 동일하다.
- 권장 접지Pivot: PNG좌상단 기준 **(60,92)** → Unity 정규화 **(0.46875,0.28125)**. 발밑 가장아래 픽셀은Y91, 아래쪽경계Y92다.
- 중앙Pivot(0.5,0.5)을 쓸 경우 같은 바닥좌표에 맞추려면 스프라이트 중심을 접지점보다 화면상(4,-28)px 위치에 놓는다. 접지Pivot 사용이 배치하기 쉽다.
- PPU64를 원본 플레이어와 같은 초기시험값으로 제안한다. 실제 타이틀을UI Image로 표시한다면128×128 Rect의 픽셀크기와Canvas.referencePixelsPerUnit/SetNativeSize 규칙을 후속Codex가 확인한다. PPU만으로 타이틀 최종크기를 확정하지 않는다.
- Point / Compression None / Mipmaps Off / sRGB On / FullRect 권장. 본체알파는0·255로 선명하며, 그림자만의도적인반투명이다.

모든 프레임에서 하단접지2개행(Y90~91)의 RGBA가 정확히 같다. 원본전체를상하이동시키지 않고 상부몸체만누르거나복원했다. 미리보기그림자는 캐릭터접지앵커에서원본픽셀기준(0,+3)px를중심으로64×16을놓는다. 같은표시배율로함께조정한다.

## Idle —6프레임·1.44초 시험

프레임당240ms,6개,총1440ms. 권장검토범위는1.2~1.6초이며 최종수치가아니다.

|F|시작|몸체|헬멧·곡괭이 반응|
|---|---:|---|---|
|1|0.00초|기본준비형태|정지기준,도구55도|
|2|0.24초|상부1px압축,중간폭최대1px확장|헬멧1px하강,도구54도|
|3|0.48초|상부2px압축,바닥고정|헬멧은1px까지만따라오고도구53도|
|4|0.72초|상부1px복원신장|헬멧은기본위치,도구54도|
|5|0.96초|몸체기본위치|헬멧이1px늦게올라오고도구56도|
|6|1.20초|몸체안정화|헬멧기본,도구56도에서다음F1의55도로연결|

몸체는부위별로변형하고, 헬멧은단단한레이어로, 도구는원본회전으로반응시켰다. 얼굴을별도의큰표정으로바꾸지않는다. 곡괭이각도는미리보기이미지의위쪽으로기울어진자세를표시한값이며 실제공격방향/판정과무관하다. 소켓/그립의정확한프레임별좌표는JSON에있다.

## 배경 배치3안

배경기준은 `TitleLantern_v1/Previews/Title_Lanterns_ON_FullResolution.png`의2752×1536이다. 기존원본/랜턴파일을바꾸지않고그이미지위에합성했다. 미리보기에서는랜턴이정적으로켜져있으며 이전랜턴의동적2.6/2.9초루프까지같이재생한것은아니다.

좌표는배경좌상단(0,0),오른쪽+X,아래쪽+Y이며 캐릭터의**접지Pivot** 위치다.

|안|접지좌표|원본픽셀대비표시배율|캐릭터셀표시크기|판단|
|---|---|---:|---|---|
|A·추천|(2180,1310)|3.0|384×384|레일오른쪽바닥,광차/통과거리를두고입구를바라봄|
|B|(2240,1210)|2.75|352×352|입구에더가깝고조금작음,통/지지대와의간격을후속확인|
|C|(2070,1395)|3.5|448×448|하단전경에서더크게보임,타이틀하단UI가있으면여유확인|

표시셀은투명여백을포함한다. A의실제가시실루엣은약249×168px이며셀전체가384px크기의슬라임이라는뜻은아니다. A는지나치게캐릭터를크게만들지않으면서기존광차와레일을덜가리고,입구/랜턴에시선이이어져우선추천한다.

배경중앙을기준으로한UI좌표는(x-1376,768-y)다. 따라서A접지점은(804,-542),B는(864,-442),C는(694,-627)다. 이값은원본배경을2752×1536로보는단계의좌표이며최종Canvas월드단위가아니다. 배경과캐릭터의공통부모에동일한Fit/Cover배율및크롭오프셋을적용한다. 배경만가로세로비율을다르게늘이지않는다.

세후보모두캐릭터의불투명영역이배경왼쪽절반X0~1376에들어가지않는지검사했다. 이는비어있는메뉴/로고예상영역을보호하는레이아웃시험이다. 실제타이틀로고·메뉴Rect는이번에확인하지않아최종UI간섭까지검증됐다는뜻은아니다. UI연결전에실제로고/버튼과함께확인한다.

## 미리보기

- `TitleMinerSlime_Idle_Native128.gif`:128×128 원배율 캐릭터.
- `TitleMinerSlime_Idle_4x.gif`:4배 최근접 확대.
- `Title_Placement_A_Idle_1p44s.gif`:추천A와켜진타이틀배경 합성,1376×768 검토용.
- `Gameplay_Title_Design_Comparison_3x.png`:같은논리적픽셀배율의게임플레이정면과새타이틀후측면비교.
- `Title_Placement_A/B/C_2752x1536.png`:각배치의원본해상도합성.
- `Three_Placements_Comparison.png`:배치3안나란히비교.
- `Idle_AllFrames_Native.png`, `Idle_AllFrames_4x.png`:6프레임포즈검토.

배경과캐릭터가합쳐진이미지는**Previews의검토용**이다. 게임에는Sprites/Frames 또는선택형Layers를별도로사용한다. 현재타이틀배경이나게임내플레이어를교체한것은아니다.

## 검증 결과와 미검증

- 실제PNG를다시읽어RGBA·128셀·768×128시트·6프레임·기본포즈/F1일치를확인했다. 본체의투명알파는0·255다.
- 모든셀에불투명외곽이들어가고6개프레임이서로다르다. 곡괭이변환의계산상외곽도셀안에있다.
- 접지2개행을6프레임에서픽셀단위로대조했다. 몸체와도구/소켓레이어가모든프레임에서겹쳐있어정적연결이유지됨을검사했다.
- 게임원본의색상팔레트를참고했고기존곡괭이와소켓픽셀을그대로재사용했다. 새후측면의캐릭터인상은원본비교시트로사용자검토가필요하다.
- 원배율/확대프레임과게임캐릭터비교,배치3안을정적으로검토했다. 팔·다리/추가장식없음,얼굴/헬멧을도구가과하게가리지않는것을확인했다.
- GIF3개의실제프레임수·240ms표시시간·디코딩픽셀을검사했다. **실시간GIF를직접관찰한자연스러움검증은미실시**다. F6→F1의동적리듬과큰화면의위화감은후속검토가필요하다.
- UnityImport/Play·최종PPU/Canvas표시크기·실제로고/메뉴가림은미검증이다.
- 기존플레이어/곡괭이원본·관련meta·랜턴리소스·배경·참조공식문서의전후SHA256을비교해동일함을확인했다. 원본변경·Unity코드/씬/프리팹수정·Git커밋/푸시는하지않았다.

정확한프레임좌표·파일규격·배치값·보존해시는 `Specs/Placement_and_Validation.json`에있다.6프레임/1.44초/PPU64/A배치는초기시험제안이며최종확정은사용자아트검토와후속Unity확인후진행한다.
'''
(O/'README_KO.md').write_text(readme,encoding='utf-8')
prompt='''# Title miner slime — built-in image_gen

Reference roles: existing64x64 gameplay Idle F1 (identity, body/helmet palette and proportions); existing56x48 pickaxe (not redrawn, composited from original); lit title background (lighting/composition reference only). One rear-quarter body source is used for all6frames; no per-frame generation.

## Final proportion correction prompt
Final precise proportion correction of target image1. Identity reference image2. The slime BODY in image1 is too flattened and the HELMET still occupies too much of the total silhouette. Keep the helmet's current width but make its dome a LOW PROFILE HARDHAT, about18 logical pixels total including band/brim. Make the rounded slime BODY about50 logical pixels wide by40 logical pixels high, like gameplay reference2. Overall sprite should be approximately54 logical pixels wide by56 logical pixels tall (helmet overlaps top of jelly). This is the SAME player character, not a fat new character. Quiet blue-grey teal jelly, small original brass/ochre miner helmet with dark band and single front lamp at far upper-right, tiny face mostly obscured by direction. Keep rear-three-quarter facing upper-right, no human limbs, no arms, no legs, no hand, no tool, no backpack, no additional gear. Narrow dark pixel outline and matte limited clustered pixel shading. Reduce pale highlight stripe to the source body's muted teal highlights; only a few warm rim pixels. ONE isolated body sprite on genuinely transparent background, no floor, no cast shadow, no lettering. Essential: body round/tall enough, helmet low enough, match reference2 proportions.

## Native export
Binary alpha threshold160; crop sprite extent; nearest export56px high inside64x64 body working canvas. Palette matching uses gameplay body and helmet palettes independently.128x128 final cell is transparent padding for existing tool. Same base source is articulated locally across6frames; rigid helmet and exact-source nearest-rotated pickaxe/socket. No gameplay files edited. See included editable art source for exact transforms.
'''
(O/'Sources/GenerationPrompts.md').write_text(prompt,encoding='utf-8')
shutil.copyfile(gen,O/'Sources/TitleMinerSlime_GeneratedBody_Source.png')
shutil.copyfile(toolpath,O/'Sources/Reference_Pickaxe_Basic_v1.png');shutil.copyfile(socketpath,O/'Sources/Reference_JellySocket_8x8.png');game.save(O/'Sources/Reference_Gameplay_Idle_F1_64x64.png')
shutil.copyfile(Path(__file__),O/'Sources/build_title_miner_v1.py')
zpath=O.parent/'TitleMinerSlime_v1.zip'
with zipfile.ZipFile(zpath,'w',zipfile.ZIP_DEFLATED) as z:
 for p in sorted(O.rglob('*')):
  if p.is_file():z.write(p,Path(O.name)/p.relative_to(O))
with zipfile.ZipFile(zpath) as z:assert z.testzip() is None
print(json.dumps({'ZIP':str(zpath),'bytes':zpath.stat().st_size,'attachments_overlap_px':attachment_checks,'protected_files':len(protected)},ensure_ascii=False))
