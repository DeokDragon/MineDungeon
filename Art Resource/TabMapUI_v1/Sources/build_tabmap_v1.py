from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import numpy as np, re,json,hashlib,zipfile,shutil
W=Path('C:/Users/User/.codex/visualizations/2026/10/22/01a16344-f87e-7b93-9d18-cb1d88939ccf')
R=Path('D:/Git/MineDungeon2');O=R/'Art Resource/TabMapUI_v1'
for sub in ['Frame','Background','Icons','Terrain','Previews','Specs','Sources']:(O/sub).mkdir(parents=True,exist_ok=True)
ref=Path('C:/Users/User/AppData/Local/Temp/codex-clipboard-7fc1b9d5-b36f-4e3e-99f2-99f1e343712b.png')
gen=Path('C:/Users/User/.codex/generated_images/01a16344-f87e-7b93-9d18-cb1d88939ccf/exec-9ed84a4c-3241-40cc-9329-02aaafbe9b64.png')
files=['Assets/Scripts/CurrentRoomMapUI.cs','Assets/Scripts/CurrentRoomMapGraphic.cs','Assets/Scripts/PlayerHUD.cs','Assets/Scripts/Player/PlayerSkillHUD.cs','Assets/Prefabs/Rooms/RegularCaves/Map_01.prefab','Assets/Prefabs/Rooms/BossRoom24x22.prefab','Docs/Systems/UISystem.md','Docs/Systems/MapSystem.md','Docs/Systems/ArtDirection.md']
protected={str(R/p):hashlib.sha256((R/p).read_bytes()).hexdigest() for p in files}
assets=[]
def save(im,name,role,**extra):
 p=O/name;im.save(p);im2=Image.open(p);assert im2.mode=='RGBA' and im2.size==im.size
 a=np.array(im2);assets.append(dict(file=name,size=list(im.size),mode=im2.mode,alpha_values=np.unique(a[:,:,3]).tolist(),role=role,**extra));return im
