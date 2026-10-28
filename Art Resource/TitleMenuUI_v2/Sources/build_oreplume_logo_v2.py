from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import numpy as np,json,hashlib,shutil,zipfile
W=Path('C:/Users/User/.codex/visualizations/2026/10/22/01a16344-f87e-7b93-9d18-cb1d88939ccf');R=Path('D:/Git/MineDungeon2');O=R/'Art Resource/TitleMenuUI_v2';D=Path('C:/Users/User/.codex/generated_images/01a16344-f87e-7b93-9d18-cb1d88939ccf')
for sub in ['Logos','Previews','Specs','Sources']:(O/sub).mkdir(parents=True,exist_ok=True)
sourcefiles={'A':D/'exec-c87e9727-92ab-47ba-8777-2de8c7044d53.png','B':D/'exec-540f6070-c975-4940-ac0e-2af0bbc0b4f7.png'}
protected={}
for folder in ['Art Resource/TitleMenuUI_v1','Art Resource/TitleMinerSlime_v1','Art Resource/TitleLantern_v1']:
 for p in (R/folder).rglob('*'):
  if p.is_file():protected[str(p)]=hashlib.sha256(p.read_bytes()).hexdigest()
for p in [Path('C:/Users/User/Downloads/TitleScene_background.png'),R/'Docs/Systems/ArtDirection.md',R/'Docs/Systems/UISystem.md']:protected[str(p)]=hashlib.sha256(p.read_bytes()).hexdigest()
logos={};assets=[]
for key,p in sourcefiles.items():
 im=Image.open(p).convert('RGBA');a=np.array(im);a[:,:,3]=np.where(a[:,:,3]>=160,255,0);a[a[:,:,3]==0,:3]=0;im=Image.fromarray(a);im=im.crop(im.getbbox());s=min(704/im.width,224/im.height);wh=(round(im.width*s),round(im.height*s));im=im.resize(wh,Image.Resampling.NEAREST)
 out=Image.new('RGBA',(720,240));out.paste(im,((720-wh[0])//2,(240-wh[1])//2));a=np.array(out);q=out.convert('RGB').quantize(colors=80,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB');a[:,:,:3]=np.array(q);a[a[:,:,3]==0,:3]=0;out=Image.fromarray(a)
 name=f'Logos/OREPLUME_Logo_{key}_720x240_v2.png';out.save(O/name);check=Image.open(O/name);assert check.size==(720,240) and check.mode=='RGBA' and np.array_equal(np.array(out),np.array(check));assert set(np.unique(a[:,:,3]))=={0,255};assert not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any()
 logos[key]=out;assets.append(dict(file=name,size=[720,240],mode='RGBA',alpha=[0,255],bbox=list(out.getbbox()),text='OREPLUME'))
candidate='Logos/OREPLUME_Logo_Recommended_A_720x240_v2.png';shutil.copyfile(O/assets[0]['file'],O/candidate);assets.append(dict(file=candidate,size=[720,240],same_as='A',status='recommended, user design review pending'))
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
old=Image.open(R/'Art Resource/TitleMenuUI_v1/Logos/TitleLogo_A_720x240_v1.png').convert('RGBA')
compare=Image.new('RGBA',(1480,336),(21,27,29,255));d=ImageDraw.Draw(compare)
for i,key in enumerate(['A','B']):
 compare.alpha_composite(logos[key],(10+i*740,48));d.text((20+i*740,10),'A · 글자 중심 / 추천' if key=='A' else 'B · 작은 광물 장식',font=font(20),fill=(228,222,199,255));d.text((110+i*740,301),'OREPLUME  /  O R E P L U M E',font=font(20),fill=(221,214,187,255))
compare.save(O/'Previews/OREPLUME_Logo_A_B_Comparison.png')
history=Image.new('RGBA',(760,876),(21,27,29,255));d=ImageDraw.Draw(history)
for i,(label,im) in enumerate([('기존 암석형 A · 원본 보존',old),('OREPLUME A · 추천 후보',logos['A']),('OREPLUME B · 장식형 비교',logos['B'])]):
 history.alpha_composite(im,(20,40+i*288));d.text((20,7+i*288),label,font=font(21),fill=(229,221,195,255))
history.save(O/'Previews/Old_Korean_A_New_English_Comparison.png')
edgeproof=Image.new('RGBA',(760,768));d=ImageDraw.Draw(edgeproof)
for i,color in enumerate([(8,15,19,255),(73,79,77,255),(235,231,218,255)]):
 d.rectangle((0,i*256,759,(i+1)*256-1),fill=color);edgeproof.alpha_composite(logos['A'],(20,i*256+8))
edgeproof.save(O/'Previews/OREPLUME_A_Alpha_Background_Check.png')

# Use the existing v1 full scene as an immutable preview source. Replace only its logo rectangle.
v1scene=R/'Art Resource/TitleMenuUI_v1/Previews/TitleMenu_A_1920x1080.png';scene=Image.open(v1scene).convert('RGBA')
rawbg=Image.open(R/'Art Resource/TitleMinerSlime_v1/Previews/Title_Placement_A_2752x1536.png').convert('RGBA');s=min(1920/rawbg.width,1080/rawbg.height);wh=(round(rawbg.width*s),round(rawbg.height*s));base=Image.new('RGBA',(1920,1080),(12,20,23,255));base.alpha_composite(rawbg.resize(wh,Image.Resampling.NEAREST),((1920-wh[0])//2,(1080-wh[1])//2))
rect=(120,120,840,360);results={}
for key,logo in logos.items():
 out=scene.copy();out.paste(base.crop(rect),rect[:2]);out.alpha_composite(logo,rect[:2]);out.save(O/'Previews'/f'OREPLUME_Title_{key}_1920x1080.png');results[key]=out
 m=np.ones((1080,1920),bool);m[120:360,120:840]=False;assert np.array_equal(np.array(out)[m],np.array(scene)[m]);assert np.array_equal(np.array(out)[:,960:],np.array(scene)[:,960:])
both=Image.new('RGBA',(1920,578),(21,27,29,255));d=ImageDraw.Draw(both)
for i,key in enumerate(['A','B']):both.alpha_composite(results[key].resize((960,540),Image.Resampling.NEAREST),(i*960,38));d.text((16+i*960,8),'OREPLUME '+key+(' · 추천 후보' if key=='A' else ' · 비교 후보'),font=font(20),fill=(229,221,195,255))
both.save(O/'Previews/OREPLUME_FullTitle_A_B_Comparison.png')
for p,h in protected.items():assert hashlib.sha256(Path(p).read_bytes()).hexdigest()==h,p
report={'brand':'OREPLUME','letter_sequence':list('OREPLUME'),'subtitle':None,'assets':assets,'recommended':'A; not final user-approved','layout':{'screen':[1920,1080],'logo_rect_top_left':[120,120,720,240],'UI_center_position':[-480,300],'pivot':[.5,.5],'image_type':'Simple, preserve aspect','PPU_trial':100},'checks':{'RGBA_size_pixel_readback':True,'transparent_binary_alpha':True,'old_logo_and_resources_hash_unchanged':True,'outside_logo_rect_identical_to_v1_scene':True,'right_half_identical_to_v1_scene':True,'text_and_spacing_visual_review':'Final720px A/B exports visually inspected against plain-font O R E P L U M E proof. All8 uppercase letters, O/R/P counters,U/M/E distinction and optical spacing checked; not OCR. B first-O copper seam removed for clarity.','Unity_import_Play':'not performed / out of scope'},'source_reference':'TitleMenuUI_v1 stone A','generated_sources':{k:str(p) for k,p in sourcefiles.items()},'export':'nearest resize,80color palette, binary alpha threshold160; no scene text/image baked into logo','protected_sha256':protected}
(O/'Specs/OREPLUME_Logo_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'output':str(O),'asset_count':len(assets),'protected_files':len(protected),'bboxes':{k:im.getbbox() for k,im in logos.items()}},ensure_ascii=False))

readme='''# OREPLUME 타이틀 로고 v2

새 게임명 **OREPLUME**을 반영한 로고 아트다. 기존 `TitleMenuUI_v1`의 암석형 A안을 참고하여 밝은 암석면·어두운 절삭 깊이·구리색 이음새를 유지했다. 한글 로고와 이전 리소스는 보존하고 이 폴더에 신규 제작했다.

## 로고2안

- **A·추천 후보:** 넓은 대문자와 단순한 암석 실루엣으로 이름 자체의 가독성을 우선했다. 별도 장식이 없어 작은 화면에서도 덜 복잡하다.
- **B·비교 후보:** 조금 더 직립한 글자와 깊은 절삭면, 아래쪽의 작은 광물 조각을 사용했다. 첫 글자O는Q처럼 보이지 않도록 대각선 구리선을 제거해 고리 형태를 분명히 했다.

두 안 모두 한 줄 **OREPLUME**만 표시한다. 한글·부제목·별도 영문 문구는 넣지 않았다. 최종 선택은 사용자 검토 대기이며 추천 후보 A가 확정 디자인이라는 뜻은 아니다.

## 게임용 독립 PNG

|파일|실제 크기|설명|
|---|---|---|
|Logos/OREPLUME_Logo_A_720x240_v2.png|720×240|글자 중심 암석형 A|
|Logos/OREPLUME_Logo_B_720x240_v2.png|720×240|작은 광물 장식형 B|
|Logos/OREPLUME_Logo_Recommended_A_720x240_v2.png|720×240|A의 동일 복사본, 추천 파일 경로|

모든 파일은 **RGBA PNG·실제 투명 배경·알파0/255**다. 글자 안쪽의빈공간도투명하며 배경·버튼·캐릭터·랜턴은로고에포함하지않았다. 프레임안쪽투명여백을유지하여기존720×240 로고슬롯에맞췄다.

내장 image_gen으로 기존 A안의 재질을 참고해 생성하고, 최종 PNG는 최근접 리샘플링·80색 팔레트·이진 알파로 정리했다. B의 O 판독성을 보완하는 수정도 내장 도구로 수행했다. 생성 원본과 최종 프롬프트는 `Sources/`에 있다.

## 영문 철자·간격 검수

정확한 표기는 **O R E P L U M E** — 총8글자, 전부 대문자, 붙여 쓰는 한 단어다.

최종720px A/B PNG와 일반 글꼴 대조 표기를 나란히 보고 육안 검수했다. O가Q로읽히지않는지, R과P의속공간, E의세가로획, U/M의형태, 전체글자순서와간격을확인했다. 장식이나구리선이핵심획을가리지않도록검토했다. 자동OCR검사는아니다.

`Previews/OREPLUME_Logo_A_B_Comparison.png`의 아랫줄은 검수용 표기이며 게임용 로고에 들어 있는 문구가 아니다. 최종 작은 화면에서의 체감 가독성과 미술적 선택은 후속 사용자 검토 대상이다.

##1920×1080 타이틀 합성

기존 `TitleMenuUI_v1/Previews/TitleMenu_A_1920x1080.png`를 참고하고, 기존 로고 영역만 원래 배경으로 복원한 뒤 새 로고를 얹었다. 전체 배경을 재생성하거나 기존 메뉴를 다시 그리지 않았다. 광부 슬라임은 기존 A안이고 랜턴은 기존 켜짐 상태다. 이번에는 정지 합성만 제공한다.

- 로고 사각형: 좌상단기준 **(x120,y120,w720,h240)**.
- 화면 중앙기준 UI 중심좌표: **(-480,300)**, Pivot(0.5,0.5).
- 기존 게임 시작·설정·게임 종료 버튼은 위치·텍스트·아트 모두 그대로다.
- 로고 사각형 바깥의 모든 픽셀이 기존 v1 합성과 동일함을 검사했다.
- 오른쪽 절반X960~1919도 픽셀 단위로 동일하다. 새 로고가 광산 입구·광차·슬라임·랜턴을 가리지 않는다.

720×240은 투명 패딩을 포함한 캔버스다. A의 가시영역은(8,70)~(712,170), B는(8,36)~(712,204)이며 B의 아래쪽 장식 때문에 높이가 다르다. 자동 Tight Crop으로 이미지 크기를 줄이면 같은배치값에서정렬이달라질수있으므로캔버스를그대로유지한다.

## Unity 적용 제안

- 로고는 UI Image **Simple / Preserve Aspect**, 중앙Pivot 권장. 글자형이찌그러지므로9-Slice나비균등확대를하지않는다.
- Point / Compression None / Mipmaps Off / sRGB On / FullRect 권장.
- UI Sprite PPU100은 Canvas.referencePixelsPerUnit100에맞출때의시험값이다. 실제기존Canvas설정을우선확인한다. 이번에는Import/.meta를만들거나수정하지않았다.
- Image Tint는흰색에서시작해원래암석·구리색이중복착색되지않도록한다.
- 사용자가 A/B를 선택한 뒤 후속Codex가 새로고PNG만연결한다. 메뉴버튼·배경·캐릭터·랜턴의리소스나기능은변경할필요가없다.

## 비교·미리보기

- `OREPLUME_Logo_A_B_Comparison.png`: 신규2안 원배율 비교와 철자 대조.
- `Old_Korean_A_New_English_Comparison.png`: 기존 한글A와 새 영문2안을 나란히 비교.
- `OREPLUME_A_Alpha_Background_Check.png`: 검정·중간회색·밝은배경에서 A의 투명가장자리 확인.
- `OREPLUME_Title_A_1920x1080.png`, `OREPLUME_Title_B_1920x1080.png`: 전체타이틀 합성2종.
- `OREPLUME_FullTitle_A_B_Comparison.png`: 전체화면 나란히 비교.

배경과합쳐진이미지는 `Previews/`의검토용이다. 실제사용PNG는 `Logos/`의투명파일로분리했다. 기존메뉴PNG를이폴더에복제하거나새버튼을만들지않았다.

## 실제 검증과 미검증

- 신규로고PNG의720×240/RGBA/알파0·255/안전여백을다시읽어검사했다.
- 추천A파일과A원본이동일한바이트임을확인했다.
- 최종PNG에서8개대문자와철자·광학적간격을정적으로육안검토했다.
- 전체타이틀합성에서로고영역밖의픽셀과오른쪽영역이기존과정확히동일하다.
- 기존TitleMenuUI_v1·TitleMinerSlime_v1·TitleLantern_v1·배경·참조공식문서99개파일의SHA256이작업전후동일하다.
- UnityImport/실시간표시/해상도별UI배치/Play는미검증이며이번범위밖이다.
- Unity코드·프리팹·씬·공식문서를수정하지않았다. 기존한글로고를삭제/덮어쓰지않았으며Git커밋·푸시도하지않았다.

세부파일·배치·검증·보존해시는 `Specs/OREPLUME_Logo_Validation.json`에기록했다.
'''.replace('##1920','## 1920')
(O/'README_KO.md').write_text(readme,encoding='utf-8')
prompts='''# OREPLUME logo — built-in image_gen

## Shared prompt (reference: existing TitleMenuUI_v1 stone A PNG)
Create a standalone transparent RGBA game title logo, derived from this exact approved reference's rock-and-copper PIXEL ART style. REPLACE ALL existing Korean and subtitle lettering. Only exact text: OREPLUME (eight uppercase letters O R E P L U M E, in this order, ONE WORD, ONE HORIZONTAL LINE). NO Korean, NO MINE DUNGEON, NO PICK & DEEP, NO ampersand, NO subtitle, NO extra lettering. Warm grey stone carved block letters, pale sandstone top bevels, charcoal dark stone extrusion and solid dark outline, fine muted copper seams, crisp stepped pixel clusters. Clear counters in O,R,P; E must have three horizontal arms; U distinguishable from V; M distinguishable from N. Consistent optical letter spacing, no joined letters or obscured strokes. Transparent inside counters and outside logo, no backplate or scene. Intended final960x240 wide4:1 art, fitting screen top-left at720px wide on a dark mine title. Entire logo safely inside canvas. Restrained no magic glow, no flames, no giant decorative weapons.

## A suffix
Variant A: maximum legibility and faithful reuse of reference A material, broad squared monolithic capitals, quiet shallow angular rock facets, a few fine copper seams not crossing critical apertures. Letters themselves are the entire mark, no separate ornament.

## B suffix
Variant B: slightly more upright compact stone capitals, stronger chiseled depth, restrained copper base-edge trim, one tiny copper/grey faceted mineral-chip emblem centered BELOW the word with two very short copper ticks flanking it. Ornament much smaller than a letter and outside all strokes; word remains dominant. Same existing A stone colors, not all-gold metal letters.

## Final B correction (target: B generated logo)
Precise lettering legibility correction of this same transparent stone/copper logo. Exact word OREPLUME, eight uppercase letters O R E P L U M E. Keep the current word, same style, stone colors, dark extrusion and small copper ore emblem underneath. Critical correction: first letter must be a completely unmistakable O, never Q: remove the diagonal copper seam from the FIRST LETTER O entirely and give O a clean continuous rounded/octagonal ring, clear centered enclosed counter and no diagonal leg/tail/crossbar inside or outside. Keep copper seams in other letters. Increase consistent small transparent spacing between ALL letters, so adjacent letters' main strokes never merge; do not insert spaces into the word or change alignment. Especially P-L and L-U and U-M need a little optical breathing room. Single horizontal word, no subtitle, no other words, no Korean, no ampersand. Entire isolated mark on true transparent RGBA, crisp clustered pixel art, no blurry antialiasing, no extra ornaments.

Final export uses720x240 to retain existing UI slot. Generated aspect is fitted uniformly inside704x224, no glyph distortion. Nearest sampling,80-color palette, binary alpha160 threshold. Raw sources remain alongside these prompts; preview labels are not baked into runtime logos.
'''
(O/'Sources/GenerationPrompts.md').write_text(prompts,encoding='utf-8')
for key,p in sourcefiles.items():shutil.copyfile(p,O/'Sources'/f'OREPLUME_Generated_{key}_Source.png')
shutil.copyfile(Path(__file__),O/'Sources/build_oreplume_logo_v2.py')
zpath=O.parent/'TitleMenuUI_v2.zip'
with zipfile.ZipFile(zpath,'w',zipfile.ZIP_DEFLATED) as z:
 for p in sorted(O.rglob('*')):
  if p.is_file():z.write(p,Path(O.name)/p.relative_to(O))
with zipfile.ZipFile(zpath) as z:assert z.testzip() is None
print(json.dumps({'zip':str(zpath),'bytes':zpath.stat().st_size,'old_assets_preserved':len(protected)},ensure_ascii=False))
