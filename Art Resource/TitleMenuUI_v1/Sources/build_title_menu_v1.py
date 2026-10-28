from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import numpy as np,json,hashlib,shutil,zipfile
W=Path('C:/Users/User/.codex/visualizations/2026/10/22/01a16344-f87e-7b93-9d18-cb1d88939ccf');R=Path('D:/Git/MineDungeon2');O=R/'Art Resource/TitleMenuUI_v1';D=Path('C:/Users/User/.codex/generated_images/01a16344-f87e-7b93-9d18-cb1d88939ccf')
for sub in ['Logos','Buttons','Previews','Specs','Sources']:(O/sub).mkdir(parents=True,exist_ok=True)
sources={'Logo_A':D/'exec-89031362-8315-4e63-bd54-d705750e3b05.png','Logo_B':D/'exec-4d89ad64-ec35-4023-8181-10f04c8a64ea.png','Normal':D/'exec-35d8771d-a13b-4954-9617-0a5b9acfefa1.png','Hover':D/'exec-8bceb910-37c0-4d7e-89a3-640e4b31d61c.png','Pressed':D/'exec-aabc56a7-d01b-4336-a869-35f700b081b8.png'}
protected={}
for folder in ['Art Resource/TitleLantern_v1','Art Resource/TitleMinerSlime_v1']:
 for p in (R/folder).rglob('*'):
  if p.is_file():protected[str(p)]=hashlib.sha256(p.read_bytes()).hexdigest()
for p in [Path('C:/Users/User/Downloads/TitleScene_background.png'),R/'Docs/Systems/ArtDirection.md',R/'Docs/Systems/UISystem.md']:protected[str(p)]=hashlib.sha256(p.read_bytes()).hexdigest()
assets=[]
def save(im,path,role,**extra):
 im.save(O/path);q=Image.open(O/path);assert q.mode=='RGBA' and q.size==im.size;assert np.array_equal(np.array(im),np.array(q));a=np.array(q);assets.append(dict(file=path,size=list(q.size),alpha_values=np.unique(a[:,:,3]).tolist(),role=role,**extra));return im
def clean(im,colors=80):
 a=np.array(im.convert('RGBA'));a[:,:,3]=np.where(a[:,:,3]>=160,255,0);a[a[:,:,3]==0,:3]=0
 rgb=Image.fromarray(a[:,:,:3]).quantize(colors=colors,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB');a[:,:,:3]=np.array(rgb);a[a[:,:,3]==0,:3]=0;return Image.fromarray(a)
def bbox(im):
 a=np.array(im)[:,:,3]>=160;yy,xx=np.where(a);return int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)
