# OREPLUME — TitleEnvironmentPolish_v1

## 결과

1. 원본의 흰색 별 표시만 제거한 **2752×1536 RGBA 배경**을 별도로 저장했다.
2. 기존 랜턴의 불꽃 6프레임×2종과 유리창 빛은 그대로 재사용했다.
3. 목재·암벽·바닥 표면 조명 6종과 공유 Halo 1종을 보완했다. 배경에는 조명을 합치지 않았다.

Unity Import/씬 적용/코드 변경은 하지 않았다. 게임용 신규·수정 PNG 8개(배경1+조명7), 그대로 재사용한 PNG16개(불꽃시트2+개별프레임12+유리창2), 합계 **게임용 PNG24개**다. Previews와 Sources/Specs는 게임용 리소스가 아니다.

## 확인한 실제 자료

- 사용자 원본 `C:/Users/User/Downloads/TitleScene_background.png`: 2752×1536 RGBA.
- Unity 사용본 `Assets/Art/Title/OREPLUME_v1/Background.png`: 사용자 원본과 바이트 동일.
- `Art Resource/TitleLantern_v1`: 불꽃·유리·Halo·재질별 표면 조명 및 README.
- `Art Resource/TitleMenuUI_v2`와 실제 사용 LogoA/Buttons/Slime 리소스.
- 실제 Game 캡처: `Library/TitleScreenTrial/11006c0edd7746e29a9c8f58c4c55792/title-1920x1080.png`, 파일 기록 2026-10-28 21:01경. 이번 새 Play 캡처는 아니다.
- `Assets/Scripts/TitleScreen.cs`, `Assets/Editor/TitleScreenSetup.cs`, 실제 Title 아트 README와 ProjectSettings의 Linear 색공간 설정을 읽기 전용 확인했다. 씬/프리팹을 열거나 변경하지 않았다.

사용자가 별도의 개선 배경 선택 파일을 추가한 것으로 간주하지 않았다. 이번에는 명시된 TitleScene_background.png를 기준으로 삼았다.

## 배경 마크 복원

사용 파일: `Background/TitleScene_background_NoMark_v1.png`.

- 원본 구도, 레일, 광차, 목재, 암벽, 랜턴 위치 및 해상도 유지.
- 내장 image_gen으로 우측 하단 원본 부분의 별 표시 제거·암석 복원을 수행했다. 전체 생성 이미지로 배경을 교체하지 않고 **표시 주변의 제한된 마스크 안에서만** 복원 픽셀을 반영했다.
- 실제 변경 픽셀의 외접 범위: PNG 좌상단 기준 **(2586,1370)~(2694,1478)**, 108×108px. 이 범위 내 실제 변경5177px.
- 이 범위 밖은 RGBA 픽셀 단위로 원본과 동일하다. 배경 알파는 원본과 같이 전체255다. 투명 배경 Sprite가 아니라 원래 전체 화면 배경의 대체 후보이기 때문이다.
- 복원 영역의 최대 RGB35로 흰색 표시가 남지 않음을 검사하고 확대 이미지를 검토했다. 원본 파일을 덮어쓰지 않았다.

## 사각 조명 문제와 수정

v1 파일은 캔버스 외곽 알파가 이미0이었다. 따라서 외곽1줄만 지우는 것으로 해결되지 않는다. 실제 리소스에서 목재/암벽용 **내부 다각형 마스크가 직선으로 끊기는 부분**과 넓은 주황색 반사광이 확인됐다. Linear 색공간에서 이 형태가 더 강하게 읽힐 수 있다. 현재 코드에는 표면 조명을0.35배로 줄이는 시험값이 있지만, 모양 자체의 절단선은 남는다.

이번 보완:

