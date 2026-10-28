# MineBoundary_v1 — 기존 아트 분석·재사용 제안

## 결론

**이번 신규 게임용 PNG는0개다.** 기존 MineEnvironment_v1에 방향별 경계·내외측 코너·끝 캡·그림자·잔돌이 이미 있고, 현재Map01에서 일부 유형은 사용되지 않는다. 새 그림을 추가하기 전에 **현재 자산의 경계 샘플링·겹침·배치 방식부터 보완하는 것이 우선**이라고 판단했다.

기존 리소스만 사용한 Map01 예상 합성을 만들었다. 동일한 돌이 줄지어 보이는 인상을 일부 완화할 수 있는 구성을 확인했지만, 실제 Unity 화면의 최종 자연스러움이 검증되거나 문제가 완전히 해결됐다고 주장하지 않는다. 신규 원본 PNG를 억지로 만들지 않았고 기존 PNG를 수정/재내보내기하지 않았다.

## 실제로 확인한 자료

|자료|확인 방법·범위|
|---|---|
|Art Resource/MineEnvironment_v1|게임용50개 PNG·README·Manifest·조합 예시를 확인|
|Assets/Art/Environment/MineEnvironment|실제 Import된50개 PNG 및meta 확인. 원본50개와 SHA256 모두 동일|
|Map_01.prefab|Unity MCP로 읽기 전용 조회. 바닥14영역·암석230영역, 환경메시14개와 환경Sprite 사용 목록 확인|
|실제 Map01 화면|Library/Map01RefinementVerification/tone-100.png. 파일시각2026-10-26 21:52경, 기존검증Game캡처. 이번새Play캡처가아님|
|MapSystem/ArtDirection|일반맵 환경 아트·이동영역/충돌 보존·확정/미확정규격 관련 항목 확인|
|기존 배치 기록|Library/Map01ArtVerification/build_plan.py, Map01RefinementVerification/build_plan.py를 읽어0.25유닛메시/UV반복과표면혼합방식을 대조|

현재 Unity Editor에서는 프리팹과 Import정보만 읽었다. LiveMap01 새화면, 실제전투/길찾기, 변경후Play는 확인하지 않았다. 과거 캡처에 있는 출구/HUD/몬스터상태를 현재 최신 상태라고 확대 해석하지 않는다.

## 색상·픽셀밀도·시점 분석

- 현재 환경PNG50개의meta에서 **PPU64·Point·MipmapsOff**를 확인했다. 대표Rim/InnerCorner/Wall_Protruding3종은 Unity Importer에서Uncompressed와StandaloneOverride없음도확인했다.
- 바닥/벽 표면64×64는1×1유닛, Rim/Corner/Cap32×32는0.5×0.5유닛이다. 좁은 벽16×64는0.25×1유닛이다. 실제Map01메시Scale은(1,1,1)이다.
- 메시는0.25유닛 격자로 잘라 원래픽셀크기의UV를사용한다. 경계는32px이미지의반쪽단위를반복하므로같은돌의위상/간격이연속으로눈에띄기쉽다.
- South Rim의실제불투명bbox는(0,7)~(32,24), 즉세로17px다. East는(7,0)~(24,32)로폭17px다. **32px캔버스높이전체가벽높이라는뜻은아니다.**64PPU에서보이는돌띠두께는약0.266유닛이다. 실제3D암벽높이를확인한것은아니며,2D탑뷰/3⁄4암면과둘레음영으로높이를암시한다.
- Wall_RockMass 주요색: #57584F, #676244, #747064. Floor_RockGray 주요색: #545147, #514E44, #4E4B41. 탁한회갈색/올리브암면과청회색그림자,어두운틈이기준이다.새팔레트를도입하지않았다.
- Wall_Protruding/Fractured/AgedRock64×64는불투명반복표면이다. 자유로운투명바위소품이아니므로그대로바닥위에Sprite로붙이면네모가보인다. **기존암석영역안에클리핑하고표면패치가중치를완만하게줄이는방식**으로사용해야한다.

## 현재Map01 연결 방식과 원인

환경메시14개는 바닥4종, Wall_RockMass1종, 방향별Rim4종, OuterCorner4종, WallFoot_Shadow1종이다. 실제조회 결과는 `Specs/Unity_ReadOnly_Evidence.json`에 있다.