logos={}
for key in ['Logo_A','Logo_B']:
 im=Image.open(sources[key]).convert('RGBA');im=im.crop(bbox(im));s=min(704/im.width,224/im.height);size=(round(im.width*s),round(im.height*s));out=Image.new('RGBA',(720,240));out.paste(im.resize(size,Image.Resampling.NEAREST),((720-size[0])//2,(240-size[1])//2));logos[key]=save(clean(out),f'Logos/TitleLogo_{key[-1]}_720x240_v1.png','logo proposal '+key[-1],text_korean='광산던전',text_english='MINE DUNGEON')
# Candidate copy is explicitly a recommendation, not a final user-approved selection.
save(logos['Logo_A'].copy(),'Logos/TitleLogo_FinalCandidate_A_720x240_v1.png','recommended candidate; identical A; pending user decision')
base=Image.open(sources['Normal']).convert('RGBA');box=bbox(base);buttons={}
for key in ['Normal','Hover','Pressed']:
 im=Image.open(sources[key]).convert('RGBA');assert im.size==base.size
 im=im.crop(box).resize((352,72),Image.Resampling.NEAREST);out=Image.new('RGBA',(360,80));out.paste(im,(4,4));out=clean(out,64)
 if key=='Normal':alpha=np.array(out)[:,:,3].copy()
 else:
  a=np.array(out);a[:,:,3]=alpha;a[alpha==0,:3]=0;out=Image.fromarray(a)
 buttons[key]=save(out,f'Buttons/TitleMenu_Button_{key}_360x80_v1.png','shared text-free button state '+key,border_LBRT=[28,20,28,20],pivot=[.5,.5],safe_label_rect=[30,20,300,40])
for f in buttons.values():assert np.array_equal(np.array(f)[:,:,3],alpha)
assert len({hashlib.sha256(f.tobytes()).hexdigest() for f in buttons.values()})==3
def font(size,bold=False):return ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf' if bold else 'C:/Windows/Fonts/malgun.ttf',size)
def centered(d,xy,text,size=28,fill=(228,221,200,255),bold=False):
 f=font(size,bold);bb=d.textbbox((0,0),text,font=f);d.text((round(xy[0]-(bb[2]-bb[0])/2),round(xy[1]-(bb[3]-bb[1])/2-bb[1])),text,font=f,fill=fill)
def nine(im,w,h):
 xs=[0,28,332,360];ys=[0,20,60,80];tx=[0,28,w-28,w];ty=[0,20,h-20,h];out=Image.new('RGBA',(w,h))
 for j in range(3):
  for i in range(3):
   q=im.crop((xs[i],ys[j],xs[i+1],ys[j+1])).resize((tx[i+1]-tx[i],ty[j+1]-ty[j]),Image.Resampling.NEAREST);out.paste(q,(tx[i],ty[j]))
 return out
tests={}
for key,b in buttons.items():
 test=nine(b,480,96)
 for box1,box2 in [((0,0,28,20),(0,0,28,20)),((332,0,360,20),(452,0,480,20)),((0,60,28,80),(0,76,28,96)),((332,60,360,80),(452,76,480,96))]:assert np.array_equal(np.array(b.crop(box1)),np.array(test.crop(box2)))
 tests[key]=test

comp=Image.new('RGBA',(1480,330),(21,27,29,255));d=ImageDraw.Draw(comp)
for i,key in enumerate(['Logo_A','Logo_B']):
 comp.alpha_composite(logos[key],(10+i*740,42));d.text((20+i*740,10),'A · 암석 / 추천 후보' if i==0 else 'B · 구리 금속 / 비교 후보',font=font(21),fill=(226,219,196,255))
 centered(d,(380+i*740,306),'검수 표기: 광산던전 / MINE DUNGEON',18)
comp.save(O/'Previews/Logo_A_B_Comparison_Native.png')
states=Image.new('RGBA',(1160,300),(21,27,29,255));d=ImageDraw.Draw(states)
for i,key in enumerate(['Normal','Hover','Pressed']):
 x=10+i*385;d.text((x+8,12),key,font=font(21),fill=(226,219,196,255));states.alpha_composite(buttons[key],(x,58));states.alpha_composite(buttons[key],(x,178));centered(d,(x+180,218+(1 if key=='Pressed' else 0)),'게임 시작',28,bold=True)
states.save(O/'Previews/Button_States_TextFree_and_LabelDemo.png')
slicecomp=Image.new('RGBA',(500,348),(21,27,29,255));d=ImageDraw.Draw(slicecomp)
for i,(key,test) in enumerate(tests.items()):slicecomp.alpha_composite(test,(10,i*112+22));d.text((12,i*112+2),key,font=font(13),fill=(225,216,190,255))
slicecomp.save(O/'Previews/Button_9Slice_480x96_Test.png')

source_scene=R/'Art Resource/TitleMinerSlime_v1/Previews/Title_Placement_A_2752x1536.png';scene=Image.open(source_scene).convert('RGBA');s=min(1920/scene.width,1080/scene.height);wh=(round(scene.width*s),round(scene.height*s));base_scene=Image.new('RGBA',(1920,1080),(12,20,23,255));base_scene.alpha_composite(scene.resize(wh,Image.Resampling.NEAREST),((1920-wh[0])//2,(1080-wh[1])//2))
layout={'canvas':[1920,1080],'logo_rect':[120,120,720,240],'buttons':[{'label':'게임 시작','rect':[300,500,360,80]},{'label':'설정','rect':[300,604,360,80]},{'label':'게임 종료','rect':[300,708,360,80]}],'button_font_preview_only':{'font':'Windows Malgun Gothic Bold (not included, not a replacement font)','size':28,'normal_color_RGBA':[228,221,200,255],'pressed_preview_text_dy':1},'background_fit':{'scale':s,'display_size':list(wh),'offset':[(1920-wh[0])//2,(1080-wh[1])//2]},'right_scene_protected_rect':[960,0,960,1080]}
def title(key='Logo_A',selected=None):
 out=base_scene.copy();out.alpha_composite(logos[key],(120,120));d=ImageDraw.Draw(out)
 for i,entry in enumerate(layout['buttons']):
  x,y,w,h=entry['rect'];state=selected if i==0 and selected else 'Normal';out.alpha_composite(buttons[state],(x,y));centered(d,(x+w/2,y+h/2+(1 if state=='Pressed' else 0)),entry['label'],28,bold=True)
 return out
fullA=title();fullB=title('Logo_B');fullA.save(O/'Previews/TitleMenu_A_1920x1080.png');fullB.save(O/'Previews/TitleMenu_B_1920x1080.png');title(selected='Hover').save(O/'Previews/TitleMenu_A_StartHovered_1920x1080.png');title(selected='Pressed').save(O/'Previews/TitleMenu_A_StartPressed_1920x1080.png')
assert np.array_equal(np.array(fullA)[:,960:],np.array(base_scene)[:,960:])
assert np.array_equal(np.array(fullB)[:,960:],np.array(base_scene)[:,960:])
both=Image.new('RGBA',(1920,578),(21,27,29,255));d=ImageDraw.Draw(both)
for i,f in enumerate([fullA,fullB]):both.alpha_composite(f.resize((960,540),Image.Resampling.NEAREST),(960*i,38));d.text((18+960*i,7),'A · 암석형 추천' if i==0 else 'B · 구리형 비교',font=font(20),fill=(227,219,194,255))
both.save(O/'Previews/TitleMenu_A_B_FullScene_Comparison.png')
guide=fullA.copy();d=ImageDraw.Draw(guide);d.rectangle((120,120,839,359),outline=(61,220,215,255),width=2)
for entry in layout['buttons']:
 x,y,w,h=entry['rect'];d.rectangle((x,y,x+w-1,y+h-1),outline=(61,220,215,255),width=2)
d.line((960,0,960,1080),fill=(83,155,152,255),width=2);guide.save(O/'Previews/TitleMenu_A_Layout_Guide.png')
for a in assets:
 ar=np.array(Image.open(O/a['file']));assert set(np.unique(ar[:,:,3]))=={0,255};assert not ar[0,:,3].any() and not ar[-1,:,3].any() and not ar[:,0,3].any() and not ar[:,-1,3].any()
assert hashlib.sha256((O/'Logos/TitleLogo_A_720x240_v1.png').read_bytes()).hexdigest()==hashlib.sha256((O/'Logos/TitleLogo_FinalCandidate_A_720x240_v1.png').read_bytes()).hexdigest()
for p,h in protected.items():assert hashlib.sha256(Path(p).read_bytes()).hexdigest()==h,p
report={'assets':assets,'layout':layout,'candidate':'A recommended, user final decision pending','button_shared_assets':{'게임 시작':'common3states','설정':'common3states','게임 종료':'common3states'},'button_runtime_text_baked':False,'logo_text_baked':{'ko':'광산던전','en':'MINE DUNGEON'},'checks':{'PNG_RGBA_dimensions_pixels_readback':True,'alpha_binary_transparent_background':True,'button_outer_alpha_mask_identical_3states':True,'nine_slice_4corners_pixel_equal':True,'right_half_scene_identical_to_reference':True,'source_assets_docs_hash_unchanged':True,'Hangul_English_visual_proof':'both exported logos visually compared with plain-font proof: 광/산/던/전 and MINE DUNGEON; manual static inspection, not OCR','button_states_text_free_visual_review':True,'full1920_static_layout_review':True,'Unity_import_Play':'not performed / out of scope'},'source_scene':str(source_scene),'source_SHA256':protected,'generation':{'mode':'built-in image_gen','source_files':{k:str(p) for k,p in sources.items()},'export':'trim empty margins, nearest exact-size export, limited palette, binary alpha, common button alpha footprint; no labels in buttons'}}
(O/'Specs/Layout_and_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'output':str(O),'assets':len(assets),'PNG_sizes':[(a['file'],a['size'])for a in assets],'source_files_checked':len(protected)},ensure_ascii=False))

readme='''# 광산던전 메인 타이틀 로고·메뉴 UI 아트 v1

## 제작 범위

`TitleLantern_v1`의 켜진 조명과 `TitleMinerSlime_v1`의 A안 배치를 기준으로 로고2안·공통 메뉴버튼3상태를 제작했다. 기존 배경·랜턴·슬라임을 수정하지 않고 별도 UI 이미지로 구성했다. 사용자 검토 전이므로 **추천 후보는 A이며 최종 승인본은 아니다.** B안도 동일한 배치로 비교할 수 있다.

내장 image_gen으로 암석형/구리형 로고와 버튼 기본형을 만들고, 같은 버튼을 참고하여 호버/눌림 상태를 제작했다. 최종 내보내기에서는 투명 여백을 정리하고 최근접 리샘플링·제한 팔레트·이진 알파를 적용했다. 버튼은 세 상태의 외곽 알파를 같은 마스크로 맞춰 교체할 때 위치나 크기가 튀지 않게 했다. 프롬프트와 생성 원본은 `Sources/`에 있다.

## 게임용 PNG — 독립6개

|파일|실제 크기|용도|
|---|---|---|
|Logos/TitleLogo_A_720x240_v1.png|720×240|A안·암석 절삭면과 얕은 구리 이음새|
|Logos/TitleLogo_B_720x240_v1.png|720×240|B안·구리/황동 금속판과 작은 리벳|
|Logos/TitleLogo_FinalCandidate_A_720x240_v1.png|720×240|추천 후보 A의 동일 복사본·사용자 확정 대기|
|Buttons/TitleMenu_Button_Normal_360x80_v1.png|360×80|기본·어두운 목재와 낮은 구리 테두리|
|Buttons/TitleMenu_Button_Hover_360x80_v1.png|360×80|호버·따뜻한 테두리 강조와 작은 내부 명도 상승|
|Buttons/TitleMenu_Button_Pressed_360x80_v1.png|360×80|눌림·어두운 상단 안쪽 면과 낮아진 내부 명도|

모두 PNG RGBA이며 외부 배경은 실제 알파0, 도형은255다. 원본 가장자리의 흐린 반투명 잔여 픽셀을 정리했다. 모든 파일에 투명 안전 여백이 있다. 게임용 PNG에는 광산 배경·슬라임·랜턴을 합치지 않았다.

### 로고 글자 검수

- 한글은 **광산던전**: 광 / 산 / 던 / 전, 네 글자다.
- 보조 영문은 **MINE DUNGEON**이다.
- 최종720×240 PNG2개를 일반 글꼴의 대조 표기와 나란히 놓고 정적으로 육안 검수했다. 받침ㅇ/ㄴ과던/전의 형태, 영문 철자를 확인했다. 자동 OCR 검수는 아니다.
- 로고 자체의 한글/영문은 의도적으로 PNG에 포함된다. 로고는 Image Type Simple로 사용하며 글자 형태가 변하므로9-Slice하거나 가로/세로를 따로 늘이지 않는다.
- A는 밝은 회갈색 암석면 때문에 어두운 광산 배경에서 읽기 쉽다. B는 구리 재질의 통일감이 강하고 더 따뜻하다. 두 안 모두 장식이 핵심 글자 획을 가리지 않도록 정리했다.

## 버튼3종과3상태의 관계

메뉴는 `게임 시작`, `설정`, `게임 종료`3종이다. **배경은 같은3상태 PNG를 공통 재사용**하고 문구만 기존 Unity 텍스트로 표시한다. 동일한 아트를 메뉴별로9개 복제하지 않았다. `Specs/Button_Bindings.json`에 세 메뉴와 공통 상태 파일의 대응을 기록했다.

버튼 PNG에는 글자·아이콘이 없다. `Previews/`에서 보이는 한글은 검토용으로만 별도 합성했다. 실제 메뉴 기능·저장/로비 진입·종료 규칙은 변경하거나 새로 구현하지 않았다. 기존 한국어 TMP 글꼴을 재사용하며 신규 폰트 파일을 제작·교체·동봉하지 않았다.

## 9-Slice와 UI 표시 시험값

- 버튼 기본 크기360×80, 중앙Pivot(0.5,0.5).
- 권장 Sprite Border **L28 / B20 / R28 / T20px**. 모서리 리벳은 고정 구역 안에 둔다.
- Image Type Sliced, Fill Center On. 가운데 목재면만 늘어나며 모서리는 고정한다.
- 세 상태를480×96으로 슬라이스 재조립해 네 모서리 픽셀이 원본과 동일함을 검사했다. `Button_9Slice_480x96_Test.png` 참조. 실제 Unity Sliced 표시 검증은 별도다.
- 문구 안전 영역은 버튼 좌상단 기준(x30,y20,w300,h40). 검토 합성은 한국어 굵은 글꼴28px·중앙정렬·크림색RGBA(228,221,200,255)이다. 실제 폰트 크기/굵기는 기존 TMP에서 확인한다.
- 눌림 미리보기에서만 글자를 아래로1px 옮겨 깊이를 표현했다. 게임용 버튼 PNG에 텍스트 변위는 들어 있지 않으며 실제 구현 여부는 후속Codex가 결정한다.
- Point / Compression None / Mipmaps Off / sRGB On / FullRect를 권장한다. UI Sprite PPU100은 표준 Canvas.referencePixelsPerUnit100과 맞출 때의 시험값이다. 기존 Canvas 설정이 다르면 먼저 확인한다. 아트 폴더만 저장했으며 Import 설정을 실제 적용하지 않았다.
- Image Tint는 흰색에서 시작한다. 상태별 PNG와 추가 Color Tint를 동시에 강하게 적용하면 원래 명도/색이 달라질 수 있으므로 후속 연결 시 중복을 확인한다. 실제 버튼 기능이나 입력 방식은 기존 것을 유지한다.

## 1920×1080 합성 배치

좌표는 화면 좌상단(0,0), 오른쪽+X, 아래쪽+Y다.

|요소|표시 사각형(x,y,w,h)|화면 중앙 기준Unity UI 중심좌표|
|---|---|---|
|로고|(120,120,720,240)|(-480,300)|
|게임 시작|(300,500,360,80)|(-480,0)|
|설정|(300,604,360,80)|(-480,-104)|
|게임 종료|(300,708,360,80)|(-480,-208)|

기존 A안 전체 합성 이미지2752×1536을 종횡비 유지 Fit로1920×1072에 표시하고 위/아래4px 여백을 둔1920×1080 검토 화면이다. 배경만 가로/세로를 다르게 늘이지 않았다. 이 프리뷰의 슬라임은 기존 A안 위치·크기와 같은 상대 배치이며 랜턴도 기존 켜짐 합성 상태를 재사용했다. 불꽃/캐릭터 애니메이션을 동시에 재생한 새 영상은 아니다.

로고는 오른쪽끝X840, 버튼은X660까지로 왼쪽에만 배치했다. **오른쪽 절반X960~1919의 모든 픽셀이 UI를 얹기 전 합성 배경과 동일**한지 대조했다. 따라서 해당 미리보기에서 입구·랜턴·광차·슬라임을 새 UI가 덮지 않는다. 실제 TitleScene의 Canvas/안전영역/해상도별 배치는 이번에 연결하거나 검증하지 않았다.

## 미리보기 파일

- `Logo_A_B_Comparison_Native.png`:720×240 로고2안과 철자 대조 표기.
- `Button_States_TextFree_and_LabelDemo.png`: 윗줄은 배경만, 아랫줄은 별도 텍스트 합성 예시.
- `Button_9Slice_480x96_Test.png`: 늘림 시험, 버튼 크기를 변경하라는 확정 지시가 아니다.
- `TitleMenu_A_1920x1080.png`, `TitleMenu_B_1920x1080.png`: 두 로고의 전체 타이틀 정지 합성.
- `TitleMenu_A_StartHovered_1920x1080.png`, `TitleMenu_A_StartPressed_1920x1080.png`: 첫 버튼의 상태 비교.
- `TitleMenu_A_B_FullScene_Comparison.png`: 두 전체 화면을 나란히 비교.
- `TitleMenu_A_Layout_Guide.png`: UI Rect와 오른쪽 보호 영역을 표시한 위치 검토용.

Previews는 검토용 합성이다. 게임에는 Logos/Buttons의 독립 PNG만 사용하고 기존 배경/랜턴/캐릭터를 별도로 유지한다. 프리뷰 텍스트를 잘라 버튼 리소스로 사용하지 않는다.

## 검증 결과와 남은 확인

- 게임용6개 PNG를 실제로 다시 읽어 RGBA·해상도·알파0/255·저장 픽셀을 확인했다.
- A 추천 후보 파일이 A 원본과 바이트 단위로 동일함을 확인했다. B 선택 시 바꿔 쓸 수 있지만 최종 디자인은 아직 미확정이다.
- 한글4글자와 영문 철자를 두 최종 PNG에서 육안 대조했다. 버튼 배경에 글자가 없는 것도 확인했다.
- 세 버튼 상태가 서로 다른 픽셀이면서 동일 외곽 알파 마스크를 갖는지 검사했다.
- 9-Slice 네 모서리의 픽셀 보존을 검사했다. 실제 Unity9-Slice·호버·눌림 입력/전환은 미검증이다.
- 1920×1080 정지 합성을 검토해 제목/버튼 가독성과 오른쪽 씬 가림 없음을 확인했다. 실제 사용자 화면의 최종 미술 평가는 사용자 검토 대상이다.
- 기존 배경·TitleLantern·TitleMinerSlime·참조 공식문서74개 파일의 SHA256이 작업 전후 동일하다. 기존 아트/.meta/GUID를 덮어쓰지 않았다.
- Unity Import·코드·씬·프리팹·버튼 이벤트·로비/저장 규칙·게임 기능은 수정하지 않았다. Git 커밋/푸시도 하지 않았다. 실제Unity Play는 범위 밖으로 미실시다.

파일별 수치·검증·보존 해시는 `Specs/Layout_and_Validation.json`, 메뉴별 공통파일 대응은 `Specs/Button_Bindings.json`에 기록했다.
'''
(O/'README_KO.md').write_text(readme,encoding='utf-8')
bindings={}
for label in ['게임 시작','설정','게임 종료']:
 bindings[label]={'backgrounds':{key:f'Buttons/TitleMenu_Button_{key}_360x80_v1.png' for key in buttons},'text_separate':True,'text':label,'new_menu_function_created':False}
(O/'Specs/Button_Bindings.json').write_text(json.dumps(bindings,ensure_ascii=False,indent=2),encoding='utf-8')
prompts='''# Built-in image_gen prompt set

## Shared logo prompt
Standalone production game title logo, transparent RGBA background, no backdrop, no plaque rectangle, no mockup. Exact main Korean text four syllables in ONE HORIZONTAL LINE: 광산던전 . It MUST read 광 산 던 전, no other Hangul, preserve all correct letter shapes and spacing, broad clear block construction. Exact smaller subtitle below: MINE DUNGEON. Straight-on 2D pixel art game UI logo, clustered crisp stepped pixels and dark near-black outline, restrained mining mood from reference background used ONLY for palette/style, no background/character included. Intended final logo720x240, wide3:1 composition, generous transparent padding around letters. Main lettering large and highly readable at screen size. No extra words, no watermark, no decorative weapons, no glow, no crystals protruding over letters, no mascot.

### A suffix
Variant A: stone-cut Korean lettering, warm grey mineral rock faces with subtle shallow facets, very restrained copper seam/inlay, dark chiseled depth, light sandstone upper bevel for legibility against dark mine. Subtitle small aged bronze, clean block sans capitals. Minimal fine cracks away from critical Hangul strokes.

### B suffix
Variant B: aged copper/bronze cast-metal Korean lettering, warm restrained brass top bevel, charcoal inset sidewalls, a few small iron rivets in solid broad letter faces, no stone slabs behind text. Distinctly simpler solid squared silhouette than stone variant, subtitle cream/desaturated bronze, no flourish.

Reference: TitleMinerSlime_v1/Previews/Title_Placement_A_2752x1536.png, for scene palette only.

## Normal button
One standalone EMPTY game menu button background sprite. Transparent RGBA outside, no text no symbols no icons. Intended final360x80, horizontal4.5:1 rectangular thin dark stained wood plank inset within restrained aged copper/bronze edge and tiny dark iron corner pins, almost-square clipped2px corners. Face dark muted brown charcoal, low contrast subtle horizontal grain, a quiet dark interior so cream Korean labels from Unity can read later.4px copper border in final360x80 logical pixels, corners decorations contained within20px. NORMAL unselected state, no glow, no highlight selection, no drop shadow outside bounds. Exact centered front-on rectangular geometry, clean uniform quiet middle rails for9-slice. Frame style/palette harmonizes reference title mine scene but no source scene included. Crisp clustered pixel art, no blurry antialiasing, no huge ornaments, no bevel taking over the label area. Fill canvas with this one long low rectangle and minimal transparent outer padding, not multiple assets or states.

## State edit shared prompt
Edit ONLY the visual state of this exact empty wooden/copper pixel-art game button. Preserve exact rectangular outline, canvas, corners, same four rivet positions, same wood-grain structures, same border thickness. [STATE] One standalone button, genuine transparent RGBA outside. NO TEXT NO ICONS NO symbols. Do not add ornaments, don't redraw geometry, don't move any part or grow/shrink. Intended export360x80 and9-slice. Keep dark mining palette and clear solid pixel edges.

Hover STATE: HOVER / highlighted state: restrained warmer brighter copper edge, a very slight warm lift in wood face, no magical glow, no outside glow. Border visibility slightly stronger than normal, center stays dark for text.

Pressed STATE: PRESSED / held down state: darker upper interior bevel and wood face, subtle inset depth, slightly subdued copper. No external displacement or scale. Keep the same visible outer frame extents; should read physically pressed rather than disabled.

Export: nearest exact-size PNGs;80-color logos/64-color buttons, binary alpha threshold160; identical outer alpha footprint across button states. Final borders measured conservatively asL28/B20/R28/T20. Prompt intended border thickness is not the same thing as the9-slice fixed corner region.
'''
(O/'Sources/GenerationPrompts.md').write_text(prompts,encoding='utf-8')
for key,p in sources.items():shutil.copyfile(p,O/'Sources'/f'Generated_{key}_Source.png')
shutil.copyfile(Path(__file__),O/'Sources/build_title_menu_v1.py')
zpath=O.parent/'TitleMenuUI_v1.zip'
with zipfile.ZipFile(zpath,'w',zipfile.ZIP_DEFLATED) as z:
 for p in sorted(O.rglob('*')):
  if p.is_file():z.write(p,Path(O.name)/p.relative_to(O))
with zipfile.ZipFile(zpath) as z:assert z.testzip() is None
print(json.dumps({'zip':str(zpath),'bytes':zpath.stat().st_size,'Hangul_English':'visually checked in final720px exports','right_half_untouched':True},ensure_ascii=False))