- 기존 재질별 리소스·위치·색상 계열을 유지하면서 내부 마스크의 경계 안쪽으로 빛이 감쇠하도록 정리.
- 두 랜턴을 중심으로 국소적인 빛 범위로 제한. 유리창에서 멀리 떨어진 표면의 주황색 면적 축소.
- 새 조명 참고 편집에서 거친 밝기 분포만 참고했다. 생성된 목재/암석/통의 RGB나 형상을 복사하지 않았다.
- 4px 단위의 기존 표면 표현을 유지하면서 낮은 단계 알파로 감쇠. 별도의 흐린 환경 이미지나 사각 배경을 레이어에 넣지 않았다.
- 표면6종의 바깥32px는 완전 투명. 알파0 픽셀의 RGB도0으로 정리했다. Halo는 원형 범위 밖과 모서리가 완전 투명하다.
- 유리창의 작은 사각 발광은 실제 유리판 내부 마스크이므로 유지했다. 넓은 환경 조명 패치와 구분한다.

|표면|v1 최대 알파|개선 최대 알파|
|---|---:|---:|
|기둥 Wood / Rock / Floor|55 / 24 / 20|35 / 4 / 10|
|통 Wood / Rock / Floor|63 / 34 / 28|33 / 11 / 15|

이 숫자만으로 시각 품질을 판정하지 않았다. 실제 캡처와 두 랜턴의 동일 구도 확대 합성을 함께 검토했다. 사각·다각형 패치와 넓은 색 덮임은 합성에서 줄었지만 실제 Unity 최종 표시까지 검증한 것은 아니다.

## 파일·배치 규격

모든 좌표는 원본2752×1536의 좌상단 원점, 오른쪽+X/아래쪽+Y다. PNG 자체에 PPU/Pivot 설정은 저장되지 않는다.

|폴더/파일|규격|배치|
|---|---|---|
|Background/TitleScene_background_NoMark_v1.png|2752×1536|원래 배경과 동일|
|SurfaceLighting/TitleLantern_Hanging_Wood/Rock/Floor_Light_polish_v1.png|각1024×1024|좌상단(1728,512), 중앙(2240,1024)|
|SurfaceLighting/TitleLantern_Barrel_Wood/Rock/Floor_Light_polish_v1.png|각1024×1024|위와 동일|
|Glow/TitleLantern_WarmHalo_384x384_polish_v1.png|384×384|기둥 중앙(2344,800), 384×384 표시. 통 중앙(2476,1078), 256×256 표시|
|Glass/기존 Hanging/Barrel GlassLight v1|64×80 / 40×48|기존 좌상단(2312,760) / (2456,1054)|
|Flames/기존 Hanging 6f 시트 및 개별 프레임|192×48, 셀32×48|밑변 앵커(2338,820), 셀앵커(16,42), Pivot(.5,.125)|
|Flames/기존 Barrel 6f 시트 및 개별 프레임|144×32, 셀24×32|밑변 앵커(2476,1090), 셀앵커(12,27), Pivot(.5,.15625)|

표면/Halo/유리창은 중앙 Pivot, FullRect 사용 권장. 투명 여백을 자동으로 잘라내면 배치가 어긋난다. Point, 압축없음, Mipmap Off, sRGB On. 기존 UI PPU/Canvas와 동일하게 적용하며 월드 환경 PPU64를 임의로 강제하지 않는다.

순서: 배경 → 표면 Wood/Rock/Floor → Halo → 유리창 빛 → 불꽃. 무기/슬라임/로고/메뉴는 기존 상태 유지. 표면 조명과 배경을 합친 Previews 파일을 배경 대신 Import하면 밝기를 독립 제어할 수 없으므로 사용하지 않는다. 새로운 Combined 파일은 제작하지 않았다.

## 밝기와 애니메이션 연동

불꽃/유리창은 원본16개 PNG를 그대로 복사했으며 바이트 동일성을 검사했다. 불꽃 자체를 다시 생성하지 않았다.