- 현재표시순서:바닥-50~-47,접점그림자-40,벽-30,Rim-20,OuterCorner-19.장식은별도Sprite다.기존렌더링그룹안에서작업할수있으며새Light2D가필요하지않다.
- 작은돌이띠모양인Rim을같은위상으로연속사용하고,벽내부는Wall_RockMass한종으로반복한다. 자료와실제캡처에서울타리같은일정한줄무늬가읽힌다.
- 기존초기배치코드는Corner도0.25유닛쿼드에잘라사용한다.32px전체코너를놓은것과다르므로UV상태에따라일부조각만보이는지후속배치단계에서확인해야한다.
- **CornerInner4종,RockEndCap,Wall_Protruding/Fractured/AgedRock/NarrowStrip,RockSpine**는현재조회한Map01환경메시/활성환경Sprite사용목록에없다. 존재하지않는아트가아니라아직활용되지않은재료다.
- 기존Pebbles_Small은14개Sprite,EmbeddedGravel은5개Sprite로이미사용중이다. 따라서잔돌종류자체가부족하다고판단하지않았다.

## 재사용할 파일과 방법

아래 상대경로는 두 루트에 모두 존재한다.

- 제작원본루트: `D:/Git/MineDungeon2/Art Resource/MineEnvironment_v1/`
- Unity사용루트: `D:/Git/MineDungeon2/Assets/Art/Environment/MineEnvironment/`

|기존파일|재사용 제안|
|---|---|
|Borders/RockRim_South/East/North/West_32x32.png|기존직선경계유지.긴반복구간만위상과겹침을분산하고작은돌의대비를일부낮추는시험|
|Borders/CornerOuter_NE/NW/SE/SW_32x32.png|이미존재·일부사용.쿼드반복보다실제코너에전체32px모듈을맞춘뒤벽합집합으로클리핑검토|
|Borders/CornerInner_NE/NW/SE/SW_32x32.png|현재미사용.들어가는코너/통로접점의일정한직각이음을완화|
|Borders/RockEndCap_32x32.png|현재미사용.긴Rim의일부를불규칙한돌군집으로끊고이음새를겹쳐마감|
|Borders/WallFoot_Shadow_32x16.png|기존알파110음영.좁은접점만부드럽게연결하며진한새검은띠를만들지않음|
|Walls/Wall_Protruding_64x64.png / Wall_Fractured_64x64.png / Wall_AgedRock_64x64.png|벽내부반복깨기.불투명사각타일이므로벽마스크안에서표면가중치/겹침처리|
|Walls/Wall_NarrowStrip_16x64.png / RockSpine_Narrow_16x64.png|좁은암석조각에맞는폭.통행공간위로돌출하는전경장식으로쓰지않음|
|Rubble/Pebbles_Small_24x24.png / RockShards_Broken_32x32.png / EmbeddedGravel_32x32.png|기존잔돌재사용.벽가까이에만소량,새장애물처럼보이지않도록낮은대비|
|Floors/Decal_FloorCrack_64x64.png / Decal_WornPatch_64x48.png|바닥질감이끊기는곳의낮은대비보조.중앙전투공간에과도하게추가하지않음|

50개전체경로·실제크기·현재Map01사용여부는 `Specs/Existing_50_Asset_Catalog.csv`에있다.

## 신규제작 필요성 판정

|후보|판정·이유|
|---|---|
|내측/외측코너|신규불필요.각4종이이미있음|
|접점그림자|신규불필요.WallFoot_Shadow가있음|
|잔돌/흙/바닥전이|신규불필요.잔돌4종과낮은대비바닥데칼이있음|
|끝부분연결보정|신규불필요.RockEndCap과좁은Spine재사용가능|
|긴불규칙경계변형|현재신규제작보류.다른길이의비주기Rim이향후도움될수있지만,기존미사용자산과코너/UV배치수정효과를확인하기전필수부족분으로확정할수없음|

새RimPNG를현재와똑같이균일반복해도같은문제가남을수있다. 먼저재사용배치가실제카메라에서충분한지검토하고,그이후에도같은17px돌띠가반복되어보일때에만비주기경계변형을제작하는것이합리적이다.

## Map01 예상 합성·시험배치

현재프리팹에서읽은floorAreas14개/rockAreas230개를같은64px/유닛마스크로만들고,기존PNG픽셀만합성했다. 충돌/이동마스크는전후동일하다. 전체Map01형태는유지하며중앙공동의서측/동측을집중검토했다.