shutil.copyfile(ref,O/'Sources/Reference_TabMap_Screenshot.png')
shutil.copyfile(gen,O/'Sources/Frame_Generated_Source.png')
# Final-size export only: nearest sampling, finite palette, binary edge alpha.
source=Image.open(gen).convert('RGBA');am=np.array(source)[:,:,3]>=160;yy,xx=np.where(am)
cropped=source.crop((int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)))
im=Image.new('RGBA',(560,400));im.paste(cropped.resize((554,394),Image.Resampling.NEAREST),(3,3))
a=np.array(im);a[:,:,3]=np.where(a[:,:,3]>=160,255,0);a[a[:,:,3]==0,:3]=0
rgbim=Image.fromarray(a[:,:,:3]).quantize(colors=48,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB');a[:,:,:3]=np.array(rgbim);a[a[:,:,3]==0,:3]=0
frame=save(Image.fromarray(a),'Frame/TabMap_Frame_560x400_v1.png','9-slice outer frame',border_LBRT=[48,48,48,48],pivot=[.5,.5])
assert np.array(frame)[45:355,24:536,3].max()==0

# Low contrast repeatable code-native map surfaces. Tile borders match exactly.
def texture(size,base,delta,seed,alpha=255):
 rng=np.random.default_rng(seed);q=np.zeros((size,size,4),np.uint8);q[:,:,:3]=base;q[:,:,3]=alpha
 for i in range(size*size//32):
  x=int(rng.integers(2,size-3));y=int(rng.integers(2,size-3));d=int(rng.choice([-delta,delta]));q[y,x:x+int(rng.integers(1,4)),:3]=np.clip(np.array(base)+d,0,255)
 # Identical opposing borders guarantee seam continuity under nearest tile repeat.
 q[-1]=q[0];q[:,-1]=q[:,0]
 return Image.fromarray(q)
bg_tile=save(texture(64,[19,25,27],2,13,204),'Background/TabMap_BackgroundTile_64x64_A80.png','optional repeating background, baked 80% alpha')
def tile(im,size):
 out=Image.new('RGBA',size)
 for y in range(0,size[1],im.height):
  for x in range(0,size[0],im.width):out.paste(im,(x,y))
 return out
background=save(tile(bg_tile,(560,400)),'Background/TabMap_Background_560x400_A80.png','full panel background, baked 80% alpha')
floor=save(texture(32,[104,99,83],3,4),'Terrain/TabMap_Floor_32x32.png','walkable floor, repeat')
wall=texture(32,[43,49,51],2,5);wd=ImageDraw.Draw(wall)
for pts,col in [([(3,4),(10,2),(15,5),(13,12),(5,13),(2,9)],(62,67,65,255)),([(19,3),(27,4),(29,10),(23,14),(17,11)],(55,62,63,255)),([(4,19),(12,16),(17,21),(13,28),(5,27)],(57,63,62,255)),([(22,18),(29,20),(28,28),(21,29),(18,24)],(64,68,64,255))]:
 wd.polygon(pts,fill=col);wd.line([pts[0],pts[1],pts[2]],fill=(77,79,72,255),width=1);wd.line([pts[-2],pts[-1]],fill=(31,38,41,255),width=1)
wall=save(wall,'Terrain/TabMap_WallRock_32x32.png','simplified faceted nonwalkable stone, repeat')
void=save(texture(32,[25,30,33],1,6),'Terrain/TabMap_Unwalkable_32x32.png','unwalkable far space, repeat')
edge=Image.new('RGBA',(32,32));d=ImageDraw.Draw(edge)
d.rectangle((0,0,31,1),fill=(17,21,23,255));d.rectangle((0,2,31,2),fill=(77,80,72,255))
for x in [3,13,24]:d.rectangle((x,3,x+2,3),fill=(65,69,64,255))
edges={}
for name,angle in [('North',0),('East',-90),('South',180),('West',90)]:edges[name]=save(edge.rotate(angle),'Terrain/TabMap_Boundary_'+name+'_32x32.png','optional wall-side transition, do not change walkable mask')

# Code-native pixel glyphs match the simple existing UI markers; no generated text.
ink=(11,20,23,255);cream=(216,213,180,255)
icons={}
def icon(name):
 im=Image.new('RGBA',(24,24));return im,ImageDraw.Draw(im)
im,d=icon('player');d.polygon([(12,2),(22,21),(12,17),(2,21)],fill=ink);d.polygon([(12,5),(19,18),(12,15),(5,18)],fill=(46,211,219,255));d.line([(12,6),(7,16)],fill=(140,248,239,255),width=1)
icons['Player']=save(im,'Icons/TabMap_PlayerArrow_24x24.png','player direction; points up; center pivot',pivot=[.5,.5])
def skull(boss=False):
 im,d=icon('skull')
 if boss:
  d.polygon([(2,2),(8,5),(15,5),(21,2),(21,10),(19,19),(15,22),(8,22),(4,19),(2,10)],fill=ink)
  d.polygon([(4,5),(8,8),(15,8),(19,5),(18,12),(16,18),(7,18),(5,12)],fill=(207,115,61,255))
  d.rectangle((9,18,14,20),fill=(230,151,72,255))
 else:
  d.polygon([(7,3),(16,3),(21,7),(21,15),(17,18),(16,21),(7,21),(6,18),(2,15),(2,7)],fill=ink)
  d.polygon([(7,5),(16,5),(19,8),(19,14),(16,16),(15,19),(8,19),(7,16),(4,14),(4,8)],fill=(224,87,101,255))
  d.line((8,6,15,6),fill=(249,136,138,255))
 for x in [7,14]:d.rectangle((x,10,x+2,12),fill=ink)
 d.polygon([(11,13),(13,13),(12,15)],fill=ink);d.rectangle((10,17,13,18),fill=ink)
 if boss:d.line((7,8,9,8),fill=(255,190,94,255));d.line((14,8,16,8),fill=(255,190,94,255))
 return im
icons['Monster']=save(skull(),'Icons/TabMap_MonsterSkull_24x24.png','normal monster red skull')
icons['Boss']=save(skull(True),'Icons/TabMap_BossHornedSkull_24x24.png','boss horned amber skull, distinct silhouette')
for opened in [False,True]:
 im,d=icon('door');d.rectangle((4,2,20,22),fill=ink);d.rectangle((6,4,18,20),fill=(129,100,53,255));d.rectangle((8,6,16,20),fill=(20,28,28,255));d.line((6,4,18,4),fill=(224,186,101,255))
 if not opened:
  d.rectangle((9,7,15,19),fill=(170,125,51,255));d.rectangle((11,11,14,16),fill=ink);d.rectangle((12,14,13,18),fill=ink)
 else:
  d.polygon([(8,6),(12,8),(12,21),(8,19)],fill=(147,123,66,255));d.line((14,17,16,19),fill=(87,219,167,255),width=2);d.line((16,19,21,13),fill=(87,219,167,255),width=2)
 k='ExitOpen' if opened else 'ExitLocked';icons[k]=save(im,'Icons/TabMap_'+k+'_24x24.png',k+'; no state logic')

def font(sz):return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',sz)
def textcenter(d,xy,text,sz=16,color=(225,221,201,255)):
 box=d.textbbox((0,0),text,font=font(sz));d.text((xy[0]-(box[2]-box[0])/2,xy[1]),text,font=font(sz),fill=color)
def read_rects(path,key):
 s=path.read_text(encoding='utf-8');s=s.split('  '+key+':',1)[1].split('\n  '+('rockAreas' if key=='floorAreas' else 'monsterAreas')+':',1)[0]
 return [[float(v) for v in q] for q in re.findall(r'    x: ([-\d.]+)\s+    y: ([-\d.]+)\s+    width: ([-\d.]+)\s+    height: ([-\d.]+)',s)]
layouts={};mapinfo={}
def maprender(boss=False,old=False):
 key='BossRoom24x22' if boss else 'Map01';p=R/('Assets/Prefabs/Rooms/BossRoom24x22.prefab' if boss else 'Assets/Prefabs/Rooms/RegularCaves/Map_01.prefab')
 rects=read_rects(p,'floorAreas');rocks=read_rects(p,'rockAreas');wh=(24,22) if boss else (28,36);scale=min(512/wh[0],310/wh[1]);rw,rh=round(wh[0]*scale),round(wh[1]*scale);ox=(512-rw)//2;oy=(310-rh)//2
 m=Image.new('L',(512,310));md=ImageDraw.Draw(m)
 def coords(r):
  x,y,w,h=r;return (ox+round(x*scale),oy+round((wh[1]-y-h)*scale),ox+round((x+w)*scale)-1,oy+round((wh[1]-y)*scale)-1)
 for r in rects:md.rectangle(coords(r),fill=255)
 for r in rocks:md.rectangle(coords(r),fill=0)
 ar=np.array(m)>0
 if old:
  image=Image.new('RGBA',(512,310));ImageDraw.Draw(image).rectangle((ox,oy,ox+rw-1,oy+rh-1),fill=(46,48,51,242));image.paste((107,105,92,235),(0,0,512,310),m)
 else:
  stone=np.array(m.filter(ImageFilter.MaxFilter(13)))>0;image=tile(void,(512,310));v=np.array(image);fa=np.array(tile(floor,(512,310)));wa=np.array(tile(wall,(512,310)));v[stone]=wa[stone];v[ar]=fa[ar]
  # A one-pixel wall-side outline never covers/shrinks a walkable floor pixel.
  near=np.array(m.filter(ImageFilter.MaxFilter(3)))>0;rim=near&~ar;v[rim,:3]=(23,29,29)
  v[:oy,:,3]=0;v[oy+rh:,:,3]=0;v[:,:ox,3]=0;v[:,ox+rw:,3]=0
  image=Image.fromarray(v)
 def point(x,y):return (ox+round(x*scale),oy+round((wh[1]-y)*scale))
 marks=[('Player',(12,5))] if boss else [('Player',(14,18)),('Monster',(17,12)),('Monster',(11,21)),('Monster',(15,29)),('ExitLocked',(14,32.25))]
 if boss:marks.append(('Boss',(12,14)))
 d=ImageDraw.Draw(image)
 for kind,pos in marks:
  x,y=point(*pos)
  if old:
   if kind=='Player':d.polygon([(x,y-7),(x+6,y+5),(x-6,y+5)],fill=(51,242,255))
   elif kind.startswith('Exit'):d.rectangle((x-5,y-5,x+5,y+5),fill=(255,186,64))
   else:
    radius=7 if kind=='Boss' else 4.5;d.polygon([(x,y-radius),(x+radius,y),(x,y+radius),(x-radius,y)],fill=(255,128,51) if kind=='Boss' else (255,76,89))
  else:
   # 24px source exported separately. 18px display is an initial readability trial.
   sz=24 if kind=='Boss' else 18;mark=icons[kind].resize((sz,sz),Image.Resampling.NEAREST);image.alpha_composite(mark,(x-sz//2,y-sz//2))
 layouts[key]=dict(room_size=list(wh),map_rect=[24,45,512,310],display_size=[rw,rh],uniform_scale=scale,floor_rectangles=rects,rock_rectangles=rocks,marker_positions_are_illustrative=True,has_exit_marker=not boss)
 return image
def panel(boss=False,old=False):
 im=Image.new('RGBA',(560,400));im.alpha_composite(background)
 if not old:im.alpha_composite(frame)
 im.alpha_composite(maprender(boss,old),(24,45));d=ImageDraw.Draw(im)
 textcenter(d,(280,14),'현재 방  |  TAB',18)
 if not old:
  d.line((80,42,480,42),fill=(81,76,61,255));d.line((24,360,536,360),fill=(76,77,67,255))
  labels=[('Player','플레이어'),('Monster','몬스터'),('ExitLocked','출구'),('Floor','바닥'),('Wall','벽/암석')]
  for i,(key,label) in enumerate(labels):
   x=24+i*103
   glyph=icons[key] if key in icons else floor if key=='Floor' else wall
   im.alpha_composite(glyph.resize((20,20),Image.Resampling.NEAREST),(x,369));d.text((x+25,369),label,font=font(13),fill=(219,216,199,255))
 else:textcenter(d,(280,369),'▲ 플레이어   ◆ 적/보스   ■ 출구: 잠김 / 개방',14)
 return im
regular=panel();boss=panel(True);old=panel(old=True)
for name,im in [('Regular_Map01_560x400',regular),('BossRoom24x22_560x400',boss),('CurrentCode_Reconstruction_560x400',old)]:
 im.save(O/'Previews'/f'{name}.png');im.resize((1120,800),Image.Resampling.NEAREST).save(O/'Previews'/f'{name}_2x.png')

environment=Image.open(R/'Art Resource/MineEnvironment_v1/Previews/Concept_Mine_16x9_EnvironmentOnly_1x.png').convert('RGBA').resize((1920,1080),Image.Resampling.NEAREST)
full=environment.copy();full.alpha_composite(regular,(680,340));ImageDraw.Draw(full).text((30,25),'1920x1080 / ART COMPOSITE / 560x400 centered / NOT Unity Play',font=font(18),fill=(245,229,202));full.save(O/'Previews/Regular_1920x1080_Centered_ArtComposite.png')
envboss=Image.open(R/'Art Resource/MineEnvironment_v1/Previews/Concept_BossRoom_16x9_EnvironmentOnly_1x.png').convert('RGBA').resize((1920,1080),Image.Resampling.NEAREST)
envboss.alpha_composite(boss,(680,340));ImageDraw.Draw(envboss).text((30,25),'BossRoom24x22 / NO EXIT MARKER / ART COMPOSITE, NOT Unity Play',font=font(18),fill=(245,229,202));envboss.save(O/'Previews/Boss_1920x1080_Centered_ArtComposite.png')

cmp=Image.new('RGBA',(1160,448),(26,30,31,255));d=ImageDraw.Draw(cmp);cmp.alpha_composite(old,(10,40));cmp.alpha_composite(regular,(590,40));d.text((12,8),'현재 코드 재구성 / Map01',font=font(18),fill=cream);d.text((592,8),'개선 아트 시험 합성 / 같은 Map01',font=font(18),fill=cream);cmp.save(O/'Previews/CodeBefore_ArtAfter_SameMap01.png')
refim=Image.open(ref).convert('RGBA');cmp=Image.new('RGBA',(1160,550),(26,30,31,255));d=ImageDraw.Draw(cmp)
refpreview=refim.copy();refpreview.thumbnail((560,490),Image.Resampling.NEAREST);cmp.alpha_composite(refpreview,(10+(560-refpreview.width)//2,46));cmp.alpha_composite(regular,(590,88));d.text((12,8),'첨부 Tab 지도 / 디자인 기준',font=font(18),fill=cream);d.text((592,8),'TabMapUI_v1 / Map01 시험 합성',font=font(18),fill=cream);d.text((592,505),'방 형태는 서로 다름 · 실제 Play 캡처 아님',font=font(15),fill=(173,177,166,255));cmp.save(O/'Previews/Reference_Improved_Comparison.png')

# Standalone icon and surface QA: native/2x/4x plus black/white/floor backgrounds.
review=Image.new('RGBA',(840,520),(27,31,32,255));d=ImageDraw.Draw(review)
for i,(key,im) in enumerate(icons.items()):
 x=20+i*164;d.text((x,12),key,font=font(16),fill=cream)
 for row,(color,scale) in enumerate([((12,15,17,255),1),((95,91,77,255),2),((240,237,225,255),4)]):
  y=50+row*122;d.rectangle((x,y,x+135,y+108),fill=color);review.alpha_composite(im.resize((24*scale,24*scale),Image.Resampling.NEAREST),(x+14,y+6))
for i,(label,im) in enumerate([('Floor',floor),('Wall',wall),('Void',void)]):
 x=20+i*164;review.alpha_composite(tile(im,(128,64)),(x,440));d.text((x,412),label,font=font(16),fill=cream)
review.save(O/'Previews/Icons_Terrain_Native_2x_4x_QA.png')
# 9-slice geometry test. Corners remain pixel-identical, no full-frame scaling.
def nine_slice(im,w,h,b=48):
 out=Image.new('RGBA',(w,h));xs=[0,b,im.width-b,im.width];ys=[0,b,im.height-b,im.height];tx=[0,b,w-b,w];ty=[0,b,h-b,h]
 for j in range(3):
  for i in range(3):
   part=im.crop((xs[i],ys[j],xs[i+1],ys[j+1])).resize((tx[i+1]-tx[i],ty[j+1]-ty[j]),Image.Resampling.NEAREST);out.paste(part,(tx[i],ty[j]))
 return out
test=nine_slice(frame,720,480)
for box1,box2 in [((0,0,48,48),(0,0,48,48)),((512,0,560,48),(672,0,720,48)),((0,352,48,400),(0,432,48,480)),((512,352,560,400),(672,432,720,480))]:assert np.array_equal(np.array(frame.crop(box1)),np.array(test.crop(box2)))
test.save(O/'Previews/Frame_9Slice_720x480_CornerTest.png')
for name,im in [('floor',floor),('wall',wall),('void',void),('background',bg_tile)]:
 a=np.array(im);assert np.array_equal(a[0],a[-1]) and np.array_equal(a[:,0],a[:,-1])
for im in icons.values():
 a=np.array(im);assert not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any()
assert len(icons)==5 and not layouts['BossRoom24x22']['has_exit_marker']
for p,h in protected.items():assert hashlib.sha256(Path(p).read_bytes()).hexdigest()==h,p
manifest={'assets':assets,'layout':{'panel':[560,400],'panel_center_screen_1920x1080':[960,540],'map_rect_top_left':[24,45,512,310],'title_center_top_left':[280,14],'legend_top':369,'frame_border_LBRT':[48,48,48,48],'background_alpha_baked':204,'image_tint_alpha':1,'parent_CanvasGroup_alpha':1,'icons_source':[24,24],'preview_icon_display_sizes':{'player_monster_exit':18,'boss':24},'icon_pivot':[.5,.5],'arrow_points':'up / +Y','render_order':['background','terrain','boundary clipped to wall','markers','frame','existing TMP labels']},'rooms':layouts,'checks':{'PNG_readback_dimensions_RGBA':True,'frame_map_area_fully_transparent':True,'frame_icons_binary_alpha':True,'background_alpha_exact_204':True,'tile_opposite_edges_equal':True,'nine_slice_corner_pixels_unchanged':True,'icons_no_canvas_edge_clipping':True,'boss_preview_no_exit':True,'protected_sources_unchanged':True,'visual_static_review':'native and enlarged panel, screenshot comparison, icons on dark/floor/light backgrounds inspected','Unity_import_Play':'not performed / out of scope','reference_HUD_exact_match':'HUD scripts and older environment screenshot consulted, current HUD art screenshot not supplied'},'protected_sha256':protected,'generation':{'tool':'built-in image_gen','final_frame_source':str(gen),'frame_export':'trim outer blank padding, nearest554x394 with3px padding to560x400,48RGBpalette,binaryalpha threshold160; mechanical final-size export','icons_and_terrain':'editable deterministic pixel glyphs and repeat patterns; separate PNG; no text baked'},'reference_screenshot':str(ref),'notes':['No fixed room map PNG belongs to runtime resources; room composites are Previews only','CurrentRoomMapGraphic draws one mesh, not independent Image sprites: follow-up Unity adapter required, not implemented']}
(O/'Specs/AssetManifest_and_Validation.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'output':str(O),'runtime_PNG':len(assets),'Map01_rects':len(layouts['Map01']['floor_rectangles']),'Boss_rects':len(layouts['BossRoom24x22']['floor_rectangles'])},ensure_ascii=False))

readme='''# 광산던전 Tab 지도 UI 아트 v1

## 범위와 디자인 기준

사용자가 제공한 Tab 지도 스크린샷의 얇은 구리 테두리·각진 모서리 장식·어두운 석판 분위기·현재 방/TAB 제목·중앙 지도·하단5개 범례 구성을 유지했다. 광산 환경의 저채도 회갈색/청회색, 기존 HUD 스크립트의 청록·금색 강조색을 참고했다. 전체 프레임을 화려한 판타지 장식으로 바꾸지 않았다.

프레임은 기본 내장 image_gen으로 제작·참고 이미지에 맞춰 수정한 뒤, 최종 규격으로 최근접 축소·48색 정리·알파 이진화했다. 아이콘과 단순 지도 표면은 기존 코드형 기하 표식에 맞춘 편집 가능한 픽셀 도형/패턴으로 제작했다. 프롬프트는 `Sources/GenerationPrompts.md`, 제작 원본은 `Sources/`에 보존한다.

이 폴더는 **아트만** 포함한다. Assets Import, Unity 코드·씬·프리팹·Canvas·데이터/좌표 계산·Tab 입력 규칙·Raycast·.meta/GUID를 변경하지 않았다. 공식 문서도 수정하지 않았으며 Git 커밋/푸시를 하지 않았다.

## 게임용 개별 PNG 15개

|폴더·파일|실제 크기|용도|
|---|---|---|
|Frame/TabMap_Frame_560x400_v1.png|560×400|중앙이 완전히 빈 RGBA 프레임|
|Background/TabMap_Background_560x400_A80.png|560×400|기본 패널 배경·알파204|
|Background/TabMap_BackgroundTile_64x64_A80.png|64×64|선택형 반복 배경·알파204|
|Icons/TabMap_PlayerArrow_24x24.png|24×24|청록 방향 화살표·기본 위쪽|
|Icons/TabMap_MonsterSkull_24x24.png|24×24|붉은 해골|
|Icons/TabMap_BossHornedSkull_24x24.png|24×24|뿔이 있는 황동/주황 해골·다른 실루엣|
|Icons/TabMap_ExitLocked_24x24.png|24×24|닫힌 황동 문·검은 잠금구|
|Icons/TabMap_ExitOpen_24x24.png|24×24|열린 문·작은 청록 활성 표시|
|Terrain/TabMap_Floor_32x32.png|32×32|저채도 밝은 회갈색 바닥|
|Terrain/TabMap_WallRock_32x32.png|32×32|어두운 면 분할 암석|
|Terrain/TabMap_Unwalkable_32x32.png|32×32|더 어두운 이동 불가 공간|
|Terrain/TabMap_Boundary_North/East/South/West_32x32.png|각32×32·4개|선택형 벽 쪽 경계 전이|

모두 PNG RGBA다. 프레임·아이콘·경계는 배경 알파0, 선/도형은255이며 흐린 가장자리가 없다. 지형 반복 타일3개는 지형 영역을 채우는 용도라 알파255다. 배경2개는 **파일 전체에 알파204/255=80%가 이미 포함**되어 있다. 텍스처에 글자·라벨·범례를 넣지 않았다. `Previews/`의 글자는 검토 이미지에만 합성했으며 게임용 글자는 기존 TMP를 사용한다. 폰트를 신규 제작·교체·동봉하지 않는다.

## 기존560×400 배치와9-Slice

- 기준1920×1080, 패널 중심(960,540), 패널560×400을 유지한다.
- 기존 코드의 지도 Rect512×310, 중앙정렬을 유지한다. PNG 좌상단 기준 지도영역은(x24,y45,w512,h310)이다. 이 영역에서 프레임 알파는 전부0이다.
- 프레임 Pivot(0.5,0.5), Sprite Border **L48/B48/R48/T48** 권장. 장식 전체가 고정되는 안전 범위다. 프레임 선의 실제 굵기는 이48px보다 훨씬 얇다.
- UI Image Type=Sliced, Fill Center는끄는 것을 권장한다. 내부는 실제 투명이므로 켜도 본체 지도를 가리지 않는다. 9-Slice는 선택적인 재사용 대비이며 이번 패널 크기를 변경하자는 제안이 아니다.
- 프레임을720×480으로 시험 재조립한 결과 네 모서리48×48은 원본과 픽셀 단위로 동일하다. `Previews/Frame_9Slice_720x480_CornerTest.png` 참조. Unity 엔진에서 실제9-Slice 표시 검증은 별도다.
- Point / Compression None / Mipmaps Off / sRGB On / SpriteMesh FullRect / TightPacking Off를 권장한다. UI Sprite PPU는 기존Canvas 규약을 우선한다. 표준UI의100PPU를 사용하는 경우 pixelsPerUnitMultiplier1을 시작값으로 시험하되 기존Canvas.referencePixelsPerUnit과 확인한다. 몬스터의PPU64를 지도UI에 그대로 적용하지 않는다.

## 배경 투명도와 레이어

기본 배경560×400 또는 반복64×64 **하나만** 사용한다. 둘을 겹치지 않는다. Image.color=(1,1,1,1), 부모 CanvasGroup.alpha=1로 파일의80% 알파를 그대로 사용한다. 기존 background.color.a=.8을 추가 곱하면64%가 되어 어두움/투명도가 달라진다. 색 Tint 또한흰색으로 시작하여 중복 착색을 피한다. 배경80%는 전체프레임·아이콘을80%로 흐리게 하라는 뜻이 아니다.

권장 겹침: 배경→지형→벽 쪽 경계→표식→프레임→기존TMP 제목/범례. 기존 RaycastTarget=false, CanvasGroup.blocksRaycasts=false/interactable=false 규칙을 보존한다. 이번에는 해당 설정을 실제 연결하지 않았다.

## 아이콘 규격과 방향

24×24 원본, 중앙Pivot(12,12)/(0.5,0.5). 미리보기 표시크기는 플레이어/일반몹/출구18px, 보스24px를 가독성 시험값으로 사용했다. **현재 코드는 일반몹 마름모 지름9px, 보스14px, 플레이어 약12~14px, 출구10px이므로**, PNG를 넣을 때 표시크기를 자동 확대하면 안 된다. 원래크기/18px 시험값에서 겹침을 확인한 후 후속 통합에서 결정한다. 패널크기나 지형 좌표를 변경할 필요는 없다.

화살표 원본은+Y(위쪽)를 향한다. UI local+X 기준 atan2(y,x) 각도를 쓰면 기본방향 보정을 위해90도를 빼는 방식 등을 기존각도규약에 맞춰 확인한다. 이 문서는 플레이어 조준/이동 방향 선택 규칙을 변경하지 않는다. Unity RectTransform 회전으로 사용하고, 방향별 이미지 복제를 하지 않는다. 45도 회전 시 Point 특유의 계단 형태와 가장자리 지도 Mask에 의한 잘림은 Play에서 확인한다.

보스는 색상뿐 아니라 뿔·넓은 윗부분·턱 형태로 일반 해골과 구분된다. 잠김/열림 출구는 문짝과 작은 활성 체크의 차이를 함께 사용한다. 아이콘이 과도하게 빛나지 않는다. 살아있는 몬스터만 표시하고, BossRoom 지도에는 출구 표식을 생성하지 않는 기존 데이터 규칙을 그대로 사용한다. 공통 하단범례의 출구 설명은 실제 지도내 출구 표식과 별개다.

## 지형과 실제 데이터 연결

지형타일은 특정방 지도그림이 아니다. 현행 floorAreas/rockAreas/Room.bounds 데이터를 읽어 바닥/암석/이동불가 마스크에 반복 텍스처를 입히는 재료다. 바닥 경계를 텍스처에 맞춰 이동시키지 않는다. 얇은 경계는 **벽 영역 안으로만** 배치/클리핑하며 이동 가능한 픽셀을 덮지 않는다. 미리보기의 경계는실제 마스크 바깥의1px 어두운선과6px 암석띠로 표시했다. 폴리곤 데이터나Collider를 바꾸지 않았다.

중앙 지도영역512×310에서 s=min(512/방가로,310/방세로). 예시 Map01은28×36→약241×310, BossRoom은24×22→약338×310이다. 방 종횡비를 유지하고 남는영역은 배경으로 둔다. Map02~10도동일한 재료를 적용할 수 있지만 모든방 개별 프리뷰를 제작/검증하지 않았다.

**중요한 기존 구조:** `CurrentRoomMapGraphic.cs`는 현재단일 MaskableGraphic 메시에 단색사각형/마름모/삼각형을 직접 그린다. 아이콘별Image나Sprite 슬롯이 없다. PNG를 폴더에 놓는 것만으로 자동표시되지 않는다. 후속Codex가 동일 데이터·좌표계·생존/출구상태 규칙을 유지한 채UV/별도Image 등 기존렌더링에 맞는연결방식을 결정해야 한다. 이번에는 어댑터코드·아틀라스·새Canvas를 만들지 않았다.

## 미리보기와 비교의 의미

- `Reference_Improved_Comparison.png`: 사용자첨부실제스크린샷 대 신규Map01 합성. 방형태가 달라 외형/정보배치 참고용이며 지형변화 전후증거가 아니다.
- `CodeBefore_ArtAfter_SameMap01.png`: 현재코드의 단색표현을 오프라인 재구성한 것과 동일Map01 지형의신규아트 합성. 왼쪽도실제Play캡처가 아니다.
- `Regular_Map01_560x400.png`, `BossRoom24x22_560x400.png`: 원배율UI. `_2x`는최근접 확대검토용이다.
- `Regular_1920x1080_Centered_ArtComposite.png`, `Boss_1920x1080_Centered_ArtComposite.png`: 기존MineEnvironment환경콘셉트에560×400을중앙합성. 실제맵카메라/현재HUD를캡처한결과가 아니다.
- `Icons_Terrain_Native_2x_4x_QA.png`: 원배율/2배/4배아이콘을 어두운배경·바닥색·밝은배경에서비교.
- `Frame_9Slice_720x480_CornerTest.png`: 아트슬라이스기하검사전용, UI크기변경지시가아니다.

Map01/보스룸 예시는 실제프리팹의 floorAreas/rockAreas를 읽어 만들었다. 플레이어·몬스터위치는 **예시 배치**이며 실시간게임상태나스폰좌표를검증한것이아니다. Map01출구는직렬화된후보(14,32.25)를참고했다. 보스룸지도에출구표식은없다.

## 검증 결과 / 남은 확인

- 게임용PNG15개를실제로다시읽어해상도·RGBA·알파값을검사했다. 전체목록은 `Specs/AssetManifest_and_Validation.json`.
- 프레임중앙지도영역의완전투명, 아이콘외곽여백/알파0·255, 배경알파204를확인했다.
- 바닥·벽·이동불가·배경타일의맞닿는반대쪽테두리픽셀을비교하여연속성을확인했다.
- 원배율/확대UI와스크린샷비교를정적으로검토하고 제목/범례와프레임겹침을보정했다. 아이콘5종을배경3종위에서검토했다. 벽/바닥은명도와면패턴으로구분한다.
- 9-Slice의네모서리픽셀보존을검사했다. 실제Unity9-Slice렌더링/극단축소는미검증이다.
- 현재HUD의실제최신스크린샷은없으므로 정밀한HUD재질일치는미검증이다. 기존HUD코드의색상계열과광산환경아트/제공Tab화면을참고했다.
- 실제UnityImport/Play, Tab홀드/해제,실시간표식갱신,다양한방의겹침,회전플레이어,출구상태변경,Raycast회귀는이번범위밖으로미실시다.
- 기존관련코드·프리팹·공식문서9개파일의전후SHA256이동일하다. 기존아트/.meta/GUID를덮어쓰지않았다.

최종미술품질과실시간표시는사용자검토및후속Unity검증에서판단한다. 제안된표시크기·9-Slice값은시험기준이며게임전체규격을확정하지않는다.
'''
(O/'README_KO.md').write_text(readme,encoding='utf-8')
prompt='''Built-in image_gen. Final edit prompt (reference1 prior empty frame; reference2 user Tab screenshot):
Edit image1 empty border only. Image2 is the approved existing Tab map screenshot and STYLE/PROPORTION reference only. Preserve the existing screenshot's restrained thin copper/bronze double rule and tiny chipped metal/stone corner ornaments. Current image1 frame rails are much too heavy. Make long rails dramatically thinner, 4-6px in intended final560x400, corners compact24px, corners shaped like image2's subtle angular chipped bronze brackets. Single rectangular560x400 aspect7:5 empty UI border, NO content from screenshot, NO map, NO icons, NO text, NO north letter, NO titlebar, no extra panels. True transparent RGBA outside and all center. Crisp low-resolution pixel art, dark charcoal keyline, aged copper highlight with restrained warm stone accents at corners, almost all interior clear. Work is refinement of screenshot atmosphere, not a new ornate fantasy frame. Fit within canvas, tiny4px outer safe margin. 9-slice appropriate32px corners and straight uniform edges. Export standalone border asset.

Export note: generated source is preserved. Final560x400 export uses nearest sampling,48-color palette,binary alpha. Actual corner assemblies require48px safe9-slice borders, overriding the prompt's intended32px. Icons and simple surfaces are deterministic pixel geometry/patterns from the companion art build source, not generated lettering.
'''
(O/'Sources/GenerationPrompts.md').write_text(prompt,encoding='utf-8')
shutil.copyfile(Path(__file__),O/'Sources/build_tabmap_v1.py')
zpath=O.parent/'TabMapUI_v1.zip'
with zipfile.ZipFile(zpath,'w',zipfile.ZIP_DEFLATED) as z:
 for p in sorted(O.rglob('*')):
  if p.is_file():z.write(p,Path(O.name)/p.relative_to(O))
with zipfile.ZipFile(zpath) as z:assert z.testzip() is None
print(json.dumps({'zip':str(zpath),'bytes':zpath.stat().st_size},ensure_ascii=False))