- 기둥: 6×140ms=0.84초, 통: 6×160ms=0.96초, 통 위상0.32초 유지.
- 느린 밝기 시험: 기둥2.6초, 통2.9초/위상1.1rad, 배수0.88~1.00 유지.
- 각 랜턴의 Halo·유리·표면은 **하나의 공통 밝기값**을 쓰고, 불꽃 RGB도 같은 값으로 반응시키는 구조다. 불꽃 알파는1을 유지한다.
- 선택 시험값으로 프레임별 불꽃 에너지에 따른0.96~1.04 배수를 추가했다. Specs의 `frame_light_energy_multiplier`에6개 값이 있다. 미리보기에서는 이 값도 동일 랜턴의 모든 레이어에 함께 적용했다. 이는 **후속 적용 제안이며 현재 Unity 코드를 변경한 것이 아니다**. 기존 느린 공통 밝기만 사용해도 리소스 구조는 호환된다.
- 기존 Unity의 표면0.35배를 기준으로 전후 합성을 비교했다. 새 PNG에 이0.35를 다시 구워 넣지 않았다. 후속 적용 시 PNG 알파, Image.color, Material의 중복 감쇠/증폭을 확인한다. Additive로 임의 전환하지 않는다.

## 비교 파일과 한계

- `Existing_Unity_GameCapture.png`: 기존 실제 Game 캡처 복사본.
- `Background_LightingOFF_2752x1536.png`: 마크 제거 배경 확인용.
- `Background_LightingON_Expected_2752x1536.png`: 분리 레이어를 합성한 조명 예상 화면. 게임용 배경이 아니다.
- `Lighting_OFF_ON_Comparison.png`: 동일 배경 OFF/개선ON.
- `Lighting_v1_Polish_SameSettings.png`: 동일 크기·동일 Linear 모델·표면0.35에서 v1/개선 비교.
- `Hanging_Closeup_Comparison_2x.png`, `Barrel_Closeup_Comparison_2x.png`: 두 랜턴 OFF/v1/개선 동일 구도 확대.
- `Mark_Removal_Closeup.png`: 흰색 표시 제거 전후.
- `Title_Polished_Expected_1920x1080.png`, `ActualGame_vs_PolishedExpected.png`: 기존 캡처의 로고·버튼·슬라임 픽셀을 기존 위치에 보존하고 새 배경/조명을 합성한 예상 화면. 캡처된 슬라임 경계의 일부 반투명 픽셀에는 기존 조명색이 포함될 수 있다. 원본 슬라임을 수정한 것이 아니다.
- `Lantern_Polish_2p88s.gif`:24프레임×120ms의 예상 재생. 불꽃은 실제 기존 프레임을 썼다. 서로 다른 주기의 공배수 길이가 아니므로 GIF 끝→시작은 실제 연속 재생과 같지 않다.

조명 미리보기는 sRGB 텍스처를 Linear로 변환해 알파 합성한 뒤 다시 sRGB로 표시하는 근사 모델이다. 실제 Unity의 텍스처 샘플링·Canvas 색상 처리·최종 색관리까지 픽셀 단위로 재현했다고 주장하지 않는다. 기존 실제 캡처와 새 예상 합성은 별도로 표시했다.

## 검증 및 보존

- 게임용24개 PNG의 RGBA·크기 재확인. 표면6종의 바깥32px 알파0, 투명 픽셀RGB0 확인.
- 배경 복원 범위 밖 픽셀 동일성, 원본 해상도 유지 확인.
- 불꽃/유리16파일 재사용 바이트 동일, 기존 관련162파일의 작업 전후 SHA256 동일성 확인.
- 마크 복원 확대, 전체 예상 화면, 두 랜턴 전후 확대 합성을 정적으로 검토했다.
- GIF24프레임·2.88초 디코딩 확인. **실시간 GIF 움직임 관찰과 Unity Play는 미실시**다.
- Unity 코드·씬·프리팹·공식 문서 수정, 기존 원본 덮어쓰기, Git 커밋/푸시는 하지 않았다.

최종 적용 시 사각 패치가 사라졌는지, 밝기·원근·가림·화면비가 자연스러운지 Unity에서 별도 확인한다.