시험CSV에는 **26곳**이있다:벽표면혼합4,끝캡10,내측코너8,작은자갈4.값은제작검토용으로실제Unity배치명령이아니다.좌표는Map01로컬유닛이며PPU64다.코너방향은바닥사분면을기준으로추정했으므로실제그림을겹쳐보고최종방향을확인한다.

- 기존Rim은긴축방향으로만0/8/16px위상을분산하고알파0.62를시험했다.직선연결을통째로없애지않았다.이값은확정색보정이아니며원본PNG를바꾼것도아니다.
- 위상변경시UV가1을넘도록그대로보내면현재Clamp샘플러에서문제가될수있다.같은이미지의0~1구간으로나눠샘플링/클리핑하고Cap으로이음을정리하는방식을후속Codex가검토한다.원본Wrap/.meta를임의로변경하지않는다.
- 불투명벽패치는원본암석마스크안에서만겹치고가장자리가중치를낮춘다.끝캡/코너도바닥으로새벽처럼튀어나오지않도록벽안으로자른다.
- 자갈4곳은바닥쪽의낮은대비시각흔적이다.콜라이더/내비게이션/파괴오브젝트를추가하지않는다.
- 기존Renderer/메시배칭·Sorting을재사용할수있는구성이며새매니저나Light2D를만들필요가없다.

**미리보기의한계:** 좌측그림은기존Rim의균일반복을보여주는오프라인조합시험이지현재14개메시를정확히복원한Game화면이아니다.기존OuterCorner조각전체·바닥feather색·배우·장식·HUD·출구는정밀복원하지않았다. 우측도Unity적용후스크린샷이아니다.실제기존Game캡처는별도로보존했다.따라서이이미지를실제성능/충돌/전투가독성통과증거로쓰지않는다.

## 결과파일 안내

|파일|내용|
|---|---|
|Previews/Actual_Map01_Capture_Analysis.png|기존실제캡처에반복띠/코너관찰구간표시|
|Previews/Existing_Reuse_Assets_3x.png|기존PNG재사용후보12종의3배확대모음|
|Previews/Map01_Reuse_Before_After_Expected.png|동일Map01마스크위의균일반복시험/재사용예상구성비교|
|Previews/Map01_Offline_Before_Overview.png|기존자산균일반복의전체Map01오프라인검토|
|Previews/Map01_Offline_ReuseProposal_Overview.png|재사용제안전체Map01검토|
|Previews/Map01_Unchanged_Geometry_Mask.png|동일한바닥/암석마스크확인용|
|Specs/Unity_ReadOnly_Evidence.json|현재프리팹메시·Sprite사용목록·대표Importer읽기결과|
|Specs/Existing_50_Asset_Catalog.csv|기존50PNG실제경로·규격·사용여부|
|Specs/Reuse_Placement_Trial.csv|Map01시각시험26곳의좌표/자산/마스크/알파제안|
|Specs/Reuse_Analysis_and_Validation.json|보존해시·분석·실제검증/미검증범위|
|Sources/Map01_ActualCapture_2026-10-26.png|기존Game캡처복사본|

이폴더의PNG는모두**분석/미리보기또는기존캡처복사본**이다. 새게임용스프라이트는0개이며Previews를Unity에새맵배경으로Import하지않는다.기존50PNG는프로젝트경로에서그대로재사용한다.이ZIP은신규아트팩이아닌재사용분석/전달팩이다.

## 보존·검증·미검증

- 기존50개제작PNG와Unity사용PNG50개가바이트단위로동일함을검사했다.기존PNG/meta·Map01프리팹/관련스크립트/공식문서/참조캡처총157파일의작업전후해시가같다.
- 신규PNG재작화/기존원본수정/Unity코드·씬·프리팹·Collider·이동영역변경을하지않았다. Git커밋·푸시도없다.
- 기존실제캡처·모듈모음·동일마스크의정적합성을직접검토했다.기존색상과픽셀밀도를그대로쓸수있는구성은확인했다.
- 새LiveMap01캡처·Unity적용·플레이어/몬스터/VFX가림·길찾기·충돌·출구회귀는미실시다.이번에는환경을실제수정하지않았으므로변경후Play통과로보고하지않는다.
- 여전히긴격자형윤곽과기본암벽표면반복은남는다.재사용안으로완전히해결됐다고보지않으며후속Map01실화면검토후추가아트필요여부를판정한다.

이번완료범위는**기존아트분석과재사용가능구성/시험좌표/예상합성제공**까지다.
