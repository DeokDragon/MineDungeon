진행 상황과 남은 작업 갱신 작성

## 플레이어 액티브 스킬 — 구현 완료
- 프로젝트: D:/Git/MineDungeon2, Unity 6000.3.12f1, TestRoom.
- SkillData 4개: EarthSmash / DrillDash / TealBeam / OreBlast. 기본 피해 19 / 14 / 16 / 19.
- PlayerSkills 및 PlayerSkillsMotion: E/R 단발 입력, E 우선, 기존 행동 상태 기반 제한, 독립 쿨타임, 피해 스냅샷, EnemyHealth 단위 중복 제거, 벽 검사.
- PlayerSkillAttackPower 기본 물리 7 / 마법 6. 기존 평타 10은 변경하지 않음.
- PlayerSkillVisual 프리팹·재질 및 PlayerSkillHUD와 한글 폰트 추가. 기존 Canvas 하단 중앙에 두 슬롯 저장. 물약 HUD 원래 위치 보존.
- 이동/공격/물약에는 스킬 상태 확인만 추가. DungeonRoomManager에서 방 교체 직전 스킬 취소.
- 사망/비활성화/결과 상태에서 소유 효과·예약 피해·행동 제한 정리. 방 이동/부활 시 쿨타임 유지, 재도전 씬 재로드 시 초기화.
- Unity MCP로 스킬 데이터, 효과 프리팹, Player 컴포넌트, Canvas 참조, TestRoom 저장 완료.

### 실제 검증
- 첫 두 스킬 1차 22개 통과 후 나머지 두 스킬을 추가함.
- 통합 플레이 검증 48개 및 경계/결과 연동 12개, 총 60개 통과.
- 검증: 피해 공식/시전 시작 스냅샷, 다중 Collider 중복 제거, 벽·폭·거리 제한, 광선 단발 피해와 이동 중 고정 방향, 돌격 벽 충돌/종료 겹침 복구/충돌 무시 복구, 발파 1초 순서.
- 검증: E/R 동시 입력 E 우선, 홀드 반복 없음, 평타/회피/물약과 상호 제한, 일시정지, 일반 피격 지속, 사망·부활·방 전환 예약 공격 취소, 실패/클리어 재도전.
- 검증: 스킬 보스 타격과 HP바, 보스 처치 시 기존 0.7초 결과 연출 및 HUD 숨김 유지.
- 분쇄·광선 효과와 하단 HUD는 캡처로 확인. 캡처는 Temp/PlayerSkillVerification에 있으며 Assets에 포함하지 않음.
- 임시 SkillVerification 스크립트 제거. 런타임 배치/장비/시간/오디오 테스트 변경은 복구함.

### 유지/Inspector 위치
- Player > Player Skills: E Skill=EarthSmash, R Skill=TealBeam. Play Mode 전에 서로 다른 두 SkillData로 변경.
- Player > Player Skill Attack Power: 물리/마법 임시 공격력.
- Assets/Skills/*.asset: 계수/쿨타임/선딜/범위/시간. Assets/Prefabs/PlayerSkills/PlayerSkillVisual.prefab: 효과 재질/스프라이트.
- Canvas > PlayerSkillSlots: HUD 배치와 슬롯 참조.
- 일반 방 3개 → 보스 방 1개, 보스 수치/5패턴/HP바 숨김 시점, 기존 UI와 피격 효과는 변경하지 않음.

### 사용자 확인이 필요한 항목
- 실키보드/마우스 조작의 손맛, 실제 전투 중 효과·HUD 가독성 및 다른 화면 비율.
- 기존 보스 5패턴 전체를 상대하면서 네 스킬을 사용한 장시간 플레이는 자동 검증 범위 밖.
- 극단적으로 많은 이동 중 적이 돌진의 전체 지나온 경로를 동시에 점유하는 밀집 상황은 미검증.

### 최종 정적 확인
- 컴파일 오류·콘솔 오류/경고 없음. TestRoom Missing Script 0, 모든 스킬/HUD/효과 참조 연결 확인.
- 씬 저장 완료(Dirty=false), E 분쇄/R 광선, 물리7/마법6, 일반 방 수3 확인.
- 테스트용 백그라운드 실행 설정은 원래 값(false)으로 복구.


## 2026-10-18 인벤토리 구현 완료
- 기존 변경은 WORK_STATUS.md만 있었음. 스킬/전투 수치 및 원래 UI 참조 보존.
- 세션 보유 목록/장착 서비스, 준비 상태 시작 게이트, 데이터 기반 정렬/툴팁 구현 및 UI/프리팹/씬 연결 저장 완료.
- 입력·일시정지, 준비→전투→재도전 및 장착 유지의 1차 통합 검증 완료. 추가 검증과 임시 테스트 코드 정리가 남음.

### 인벤토리 중간 검증
- UI/카드 프리팹/폰트/씬 저장 완료. 52개 통합 검증 중 51개 통과. I 문자 검증은 InputSystem QueueTextEvent가 TMP의 Event 문자 경로에 전달되지 않아 실패했으며, TMP ProcessEvent와 실제 I 키 상태 이벤트를 함께 전달해 창 유지·문자 입력 확인.
- 추가 확인: 툴팁 갱신, 드롭다운, 스크롤; 검증 코드 정리 및 최종 저장 예정.

### 재개 지점 / Unity MCP 도구 연결 대기
- 2026-10-18 재확인: 사용자가 MCP 재활성화를 알렸으나 현재 대화의 호출 가능한 도구 및 리소스 템플릿에 Unity MCP가 노출되지 않음. 새 플레이 테스트/씬 재저장은 실행하지 않았음.
- 마지막 InventoryUI 변경은 플레이어 사망 시 열린 인벤토리를 닫아 인벤토리 소유 정지를 해제하는 처리. 이 변경 이후 플레이 검증은 미실행.
- Assembly-CSharp.dll 수정 시각 01:23:59, InventoryUI.cs 및 InventoryVerification.cs 수정 시각 01:23:54. 현재 Editor.log에서 C# 컴파일 오류/컴파일 실패 문구 없음. 라이브 Console 및 Missing Reference 최종 확인은 남아 있음.
- Git 변경 검사에서 Unity 직렬화의 빈 필드 뒤 공백 외 오류 없음. 씬을 직접 텍스트 수정해 공백을 정리하지 않았음.
- Assets/InventoryVerification.cs는 UNITY_EDITOR 전용 임시 검증 코드이며 후속 검증을 위해 보존. 툴팁 장착/능력치 갱신, 정지 중 드롭다운, 좁은 뷰포트 스크롤 검증을 추가했지만 추가분은 아직 실행하지 않았음. 최종 실행 후 이 스크립트와 .meta를 제거할 것.
- 이어서: Unity MCP로 TestRoom/편집 상태 확인 → 최신 검증 실행 → Console/Missing Reference 확인 → 툴팁·드롭다운 시각 확인 → 임시 검증 코드 제거 → 기존 기본 E 분쇄/R 광선 및 일반 방 수 3 확인 → 저장.
- 실제 키보드 한글 입력과 다른 화면 비율의 가독성은 사용자 확인 필요.

### 인벤토리 사용 / 설정 위치
- I로 열기/닫기, ESC 또는 ×로 닫기. 준비 중 카드 선택 후 왼쪽 E/R 클릭으로 장착·교환, 던전 시작 버튼으로 시작. 도전 중 조회만 가능하며 창을 열면 시간 정지.
- TestRoom의 DungeonRunManager > SkillInventorySession > Test Starting Skills: 테스트 보유 4종과 최초 등록 순서.
- Player > PlayerSkills: 최초 세션 기본 장착. 이후 방 이동·재도전은 세션의 선택을 유지.
- Canvas > InventoryUI: UI 참조 및 툴팁 지연. Assets/Prefabs/Inventory/InventorySkillCard.prefab: 카드 표현. SkillData의 rarity: 표시 희귀도.

### 최종 검증 및 정리 (위 연결 대기 기록 이후 완료)
- Unity 재시작 후 MCP 연결 복구. MineDungeon2 / D:/Git/MineDungeon2 / Unity 6000.3.12f1 / TestRoom 확인.
- 최신 통합 플레이 검증 57개 모두 PASS. 검색/분류/정렬/툴팁/장착/교환, E/R 실행 연결, 전투 장착 금지, 입력 누출 방지, 시간·오디오·쿨타임 정지, 방 이동, 실패·클리어 재도전 두 차례와 중복 생성 방지 검증 포함.
- 추가 실험: 전투 중 열린 인벤토리에서 외부 피해로 사망 시 창 닫힘, timeScale 1 및 오디오 복원, 이후 정상 부활 확인.
- 툴팁과 정렬 드롭다운을 1280×720 실제 화면 캡처로 확인. Temp/InventoryVerification/final-tooltip.png 및 final-dropdown.png. 중간 ScreenCapture 요청 1회 저장 실패 후 MCP 캡처로 성공.
- 임시 Assets/InventoryVerification.cs 및 .meta 제거. 기본 E EarthSmash / R TealBeam, 일반 방 수 3 확인. 테스트의 공격력 변경은 7로 복구, Play Mode 종료로 런타임 장착·뷰포트·방 변경 복구.
- Time.timeScale 1, AudioListener.pause false, runInBackground false 복구. 테스트로 생긴 TimeManager 직렬화 변경도 원본 복구.
- TestRoom 저장 완료, Dirty=false. 씬 Missing Script 0 및 끊어진 직렬화 참조 0. 최종 코드 정리에서 TMP의 폐기 예정 enableWordWrapping 사용을 textWrappingMode로 교체.
- 위 정리 이후 Unity 컴파일 완료 확인, 최종 Console 오류·경고 0.
- 사용자 확인: 실제 한글 IME/키보드·마우스 조작, 다른 해상도·화면 비율의 가독성, 기존 보스 5패턴 전체와 장시간 전투 회귀. I 입력 검증은 InputSystem 키 상태와 TMP ProcessEvent를 사용한 자동 검증임.

## 2026-10-18 경험치·레벨 / 플레이어 HUD 완료
- 작업 시작 시 Git 변경은 사용자 WORK_STATUS.md만 있었음. 기존 인벤토리 구현은 커밋된 상태.
- LevelProgressionData: 임시 최대 레벨 5, 필요 경험치 100/150/200/250. 잘못된/누락된 표는 마지막 유효 구간까지만 진행. 음수 거부, 0 무변화, long 중간 계산으로 오버플로 방어, 최대 레벨 초과분 버림.
- PlayerProgression: 기존 DungeonRunManager 오브젝트에 연결, 새 싱글톤 없음. 레벨/현재 경험치/미사용 포인트는 실행 세션 동안 유지. BeginNewRun에서 초기화하지 않음. 향후 보상은 GrantExperience(int) 사용.
- DungeonRunManager: RemainingLives 및 ResourcesChanged 추가. 실제 maxRevives=2 유지. 최종 실패는 생명 0, 부활 대기 중에도 사망 확정 직후 부적 갱신.
- PlayerHUD: 좌상단 상태 패널, 기존 HP Slider/텍스트 재사용, 얇은 EXP 바, 청록/회색 부적 3개, 레벨업 합산 알림(1.5초 유지+0.3초 페이드). 기존 플레이어 HP 잔상은 원래 없었음.
- 기존 PlayerSkillHUD E/R 슬롯과 PlayerHUD 물약 텍스트를 재배치하여 Q/E/R 통일. 스킬·물약 실행 코드는 수정하지 않음. 기존 Revives 텍스트는 비활성화하여 중복 제거. 보스/결과/인벤토리 연결 유지.
- InventoryUI 능력치 영역에 실제 레벨과 미사용 스탯 포인트 읽기 전용 표시 추가.
- TestRoom 씬 연결·저장 완료. 기본 E EarthSmash/R TealBeam, 일반 방 3개+보스 방 유지. Progression Korean 폰트는 기존 사용 OS 폰트를 같은 방식으로 정적 부분집합 생성; 외부 다운로드 없음.

### 실제 검증
- 최신 통합 플레이 검증 37개 모두 PASS. 99+1, 초기260, MAX 추가포인트 없음, int.MaxValue 합산, 잘못된 표, 0/음수, 자원/쿨타임 무회복, 방 이동/부활/게임오버/씬 재로드 재도전의 성장 유지 포함.
- 3→2→1→0 및 실제 부활 2회, 중복 사망 피해 무소모, 대기 중 부적 즉시 반영, 새 도전 생명3/물약3/쿨타임 초기화 확인.
- PauseMenu 정지 직후 같은 프레임의 알림 타이머 진행을 발견하여 수정 후 전체 재검증 통과. 인벤토리 일시정지, 알림 합산/페이드/결과 시 즉시 정리 확인.
- 별도 MCP 입력 검증: Q로 HP50→85, 물약3→2 및 HUD 2/3 반영. 준비 중 E/R 교환 후 HUD 이름/실제 E 광선 쿨타임 확인. R 슬롯 실행 API로 광선 시전/쿨타임7 확인.
- 1280×720 화면에서 최종 HP/EXP/부적/QER 배치 확인. 캡처: Temp/ProgressionVerification/hud-final-1.png, MAX 이전 캡처 hud-max.png.
- 최종 컴파일 완료, Missing Script 0, 끊어진 직렬화 참조 0, Scene Dirty=false. 게임 코드 오류 없음. 재컴파일 중 MCP WebSocket 초기화 경고 1건은 도구 연결 경고이며 이후 호출 정상.
- 임시 ProgressionVerification.cs 및 meta 삭제. Play Mode 종료로 테스트 진행/장착/자원 변경 폐기. 실제 사용자의 영속 데이터 초기화 함수는 추가/호출하지 않음. runInBackground=false, timeScale=1, AudioListener.pause=false 복구. 테스트 중 생긴 TimeManager 직렬화 변경 원복.

### 조정 / 개발 경험치 지급
- Assets/Skills/LevelProgression.asset: Test Maximum Level과 Experience To Next Level 배열. 레벨5는 임시 테스트 상한.
- Play Mode에서 살아남은 DungeonRunManager > PlayerProgression > Debug Experience 값 설정 후 컴포넌트 Context Menu의 '개발용/설정한 경험치 지급 (Play Mode)' 실행. 기본260. 일반 조작키 치트 없음.
- Canvas > PlayerHUD: 부적 색, 알림 유지/페이드 시간, UI 참조. Canvas/PlayerStatusPanel, PlayerSkillSlots에서 배치 조정.
- 성장 상태는 SO 원본에 기록하지 않으며 앱 종료 후 저장 기능은 추가하지 않았음. 확인한 기존 인벤토리/도전 관리에도 디스크 저장 호출 없음.

### 사용자 확인이 필요한 항목
- 다른 해상도/울트라와이드/좁은 화면에서의 가독성과 화면 이탈은 미검증(기존 CanvasScaler 및 비율 앵커 사용).
- 실제 키보드·마우스로 Q/E/R 사용감 및 장시간 보스 5패턴 전투 회귀는 사용자 확인 필요.

## 2026-10-18 몬스터 보상·돈·재료·획득 알림 완료
- 작업 시작 시 이전 경험치/HUD 작업의 미커밋 변경이 있었으며 보존함. 기존 보상·재료·돈 시스템은 없었음.
- 새 MaterialData(고유 ID/이름/아이콘/희귀도/설명), MonsterRewardData(돈/경험치/독립 확률·최소/최대 재료 목록), EnemyReward 추가.
- EnemyHealth는 치명타에서 isDead를 콜백보다 먼저 확정하고 보상을 지급한 다음 기존 체력/사망 이벤트·출구/결과 처리를 진행. EnemyReward 자체 재진입 가드도 사용. OnDestroy/OnDisable 보상 없음. 전투 수치 변경 없음.
- RewardInventorySession을 기존 DungeonRunManager 오브젝트에 연결. 새 싱글톤 없음. 보유 돈/재료는 세션 유지, 이번 도전 돈/원래 처치 보상 경험치/재료 수량 기록을 별도 보관. 음수와 long 오버플로 방어. 경험치는 기존 PlayerProgression.GrantExperience만 호출.
- 새 도전 기록 초기화는 DungeonRoomManager.StartDungeon에서만 실행. 결과·실패·재도전 준비 상태까지 기존 기록 유지, 실제 던전 시작 때 기록만 초기화. 보유 돈·성장·재료·스킬 목록/선택은 유지.
- 보상 임시값: StoneMonsterReward 돈10/EXP10/돌100% 1~2, StoneGolemReward 돈50/EXP50/돌100% 5. Room_Normal_01의 6마리 연결, Room_Normal_02 Variant는 상속, Room_Boss_Test Variant의 보스는 보스 데이터로 연결. 엘리트 생성 없음. 향후 같은 EnemyReward에 별도 데이터를 연결 가능.
- 재료 탭 실제 연결, 전체 탭 통합, 우상단 수량, 재료 툴팁, 검색/공통 정렬/수량 많은순. 재료 클릭은 E/R 장착 불가. 기존 카드 프리팹과 호버/목록 구조 확장. 스킬/재료 최초 등록 순서는 공유 증가 번호로 안정 정렬.
- HUD 상태 패널 아래 돈 표시 및 인벤토리 미리보기 아래 돈 표시. 기존 HP/EXP/부적/QER 배치 유지. 결과 카드에 이미 지급된 돈 및 원래 처치 보상 EXP 요약 추가. MAX 이후 EXP도 원래 보상 합계로 표기.
- 우측 중앙 획득 알림: 아이콘/이름/이번 획득량, 최신 상단, 기존 항목 부드럽게 아래로 이동, 3초+0.3초 페이드, 5줄+FIFO 대기열. 같은 처치만 재료 ID별 합산, 다른 처치는 별도 줄. 타이머는 일시정지/인벤토리 정지 준수. 방 이동 유지, 결과/새 도전 정리. 입력 차단 없음.
- 클리어 추가 보상, 몬스터 스킬 드롭, 바닥 드롭, 디스크 저장은 추가하지 않음.

### 실제 검증
- 통합 플레이 검증 최종 54개 모두 PASS. 정상/연속 처치, 중복 사망 및 체력 이벤트 재진입, 방 제거 무지급, 0%/100% 및 양 끝 수량, 음수·상한 방어, 중복 재료 합산, 기존 레벨업, MAX 경험치 기록 포함.
- 재료/전체 탭, 수량·이름 검색·정렬·툴팁, 준비/전투 재료 장착 차단, 스킬 교환 유지, 알림 최신순·5줄 제한·FIFO 대기열의 새 표시 시간·부드러운 아래 이동, 일시정지 검증.
- 보스 보상 지급 후 클리어 결과 요약 일치, 결과 중복 무지급, 실패 알림 정리/보상 보존, 클리어·실패 재도전 후 보유량/성장/스킬 유지, 실제 새 도전에서 기록만 초기화, 생명3/부활2 유지 확인.
- 1280×720 재료 인벤토리/획득 알림/클리어 요약 캡처 확인. Temp/RewardVerification의 이미지 참조. 최초 인벤토리 캡처 이후 돈 표시는 미리보기 아래 빈 공간으로 옮겨 저장함.
- 최초 임시 폰트 표식/검증용 재료명 누락 경고를 수정한 후 전체 재검증에서 게임 오류·경고 없음.
- 최종 컴파일 완료, 씬 Missing Script 0, 끊어진 직렬화 참조0, 일반 두 프리팹 12마리/보스1마리 보상 참조 확인. TestRoom 저장 Dirty=false, 일반 방3→보스1, E EarthSmash/R TealBeam 유지.
- 임시 RewardVerification.cs 및 meta 삭제. Play Mode 종료로 검증 재화/임시 재료/진행 변경 폐기. 사용자 저장 데이터 초기화 없음. runInBackground=false/timeScale1/audio false 복구, 테스트 중 생긴 TimeManager 재직렬화 원복.
- 마지막 도구 검증 코드의 일회성 타입 오타는 수정 후 정상 실행. 최종 Console에는 재컴파일 중 MCP WebSocket 초기화 경고1건만 남음(게임 코드 오류 아님).

### 변경 파일 / 조절 위치
- 추가: MonsterRewardData.cs, EnemyReward.cs, Inventory/MaterialData.cs, RewardInventorySession.cs, RewardHUD.cs, Editor/RewardUISetup.cs.
- 확장: EnemyHealth.cs, DungeonRoomManager.cs, DungeonResultUI.cs, InventoryUI/InventoryPresentation/InventorySkillCard/InventoryHover/SkillInventorySession.cs.
- 에셋: Assets/Rewards/Stone.asset, StoneMonsterReward.asset, StoneGolemReward.asset, Reward Korean.asset, 카드 프리팹, 일반 방 원본/보스 방 Variant, TestRoom.
- 수치 조절: Assets/Rewards의 두 Reward 에셋에서 돈/경험치, Chance Percent(0~100), Minimum/Maximum(양 끝 포함). 잘못된 항목은 Inspector 검증 경고 및 런타임 무지급 방어.
- 재료 정의: Stone.asset. 몬스터 연결: 각 EnemyReward > Reward. 알림 위치: Canvas/MaterialAcquisitionNotifications. 시간/간격: Canvas > RewardHUD.
- 다른 화면 비율의 알림/결과/인벤토리 가독성, 장시간 실제 전투 및 보스5패턴 전체 회귀는 사용자 확인 필요.

## 2026-10-18 광산 입구 / Main 흐름 완료
- Main.unity 추가 및 시작 씬 등록. TestRoom도 동일 흐름으로 저장. 기존 Player/Canvas/DDOL 세션 관리자/방 프리팹 재사용; SampleScene 미수정.
- MainMineEntrance(4,2), Canvas의 MineEntranceUI Interaction Radius=2.5. 접근 T 안내, 새 T 입력, X/ESC 닫기, I 인벤토리 전환, 모달 시간/오디오 소유권과 새 공격 입력 보호 연결.
- 기존 PreparationPanel, 인벤토리 StartDungeon 버튼/자동 준비 모달/전용 이벤트 제거. InventoryUISetup의 구형 UI 생성 코드도 정리. 공용 인벤토리와 DungeonRoomManager 시작 로직 유지.
- 실제 입장: 1층 암석 갱도/쉬움 Lv.1. 기존 일반3→보스1. 보통/어려움/악몽과 2층 추가 갱도는 준비 중. 클리어 기록 조건 없음, UI와 실제 StartDungeon 모두 레벨 검사.
- DungeonFloorData Inspector: 층 이름/이미지/난이도별 requiredLevel/implemented/기존 방 구성. 1층 임시 필요레벨1/2/3/5, 2층2/3/4/5.
- ChestRewardPreviewData는 명시적 미연결 테스트 미리보기. 1층 난이도별 기존 대지 분쇄 10/20/30/40% 1개, 돌100% 1~3개. 표시 순서 고정. 실제 상자 생성/지급 없음, 처치 보상 변경 없음. 기존 인벤토리 툴팁 재사용, 장착 안내 제외. 도형 대표 이미지는 교체 가능.
- 결과 버튼은 메인으로 돌아가기. 기존 씬 재로드 재사용, 성장/보유 재화/재료/장착 유지. 실제 입장 때 체력/물약/생명3/쿨타임/도전기록 초기화.
- 실행 검증: 임시 Play Mode runner 38항목 PASS. 범위/T, UI정보/준비중, 필요레벨 미만/동일/초과, API 재검사, 알림1.2초 갱신, 보상 순서/툴팁, ESC/Pause 중복방지, I전환/검색문자, 이동/전투입력 차단, 클릭 누출 방지, 중복입장, 자원초기화/성장유지, 실패→메인→재입장/관리자중복없음. 추가 실제 X버튼 Invoke 정상, 임시 카드8개로 가로 overflow/끝스크롤 확인.
- 두 씬 Missing Script=0. 1280x720 화면 직접 확인, 한글 누락 보완. 캡처 도구 실행 중 Unity PlayerLoop 재진입 경고 발생했으나 캡처 없이 재실행/창열기/난이도/입장거부 후 Console error/warning 0. 컴파일 오류 없음.
- 임시 검증 스크립트/meta 제거, Play 종료. 검증 레벨/재화는 Play 메모리에서만 변경; 실제 원본 설정/성장 초기화 없음. 시간1/오디오해제. TimeManager 자동 재직렬화 원복.
- 사용자 확인: Main Play → 우상단 광산으로 이동 → T → 쉬움 입장; X/ESC/I 조작 및 실제 마우스 가로 드래그/툴팁, 다른 해상도 가독성. 이번 작업에서 4개 방 전체 클리어/보스5패턴/생명3회 소모의 장시간 회귀 및 빌드 실행은 수행하지 않음.

## 2026-10-18 추가 수정: 일반 UI 비정지 / 1440×810
- 최신 지시가 앞선 일시정지 정책을 대체함. InventoryUI와 MineEntranceUI의 Time.timeScale/AudioListener.pause 소유·복구 코드를 제거. 일반 창은 입력만 차단; 월드/적/쿨타임은 계속 진행.
- PlayerMovement Update/FixedUpdate에 기존 InventoryUI.BlockCombatInput을 연결. 기존 공격/회피/물약/스킬 입력 차단과 닫기 직후 새 입력 보호 유지. PauseMenu ESC는 하위 페이지에서도 닫고 재개. 결과 상태 시간 처리는 미변경.
- CenteredWindowLayout 추가: preferredSize1440×810, 화면 여백24, Canvas 크기 안에서 RectTransform 크기만 제한. Transform Scale1. Main/TestRoom에 연결 저장. 전체화면 반투명 입력 차단은 유지.
- InventoryLayout 본체와 MineSelectionPanel/WindowBody 중앙 정렬. 인벤토리 능력치 영역 높이 확보, 카드 그리드는 실제 너비 변경 시 재계산(축소 후 과거4열이 남는 문제 수정). 툴팁은 기존 Canvas 최상단/화면 경계 제한 유지.
- 실행 검증14항목 PASS: T 월드시간/오디오 유지, 두 창 이동·공격·스킬·회피 입력 차단, I 전환, ESC 창만닫기/일시정지/재개/하위페이지닫기, 쿨타임 진행, 적 이동 지속, 닫기 클릭 누출 방지, 외부timeScale0.5 값 보존.
- 실제 Game View1920×1080: 두 패널1440×810, scale1, 좌우240/상하135 여백. 인벤토리 카드3열+다음행, 검색/탭/장착/스탯/툴팁 및 광산창 제목/보상/조건/입장버튼 화면 확인. 1366×768 창 경계(170.88,95.93)~(1195.13,672.07) 화면 안 확인.
- 최종 테스트 Console 오류/경고0, 두 씬 Missing Script0. 임시 UIFlowVerification 및meta 삭제, Play종료, TimeManager 자동 재직렬화 원복. 성장/재화 원본 변경 없음.
- 미검증: 모든 보스 투사체 개별 회귀, 극단적인 세로 화면/장문 데이터 전체 조합. 1920×1080 가독성과 1366×768 경계는 실제 확인.

## 2026-10-19 성장 자동 저장 / 퍼즈 귀환·종료
### 구현 및 연결 저장 완료
- 기존 디스크 저장 없음 확인. 기존 DungeonRunManager 오브젝트에 CharacterSaveService 추가(새 singleton 없음). Main/TestRoom 씬 저장, SkillData4종/MaterialData 돌 카탈로그 연결.
- CharacterSaveFile: v1 JSON payload+SHA256, 임시 파일 Flush(true) 후 교체, 정상 백업 .bak. 손상 주 파일은 .corrupt-GUID로 보존하며 백업 복구. 둘 다 실패하면 LoadBlocked로 자동 저장/명시적 Flush 차단, 화면 상태 안내.
- CharacterSaveService: Awake에서 최초1회 로드 후 변경 구독. 레벨/현재EXP/미사용포인트/돈/재료수량/등록순서/보유스킬ID/E/R/카메라흔들림 배율 저장. unknown ID는 경고하고 별도 보존 후 재저장. 방/적/체력/물약/생명/CD/UI는 저장 안 함.
- 실제 장비 시스템/분배스탯/상자지급/제작은 없음. 해당 기능을 새로 만들지 않음. 최종공격력/최대HP 중복계산 없음. 기존4스킬 테스트지급은 로드소유목록으로 교체되어 중복지급 안 함.
- 영구데이터 Changed→0.4초 unscaled debounce, 처치보상 transaction으로 중간 Flush 차단. 입장/귀환/게임종료전 Flush, 정상종료 마지막시도. 후속 기능 RequestSave/BeginTransaction/EndTransaction API.
- 저장 경로(build): C:/Users/User/AppData/LocalLow/DefaultCompany/mineDungeon/character.json. Editor는 같은 경로의 Editor/character.json으로 분리. 테스트는 프로젝트 Temp/CharacterSaveVerification-*/character.json만 사용, 실제사용자저장 미접근/미삭제.
- 임시 보존정책: 이미 지급된 돈/EXP/재료는 귀환·실패·종료 모두 보존. 던전중간 이어하기 없음, 미획득상자/추가클리어보상 없음.
- 퍼즈620×600 중앙. 메인: 계속/설정/종료, 던전: 귀환 추가. 설정은 실제 CameraFollow 흔들림0~2만 연결. 상위설정/확인창 ESC는 퍼즈로 복귀, 기본퍼즈 ESC는 재개. 인벤토리/입장창 크기·시간처리 미변경.
- 귀환/종료 확인→Flush 성공만 진행, 실패시 재시도/취소. 결과화면 복귀도 Main 경로 사용. Editor 종료는 Play Mode만 중지.
### 실제 검증
- 격리 경로 runner17항목 PASS: 신규기본값, 정지중debounce저장, 돈10/EXP260→Lv3EXP10/포인트2/돌3/TealBeam E 저장, 설정·종료확인 취소 상태, 기존 창닫기 핸들러, 입장전Flush/생명3초기화, 실제 EnemyHealth 처치보상 돈10EXP10돌1~2 지급, 귀환취소/중복귀환방지/관리자1개/기존보상보존, 파일쓰기실패시 종료차단+취소+재시도.
- 별도 Play 재실행5항목 PASS: 성장/돈/재료/E/R/설정0.3복원, 보유스킬4개 중복없음, Main준비/방0/HP최대/생명3.
- 손상주파일+정상백업: money20복구, 새주파일checksum정상, .corrupt 원본1개보존 확인. 둘다손상: LoadBlocked=true, Flush=false, 쓰기0, 원본문자열둘다그대로 확인.
- 실제 퍼즈/종료확인 화면 캡처 확인, ConfirmQuit로 Play Mode종료 확인. 최종 코드 갱신 후 컴파일 오류0 조회했음. standalone 빌드 종료/OS강제종료/모든보스전 회귀 미검증.
### 중단 / 남은 마무리 (MCP 연결 필요)
- 키보드 자동주입은 에디터 포커스 제약으로 처리되지 않아 초기 실패. 이후17항목은 실제 핸들러/API 호출 검증이며 물리ESC 입력 검증으로 주장하지 않음.
- 임시 InputSettings 복제 테스트가 도메인 재로드에 남아 초기화오류 발생. 임시 에디터 InputSystemObject 상태 폐기/재로드 후 오류0, 기본설정 PointersAndKeyboardsRespectGameViewFocus 확인. 에디터 재시작 요청했으나 이후 native keyboard/mouse는0개로 조회됨. 실제 하드웨어 입력은 연결 복구 후 확인 필요; 영구 InputSettings 에셋 변경 없음.
- 마지막 SaveInputVerification 실제 키보드 재검증 시작 전 MCP HTTP127.0.0.1:8080 접속실패. 사용자에게 재연결 요청함. Unity에 저장된 씬/코드는 유지됨.
- 임시 SaveFlowVerification/SaveInputVerification 및 meta는 파일시스템에서 삭제 완료(도구연결불가). 재연결 후 AssetDatabase.Refresh, 컴파일/콘솔/두씬MissingReference 검사 필요.
- 반드시 Editor SessionState.EraseString("MineDungeon.SaveTestPath") 및 SaveVerificationPhase/SaveVerificationExpected/SaveVerificationGoodPath 임시 키 정리. 현재 override는 Temp/CharacterSaveVerification-c8b22171403c445e92126cba49bae554/character.json일 수 있음. 실제 사용자 저장을 테스트로 건드리지 않도록, 키 제거 후에는 검증용 Play를 다시 실행하지 말 것.
- runInBackground=false/timeScale1/audio false 복구 및 Main edit mode 저장 확인. 테스트중 TimeManager diff는 현재git확인 후 해당자동변경만 복구할 것.

### 2026-10-19 재연결 후 최종 완료 (위 중단 항목 해소)
- 재연결 후 실제 Keyboard/Mouse 존재 및 기본 InputSettings 확인. 자동 입력검증에 기존 설정의 focus관련값만 잠시 변경하고 종료시 정확히 복구(복제객체 사용 안 함).
- 별도 신규 Temp/CharacterSaveVerification-fe64c0f60e384673a6e675feed4085f7/final.json으로 최종8항목 PASS: 실제 InputSystem 키 이벤트→Inventory ESC닫기, 기본ESC퍼즈, 설정ESC퍼즈유지, 확인창ESC퍼즈유지, 기본퍼즈ESC재개, Mine ESC닫기, unknown스킬/재료ID 보존 및 정상로드, 실제보유스킬 중복/무효추가없음.
- 필수 version/level만 있는 checksum정상 파일의 누락 선택필드 로드/기본값/list보정도 실제 통과.
- 최종 InputSettings PointersAndKeyboardsRespectGameViewFocus / ResetAndDisableNonBackgroundDevices 원복 확인. Play종료후 Keyboard/Mouse 둘다 존재.
- FinalSaveVerification 및meta MCP삭제, 기존 임시검증스크립트 삭제확인. SessionState 테스트경로 및 phase/expected/goodpath 전부 제거(testPath 빈값 확인). runInBackground=false, timeScale1, audio=false.
- Main/TestRoom 다시 MCP저장. Missing Script0 / broken serialized references0 각각 확인. 최종 Console error0. TimeManager 자동재직렬화만 원복.
- 사용자 원본 저장파일은 읽기/쓰기/삭제하지 않음. 실제 사용자 저장 경로는 다음 일반 Play/빌드 실행에서 사용됨.
- 남은 미검증: standalone 빌드 실제 프로세스종료/플랫폼별파일교체, OS강제종료 직전 아직 debounce중인 변경, 장시간 전체 보스패턴 회귀. 실제 장비/분배스탯 시스템은 없으므로 해당 복원시험은 해당없음.
- 사용자 확인: Main Play → 스킬E/R교체 → T입장 → 몬스터처치 → ESC귀환취소/확인 → 메인 → ESC종료확인(Editor는Play만종료) → Play재실행에서 돈/성장/재료/장착과 새준비상태 확인. 설정의 카메라흔들림 배율도 재실행 유지.


### 2026-10-19 일반 몬스터 5종 구현
- 사용자 확정 1층 쉬움 수치 적용. StoneMonsterAI 기존 .meta 유지, Iron/Crystal/Coal/MagnetMonsterAI 추가. NormalMonsterAI는 수명/예고/충돌조회 공통부만 공유. IMonsterAI로 DungeonRoom.Bind/EnemyHealth 사망/StopRoomCombat 통합.
- Assets/Prefabs/Monsters의 5종 프리팹 저장. Room_Normal_01, Room_Normal_02 Variant 각 6마리(돌2, 철/수정/석탄/자철석 각1) 등록. 기존 위치 유지. 보스룸/일반3→보스1/플레이어 능력치/사용자 저장 변경 없음.
- 기존 EnemyHealth, MonsterHealthBar, StoneHitFeedback, EnemyReward, 자동저장 재사용. 일반 보상 돈10/XP10/돌1~2 임시 유지. 실제 넉백/행동경직/데미지숫자 기능은 기존 대상 스크립트에서 없음; 새 면역 규칙 없음.
- PlayerHealth.TakeDamage 호환 유지하고 bool TryTakeDamage 추가. 감속은 PlayerMovement의 기본속도·스킬배율에 0.7을 곱하며 재적용은 시간만 갱신. 회피 속도는 기존 유지. 사망/방이동/결과/비활성 시 ClearSlow.
- 철 돌진: 개체별 IgnoreCollision pair만 적용하고 종료/취소 복구, hurtbox는 계속 활성. 경로 sweep은 벽과 플레이어를 검사. 예고/후딜 이동제약은 기존 constraints를 보관·복구. 새 넉백 시스템 없음.
- 시각: 기존 원/선 채움 공유, 석탄 열기/철 잔상·벽 파편/수정 파편/자기장·감속 링/돌 착지 먼지. 몸체 피격 변형과 JumpVisual 분리 유지.
- Inspector 임시 흔들림: 돌 .025/0.10초, 철 벽 .04/0.10초, 석탄 .025/0.10초; 수정·자철석 0. 기존 CameraFollow 거리감쇠 및 최대 요청 처리 재사용. 인식거리 별도 추가 없음(기존 무제한 타겟 추적).
- 실제 MCP Play 검증: 실제 입장, 신규4종 자동타겟, 5종 단발피해12/15/14/16/8, 비활성 효과정리, 감속0.7·비중첩갱신·만료, 철 얇은벽 정지/벽후딜, 수정 얇은벽 차단, TimeScale0 타이머/이동정지, 무적 시 감속/피해없음, 플레이어사망 즉시공격취소·부활정상, 중복치명타에도 사망6회, 혼합방전멸 출구개방 PASS. 돈/처치XP각60 확인. 인벤토리 열린 채 timeScale1/Time.time 진행 확인(실제 I/ESC 입력 테스트는 아님).
- 최초 테스트 Stone1 실패는 검증용 위치이동에서 transform/Rigidbody 동기화 누락으로 출발점이 -9,-7에 남은 원인. 테스트 수정 후 두 Stone 모두 PASS. Start에서 이미 전달된 타겟 중복 초기화 제거.
- 모든 검증은 Temp/MonsterValidation-639279820971523405/character.json 사용. 사용자 원본 저장은 건드리지 않음. NormalMonsterValidation 임시스크립트/meta 제거, SessionState 저장 override 제거, runInBackground=false/timeScale1/audio pause=false 복구.
- 캡처 Temp/MonsterValidation/five-species-hp.png에서 색상 구분 및 발밑 HP바 확인.
- 미검증: 장시간 혼합전투 밸런스, 모든 벽/닫힌문 모서리와 동료 돌진겹침 조합, 물리 키 ESC 입력 전체회귀, 보스5패턴 전체회귀, 다중동시공격 화면흔들림 체감. 사용자 Main→T→1층쉬움→혼합방에서 확인 필요.

- 최종 상속 검사에서 보스룸이 Room_Normal_01의 기존 몬스터 개체 ID에 의존함을 발견. 최초 개체 교체 방식 폐기하고 Git 기준 원본 ID를 복원한 뒤 일반방 개체의 AI/수치만 제자리 변경. Room_Normal_02 Variant와 Room_Boss_Test Variant 원본 파일은 최종 Git 변경 없음. 보스룸 기존 보스1/HP240/등록1/신규AI0 보존 확인. 실제 방 수치는 Room_Normal_01 내부 각 EnemyHealth/AI에서 조절(2번 방은 상속); Assets/Prefabs/Monsters는 추가 배치용 독립 프리팹.
- 최종 별도 Temp/MonsterValidation-final/character.json으로 실제 1→2→3→4 생성 진행 재검증: 각 일반방6/신규타겟4, 전멸clear/다음방 PASS. 4번방 보스1 HP240, 보스처치 즉시run clear 및 총처치보상 돈230/XP230 PASS. 콘솔오류0. 검증용 치명타는 API 호출이며 사용자의 수동 전투로 주장하지 않음.
- 최종 5종프리팹 및 3종방 MissingScript0/broken serialized reference0, 일반방 등록6/유효6, 보스방 등록1/유효1. 최종 Play종료 후 테스트경로 빈값 확인, timeScale1/audio false/runInBackground false 복구. 임시 검증 코드는 없음.

## 2026-10-20 통합 클리어/실패 결과창
- Main/TestRoom의 기존 결과 패널을 공용 Assets/Prefabs/UI/DungeonResultView.prefab으로 교체하고 기존 DungeonResultUI 참조 연결. 1920×1080 기준 콘텐츠1440×810, 전체 화면 암전, 4열 아이템 스크롤, 기록6행, 다시 시작/로비/닫기/결과 보기/저장 재시도. 기존 InventoryUI 툴팁 재사용. 첫 표시2.1초 연출은 표시만 수행하고 재열기는 즉시 완료 상태.
- DungeonRunSettlement.cs: 새 플레이 ID, 실제 피해/회복/처치/시간 및 획득 이벤트 집계. LateUpdate에서 결과를 한 번 정산하고 같은 프레임 클리어 우선. 완료 후 통계/피해/방 전환 차단, 클리어 이동 허용 및 실패 사망 유지. 같은 층/난이도 재시작 연결.
- CharacterSaveFile/Service: 보유 데이터와 lastSettledPlayId를 같은 저장 스냅샷에 포함. 결과 확정 시 즉시 Flush, 저장 실패 시 메모리 보상 유지 및 이동 차단, 재시도는 재추첨/재지급 없음. 기존 원자적 교체/백업 방식 유지.
- 상자 생성/T 개봉 흐름은 사용하지 않음. 기존 ChestRewardPreviewData의 직렬화 이름/참조를 보존하면서 입장창 명칭을 클리어 보상으로 변경하고 실제 자동 추첨에 연결. 현재 실제 입장 가능한 1층 쉬움: 대지 분쇄10%, 돌100% 1~3개, 항목별 독립 추첨. 기존 테스트 표를 유지한 임시값. 이미 보유한 스킬은 추가 지급·획득 표시하지 않음. 클리어 추가 돈/XP 없음. 실패와 귀환에서도 이미 받은 보상100% 유지.
- 실제 Play API 검증: 일반3방→보스 처치, 마지막 피해/19처치/돈230·XP230 포함, 초과 피해 제외, 실제 회복3 기록, 두 번 부활 후 세 번째 사망 실패2/2, 동일 프레임 양쪽 사망 호출 순서를 바꿔도 클리어 우선, 최소 생존 복구, 결과 닫기/재열기 중복 지급 없음, 같은 층 재시작 새ID/첫방/HP·물약·생명 초기화 및 보유 유지. 저장 파일과 정산ID 일치, 강제 저장 실패 시 귀환/재시작 차단 및 저장 재시도 시 재지급 없음 확인.
- 실제 InputSystem 키 주입 검사9개 PASS: 결과 열린 상태 W 이동, Space/E/Q 차단, 결과 후 적 피해 차단, ESC 결과만 닫기→다음 ESC 퍼즈→ESC 재개, 퍼즈 이동 차단, I 중복 UI 차단, 재열기 즉시 버튼 사용. 입력 설정 원복.
- 1920×1080 Game View에서 배치 캡처, 0개 표시 및 9개 합성 UI 항목의 스크롤(content358/viewport262.2) 확인. 합성 항목은 인벤토리에 지급하지 않았음. 난이도 한글 누락 글리프 추가 후 HasCharacters 확인. 결과 후에도 ESC에서 붕괴 효과/흔들림이 멈추도록 해당 월드 연출은 scaled time 사용.
- 최종 컴파일/Console 오류0. Main/TestRoom 각 결과View1개, UI 참조 정상, MissingScript0/broken serialized reference0. 임시 ResultVerification/ResultInputVerification 스크립트 제거, Main 편집 모드, timeScale1/audio false/runInBackground false, 저장 override 빈값.
- 저장 보호 주의: 초기 정산 검증은 별도 테스트 경로에서 진행. 마지막 입력 검사 시 SessionState 테스트 경로가 비어 Editor 저장에 돌1개와 테스트 정산ID가 기록됨을 발견. 직전 character.json.bak(돌31/정산ID빈값)으로 정확히 복구했고 두 파일 SHA256 일치 확인. 사용자에게 고지. 이후 추가 Play 실행 안 함. 향후 테스트 시작 전 실제 CharacterSaveService.SavePath를 반드시 확인할 것.
- 미검증: 별도 빌드 종료/재실행, 장시간 전투 전체회귀, 실제 마우스 호버 툴팁 화면 가장자리, 정확히8개 항목, 다중 레벨업 연출의 중간 프레임, 최종 시간수정 후 붕괴/흔들림 실플레이. 보상 데이터는 Assets/Dungeons의 기존 Floor1ChestPreview_*에서 조절. 수정된 font는 기존 로컬 폰트의 정적 결과용 에셋이며 외부 패키지 없음.

## 2026-10-20 일반 몬스터 전용 재료
- 기존 Stone.asset GUID/ID Stone 보존, 표시명 돌→돌 조각. IronOre/CrystalShard/Coal/Magnetite 신규 일반 재료와 교체 가능한 64px 임시 아이콘 추가. 기존 저장 수량은 ID 기반으로 유지.
- 기존 StoneMonsterReward 및 신규 Iron/Crystal/Coal/MagnetMonsterReward: 돈10/XP10 유지, 재료 항목 정확히1개, 전용 재료50%/최소1/최대2. 기존 Drop.Roll의 성공1회→수량선택 방식 그대로 사용(0/1/2=50/25/25%). 별도 스크립트·제작 기능 없음.
- 개별5종 프리팹과 Room_Normal_01 실제6개 개체 연결. Room_Normal_02 Variant 상속 확인. Boss Variant는 돈50/XP50/Stone100%5개 유지. 입장창 클리어 보상표 변경 없음(동일 Stone 표시명만 공유).
- Main/TestRoom CharacterSaveService.materialCatalog에 신규4종 추가. Inventory/Reward/Result/Mine Entrance 기존 정적 폰트에 재료명·설명 글리프 보완.
- 실제 Play 검증: 각5종에 난수 시드를 골라 0/1/2 분기를 실제 EnemyHealth 치명타로 실행(설정은 변경하지 않음). 15경로 모두 예상 수량, 실패 알림0/성공 알림1, 중복 치명타 후 돈/XP1회 PASS. 실제 일반방 전멸clear PASS. 수량 inventory/run/result 각각 Stone4/Iron4/Crystal5/Coal3/Magnet5 일치. 알림 visible5/pending9 확인. 비활성만으로 보상없음, 결과 확정 후 신규 피해/보상없음, 실패 결과창5종 합산과 아이콘 캡처 확인. 실패는 테스트 API FailRun 호출이며 최종사망 흐름 재검증으로 주장하지 않음.
- 재료 탭 이름검색 각1개와 툴팁 데이터 확인. 저장 후 Play 재진입으로 5종 수량4/4/5/3/5 복원 확인. 사용자 수동 마우스 호버·퍼즈귀환·빌드재실행은 이번에 재검증하지 않음.
- 최초 테스트는 도구 호출 사이 플레이어가 사망해 결과 상태가 되어 피해가 차단됨. 새 실행에서 입장 직후 timeScale0으로 검증 환경을 고정하여 재실행 성공. 임시 테스트 시간/난수 원복. 최종 편집 모드, timeScale1/audio false/runInBackground false, 저장 override 제거.
- 모든 테스트는 Temp/MaterialDrops-8bb2a17db4f8466f987d7cd962dad31d/character.json. 실제 Editor 저장 SHA256 전후 5DFEC68B8C60E3796334820156F8DCA4C887AB2A4A8B6DF14BC51E426BACD6F8 동일. 임시 스크립트 생성 없음. Console 오류0.

## 2026-10-20 Editor 전용 진행 데이터 초기화
- Assets/Editor/PlayerProgressReset.cs: Play에서 Ctrl+Shift+F9 한 번, Tools/Mine Dungeon/개발용/플레이어 진행 데이터 초기화 (Play Mode). 기존 Input System Keyboard 사용, F9 중복 단축키 검색 결과 없음. native Editor 확인창(초기화/취소), ESC는 취소 버튼. 동기 모달 동안 Editor player loop와 게임 입력이 진행되지 않으며 timeScale/UI 소유 상태는 바꾸지 않음. 닫은 뒤 키보드/마우스 상태 reset으로 클릭/ESC 누출 차단.
- CharacterSaveService.EditorResetProgress 및 SkillInventorySession.EditorStartingSkills는 UNITY_EDITOR로 한정. Editor 폴더의 메뉴/키 처리와 함께 일반 플레이어 빌드에서 제외(실제 배포 빌드 실행 검증은 미실시).
- 실제 신규 지급 배열과 Main 씬의 기본 E/R을 읽음. 기본 현재값: Lv1/EXP0/포인트0/돈0/재료없음, 테스트 시작 스킬 EarthSmash·DrillDash·TealBeam·OreBlast 각1, E EarthSmash/R TealBeam. 실제 장비/분배스탯 시스템은 아직 없으며 기존 곡괭이 기본 Player 상태 사용.
- 저장/보상 트랜잭션/씬 재시작 중 거부. 이전 dirty 예약 취소, 기존 동기 atomic Write 두 번으로 주 파일+복구 .bak 모두 신규 snapshot으로 교체, 성공 후 메모리 성장/인벤토리/정산ID 초기화, 기존 전투 정리 후 ReturnToMain로 던전/결과/플레이어 제거·재생성. 실패 시 LoadBlocked로 이전 메모리 자동저장 차단, 오류 안내와 재시도 가능. 기존 설정 shakeMultiplier 보존, 다른 PlayerPrefs/슬롯/프로젝트/에셋은 삭제하지 않음.
- 별도 Temp/ResetValidation-44b59be241ec441b85bc7b29d9c3630f/character.json에서 XP260/돈999/철17/설정0.37 저장 후 던전·결과·퍼즈에서 초기화 처리 실행. 신규 기본값, 설정0.37 유지, Primary/Backup 동일, Play 종료/재진입 후 유지, 반복 초기화 스킬4/중복없음, Main/preparing/timeScale1/audiofalse/관리자1/Player1/Resultnull 확인.
- 잘못된 테스트 저장 경로(디렉터리)로 실패 유도: false/error, 메모리 unchanged, 정상경로 복원 후 재시도 성공. 사용자 실제 Editor 저장 SHA256 전후 331DEE230156F449FCAE3DA24D0BA0505B2B0B7E47F973412CF5D98D96A8E2DF 동일.
- 최종 Console 오류0, 테스트 override 제거, edit Main/timeScale1/audiofalse/runInBackgroundfalse. 실제 native 확인창 클릭/ESC 취소 및 물리 단축키는 자동화하지 않았으므로 사용자 직접 확인 필요. 메서드 호출은 확인 이후 동일 처리 경로로 검증.

## 2026-10-21 맵 4 수정 설계도 반영
- 범위: Assets/Prefabs/Rooms/ReferenceMaps/Map_04.prefab, Assets/Editor/MineMapBuilder.cs의 맵 4 생성 정의만 수정. 시작 Git 상태는 clean. 다른 맵/보스/런타임 시스템 변경 없음.
- 기존 32×44 직사각형, 외곽 암벽 2, 바닥 28×40, 입구(-9,-17), 출구(12,16) 유지. 중앙 벽 x[-2,2]를 하단 y[-20,-11] 및 상단 y[-5,8]로 분리하여 높이6의 횡단 통로 확보. 기존 0.5 그리드와 검증 몸체 폭1.3 기준으로 연결 검사. 우측 전투 바닥과 기존 주요 우측/하단 기둥 유지.
- 좌측 상단을 단계형 암벽으로 채우고 아래·오른쪽으로 좁아지는 붕괴 지형 적용. 실제 Wall 충돌체와 rockAreas 일치. 바위·부러진 목재는 기존 단색 스프라이트 기반 장식이며 별도 상호작용 없음. 암벽과 겹치는 선로는 숨김. 빨간 선·화살표 미구현.
- 배치 영역: 상단 중앙 Rect(1,10.5,5,6), 우측 상단 Rect(10.5,5.5,2.5,6), 우측 중하단 Rect(10,-7,3,6). 각 영역2마리, 기존 돌2/철1/수정1/석탄1/자철석1 및 보상/난이도 설정 유지. 기존 고정 생성 위치6개를 재배치했으며 무작위 생성 시스템을 추가하지 않음.
- Unity MCP로 기존 Regular 프리팹 내용을 수정·저장. GUID 및 RoomController/RoomExit/RespawnPoint의 기존 fileID 보존. 원본 일반 방·보스 Variant·다른 레퍼런스 맵(A 포함)·Floor1 목록·지정 보호 문서 등22개 파일 SHA256 전후 일치. .meta 변경 없음.
- 실제 Play Mode 기존 MineMapPlayProbe(04) 123개 PASS: 배치 충돌 여유/상호 간격/연결성, 세 영역과 출구 앞까지 Rigidbody 이동, 일반 이동·회피 벽 정지, 6마리 실제 추적 우회, 카메라72조합(화면비·줌·모서리·입구/출구·흔들림), 전멸 출구 개방, 중복 사망 보상 방지, 실제 출구 트리거 방 이동, HP/물약/부활/돈/XP 유지, 현재 방 입구 부활 및2회 제한.
- 추가 실제 Play 검사5개 PASS: 새 붕괴 구역으로 속도15를 준 플레이어 y1.59/몬스터 y1.49에서 정지, 맵4 출구→기존 A, A 출구→기존 보스 Variant(Missing Script0), 보스 치명타→클리어/정산 완료. 적 처치는 테스트에서 기존 TakeDamage를 호출했으며 수동 전체 전투 완료를 의미하지 않음.
- 전체 맵 렌더로 붕괴 구역·분리된 중앙 벽·하단 횡단 통로 확인. 테스트 기록: Library/MapValidation/Map04-play.txt, Library/Map04Fix/extra-tests.txt, overview.png(로컬 검증 산출물). 테스트 저장은 실제 SavePath가 Library/Map04Fix/character.json인지 확인한 뒤 실행. 종료 후 override 제거, 편집 모드/timeScale1/runInBackground false 복원.
- 변경 전후 컴파일/Console 오류0, 맵4 및 관련 보스 Missing Script0. 참조 확인용 임시 MCP 검사에서 잘못된 직렬화 필드명으로 한 번 실패했으나 공개 참조로 재검사하여 기존 ID 일치 확인. 게임 코드 오류 아님.
- 미검증: 수동 키보드 전체 전투, 개별 공격·스킬 전체 회귀, 배포 빌드, 다른 모든 맵의 재주행. 해당 기능 코드 변경 없음. 미해결 기능 오류 없음. Git diff 공백 검사는 Unity 재직렬화의 빈 m_Name 뒤 공백1건을 표시하며 프리팹 텍스트는 수동 편집하지 않음. 커밋/푸시 미실행.

## 2026-10-21 일반 방 랜덤 후보 7종 / 신규 A를 맵 5로 편입
- 실제 DungeonRoom 프리팹 목록 전체를 확인: 신규 일반 맵01/02/03/04/06/07/A, 기존 테스트 일반방2종, 보스 Variant1종. 기존 맵5 구현 프리팹은 없음. 기존5→다른 번호 재배정은 해당 없음(기획상 예전 맵5 문서는 수정하지 않음).
- Unity MCP AssetDatabase.MoveAsset 및 프리팹 저장으로 Map_A.prefab→Map_05.prefab, 루트 Map_A→Map_05. GUID 1f0d9e5a9cee2b044b68bf1bb59a8e33 및 fileID1662757022521918358 보존. .meta 내용 동일, 프리팹 내용은 루트 이름 외 동일. 삭제/덮어쓰기 없이 이름 변경이며 Git의 이전 경로 D/새 경로 ?? 표시는 미스테이징 이동 상태임.
- 최종 후보(공통 경로 Assets/Prefabs/Rooms/ReferenceMaps/):
  - 1 S자 광차 갱도 — Map_01.prefab
  - 2 Y자 분기 갱도 — Map_02.prefab
  - 3 암석 기둥 채굴장 — Map_03.prefab
  - 4 단일 직사각형 쌍둥이 갱도(수정 검증본) — Map_04.prefab
  - 5 중앙 채굴 크레인 광장(기존 신규 A) — Map_05.prefab
  - 6 좌우 포켓 통합 갱도 — Map_06.prefab
  - 7 중앙 암벽 우회 갱도 — Map_07.prefab
- Assets/Dungeons/Floor1_RockMine.asset의 현재 구현된 쉬움 난이도 normalRooms를 위7종으로 설정. Room_Normal_01 및 Room_Normal_02 Variant는 후보에서만 제외하고 원본/상속/보스 참조는 유지. 미구현 난이도1~3의 빈 후보는 유지. 일반 방3개+기존 보스1개 및 난이도/보상 설정 변경 없음.
- MineMapBuilder와 MineMapPlayProbe의 A 식별자를05로 변경하여 재생성/검증 도구도 새 경로 사용. 런타임 랜덤 선택 코드는 수정 없음: 첫 방7종 균등(각1/7), 이후 직전 맵을 뺀6종 균등(각1/6), 한 방 건너 재등장 가능. 가중치/중복 제한 새 규칙 없음.
- 7종 프리팹 참조/몬스터6개 실제 위치의 충돌 여유·입구 연결성/출구 도달 검사 PASS(맵2는 두 후보 각각), Missing Script0. 맵5 실제 Play 검사124개 PASS: 몸체 이동, 6마리 추적, 크레인 반대편 우회, 카메라72조합, 출구, 방 이동, 부활2회, 런 자원/보상 유지. 이전 수정 맵4는 이번 변경 전후 SHA256 동일.
- 실제 Floor1 데이터로 시작한 런타임 후보7종 확인. 기존 PickNormalRoom 7000회 표본에서 각 맵 선택1009/1012/1033/992/967/972/1015회, 테스트 맵/연속 동일 선택0. 표본은 통계적 균등성 증명이 아니며 균등 규칙은 기존 코드로 확인. 난수 상태와 previousIndex는 표본 후 원복.
- 실제 설정(일반방 수나 후보 교체 없는 Floor1 원본)으로 시드21105: Map03→Map07→Map01→기존 보스 진행. 각 방 입구/몬스터6개 유효 위치/카메라 화면 경계/전멸 출구 개방/실제 출구 트리거 이동 PASS. 일반 적18 집계, 보스 처치 후 클리어 및 정산 PASS. 처치는 TakeDamage API 사용, 진행 검사에서는 플레이어 위치를 고정해 접촉 밀림을 차단했으며 수동 전투 완료 주장은 아님.
- 테스트 첫 시작은 저장 서비스 singleton이 준비되지 않아 기존 안전 검사에서 거부됨(던전 시작/저장 변경 전). Play 재진입 후 실제 SavePath=Library/MapPoolFix/character.json 확인하고 실행. 최초 진행 검사에서는 적 접촉으로 입구 위치에서 밀려 검사 실패, 플레이어 고정한 새 런에서 입구 배치 및 전체 진행 재검증. 런타임 코드 수정으로 우회하지 않음.
- 기록: Library/MapPoolFix/validation.txt 및 Library/MapValidation/Map05-play.txt. 보호 대상21개 파일(다른 맵/수정 맵4/테스트 원본/보스/씬/런타임 선택 코드/GDD/Systems) SHA256 전후 동일. 종료 후 편집 모드, 저장 override 제거, timeScale1/runInBackground false. 최종 컴파일/Console 오류0.
- 미검증: 배포 빌드, 수동 전체 전투, 7종 전체의 물리 이동·모든 공격 재검증. 이번에 변경하지 않은 지형/전투는 기존 검증 기록 유지. 확인된 미해결 기능 오류 없음. 공식 GDD/Systems 수정 및 Git 커밋/푸시 없음.

## 2026-10-21 몬스터 데이터 편집기 1차
- 추가: Assets/Editor/MonsterDataEditorWindow.cs, MonsterDataStore.cs, MonsterDataEditorVerification.cs 및 Unity 생성 .meta. Editor 전용이며 런타임 AI/전투/저장 코드와 기존 프리팹/.meta는 수정하지 않음. 이번 작업 시작 Git 상태 clean. 커밋/푸시 없음.
- 메뉴: Tools > MineDungeon > 데이터 편집기. 공용 돌/철/수정/석탄/자철석 프리팹의 실제 SerializedObject 저장값을 로드. 5행 표에서 HP/공격력/이동속도/공격쿨타임/공격거리/예고/후딜레이 편집, 종류 선택 시 고유 공격 수치와 기존 흔들림 수치 표시. 참조/색상/레이어/런타임 현재HP/보상은 편집하지 않음. 초안은 창의 직렬화 상태로 유지.
- 적용: 공용 5종 + Assets/Prefabs/Rooms/ReferenceMaps/Map_01~07 고정 범위. EnemyHealth와 정확한 AI 타입으로 개체 식별(이름에 의존하지 않음). 기존 공용/맵 개체 수치 차이와 프리팹별 개체 경로/필드/변경 전후 값을 표시. 검증/미리보기 → 위 변경사항 확인 후 적용 → 확인창 승인 순서. 맵의 기존 차이도 표의 입력값으로 통일되므로 미리보기 확인 필요. 변화가 있는 프리팹만 PrefabUtility.SavePrefabAsset으로 저장.
- 안전: 빈값/숫자 형식/유한값/정수범위/음수/HP 최소1/기존 jumpDuration Min/감속비율0~1 및 진행이 멈추는 돌진·투사체 속도0 거부. 알 수 없는 ID/필드, CSV 중복행/잘못된 열/따옴표 거부. Play·컴파일·에셋 갱신 중 적용 차단. 대상 Prefab Mode/미저장 변경/읽기전용·편집불가 자산 거부. 미리보기 파일 SHA256 및 적용 직전 실제 필드값 재확인. 저장 예외 시 해당 Undo 그룹을 되돌리고 저장 복구 시도.
- Undo: persistent prefab 컴포넌트 전체 스냅샷을 하나의 Unity Undo 그룹에 등록. Ctrl+Z / Ctrl+Y 시 해당 그룹의 프리팹만 다시 저장하며 창을 닫아도 처리. 기존 개체 삭제/교체/프리팹 재생성 없음. 보스/Room_Normal_01/테스트 맵/Variant는 적용 목록에서 제외.
- CSV: UTF-8 BOM, 헤더 monsterId,field,value의 세로 형식. ID Stone/Iron/Crystal/Coal/Magnet, 예: Stone,maxHealth,31. 필드명은 기존 직렬화 이름이며 UI 툴팁과 내보낸 CSV에서 확인. 소수점은 점, 대소문자 구분. 일부 행 패치와 따옴표/CRLF/LF 지원. 가져오기는 검증된 초안 및 미리보기만 변경하며 실제 적용은 별도 확인 필요. 내보내기는 현재 표의 초안 수치(미적용 편집 포함)를 저장. 실제 게임 데이터는 기존 프리팹뿐이며 신규 능력치 SO 없음.
- Editor 통합검증: 실제 대상12종을 고유 Assets 임시 폴더에 AssetDatabase.CopyAsset으로 복사한 안전한 테스트 대상에서 모든 편집 필드 임시 변경·저장, 전체 실제 수치, 공용/개체 차이 표시, Undo 원복, 디스크 재임포트 후 원복, Redo 재저장 PASS. 모든 컴포넌트 참조/RoomController.enemies/배치·구조/내부 GlobalObjectId 유지 PASS. CSV BOM/따옴표/부분패치 왕복과 16개 비정상 입력, 최대 int 정밀도, 잘못된 초안/오래된 미리보기 거부, 변경없는 적용의 파일 바이트 불변 PASS. 임시 복사본 및 해당 Undo 기록 제거. 원본12개 프리팹 바이트 전후 동일. 기록 Library/MonsterDataVerification/editor-tests.txt.
- 실제 메뉴 실행으로 창 생성 확인. 최종 Unity 컴파일 및 Console 오류0. Play에서 기존 Floor1 후보 Map_01~07 7종/일반방3개 확인. 원본 Floor 설정 그대로 Map05→Map04→Map05→기존 HP240 보스 진행, 일반방 몬스터6개 등록/5종 AI 타겟, 전멸 출구 개방, 보스 전멸/런 클리어/결과 정산 PASS. 처치는 TakeDamage API, 방 이동은 기존 RequestNextRoom/LateUpdate 경로 호출이며 수동 전체 전투나 실제 출구 트리거 재검증으로 주장하지 않음.
- 저장 보호: Play 진입 전 MineDungeon.SaveTestPath를 Library/MonsterDataVerification/character.json으로 설정하고 실제 CharacterSaveService.SavePath 일치 확인 후 던전 시작. 사용자 Editor character.json과 .bak SHA256 전후 동일. 종료 후 편집 모드/timeScale1/AudioListener.pause=false/runInBackground 기존값/저장 override 빈값 복원. 원본 기획 수치는 처음부터 변경하지 않았음.
- 기존 경고: 결과창 Result Korean 폰트에 U+AC31(갱) 글리프 누락 경고1건. 이번 Editor 도구 범위 밖이라 폰트 수정하지 않음. 신규 도구 관련 오류 없음.
- 미검증: 실제 마우스 파일 선택/최종 확인창 취소·승인/물리 Ctrl+Z·Ctrl+Y 조작, 모든 공격패턴·수동 전투/벽 조합, 배포 빌드, 에디터 재시작 후 Undo, 강제 디스크 저장 실패의 복구. API 수준 CSV/적용/Undo/Redo 및 Play 진행 검증과 구분. 2차 배치 편집은 기존 개체/GUID/fileID/등록 참조 보존 필요. 신규 종류·필드를 추가할 때 명시적 카탈로그/검증범위/CSV 호환성과 보스 상속을 함께 점검할 것.
## 2026-10-21 몬스터 데이터 편집기 2차 — 배치 관리
- 수정: Assets/Editor/MonsterDataEditorWindow.cs에 몬스터 수치/몬스터 배치 탭 추가. 기존 수치·CSV 처리 코드는 유지. 추가: MonsterPlacementPanel.cs, MonsterPlacementStore.cs, MonsterPlacementVerification.cs 및 Unity 생성 .meta. 시작 Git 상태 clean. 런타임 코드/밸런스/원본 프리팹/기존 .meta 수정 없음. 커밋/푸시 없음.
- Tools > MineDungeon > 데이터 편집기 > 몬스터 배치. Map_01~07 선택 후 Scene / Prefab Mode에서 열기. 종류별 수량·개체 목록·부모 로컬 좌표 표시, Scene에서 찾기로 개체 선택/프레임 및 기본 W 이동 도구 연결. 선택 개체 위치는 기존 부모 로컬 좌표로 입력 가능. 선택 몸체 여유 원/종류·좌표 Scene 표시. 새로운 다중선택/배치복사/자동편성/런타임 스폰 기능 없음.
- 추가는 기존 5종 공용 프리팹을 기존 Monsters 부모 아래 PrefabUtility.InstantiatePrefab으로 생성하며 AI/HP/보상/몸체/충돌체/내부 참조 및 공용 프리팹 연결 유지. 선택한 일반 몬스터 한 개 삭제 지원. 기존 몬스터는 재생성하지 않음. 추가·삭제와 RoomController.enemies 갱신을 한 Undo 그룹으로 처리. 위치 입력 및 Unity 기본 이동 도구도 Undo/Redo 지원.
- 추가·삭제는 배치 탭 버튼 사용. Hierarchy에서 직접 Delete/복제한 경우 누락·빈 참조 검사를 실행하고 enemies 등록 목록 복구 버튼으로 명시적으로 복구 가능(Undo 지원). 자동으로 지형이나 다른 맵을 수정하지 않음.
- 배치/참조 검사: enemies 누락/중복/빈 값/외부 참조, 필수 AI·체력·보상·몸체/충돌체·시각 참조 및 깨진 참조 검사. 기존 RoomWalkableLayout.ContainsBody/Reachable 재사용, 최소0.65 및 실제 몸체 크기의 보수적 여유 적용. 바닥/암벽/실제 직사각 벽·닫힌 출구 내부, 입구→몬스터/출구 앞 접근성 검사. 겹침 가능/권장 영역 밖/입구 근접/Z 좌표/판정 불확실한 벽은 경고. 동적 전투·혼잡·비직사각 충돌의 확정 판정으로 주장하지 않음.
- Map_02 무작위 출구 두 후보 모두 검사. 별도 작은 preview-scene 프록시에 room/layout 직렬화 정보만 복사해 기존 경로 질의 실행. 원본 SelectExit를 호출하거나 출구·장벽/지형을 이동하지 않음. 검증 전후 원본 컴포넌트 직렬화 상태 불변 확인.
- 저장: 배치 편집은 Unity Prefab Mode를 사용. Auto Save 켜짐이면 추가·삭제·이동이 즉시 맵 파일에 저장될 수 있음. 검사 후 저장하려면 Auto Save를 끄고 배치 탭의 검사 후 이 맵만 저장을 사용. 이 버튼은 확정 오류 시 저장 거부하며 dirty인 선택 맵만 저장. Unity 자체 Auto Save/Ctrl+S는 도구의 오류 차단을 우회할 수 있으므로 반드시 검사 결과를 확인. 1차 수치 탭은 여전히 초안→미리보기→별도 확인 후 적용 방식.
- Editor 검증: 원본7종의 기준 배치 오류0 및 검사 전후 원본/출구 상태 불변. 고유 임시 Assets 복사본에서 5종 각각 추가·삭제·이동/Undo/Redo, enemies 일치, 기존 개체 유지, 원본 능력치·필수 컴포넌트 유지 PASS. 바닥 밖 위치/참조 누락·중복·빈 값 검출과 등록 복구 PASS. 지형/입구/출구/카메라 컴포넌트 직렬화 상태 불변 PASS. 새 nested 몬스터 저장·재로드 후 좌표/등록 유지, 1차 수치 편집 적용·디스크 Undo 및 CSV 왕복 PASS. 기록 Library/MonsterPlacementVerification/editor-tests.txt.
- 실제 편집 창의 배치 탭/Map_01 Prefab Mode 실행 확인, 개체6개 및 배치 검사 오류0. clean stage의 저장 버튼 처리 후 원본 파일 바이트 동일. 원본 일반맵7종/공용 몬스터5종/.meta/Room_Normal_01/관련 Variant/보스/Floor1 데이터는 최종 Git 변경 없음.
- Play 회귀: 격리된 Map_01 복사본에서 기존 두 번째 돌을 삭제하고 같은 로컬 위치에 철을 추가(돌1/철2/수정1/석탄1/자철석1, 총6). 메모리에 복제한 Floor 설정의 일반방 후보만 이 복사본으로 지정하고 기존 일반3방+보스를 실행. 각 방6개 등록/철2/AI 타겟, 전멸 출구 개방, 기존 RequestNextRoom/LateUpdate 경로 이동, 기존 보스 HP240/전멸/런 클리어/결과 정산 PASS. 원본 Floor의 후보7종 유지. 처치는 TakeDamage API이며 수동 전투·물리 출구 트리거 검증으로 주장하지 않음. 기록 Library/MonsterPlacementVerification/play-tests.txt.
- 검증 재개 시 Unity 세션이 새로 시작되어 이전 SessionState 테스트 훅이 비어 있음을 확인. 해당 Play에서 던전 시작 없이 종료한 뒤 테스트 훅을 재설정. 실제 CharacterSaveService.SavePath가 Library/MonsterPlacementVerification/character.json과 일치하는지 확인 후 회귀 실행. 격리 검증 전후 사용자 Editor character.json 및 .bak SHA256 동일. 종료 후 편집 모드/timeScale1/audio false/runInBackground 기존값/저장 override 빈값 복구. 임시 Assets 복사본/폴더 제거 완료. 원본 맵 구성은 변경하지 않음.
- 최종 컴파일/Console 오류0. 기존 결과창 Result Korean 폰트의 U+AC31(갱) 글리프 누락 경고1건은 범위 밖으로 유지. 미검증: 물리 마우스 W 드래그/파일 저장·삭제 확인창/키보드 Undo 조작, 현재방 입구 부활 재검증, 장시간 수동 전투/모든 동적 충돌·스킬 조합, 배포 빌드. Native Auto Save를 끈 상태의 수동 편집 동선은 사용자 확인 필요. 확인된 도구 기능 오류 없음.
## 2026-10-21 일반 몬스터 어그로 / 데이터 편집기 3차
- 추가: Assets/Scripts/MonsterAggro.cs 및 Unity 생성 .meta(상태 없는 최초 시야 질의 유틸리티). 수정: EnemyHealth.cs, StoneMonsterAI.cs, NormalMonsterAI.cs, Assets/Editor/MonsterDataStore.cs, MonsterDataEditorWindow.cs, MonsterPlacementPanel.cs. 신규 컴포넌트/매니저/싱글톤/런타임 데이터 에셋 없음. 시작 Git 상태 clean. 보스/4종 개별 공격 스크립트/맵/공용 프리팹/기존 .meta 수정 없음. 커밋/푸시 없음.
- 확정 규칙: 5종 모두 새 인스턴스는 미인식. 최초에는 반경 안 + 기존 wallLayer의 비트리거 충돌체에 가리지 않은 경우에만 추적·공격 시작. 벽 레이어 밖 바닥·장식 및 트리거는 최초 시야를 막지 않음. 인식 후 거리 이탈/벽 차단으로 해제하지 않고 기존 AI 재사용. 별도 해제거리/타이머/몬스터 간 전파 없음. bool 상태는 비직렬화이며 CancelAttacks/SetTarget/플레이어 사망·부활로 초기화하지 않음. 새 방의 새 개체는 독립된 미인식 상태.
- 피해 연결: 플레이어 기본공격 PlayerAttack 및 스킬 PlayerSkills가 기존 EnemyHealth.TryTakeDamage로 실제 판정함을 확인. 기존 거부 조건(isDead/0 이하 피해/런 종료) 이후 실제 비치명 피해 처리에서 Damaged 이벤트를 발생시키고 일반 AI만 구독. 반경·시야에 관계없이 즉시 인식. 치명 피해는 기존 보상/사망/공격취소 경로 그대로 처리하며 죽은 몬스터를 재활성화하지 않음. 보스는 구독하지 않음. 기존 HP/공격력/이동속도/공격시간/공격패턴 수치 변경 없음.
- 반경 필드 aggroRadius를 StoneMonsterAI/NormalMonsterAI에 직렬화 가능하게 추가. 5종 공통 6은 기능 확인을 위한 임시 기본값이며 최종 기획값이 아님. 최종 상태: 기존 프리팹 파일은 재저장하지 않았고 새 필드가 없는 개체는 코드 초기값6을 실제 사용함(공용5종/맵7종 확인). 사용자가 수치 탭에서 변경·확인 적용하면 기존 프리팹에 값이 저장됨. 검증용 복사본7/CSV9/Play 인스턴스10은 원본에 적용하지 않았고 제거/폐기됨.
- 수치 탭: 표에 인식 반경(임시) 열 추가, 기존 monsterId,field,value CSV에 aggroRadius 지원. 반경0 이상 유한 숫자 검증, 기존 미리보기/일괄 적용/Undo/Redo 경로 재사용. 이전 CSV의 부분행에는 새 반경을 강제로 요구하지 않음. EditorWindow 도메인 재로드 시 기존 초안을 실제 저장값 기반 카탈로그에 덮어써서 새 필드 추가로 초안이 깨지지 않게 처리.
- 배치 탭: Scene 인식 범위 표시 토글와 선택한 몬스터만/전체 몬스터 전환. 현재 프리팹 개체의 실제 직렬화 반경과 월드 위치를 매 Scene 그리기에서 읽어 청록 원/임시 반경 라벨 표시. 위치·반경 변경 시 Scene 재그리기. Handles 기반 Editor 전용이며 실제 게임 화면에는 노출하지 않음. 기존 노란 몸체 여유 원/배치검사/등록복구 기능 유지.
- 문서 관계: MonsterSystem §3.1의 거리 기반 행동과 WORK_STATUS의 과거 무제한 타겟 추적 구현 기록에 대해 이번 요청의 최초 반경+시야 규칙을 우선 적용. 과거 기록은 보존하고 공식 Systems/GDD 문서는 범위 밖으로 수정하지 않음. CombatSystem의 사망 시 공격·효과 정리/부활 규칙 및 MapSystem의 지형·이동 가능 영역을 변경하지 않음.
- Play 어그로 검증: 실제 공용 프리팹을 별도 먼 좌표에 복제한 5종 모두 범위 밖 대기/반경 내 Wall 차단/장식(Default 레이어) 통과 인식·추적/인식 후 범위 밖+벽 뒤 추적 유지/인식 전 실제 양수 피해1로 즉시 인식/피해0 거부 시 미인식/다른 개체 자동 인식 없음 PASS. 초기 조건을 고정하기 위해 timeScale0에서 실제 FixedUpdate를 호출하고 기존 Physics2D 질의/EnemyHealth 피해 경로를 사용. 수동 전투 완료를 의미하지 않음.
- 플레이어 실제 TryTakeDamage 치명타와 기존 ReviveRoutine(별도 검증용 체크포인트)으로 사망/부활 실행. 인식10개체의 latch 유지·사망 중 정지/기존 공격코루틴 정리·부활 후 추적 재개, 미인식5개체는 사망·부활로 깨어나지 않음 PASS. 총75개 어그로 관련 검사. 초기에 백그라운드 실행 설정으로 부활 코루틴이 대기하여 Application.runInBackground=true로 진행 후 기존값 복원. 플레이어 런타임 위치/부활참조/검증 오브젝트 정리.
- Editor 검증: 실제 12종 대상의 격리된 복사본에서 반경6→7 미리보기/12개 저장/Undo/Redo, 모든 컴포넌트 직렬화 수치·참조 원복과 내부 GlobalObjectId 유지 PASS. 기존 맵에 있는 다른 수치 override를 보존하기 위해 검증 계획의 반경 변경만 실행. CSV 반경9 왕복/기존 CSV 호환/빈값·음수·NaN·Infinity·문자 거부 PASS. 실제 Prefab Mode 개체를 이용한 선택 표시/전체 표시/숨김 대상 검사와 갱신된 반경 읽기 PASS. 최초 검사에서는 persistent prefab asset의 자식 선택 제약 및 Prefab Mode 선택 변경의 Undo 컨텍스트 차이를 발견했으며, 선택 검사를 실제 stage에서 수행하고 수치 Undo와 분리한 뒤 재검증 PASS(게임/도구 수치 적용 오류 아님).
- Scene 전체 인식 원은 Library/MonsterAggroVerification/aggro-all.png 캡처로 시각 확인. 원본 Map_01~07의 배치/등록/필수 참조 검사 오류0. 기존 맵 수량/위치·지형/카메라 경계/입구·출구 및 공용 수치 변경 없음.
- 실제 진행 회귀: 원본 Floor1 후보7종 그대로 새 런 시작, 새 방 최초 미인식 확인. Map02→Map01→Map06→기존 보스 HP240, 각 방 enemies 등록/전멸 출구 개방/RequestNextRoom 및 LateUpdate API 이동/보스 전멸/클리어 정산 PASS. 처치는 TakeDamage API이며 물리 출구 트리거/모든 공격·스킬 조합을 재검증한 것은 아님.
- 저장·에셋 보호: Play 전에 테스트 훅을 설정하고 실제 CharacterSaveService.SavePath=Library/MonsterAggroVerification/character.json 일치 검사 후만 수행. 사용자 Editor character.json/.bak SHA256 전후 동일. 공용5종/맵7종/Room_Normal_01/관련 Variant/보스/Floor1 및 각 .meta 총32개 파일 SHA256 전후 동일. 최종 편집 모드/timeScale1/audio false/runInBackground 기존값/저장 override 빈값. 복사본 및 임시 MonsterAggroVerification 스크립트/meta 제거 완료. 기록 Library/MonsterAggroVerification/{editor-tests.txt,play-aggro.txt,progression.txt,placement.txt}.
- 최종 Unity 컴파일/Console 오류0. 검사 도중 MCP WebSocket 재연결 경고는 있었으며 게임 코드 오류 아님. 미검증: 배포 빌드, 수동 UI 클릭/마우스 드래그/키보드 Undo 조작, 현재 실제 방 입구에서의 부활 전체 동선(이번 어그로 검사는 검증용 체크포인트 사용), 장시간 혼합전투/모든 공격·스킬 및 동적 충돌 조합. 최종 인식 반경 밸런스는 미확정이며 6을 확정 기획값으로 해석하지 않을 것. 확인된 미해결 기능 오류 없음.
## 2026-10-22 층·난이도별 몬스터 능력치 / 독립 배치 4차
- 시작 Git 변경: Assets/Prefabs/Rooms/ReferenceMaps/Map_01.prefab의 사용자 변경 존재. 이를 되돌리거나 덮어쓰지 않았으며 현재 저장된 배치를 초기 기준으로 사용. 공용5종/일반맵7종/Room_Normal_01/일반·보스 Variant 및 각 .meta 30개 SHA256 작업 전후 동일. Floor1_RockMine.asset은 monsterProfile 참조1줄만 추가(기존 레벨/후보7종/일반방3개/보스/보상/구현 플래그 직렬화 비교 동일). GDD/Systems 및 사용자 저장 형식 수정 없음. 커밋/푸시 없음.
- 신규 런타임 데이터/처리: Assets/Scripts/MonsterFloorProfile.cs, MonsterMapProfile.cs, MonsterProfileRuntime.cs. 층 프로필의 HP/공격 배율은 공통, 난이도별 배율과 5종 signed int 추가 보정은 독립. 배치 프로필은 층·난이도·맵마다 별도 .asset. 기존 기본 능력치 전체를 SO로 이전하지 않으며 각 배치 entry는 기존 프리팹/맵 내부 EnemyHealth 원본 참조 + 안정 ID + 기존 부모 로컬 위치/회전/배율만 저장. 기존 개체의 경로가 맞으면 해당 방 인스턴스 개체를 재사용, 신규 항목은 원본 프리팹을 복제, 제외된 개체는 Start 전 비활성/제거. 지형은 항상 기존 맵 인스턴스를 사용.
- 계산 확정(사용자 후속 승인): 기본값×층 배율×난이도 배율 → 종류별 추가 보정 → 마지막에 한 번 반올림. 정확히 .5는 큰 정수로 올림(Math.Floor(double 결과+.5)). 기존 SkillData의 짝수 반올림과 다른 사용자 확정 규칙을 우선하며 HP/공격은 각각 독립 계산. 최종 HP1 이상/공격0 이상/int.MaxValue 이하, 유한·0 이상 배율 및 최종값 검사. 음수 추가 보정 지원, 자동 clamp로 잘못된 수치를 숨기지 않음. 개체별 추가 보정은 안정 entry ID 및 종류별 보정과 분리한 예약 필드만 두며 nonzero는 아직 거부(우선순위 미확정/비활성).
- 수정 런타임: DungeonFloorData.CanEnter에 데이터 준비 검사, MineEntranceUI에 기존 레벨 검사 뒤 미설정 안내, DungeonRoomManager에 입장 전 profile 준비 확인 및 일반방 생성 직후/RoomController.Start 전 구성·최종 수치 반영. EnemyHealth.ConfigureSpawnHealth는 max/current HP 함께 초기화, StoneMonsterAI/NormalMonsterAI는 생성 공격력만 설정. RoomController.ConfigureSpawnEnemies는 Start 전만 허용. 원본 프리팹의 기본값을 쓰지 않으며 속도/쿨타임/인식 반경/보상/기존 공격 패턴은 적용 대상 아님. 보스 분기는 이 처리 전체를 건너뜀.
- 미설정 처리(사용자 승인): 기존 IsPlayable/레벨 조건을 유지하고 추가 안전 검사만 적용. 층 밸런스/선택 난이도 밸런스/런의 각 후보 맵 배치가 준비되지 않으면 저장 Flush/런 시작 이전에 입장 거부. bool configured로 미설정과 의도한0마리를 구분; configured=true & entries=[]는 유효한 빈 방으로 Start 시 즉시 클리어/출구 개방. 다른 층·난이도의 배치를 자동 생성하거나 던전 구현 플래그/후보/보스/입장 레벨을 자동 변경하지 않음.
- 초기 데이터: Assets/Dungeons/MonsterProfiles/ 아래 Floor1_RockMine_Monsters.asset + D0_Map_01~07.asset 총8개 추가. 1층 쉬움은 보존용 항등 배율1/보정0과 현재 원본 7맵의 개체 구성·배치·원본 참조를 그대로 스냅샷. 원본 맵 안의 기존 개별 기본값도 사용하므로 현 수치 보존. 다른 난이도는 configured=false/maps빈값, Floor2_Preview는 프로필 자체 미설정. 새 조건의 생성자 표시값1/0은 미설정 초안용일 뿐 콘텐츠나 확정 밸런스가 아님. 인식 반경6은 여전히 기존 임시값이며 이번 층·난이도 계산으로 바꾸지 않음.
- Editor 신규: MonsterBalancePanel.cs, MonsterProfileEditorStore.cs, MonsterProfileStage.cs. 기존 MonsterDataEditorWindow에 층·난이도 밸런스 탭 추가. 층/난이도 선택, 각 배율/5종 보정, 공용 기본값·최종값 및 작성된 맵 개체의 실제 최종값 비교, 변경 전후 미리보기/확인 저장/Undo·Redo. 선택 난이도만 병합 저장하며 의도적으로 공유한 층 배율만 같은 층 전체에 적용. 공유 배율 변경 시 이미 설정된 다른 난이도도 검증. 변경 전 원본 JSON 재확인으로 외부 변경 덮어쓰기 방지, 대상 asset만 저장.
- 배치 탭: 층·난이도 독립 배치 / 기본 프리팹 편집(기존) 모드. 기존 맵7종 선택 및 추가·삭제/W 이동/좌표/몸체·어그로 원/검증 기능 재사용. 독립 모드는 PreviewSceneStage의 별도 Scene 초안에서 원본 맵을 임시 복제·Unpack하고 entry 데이터만 캡처해 저장. 지형/기본 수치/필수 참조 변경은 저장 거부하며 원본 맵은 저장하지 않음. Assets/Scripts/MonsterProfileEntryMarker.cs는 UNITY_EDITOR 한정 preview 메타데이터이며 게임 프리팹/런타임에는 부착하지 않음. Scene 초안 위치 편집과 등록 갱신은 기존 Undo, 프로필 저장 Undo/Redo는 해당 .asset에 재저장. 저장 Undo/Redo 후 Scene 초안은 유지하므로 저장된 배치 다시 불러오기로 저장 결과 확인 가능. 기본 수치 적용 시에는 독립 배치 Scene을 먼저 닫고 새 기본값으로 다시 열도록 차단.
- 사용: Tools > MineDungeon > 데이터 편집기 > 층·난이도 밸런스에서 조건 선택·설정 완료·수치 입력→변경 미리보기→확인 저장. 몬스터 배치 > 독립 모드에서 층/난이도/Map 선택→미설정은 빈 프로필 명시 생성→독립 배치 Scene 열기→버튼 추가·삭제/W 이동→이 배치 설정 완료 체크(0마리 포함)→배치 변경 미리보기→이 조합만 확인 저장. 기본 프리팹 모드는 기존 Prefab Mode/Auto Save 방식 유지. 기존 1차 CSV monsterId,field,value는 유지하며 새 프로필 CSV는 구현하지 않음. 직접 기본 프리팹 개체를 삭제하면 이를 원본으로 참조하던 프로필도 원본 참조를 점검할 것.
- Editor 검증: 원본1층 쉬움7맵의 모든 entry 수량·좌표·HP·공격·원본 참조 일치, 나머지 난이도/층 자동 작성 없음 PASS. 층/난이도 독립 HP·공격 배율/음수·양수 보정/.5 올림/최종 음수 거부 PASS. 격리 프로필 A/B의 같은 맵 서로 다른 구성, 0마리 설정/미설정 구분, Scene 초안 열기/이동 Undo/추가 Undo·Redo/삭제 Undo, 다른 조합 데이터 불변, map profile 저장 Undo·Redo 및 별도 floor balance 배율·보정 저장 Undo·Redo PASS. 기존 CSV 왕복 PASS. 검사 중 Editor 전용 마커의 부착 제약을 UNITY_EDITOR 런타임 어셈블리 위치로 해결했고 Stage 전환의 미사용 테스트 에셋 unload는 재로드해 검증; 최종 재검증 PASS.
- Play 검증: 실제 SavePath=Library/MonsterProfile4/character.json 확인 후 실행. 미설정 데이터 입장 거부(런 시작 전), 원본1층 쉬움 진입 및 각 일반방 HP/공격/좌표/수량 보존, 3방 전멸 출구 개방→기존 HP240 보스→클리어·결과 정산 PASS. 별도 조건 A/B 같은 맵에서 다른 수량과 최종값 일치, 생성 HP max/current 함께 반영, Start 전 enemies 등록/전멸 출구 PASS. 실제 StartDungeon에 메모리 테스트 난이도1을 전달해 해당2마리 구성 및 계산값 적용 확인, 속도/쿨타임/어그로 반경 원본 일치/새 개체 미인식/피격 인식 유지 PASS. 의도한0마리 방은 Start 후 즉시 clear/출구 개방 PASS. 테스트용 배율/보정/입장 설정은 격리 에셋·메모리 복사본에만 사용, 최종 제거.
- 테스트 기록 Library/MonsterProfile4/{editor-tests.txt,play-tests.txt,selected-condition.txt,balance-undo.txt}. 처치는 TakeDamage 및 기존 RequestNextRoom/LateUpdate API를 호출했으며 수동 전체 전투/물리 출구 트리거 테스트로 주장하지 않음. 테스트용 Assets 복사본3폴더/추가 밸런스 테스트 에셋/임시 MonsterProfileVerification.cs/meta 제거. 사용자 Editor character.json 및 .bak SHA256 전후 동일. 편집 모드/Main/timeScale1/audiofalse/runInBackground 기존값/저장 override 빈값 복원. 최종 컴파일·Console 오류0. 기존 결과창 ‘갱’ 글리프 누락 경고1건은 범위 밖으로 유지.
- 미검증: 전체 수동 전투/배포 빌드/실제 마우스 드래그·저장 확인창 및 물리 단축키 조작/현재 방 입구 부활 전체 동선 재검증/장시간 다중 공격·스킬 조합/강제 디스크 실패. 플레이어 부활·저장·보상 코드는 수정하지 않음. 추가 기획 결정: 다른 층·난이도의 실제 배율·종류별 보정·맵별 구성, 추후 개체별 보정 활성화와 우선순위. 데이터 프로필을 작성해도 기존 던전의 미구현 콘텐츠 플래그·일반맵 후보·보스/입장조건은 자동 활성화하지 않음. 확인된 미해결 기능 오류 없음.- 후속 정리 확인: Main 씬의 미저장 표시가 있어 씬 파일은 저장·되돌리지 않음. 비교용 PreviewScene에서 TMP 텍스트 스타일 캐시 차이를 확인했으며, 이 임시 중복 씬 로딩 동안 URP 전역 조명 중복 로그가 발생했다. 비교 씬은 모두 닫았고 실제 Main 씬 파일 변경은 없음. 이 진단 로그와 게임 프로필 동작 오류를 구분함.
## 2026-10-22 데이터 편집기 5차-A — 층·난이도 밸런스 CSV
- Editor 전용: MonsterBalanceCsv.cs/.meta 신규, MonsterBalancePanel.cs/MonsterProfileEditorStore.cs/MonsterDataEditorWindow.cs/MonsterDataStore.cs 수정. 기존 CSV parser를 재사용하고 따옴표 오류의 행 정보를 보강. 런타임/AI/진행/보상/저장 코드, 기본 능력치 데이터 구조, GDD/Systems 문서 수정 없음. 시작 Git 상태 clean. 커밋/푸시 없음.
- 기존 밸런스 탭에 두 CSV 내보내기/가져오기 및 초안 검증·미리보기·확인 적용 추가. 내보내기는 기존 프로필이 있는 모든 층의 저장값 대상이며 미설정 난이도도 상태를 함께 출력. 프로필 없는 층은 생성/출력하지 않음. UTF-8 BOM/CRLF/InvariantCulture 소수점(.) 사용, 쉼표·따옴표는 기존 CSV 인용 규칙 처리.
- 배율 파일 헤더: floorId,difficultyId,floorHpMultiplier,floorAttackMultiplier,difficultyHpMultiplier,difficultyAttackMultiplier,floorConfigured,difficultyConfigured. 종류별 보정 헤더: floorId,difficultyId,monsterId,hpAdjustment,attackAdjustment,floorConfigured,difficultyConfigured. floorId는 실제 DungeonFloorData.id(현재 RockMine01/FutureMine02), difficultyId는 실제 배열 인덱스0 쉬움/1 보통/2 어려움/3 악몽, monsterId는 기존 Catalog의 Stone/Iron/Crystal/Coal/Magnet. FutureMine02는 현재 프로필이 없으므로 가져오기 거부. 두 상태 열은 읽기 전용 확인값이며 생략 가능하고 기존 상태와 다른 값은 거부.
- ID 열은 필수, 숫자 열은 하나 이상 필요. 미포함 행/생략한 숫자 열/빈 숫자 셀은 현재 초안 값을 유지. 배율과 보정 파일을 차례로 가져와 하나의 임시 묶음에 병합 가능. 같은 CSV의 동일 층·난이도(보정은 종류 포함) 중복 행 및 공유 층 배율 충돌은 거부. 이후 파일의 명시 값은 해당 초안 값을 갱신하므로 수정 CSV로 재가져오기 가능. 실패한 구문 가져오기는 이전 초안을 보존. 최종 수치가 아직 잘못된 초안은 유지하되 검증·적용을 차단하여 보완 파일을 불러올 수 있게 함.
- 가져오기는 메모리 복사본만 수정. 직접 입력 초안이 있으면 먼저 저장/초기화하도록 안내하며 CSV 초안 중 직접 편집/조건 전환은 차단. CSV 초안은 창 닫기/재컴파일 시 폐기됨을 UI에 표시. 미설정 configured 상태/배치/참조를 변경하거나 콘텐츠를 활성화하지 않음. 숫자 오류, NaN/Infinity/음수 배율, ID/헤더/중복/충돌/정수 범위 및 최종 HP1 이상·공격0 이상/int.MaxValue 이하 검사. 오류에는 파일/행/입력값 제공. 기존 인용 구문 오류에도 CSV 행 번호 표시.
- 계산은 기존 MonsterFloorProfile.Calculate/ValidateBalance/ValidateMap을 재사용. 기본×층×난이도+종류별 보정 후 한 번만 반올림, 정확히 .5 큰 정수 규칙 유지. 미설정 데이터는 검증용 비영속 복사본에서만 configured를 켜 계산하며 실제 상태 불변. 공유 층 배율 영향을 받는 모든 난이도, 공용5종과 설정된 맵의 각 실제 원본 참조에 대한 최종 HP/공격 전후를 표시. 개체별 예약 보정의 nonzero 거부도 기존 검증 유지.
- 직접 입력/CSV는 동일 ValidateBalanceDraft 및 Save/SaveMany 저장 경로 사용. 전체 대상의 원본 JSON/미저장 dirty/쓰기 가능 상태를 변경 전에 검사. 무변경은 저장하지 않음. 다중 에셋은 한 Undo 그룹으로 저장하고 Undo/Redo 시 해당 경로들만 재저장. 적용 예외에는 Undo 및 적용 전 파일 바이트 복구를 시도하며 복구 실패도 명시. 이는 처리 가능한 저장 실패 보호이며 프로세스 강제 종료/디스크 자체 고장에 대한 트랜잭션 보장을 의미하지 않음. 확인 직전 미리보기 재계산 비교로 기본 프리팹/참조 변경 시 재확인 요구.
- 사용: Tools > MineDungeon > 데이터 편집기 > 층·난이도 밸런스 > CSV 가져오기/내보내기 펼치기. 배율/종류별 보정 파일 각각 내보내기→Excel 편집 및 CSV UTF-8 저장→각 가져오기→CSV 검증·변경 미리보기→확인 후 CSV 변경 적용→확인창 적용. 필요 시 CSV 초안 버리기. 같은 층의 공유 배율은 파일의 해당 행들에 일관되게 입력하거나 변경할 한 행만 남기고 부분 가져오기. 기존 monsterId,field,value 형식은 그대로 유지.
- 실행 검증: Unity MCP의 메모리 C# 검사로 격리된 복사본 두 층 프로필을 사용. 두 CSV 왕복/BOM, 부분 열·행·빈 셀 유지, signed 보정, 실제 Calculate 기반 미리보기 및 맵 개체 표시, 미설정 상태 유지, 30×0.25-1=6.5→7/공격 독립 계산 PASS. 잘못된 ID/누락·중복·알 수 없는 헤더/NaN/Infinity/음수 배율/숫자 아님/중복 행/공유 배율 충돌/인용 오류/설정 상태 변경/정수·최종 범위 오류 거부 PASS. 원본 변경·쓰기 금지 두 번째 대상·다른 미저장 변경 존재 시 첫 번째 대상까지 불변 PASS. 실패한 후속 가져오기의 이전 초안 보존 PASS.
- 저장 검증: 다중 프로필 적용/재로드/Undo/Redo와 무변경 파일 timestamp 유지 PASS. 기존 몬스터 기본 CSV 왕복 PASS. 공유 저장 경로 변경의 회귀 확인으로 격리 배치 프로필 위치 저장·Undo·Redo PASS. 테스트 스크립트를 프로젝트에 추가하지 않았고 Assets/__MonsterCsv5_Test 및 .meta는 MCP로 제거. 검증 기록: Library/MonsterBalanceCsv5/{editor-tests.txt,safety-tests.txt,placement-save.txt,preservation.txt} 및 예시 CSV2개(격리 테스트 ID/값, 게임 데이터가 아님).
- 보존: 기존 Dungeons(밸런스/배치 포함), 공용 몬스터/일반 맵, Scripts, Systems 확인 대상201개 원본 파일 SHA256 모두 동일. 기존1층 쉬움 배치/수치/참조, 기존 .meta/GUID/내부 개체 ID 유지. 신규 밸런스값은 실제 에셋에 적용하지 않음. Play Mode 진입 및 사용자 저장/백업 파일 접근·수정 없음.
- 미검증: 실제 Excel 앱 조작, 파일 선택·확인창 수동 클릭, 물리 단축키 Undo/Redo, 전체 배치 Scene 조작/수동 전투/배포 빌드 재검증, 쓰기 진행 중 강제 디스크 실패·프로세스 종료. 런타임은 변경하지 않음. 최종 컴파일/Console 확인은 아래 후속 기록 참조. MCP WebSocket 재연결 경고가 있었으며 기능 컴파일 오류와 구분함.
- 최종 확인: 컴파일 완료/Console 오류0, 기존 MCP 연결 경고1건. 원본 Floor1 쉬움 CanEnter=true, Play 아님/Main 씬 dirty=false, 임시 Assets 테스트 폴더 없음. 편집기는 새 CSV 영역이 펼쳐진 밸런스 탭으로 열어 둠.

## 2026-10-22 데이터 편집기 5차-B — 선택 맵 즉시 전투 테스트
- 신규: Assets/Editor/SelectedMapTestPanel.cs, SelectedMapTestRunner.cs, Assets/Scripts/DungeonSelectedMapTest.cs 및 .meta. 수정: MonsterDataEditorWindow.cs, DungeonRoomManager.cs, DungeonRunSettlement.cs, CharacterSaveService.cs. 신규 런타임 위치 파일은 전체 UNITY_EDITOR 조건이며, 기존 코드의 테스트 분기/저장 격리 검사도 UNITY_EDITOR 한정. 일반 방 시작 공통 코드를 BeginConfiguredDungeon으로 추출해 기존 일반 입장 검사를 유지한 채 재사용. 신규 전투 시스템/매니저/싱글톤 및 공식 GDD/Systems 변경 없음. 시작 Git clean, 커밋/푸시 없음.
- 사용자 후속 승인: Editor 개발 테스트만 레벨·콘텐츠 구현 조건 우회. 선택 층·난이도의 설정 완료된 밸런스 및 선택 맵의 독립 배치는 필수. 정규 게임의 레벨/구현/미설정 입장 차단은 그대로 유지. 테스트 캐릭터는 씬 기본 상태로 시작하며 실제 사용자 캐릭터를 읽거나 복사하지 않음. 테스트 스킬/회피/물약/부활은 기존 씬 및 전투 코드 사용.
- UI: Tools > MineDungeon > 데이터 편집기 > 선택 맵 테스트. 층/난이도/Map_01~07 선택→설정 오류 확인→테스트 시작. 저장된 데이터만 사용(미저장 편집기 초안 제외). 배치/프리팹 Stage 또는 미저장 씬에서는 먼저 정리하도록 시작 거부. 설정 완료된0마리와 미설정을 구분. 동일 조건 재시작/테스트 종료 제공, 테스트 중 편집기 창을 닫아도 Editor Play 중지로 종료 가능. 마지막 기록은 Editor SessionState에만 유지하고 새 테스트 시작 시 초기화.
- 생성/진행: Main을 playModeStartScene으로 지정해 기존 편집 씬을 덮어쓰지 않고 Play. 매번 전체 Domain/Scene reload를 사용해 이전 객체/싱글톤/이벤트/코루틴을 제거. 런용 비영속 floor 복사본의 후보만 선택 맵1개로 구성하고 기존 BeginConfiguredDungeon→LoadRoom→MonsterProfileRuntime.Apply→RoomController.Start 흐름을 사용. 실제 공용/맵/밸런스/배치 에셋은 변경하지 않음. 생성 직후 enemies 순서·종류·로컬 위치·max/current HP·공격을 실제 프로필 및 기존 Calculate와 대조해 불일치 시 종료. 물리 갱신 전 검증하므로 이후 기존 Rigidbody 충돌 보정/AI 이동과 구분.
- 완료/실패: 선택 일반 방 전멸은 테스트 성공, 기존 부활2회 소진 후 사망은 실패. Editor 전용 완료 경로로 공격/효과/추적을 종료하고 기존 IsRunEnded 제한 사용. 다음 방/보스 이동을 막고 일반 클리어 보상/정산 marker/RunEnded 결과창 이벤트/일반 재도전 큐는 호출하지 않음. 일반 몬스터 처치 보상은 기존 코드로 격리 런에서만 발생하며 해당 임시 파일과 함께 폐기.
- 기록: 기존 DungeonResultRecord의 게임 시간 seconds(일시정지 제외), 실제 HP 감소량 damageTaken/damageDealt(초과 피해 제외), kills를 그대로 재사용. PlayerHealth.Died 이벤트로 사망 횟수, 해당 배치 entries 길이로 전체 수 집계. 성공/실패/사용자 종료/오류 표시. 별도의 피해 계산/공격 코드 및 영구 테스트 기록은 추가하지 않음.
- 저장 보호: 시작 전에 Library/SelectedMapTests/<새 GUID>/character.json만 허용하고 경로 정규화/전용 부모·GUID·파일명 및 쓰기 probe 검사. 기존 MineDungeon.SaveTestPath 훅 설정, CharacterSaveService.Awake에서 LoadOnce 이전에 실제 선택 경로 검사하여 불일치 시 LoadBlocked/ready 미완료로 저장 차단. 실제 서비스 SavePath를 재확인한 뒤에만 전투 시작. 테스트 중 경로/씬/서비스 상태 변경 및 오류 시 Play 중지. 원본 character.json/.bak 삭제·초기화·이전 경로 없음.
- 복원: OnApplicationQuit의 마지막 Flush가 끝날 때까지 override 유지하고 EnteredEditMode 후 기존 override/playModeStartScene/EnterPlayModeOptions/runInBackground/timeScale/audio 복원. 자기 소유 GUID 디렉터리만 제거. 재시작 요청은 SessionState에 보관하여 Play 종료 Domain reload로 유실되지 않음. 스크립트 재로드 중에는 테스트를 중단해 안전하게 복원. 비영속 런 floor는 manager 소유 직렬화 참조로 유지 후 singleton 파괴 순서와 무관하게 해제.
- 격리 선검증: 실제 사용자 Editor 저장 경로의 character.json/.bak SHA256 기록, 소유 테스트 경로 허용/사용자 경로 거부/Play 이전 Cleanup의 override 복원 PASS. 그 뒤 실제 Play 검증 수행. 기록 Library/SelectedMapTestVerification/isolation-preflight.txt.
- 실제 선택 테스트: 원본 Map04 즉시 진입/실제 SavePath 일치/등록6마리/입구 위치/AI 활성/피격 어그로/초과 피해를 제외한 피해·처치 기록 PASS. 실제 PlayerHealth 피해로 사망3회, 현재 방 입구 부활2회, 실패·settled=false 확인. 동일 조건 재시작의 새 경로/이전 파일 제거/플레이어·몬스터 HP/물약3·부활2/피해·처치·사망 기록 초기화 PASS. 전멸 성공/6처치·190실제피해/일반 정산 marker 없음/다음 방 요청 무시 PASS.
- 격리 프로필 복사본 검증: 미구현·필요레벨99의 난이도를 기본레벨 캐릭터로 개발 테스트 진입, 동일 원본의 일반 CanEnter 거부 유지 PASS. 독립1마리 구성 및 임시 층HP1.5×난이도HP2+종류보정2로 HP92, 공격2×0.5+보정3으로 공격15, 생성 검증 일치 PASS(기본 Stone30/12). 설정 완료된0마리 방 즉시 성공/시간0/기록0 PASS. 내부 오류 종료 후 경로·설정 복원 PASS. 임시 수치는 복사본에만 사용하고 Assets/__SelectedMapTestVerify 및 meta 제거.
- 재컴파일 검증: 원본 Map01 생성 직후 종류/좌표/최종 수치 검증 PASS. 전투 중 실제 RequestScriptCompilation 후 안전 종료/override 빈값 복원/Active=false/비영속 테스트 floor0 확인. 초기 검증에서 prefab asset의 activeInHierarchy=false를 런타임 유효성 검사와 구분했고, 초기 Play 전환 오판정/재시작 delegate 유실/숨긴 런 floor 수명 문제를 수정 후 재검증. 물리 진행 뒤 좌표를 .01 오차로 비교한 최초 검사1건은 기존 물리 보정 때문에 불일치였으며 생성 직후 순서별 검증으로 재확인, 원본 좌표 수정 없음.
- 정규 회귀: 별도 Library/SelectedMapTestVerification/regular-character.json 실제 SavePath 확인 후 일반 StartDungeon(Floor1,0), EditorSelectedMapTest=false/총4방 확인. 원본 후보를 유지한 Map06→Map03→Map05→기존 보스HP240, 각 방 전멸/이동→일반 클리어/settled=true PASS. 처치는 TakeDamage, 이동은 RequestNextRoom 및 기존 LateUpdate 호출이며 수동 전투/물리 출구 트리거 주장이 아님. 종료 후 해당 수동 검증 override와 Editor 설정 복원.
- 보존/최종: Dungeons/몬스터·방 프리팹/씬/Systems 및 meta78개 원본 SHA256 모두 동일. 사용자 character.json/.bak 최종 해시 동일. 기존1층쉬움7조합 데이터 검증 및 CanEnter=true. Main 편집 모드/dirty=false/테스트 Active=false/저장 override 원래값/임시 Assets 없음. 컴파일 및 Console 오류0. 정규 결과창에서 기존 ‘갱’ 글리프 누락 경고가 있었으며 관련 폰트 수정은 범위 밖. 근거: Library/SelectedMapTestVerification/{play-record.txt,restoration.txt,bypass-and-final.txt,zero-room.txt,regular-regression.txt,reload.txt,final.txt,user-save-after.txt,asset-preservation.txt}.
- 미검증: 실제 Game 뷰 키보드/마우스의 평타·모든 스킬·물약·회피 전체 조합 및 장시간 수동 전투, 모든7맵 전체 전투, UI 버튼 수동 클릭, 배포 빌드, Editor 강제 종료/OS·디스크 장애. 테스트 분기는 소스상 UNITY_EDITOR로 제외되지만 실제 배포 빌드를 실행한 것은 아님. 현재 테스트 전용 플레이어 성장/장비 선택 기능은 범위 밖이며 씬 기본 캐릭터 사용. 확인된 미해결 기능 오류 없음.
## 2026-10-22 선택 맵 테스트 투명벽 수정
- 범위: Assets/Scripts/MineEntranceUI.cs, Assets/Editor/SelectedMapTestRunner.cs만 기능 수정. 기존5차-B 등 미커밋 변경을 보존. 맵/로비 지형, Collider2D 설정, 프리팹/.meta/GUID, 플레이어 이동·전투·저장 로직 수정 없음. 커밋/푸시 없음.
- 실제 원인: 일반 입장 UI의 Enter는 DungeonRoomManager.StartDungeon 성공 후 Close/lobbyRoot.SetActive(false)/prompt 초기화를 수행한다. 선택 테스트는 같은 Main 씬에서 EditorStartSelectedMap으로 방을 만들면서 이 입장 마무리를 누락했다. 별도 씬 전환이나 맵 중복 생성이 아니라 Main 로비와 생성 맵이 동시에 활성화된 문제.
- 수정 전 실제 Play/격리 SavePath 확인 후 재현: Map01 spawn(-10,-17)에서 위로 이동 시 player(-10,-9.91) 정지. 실제 Rigidbody2D contact 대상 MainMineEntrance/SouthWall, 소속 Main, 충돌체 중심(0,-9), extents(15,.5), 접촉점(-9.58,-9.51)/(-10.42,-9.51), normal(0,-1). 로비 North/South/West/EastWall4개가 모두 active/enabled/non-trigger였음. 생성 DungeonRoom1개, DontDestroyOnLoad 씬의 활성 충돌체0개로 중복 방/로비 영속 객체 원인은 배제. 초기 upward cast와 실제 접촉을 함께 확인.
- 최소 수정: 기존 입장 마무리3개 동작을 MineEntranceUI.CompleteEntry로 추출하고 일반 Enter가 그대로 호출. EditorCompleteDungeonEntry(UNITY_EDITOR 한정)는 방 입장 성공 후 동일 helper를 호출. SelectedMapTestRunner에서 선택 방 생성 성공 직후 이 경로를 호출. 개별 충돌체를 삭제하거나 enabled/layer/trigger/크기를 변경하지 않으며, 기존 일반 입장과 동일하게 로비 루트 전체의 런타임 활성 상태만 변경.
- 수정 후 선택 Map01: lobby active=false/활성 로비 충돌체0/방1개. 동일 위쪽 이동은 이전 투명벽 위치를 통과하고 (-10,-8.41)의 실제 맵 BlockingRock에 정상 정지. S자 통로에 맞춰 오른쪽(10,-8.41)→위(10,-0.42)로 물리 이동하여 내부 진입 PASS. 아래 방향 외곽 이동은 (-10,-19.59)에서 기존 맵 BlockingRock과 접촉점 y=-19.99로 정지해 경계 충돌 유지 확인. 최초 경로 확인에서 x=-5.2는 원래 S자 암벽 범위였으므로 통과하지 않는 정상 충돌을 확인한 뒤 열린 오른쪽 통로로 재검증, 지형 보정 없음.
- 동일 조건 재시작 후에도 로비 충돌체0/방1개/동일 벽 접촉 및 내부 이동 PASS. 생성 직후 독립 배치/최종 능력치 VerifySpawn=true, 실제 비치명타 피격 어그로/전멸/테스트 성공 PASS. 다른 일반 맵 Map04에서도 로비 충돌체0/방1개이고, 위 이동에서 정지하는 대상은 Map04/Map/BlockingRock/Rock(중심 -8.75,-9)으로 기존 맵의 실제 암벽임을 확인. Map04 전체 경로 이동 주장은 아님.
- 일반 입장 비교: 별도 Library/SelectedMapCollisionFix/regular-character.json 실제 SavePath 확인 후 Main의 MineEntranceUI.TryOpen→Enter 사용(원본 Floor1 후보7종 유지, 재현용 난수 시드로 Map01 선택). 일반 런 EditorSelectedMapTest=false, spawn(-10,-17), 로비 비활성/충돌체0. 같은 PlayerMovement.FixedUpdate와 Physics2D 실제 물리 시뮬레이션으로 위/오른쪽/위/외곽 이동을 반복했으며 선택 테스트와 출력 좌표/접촉 대상/내부 도달 결과가 완전히 일치. 실제 ReturnToMain 후 로비 활성/충돌체4/IsPreparing=true 확인하여 정상 로비 이동용 벽 유지.
- 검증 방식/보호: 키보드 입력을 대신해 기존 PlayerMovement의 moveInput을 검사 중만 지정하고 기존 FixedUpdate 및 Physics2D.Simulate(.02)를 호출, physics simulationMode/입력/속도는 finally에서 복원. 전투 처치는 기존 TakeDamage API 사용. 실제 수동 전투·입력 주장이 아님. 매 Play 전에 실제 격리 경로 확인, 사용자 Editor character.json/.bak SHA256 최종 동일. 수동 일반 비교의 override/Play 옵션/start scene/runInBackground 복원. 선택 테스트 임시 저장은 기존 Cleanup으로 제거, 일반 비교의 임시 json/bak/tmp도 정리. Assets 테스트 에셋/스크립트를 추가하지 않음.
- 최종: 맵/몬스터/방/씬/던전 데이터 및 meta 확인 대상66개 SHA256 모두 동일. 편집 Main dirty=false/테스트 Active=false/SaveTestPath 원래값 복원. 관련 컴파일 및 Console 오류0, git diff --check 오류 없음. 원래 미커밋 파일들은 보존. 근거 Library/SelectedMapCollisionFix/{before-colliders.txt,before-movement.txt,selected-restart-after.txt,normal-entry.txt,normal-after.txt,lobby-return.txt,map04-after.txt,combat-after.txt,final.txt,preservation.txt}.
- 미검증: 나머지5맵의 실제 이동 재주행, Map04 전체 경로, 실제 키보드·마우스 전체 전투/스킬·물약·회피 및 사망·부활 재검증, 배포 빌드. 해당 전투/이동/AI/어그로/부활 코드는 변경하지 않음. 확인된 미해결 투명벽 문제 없음. 공식 GDD/Systems 문서 변경 없음.
## 2026-10-22 몬스터 데이터 편집기 6·7차 통합 — 배치 그룹 편집 / 전투 기록 분석
- 범위는 Editor 전용. 신규 Assets/Editor/MonsterPlacementBatch.cs, MonsterPlacementCopyPanel.cs, SelectedMapTestHistory.cs, SelectedMapTestHistoryPanel.cs 및 .meta. 수정 MonsterPlacementPanel.cs, MonsterPlacementStore.cs, SelectedMapTestPanel.cs, SelectedMapTestRunner.cs. 런타임 AI/전투/진행/저장 코드와 기존 MonsterMapProfile 형식, GDD/Systems는 수정하지 않음. 기존 Git 변경 되돌림 및 커밋/푸시 없음.
- 6차 다중 선택: 배치 목록 체크박스와 Unity Selection 연동. 현재 맵의 선택 목록·수량 표시, Scene/Hierarchy Ctrl·Shift 및 기존 W 도구의 다중 이동 활용. Monsters 부모 로컬 이동량으로 일괄 이동(상대 위치 유지), 확인 후 일괄 삭제, enemies 자동 동기화. 기존 한 개 선택/좌표/추가/삭제 유지. 선택 인식 범위 표시도 여러 선택 개체를 사용. 추가된 긴 UI는 기존 창 안에서 스크롤.
- 일괄 추가: 종류/수량(1~1000)/열 수/열·행 간격/기준 로컬 위치의 명시적 고정 격자. 공용 기존 프리팹을 PrefabUtility로 생성하고 기존 AI/HP/보상/필수 참조 및 prefab 연결 유지. 전체를 하나의 Undo 그룹으로 처리하고 기존 지형·참조·입구/출구 접근성 검사에서 확정 오류가 나오면 추가 전체를 취소. 임의 랜덤 좌표 생성 없음. 개별 위치는 기존 Scene/좌표 편집으로 조정.
- 그룹 복사: 선택한 종류와 첫 개체 기준 상대 위치를 Editor SessionState 클립보드에 저장. 같은/다른 맵에서 기준 위치를 입력해 붙여넣기. 새 개체는 새 배치 ID와 공용 원본을 사용하며 기존 개체를 재생성하지 않음. 붙여넣기 예상 좌표를 목록으로 표시하고 전체 추가 후 검증/오류 롤백. 벽/바닥/접근성뿐 아니라 기존 몸체 여유 Radius 기준 겹침을 오류로 처리해 복사/이동 후 저장 차단. 오류에 개체 이름과 로컬 좌표 표시. 기존 불확실한 지형/실제 동적 혼잡은 기존 경고 유지.
- 독립 프로필 복사: 배치 탭의 전용 펼침 영역에서 원본 층/난이도/맵과 대상 층/난이도/맵을 별도 지정. 대상 미생성 시 기존 빈 프로필 생성 기능으로 먼저 만들도록 안내(복사가 프로필/밸런스를 자동 활성화하지 않음). 원본은 설정 완료 필요, 의도한0마리는 설정 완료 상태로 지원. 종류/좌표/회전/배율만 복사하고 공용 몬스터 참조/새 ID 사용. 대상 configured/맵 참조/층·난이도 밸런스는 유지. 개체별 미지원 예약 보정 nonzero는 거부.
- 복사 저장: 원본·대상 목록과 수량/좌표 비교→대상 맵 지형 검사→기존 배치를 전부 대체한다는 명시 확인→기존 Save 경로로 대상 .asset만 저장. 원본/대상 JSON 및 층 프로필 연결 변경 시 재비교 요구. 독립 Scene 초안은 닫고 적용하며, 현재 Scene 초안은 기존 패널에 유지. Undo/Redo/변경 없는 저장 생략/쓰기 실패 보호는 기존 저장 경로 재사용. 기본 프리팹 모드와 독립 프로필 모드를 UI에서 구분. 기본 프리팹 Auto Save의 기존 동작과 주의문은 유지.
- 7차 기록: Library/MineDungeonEditor/SelectedMapTestHistory.json에 개발용 기록 보관, 프로젝트 기준 절대 경로 사용. 캐릭터 진행/보상과 무관하며 에셋/캐릭터 저장에 연결하지 않음. 실행마다 새 runId/UTC 시작 시각 및 당시 층 ID·이름/난이도/맵을 캡처. 기존 live Record의 전투 시간/실제 받은·가한 피해/처치·전체 수/사망 횟수 재사용. success/failed/stopped/restarted/error 상태. 완료 및 종료 시 기록하며 재시작은 별도 실행. 하나의 runId를 upsert하고 성공·실패 완료 기록은 보존해 완료→Stop→Play 종료→Cleanup 중복 방지. Stop 직전의 실제 런 수치도 다시 캡처하여 최근 프레임 누락 방지.
- 기록 파일은 임시 파일 작성 후 기존 파일 교체(.bak)로 저장. 손상된 기존 파일은 덮어쓰지 않고 오류 안내, 기록 쓰기 실패는 테스트 종료/저장 경로 복원을 막지 않음. 완료 ID를 SessionState에 기억해 매 프레임 반복 기록 저장 방지. Library 삭제 시 기록도 제거되므로 보관할 결과는 CSV로 내보낼 것.
- 분석 UI/CSV: 선택 맵 테스트 탭 아래 비교 표, 현재 선택 조건만 필터 가능. 표시 기록 CSV 내보내기, UTF-8 BOM/CRLF/InvariantCulture. 헤더 runId,startedAtUtc,floorId,floorName,difficultyId,mapId,status,seconds,damageTaken,damageDealt,kills,totalMonsters,deaths. 테스트당 한 행, 반복 조건은 runId/시각으로 구분. 문자열 쉼표/따옴표/줄바꿈 인용 및 Excel 수식 해석 방지. 결과 CSV 가져오기 없음. 기존 monsterId,field,value 및 밸런스 CSV 형식 변경 없음.
- 6차 격리 검증: 기존7개 원본 프로필을 새 겹침 오류 기준으로 검사해 모두 오류0. 임시 B Scene에서 공용 prefab 연결을 가진2마리 일괄 추가/Unity 다중 선택/그룹 이동 상대 위치/이동 Undo·Redo/그룹 삭제 Undo·Redo/그룹 붙여넣기 새 ID PASS. 같은 위치 겹침 붙여넣기는 전체 롤백 및 참조 보존 PASS. 독립 Scene Capture/지형·접근성 검사, 원본·다른 프로필 불변, 선택 프로필 저장·Undo·Redo·재로드 PASS.
- 복사 검증: 설정 완료/새 ID 유지한 프로필 전체 복사 및 대상만 변경/Undo·Redo PASS. Map01→Map04 잘못된 좌표는 대상 지형 검사 및 저장 차단, 대상 데이터 불변 PASS. 유효한 1마리의 다른 맵 복사 저장·재로드 PASS. Map01에서 복사한 그룹을 Map04 초안에 붙여넣기2마리 및 범위 밖(999,999) 추가 전체 취소, 원본 Map04 프로필 불변 PASS. Stage 전환 시 테스트의 로컬 변수만 참조한 임시 에셋 unload와 원본 수량 고정 가정은 테스트에서 에셋 재로드/실제 수량(현재 Map01 독립 프로필10) 사용으로 바로잡고 후속 검증 PASS. 실제 원본 수량을 테스트에서 바꾸지 않음.
- 7차 격리 검증: 완료→종료→오류 중복 archive 한 건, 같은 조건의 다른 실행 ID, 완료 상태 보존, 수치 재로드, CSV 한 행/CRLF/UTF8 BOM 및 쉼표·따옴표·줄바꿈 왕복 PASS. 손상된 파일 덮어쓰기 거부/읽기 전용 기록 파일 쓰기 실패 시 기존 내용 유지 PASS. 임시 개발 기록은 검증 후 기존 파일 상태로 복구(작업 전 기록 없었음).
- 실제 Play: 실제 SavePath가 새 Library/SelectedMapTests/<GUID>/character.json인지 확인 후 실행. 원본 Map01 독립10마리/최종 수치 VerifySpawn=true/로비 비활성 유지. 실제 EnemyHealth 처치1회 및 PlayerHealth 치명타로 받은 피해100·사망1 기록 후 재시작. 새 저장/플레이어 full HP/기록 초기화 후10마리 전멸 성공, 일반 정산 없음. 첫 실행 restarted(사망1/처치1/전체10), 다음 실행 success(사망0/처치10/전체10) 각각 한 행이며 Stop·Cleanup 후 중복 없음 PASS. 처치는 API 호출로 수동 전체 전투 주장이 아님.
- 공통 회귀: 별도 격리 저장의 일반 MineEntranceUI 입장/기존6마리 방 등록/전멸/다음2번 방 이동, 일반 플레이가 테스트 이력에 추가되지 않음 PASS. 입장과 같은 프레임에 죽이는 최초 검사 대신 실제 RoomController.Start 등록 후 검사하여 재확인. 기존 기본 수치 CSV와 층·난이도 배율 CSV 왕복/검증 PASS. 로비 정리 코드는 수정하지 않았고 선택 테스트에서 비활성 확인. 사용자 Editor character.json/.bak SHA256 최종 동일. 기존 에셋/프리팹/씬/던전 프로필/Scripts/Systems/meta216개 SHA256 모두 동일.
- 정리/근거: Assets/__Monster67Verify 및 meta 제거, 임시 런 저장/일반 회귀 character 파일 정리, 테스트 클립보드/실행 메타/화면 live 기록 초기화. 검증용 기록 JSON/bak는 작업 전 상태로 복구하고 결과 CSV/검증 로그만 Library/MonsterEditor67Verification에 남김. placement-tests.txt의 초기 사후검사 unload 오류 이후 profile-tests.txt/cross-map-valid.txt/group-cross-map.txt가 재로드 검증 근거. 그 외 original-validation.txt/history-tests.txt/play-history.txt/results.csv/play-results.csv/regular.txt/final.txt/preservation.txt. 최종 MainStage/편집 모드/씬 dirty=false/테스트 Active=false/저장 override 원래값/임시 Assets 없음.
- 컴파일 및 Console 오류0, 기존 Mine Entrance Korean 폰트의 일부 글리프 누락 경고5개(항/별/독/립/첨)는 범위 밖으로 유지. git diff --check 오류 없음. 미검증: 실제 마우스 다중 선택/W 드래그·단축키 및 확인창 수동 조작, Excel 앱 직접 조작, 장시간/대량1000개 성능, 모든 맵 전체 수동 전투/모든 스킬·회피·물약·부활 조합, 이번 작업에서 전체3방→보스→정산 재주행, 배포 빌드/강제 디스크·프로세스 장애. 기존 런타임/보스/보상/저장 코드 변경 없음. 확인된 미해결 기능 오류 없음. 신규 대상 프로필은 기존 생성 기능으로 먼저 준비해야 함.
## 2026-10-22 1층 쉬움 몬스터 밀도 테스트 — 사전 확인 / Unity 연결 대기
- 요청 목표 Map01~07: 10/12/11/10/12/11/10. 실제 Floor1_RockMine_D0_Map_01~07.asset entries와 기존 좌표를 확인한 현재 수량: 10/6/6/6/8/6/6. 필요한 추가 수량: 0/6/5/4/4/5/4. Map01은 목표 충족으로 수정 불필요. Map05의 기존 추가2개를 포함한 모든 배치는 보존.
- 시작 Git 상태 clean. MonsterMapProfile, 기존 프로필 저장/배치 Stage/선택 맵 테스트 기능 및 6·7차 기록, 지정 Systems 문서의 독립 배치 관련 내용을 확인. 수량은 프로필 저장값 기준이며 실제 생성 검증 결과가 아님.
- 이번 세션에는 Unity MCP 도구가 제공되지 않아 Editor의 미저장 초안/컴파일 상태 확인, 기존 배치 검사 및 선택 맵 Play 테스트를 실행할 수 없음. 기존 편집기 검증과 저장 보호 절차를 건너뛰어 프로필을 직접 수정하지 않음.
- 프로필/지형/프리팹/.meta/AI/능력치/보상/다른 층·난이도/캐릭터 저장 파일은 변경하지 않음. 변경 파일은 이 작업 기록뿐. 목표 수량 적용은 아직 미완료.
- 미검증: 배치 유효성, 실제 생성 수량·위치·몬스터 동작, 전멸 출구 개방, 컴파일 및 Missing Script, 기존 진행 회귀. 전투 시간·획득 경험치 측정 없음. Unity MCP 재연결 후 현재 데이터와 미저장 초안을 재확인하고 기존 편집 기능으로 부족한 개체만 추가·검증할 것. Git 커밋/푸시 없음.

## 2026-10-22 1층 쉬움 몬스터 밀도 테스트 — 재연결 후 적용·검증 완료
- 앞선 연결 대기 기록의 후속 실행. Unity MCP 정상 연결, MainStage/씬 dirty=false, 시작 Console 오류0 및 기존7개 배치 검사 오류0 확인. 초기 밸런스 시험이며 최종 배치 확정 아님.
- 목표 적용: Map01 10→10(파일 불변), Map02 6→12, Map03 6→11, Map04 6→10, Map05 8→12, Map06 6→11, Map07 6→10. 추가 총28마리. 기존5종을 사용하고 수가 적은 종류부터 보완. 최종 돌/철/수정/석탄/자철석: 01=2/2/3/2/1, 02=3/3/2/2/2, 03=3/2/2/2/2, 04=2/2/2/2/2, 05=3/3/2/2/2, 06=3/2/2/2/2, 07=2/2/2/2/2.
- 변경 에셋은 Assets/Dungeons/MonsterProfiles/Floor1_RockMine_D0_Map_02.asset ~ Map_07.asset 6개만. Unity MCP에서 기존 MonsterProfileStage / MonsterPlacementStore.Add·Validate / MonsterProfileEditorStore.Save를 사용. 새 AI/스폰/편집 시스템 또는 검증 스크립트 에셋 추가 없음. 기존 항목의 ID/template/embeddedPath/좌표/회전/배율/예약보정은 전후 개별 JSON 비교 동일. 기존 Map05 추가2개도 보존. 새 항목은 기존 공용 몬스터 프리팹 참조, 새 ID, 예약보정0.
- 배치 검사: 7맵 모두 오류0. 몸체 여유, 겹침, 실제 벽·바닥, 입구→개체·출구 접근성 및 참조 검사 재사용. Map04는 임시 후보 탐색의 중심 간격2~2.5 제한으로 초안 추가가 부족하여 저장하지 않고 위치를 재선정. 최종 추가4개는 월드(1.75,15.75)/(5.25,11)/(12.35,5.65)/(12.35,-3.95), 기존 검사 및 실제 움직임 통과. 지형/기존 개체 이동 없음.
- 권장 영역 밖 경고는 기존 Map01 개체5개 및 기존 Map05 하단 추가2개에서 유지. 새 개체의 영역 밖 경고는 최종 좌표에서0. 모든 맵에 기존 도구의 동적 전투·혼잡은 Play 확인 필요 안내가 있음. 기존 유효 배치를 경고만을 이유로 이동하지 않음.
- 기존 SelectedMapTestRunner.Start를 맵01~07 각각 실행. 실행마다 실제 CharacterSaveService.SavePath가 Library/SelectedMapTests 아래 고유 경로임을 확인. 기존 VerifySpawn으로 생성 종류/수량/좌표/최종 HP·공격/등록 참조가 저장 프로필과 일치함을 재검사. 실제 생성 수량10/12/11/10/12/11/10 확인.
- 실제 Play 동작 검사: 플레이어 무적·위치 고정 후 각 몬스터에 기존 TakeDamage(1)를 호출해 피격 어그로를 유도. 모든76개체에서 HasAggro=true 및 시작 위치 대비0.1 초과 이동 확인. 관측 시점의 몸체 중심 주변0.35 여유가 이동 바닥에 있음을 검사(전체 공격 궤적/모든 프레임의 충돌 검증은 아님). 이어 기존 TakeDamage 치명타로 전멸, 각 맵 IsCleared/RemainingEnemies0/RoomExit.IsOpen/선택 테스트 성공 확인. 수동 전투 완료 또는 모든 공격패턴 검증으로 주장하지 않음.
- 기존 선택 테스트 기록에 맵별 success 7건을 추가하고 기존 Stop/Cleanup 경로로 종료. Library/MineDungeonEditor/SelectedMapTestHistory.json의 기록을 삭제/초기화하지 않음. 자동 기록된 seconds는 도구 대기와 API 처치를 포함하므로 밸런스 전투 시간으로 보고하지 않음. 획득 경험치 별도 측정 없음.
- 보존 확인: Assets/Prefabs·Dungeons·Scripts·Scenes 및 지정 Systems 문서·사용자 Editor character.json/.bak 등232개 파일 SHA256 비교에서 대상 프로필6개만 변경, 나머지226개 동일. .meta/지형/능력치/고유공격/어그로/시야/보상/런타임/일반방3→보스 설정 및 다른 층·난이도 불변. 프로필 기존 부분은 diff에서도 삭제/변경 없이 추가 항목만 존재.
- 종료 상태: 편집 모드/MainStage/씬 dirty=false/선택 테스트 Active=false/SaveTestPath 빈값/timeScale1/runInBackground false. 컴파일 중 아님, Console 오류·경고0. 사후 확인용 MCP 반사 조회의 잘못된 타입 위치는 직접 참조로 수정해 재확인(게임 오류 아님).
- 근거: Library/DensityTest/baseline.txt, final-placement.txt, play.txt, final.txt, before-hashes.json, history-snapshot.json. 미검증: 수동 전투 시간·XP, 장시간 밀집 전투·모든 공격/회피/스킬/부활 조합, 최초 시야 인식 전체 회귀, 일반3방→보스 전체 재주행, 배포 빌드. 확인된 미해결 배치/생성/전멸 오류 없음. GDD/Systems 변경 및 Git 커밋/푸시 없음.

## 2026-10-22 몬스터 데이터 편집기 8차 — 층·난이도 경험치 관리
- 사용자 승인: 정규 입장은 기존 콘텐츠·레벨·밸런스·배치 조건 및 검사 순서를 유지하고 EXP 설정 완료 검사를 추가. 미설정은 구체적인 층·난이도 안내로 차단하며, 설정 완료된 EXP 0은 허용. 다른 조건을 자동 활성화하지 않음.
- 데이터: 기존 MonsterFloorProfile.Difficulty에 experienceConfigured, 일반 5종 int[5] experience(돌/철/수정/석탄/자철석 순), bossExperience 추가. 기존 프로필에 1층 쉬움만 4/5/5/4/6, 돌골렘45를 저장. 승인된 초기 테스트 수치이며 최종 밸런스 아님. 보통/어려움/악몽은 EXP 미설정 유지, 다른 층 프로필 생성 없음. 기존 6개 MonsterRewardData 기본 EXP는 수정하지 않음.
- Editor: 기존 메뉴의 '경험치' 탭에서 층·난이도 선택 → 6종 EXP 정수 입력/설정 완료 → 변경 미리보기 → 확인 후 저장. 음수·소수·NaN·빈값·int 초과 차단. 기존 MonsterProfileEditorStore의 원본 변경 감지·트랜잭션 저장·Undo/Redo를 재사용하며 선택 조합 EXP만 병합. 기존 기본 수치/밸런스 CSV 형식은 유지하고 EXP CSV 가져오기는 추가하지 않음.
- 분석: 독립 배치에서 Map_01~07의 종류별 수량과 EXP 합계를 조회. 미설정 배치/EXP와 설정 완료된 빈 방을 구분. 실제 normalRooms 후보 인덱스 및 normalRoomCount를 사용하고 첫 방 균등/이후 직전 인덱스 제외 규칙을 동적 계획법으로 계산. 후보1개와 중복 프리팹 참조도 기존 인덱스 선택 규칙대로 처리. 모든 필요한 데이터가 유효하지 않으면 전체 분석을 미확정으로 표시.
- 런타임: DungeonRoomManager의 기존 배치·능력치 초기화 직후 생성 개체의 EnemyReward에 비직렬화 EXP override를 주입. 일반 몬스터는 종류별 값, 돌골렘은 보스 값 사용. EnemyHealth의 기존 확정 사망/중복 방지 경로에서 기존 RewardInventorySession.GrantKillReward로 지급하며 돈·재료·스킬/보스 HP·공격·패턴은 보존. 결과 정산은 기존 RunRewardExperience 표시만 사용하고 클리어 EXP를 추가하지 않음.
- 선택 맵 테스트: 저장된 EXP 설정 검사, 예상 EXP와 실제 RunRewardExperience 표시, 생성 검증에 개체별 EXP 일치 검사 추가. 기존 격리 경로·로비 비활성화·재시작/종료 흐름 유지. 기록 JSON version1에 측정 여부와 예상/실제 long EXP를 추가해 과거 기록은 '미측정'으로 읽음. 기존 13열 CSV는 그대로 유지하고 'EXP 확장 CSV'에 expectedExperience,experience 두 열만 뒤에 추가(UTF-8 BOM, 과거 미측정은 빈 셀, 실제0은0).
- 변경 파일: Assets/Editor/MonsterExperiencePanel.cs, MonsterExperienceAnalysis.cs 및 신규 .meta; MonsterDataEditorWindow.cs, MonsterProfileEditorStore.cs, SelectedMapTestRunner.cs, SelectedMapTestPanel.cs, SelectedMapTestHistory.cs, SelectedMapTestHistoryPanel.cs; Assets/Scripts/MonsterFloorProfile.cs, DungeonFloorData.cs, DungeonRoomManager.cs, EnemyReward.cs, Inventory/RewardInventorySession.cs; Assets/Dungeons/MonsterProfiles/Floor1_RockMine_Monsters.asset; 이 문서.
- 계산 검증: 현재 배치 수량10/12/11/10/12/11/10에서 맵 EXP 47/57/52/48/57/52/48 일치. 일반1방 최소47/최대57/평균51.57142857, 일반3방+보스 최소187/최대216/평균199.71428571. 독립적으로 252개 유효 경로를 전수 합산해 분석 결과와 대조. 후보1개[2]의 3방=6, 후보2개[1,100]의 3방 최소102/최대201/평균151.5도 검증.
- Editor 검증: 격리된 임시 프로필 복사본에서 EXP 저장·재로드·Undo·Redo 및 0 저장 성공. 미설정 정규 입장 차단 안내 '1층 쉬움: 층·난이도 EXP 미설정', 설정 완료0 입장 허용, 음수 차단 확인. 정수 입력 -1/2147483648/NaN/1.2/빈값 거부 및0 허용 확인. 기본 수치 CSV 왕복과 두 밸런스 CSV 왕복/검증 통과, EXP 필드 보존 확인. 임시 테스트 에셋과 .meta 제거.
- Play 검증: 각 실행 전 실제 CharacterSaveService.SavePath 격리 확인. 선택 Map_01은 Library/SelectedMapTests/<고유GUID>/character.json에서10마리 생성, 처치 합계47 및 같은 적 중복 피해의 EXP 재지급 없음, 전멸 성공. 재시작은 새 격리 경로/10마리/획득EXP0으로 초기화. 기존 로비 비활성 상태 확인. 일반 던전은 Library/MonsterExperience8/regular-character.json에서 실제 입장 UI로 입장, Map02(57)→Map01(47)→Map03(52)→돌골렘(45) 진행, 합계201=결과창201, 보스HP240 유지, Lv2/잔여EXP101/기존상한5 확인. 별도 failure-character.json에서1마리 처치4EXP 후 FailRun, 실패 정산4EXP와 성장EXP4 보존. 처치는 기존 TakeDamage API를 사용했으며 수동 전투 완료 검증은 아님.
- 7맵 격리 PreviewScene 구성에서 생성 개체76마리의 종류별 적용 EXP/예상 합계 전부 일치. 기존 배치/지형/접근성/보상 참조 검증7맵 오류0. 기존 Map01의 권장 영역 밖5개, Map05의2개 및 보수적 경로검사 안내 경고는 유지.
- 기록 호환 검증: 원래14건은 측정 여부=false로 읽고 신규 성공47/47 및 재시작 후 종료47/0 기록 확인. 기존 CSV13열/확장CSV15열, UTF-8 BOM 내보내기 확인. 이번 검증 기록2건만 정리하고 기존14건 내용 및 JSON/.bak를 원본 바이트로 복구(기존 필드 전체 동일 확인 후 복구).
- 보존/종료: 보상6개·배치프로필7개·맵7개·실제 사용자 character.json/백업 등을 포함한28개 SHA256 대조에서 승인된 층 프로필1개만 변경. 해당 프로필 diff는 EXP 신규 필드만 추가, 기존 HP/공격 배율·보정·맵 참조 불변. 기존 프리팹/.meta/GUID/씬/지형/AI/저장 형식/공식 GDD·Systems 수정 없음. 테스트 종료 후 SaveTestPath 빈값 및 Play 옵션 복원, 선택 테스트 Active=false/편집모드/Main/씬 dirty=false. 관련 Console 오류0, 기존 Result Korean 폰트에 '갱' 글리프 누락 경고1건 확인(범위 밖이라 수정하지 않음).
- 근거: Library/MonsterExperience8/before-hashes.json, original-profile.txt, history.json/.bak, results-exp.csv 및 이번 Unity MCP 실행 결과. 미검증: 모든 맵의 실제 Play 처치, 수동 공격·스킬·회피·부활 전체 조합, 어그로 최초 시야 전체 회귀, 장시간 전투, Excel 앱 직접 열기, 배포 빌드. 모든 다른 층·난이도 EXP 및 초기 테스트 EXP의 최종 밸런스 확정은 후속 기획 사항. Git 커밋/푸시 없음.

## 2026-10-22 플레이어 성장 1차 — Lv.50 / 3P / v1→v2 이전
- 범위: Assets/Scripts/Player/PlayerProgression.cs, LevelProgressionData.cs, Assets/Skills/LevelProgression.asset, Assets/Scripts/CharacterSaveFile.cs, CharacterSaveService.cs 수정. Assets/Editor/ProgressionVerification.cs 및 .meta 추가. 이 문서는 기존 내용을 보존하고 기록만 추가. 지정 우선 파일 경로는 실제 프로젝트와 일치. 성장·저장 문서와 기존 연결/HUD/정산/선택 테스트 관련 부분만 조회. Git 커밋/푸시 없음.
- 경험치: asset을 MCP로 갱신, Lv.1~49의 49구간에 100+50×(현재Lv−1) 적용. 마지막 구간2500, Lv.1→50 총63700. 초기 조정용 배열이며 현재 레벨 진행 EXP 저장 방식을 유지. 최대레벨에서 잔여EXP0/진행률1/MAX 표시, 이후 EXP에 레벨/포인트/알림 추가 없음. 과거 Lv.5 상한에서 소멸한 EXP 추정 복구 없음.
- 포인트: 신규 Lv.1 3P, 레벨업당3P, Lv.2 총6P, Lv.50 총150P. 단일 지급 다중 레벨업/큰 int EXP 처리와 기존 Changed/LeveledUp HUD 연결 유지. 레벨 변화는 즉시 Flush, 처치 보상 transaction 중에는 EndTransaction에서 돈·재료 지급까지 완료한 뒤 함께 저장. 일반 변경은 기존 debounce 유지. 스탯 투자/패시브/신규 UI는 구현하지 않음.
- 이전: 새 스냅샷 version2/progressionV2Applied=true. 정상 v1은 Lv.1~5, 미사용P=Lv−1, Lv.1~4 진행EXP가 기존/현재 표 범위 안, Lv.5 진행EXP0인 기존 무투자 스키마만 허용. 투자0P로 판단해 미사용P를 Lv×3으로 최초1회 대체. 기존 포인트에 가산하지 않음. 레벨/EXP/재화/재료/보유·장착 스킬/등록순서/정산 marker/설정을 담은 전체 스냅샷을 먼저 저장 성공시킨 후 live 세션에 복원. v2 재로드에서는 포인트 재보정 없음. RestoreGrowth의 임의 clamp 제거.
- 이상 처리: 음수/범위 밖 레벨·EXP·포인트, 기존 지급량과 다른 v1 포인트, 현재 표와 충돌하는 EXP, 미지원 버전/이전 미완료 marker/알 수 없는 저장 필드는 자동 보정하지 않고 LoadBlocked 및 상태 안내. 정상 checksum의 의미 충돌은 오래된 .bak으로 자동 대체하지 않음. 투자 정보가 있는 확장 v1/v2 스키마는 임의로 버리지 않고 차단해 후속 확인 대상. 이번 단계에는 투자 필드 자체가 없으며 투자 기능 도입 시 버전/총량 검증도 함께 확장해야 함.
- 원본 보호: v2 쓰기 전에 주 파일과 .bak을 각각 character.json.pre-v2/primary-<SHA256>.json, backup-<SHA256>.json으로 별도 보호. 보호본은 임시 파일 Flush(true)→이동→바이트 대조하며 동일 내용은 재사용, 자동 삭제/덮어쓰기 없음. 이후 .bak 순환과 독립. 보호/쓰기 실패는 원본을 유지하고 live 이전 완료로 적용하지 않음. 기존 JSON payload/SHA256, .tmp Flush(true), File.Replace, .bak 읽기 복구 및 .corrupt-GUID 원본 보존 경로 유지. writer는 완료된 v2만 수용.
- 격리 Edit 검증: 최종 ProgressionVerification.Run 85항목 통과. 49개 EXP값/총량, 시작3P/Lv2 6P/다중 상승/Lv50 150P/추가EXP 무보상, v1 Lv1~5 이전, 각3회 읽기·재저장/중복없음, 재화·스킬·재료·설정·정산ID 보존, 백업 순환 후 보호본 유지, 이상 데이터 차단, 미완료 v2 writer 거부, 보호 경로 실패/잠긴 .tmp/재시도/체크섬 오류 및 .corrupt 복구 확인. 사용자 저장 파일을 테스트 입력으로 사용하지 않음.
- 실제 격리 Play: Lv2 EXP149/기존1P/4스킬/5재료/설정0.3의 v1 fixture→Lv2 EXP149 6P. 실제 EnemyHealth 처치→Lv3 EXP4 9P를 같은 호출 종료 전에 디스크에서 확인(재화 포함), 동일 적 중복 EXP 없음. 실제 PlayerHealth 치명타/부활2회, 방2→3→보스 이동 후 성장 유지. 일반3방 EXP48+52+48, 보스45/HP240, 총193=결과창193, 부활 기록2/2, Lv3 EXP192 9P 정산/저장 확인. RetryDungeon으로 동일 난이도 재시작 시 물약3/부활2/성장 유지. 다음 적6EXP 후 실패 결과/디스크 Lv3 EXP198 9P 보존.
- 재실행/실패 경로: 실제 Play 종료·재실행에서 전체 Capture가 기존 스냅샷과 일치하고9P 유지. 초기 검사의 SuccessfulWrites=0 가정은 기존 BindPlayer의 일반 저장1회 때문에 실패했으며 실제 데이터는 전부 동일, 이전 중복 검증은 저장값 일치로 바로잡음(기능 결함 아님). 별도 새 경로의 실제 Play에서 Lv1 3P→Lv2 6P→Lv50 150P 즉시 저장과 상한 이후 추가쓰기 없음. 손상 primary+정상v1 backup→Lv4 EXP77 12P/money654 복구·이전 및 양쪽 보호/.corrupt 확인. 실제 LoadOnce의 이상 포인트/투자 필드/보호경로 쓰기 실패/양쪽 손상/version8 총5시나리오에서 LoadBlocked/Flush거부/원본바이트와 live성장 불변 확인.
- 선택 맵 회귀: 기존 SelectedMapTestRunner로 Map01 실행. Library/SelectedMapTests/<GUID>/character.json 격리 경로, 신규Lv1 3P/10마리/처치EXP47/성공/일반 정산marker 없음 확인. 기존 Stop(false)/Cleanup 사용, 테스트 이력에 이번 성공 결과가 추가됨. 검증 중 Stop의 선택 매개변수는 reflection에서 명시적으로 false를 전달해 종료 완료.
- 보존/종료: 실제 사용자 Editor character.json/.bak SHA256 작업 전후 동일, build character.json/.bak는 모두 미존재 상태 유지. 사용자 저장의 실제 이전/초기화/파괴 테스트 없음. 몬스터 EXP/HP/공격/드롭/프로필/씬/프리팹/메타와 기존 전투·회피·물약·스킬·정산 코드는 변경하지 않음. Main 편집 모드/씬 dirty=false/선택 테스트 Active=false/SaveTestPath 원래 빈값/Play 옵션·start scene·runInBackground 원래값 복원.
- 컴파일/오류: 최종 Unity 스크립트 컴파일 완료, Console error0 확인. 실제 결과창에서 기존 Result Korean 폰트의 '갱' 글리프 누락 경고2건 관측(범위 밖). 의도적으로 만든 차단 fixture의 저장 복구/이상 데이터 경고는 예상 결과. 중간 Unity 호출1회는 자동 승인 검토의 사용량 제한으로 실행되지 않았고 사용자 계속 요청 후 재호출해 검증·정리 완료.
- 근거: Library/ProgressionVerification/0e106292937943ddaf5b801e7d09f1a9/edit-results.txt(최종85), Library/ProgressionVerification/play-f05ccb1156444f958997c3c03af29cf1/{play-results.txt,expected-reload.json,user-before.txt,user-after.txt} 및 하위 fixture/보호본. 반복 검증은 매번 별도 고유 디렉터리 사용. 미검증: 실제 키보드/마우스 전투·모든 스킬/회피/물약 조합, 모든 맵·난이도 전수 Play, 장시간 전투, 배포 빌드, 프로세스 강제 종료·전원/디스크 장애. 처치/피해는 기존 API 호출이며 수동 플레이 전체 검증 주장이 아님. 실사용자 저장 파일의 내용상 이상 여부는 조사하거나 보정하지 않음.

## 2026-10-22 플레이어 성장 2차 A — 스탯 투자 데이터 / v3 저장 / 기본 능력치 합산
- 범위: Assets/Scripts/Player/PlayerProgression.cs, PlayerSkillAttackPower.cs, PlayerHealth.cs, Assets/Scripts/CharacterSaveFile.cs, CharacterSaveService.cs 및 기존 Assets/Editor/ProgressionVerification.cs 수정. 신규 매니저/싱글톤/씬/프리팹/UI/테스트 스크립트 추가 없음. 기존 WORK_STATUS 보존 후 이 기록만 추가. 시작 Git clean, 기존 작업 되돌림 및 커밋/푸시 없음. 요청된 우선 코드 경로는 모두 실제 경로와 일치.
- API: PlayerProgression.Stat {Health, Physical, Magic}, HealthPoints/PhysicalPoints/MagicPoints/SpentStatPoints, PointsIn, InvestablePoints, CanInvest, TryInvest(Stat,int multiplier,out int invested,out string error). 배수는1/5/10만 허용, 실제량=min(배수,미사용P,100-현재투자P). 각0~100, 미사용+투자합=레벨×3 검증. 잘못된 enum/음수/0/지원하지 않는 배수/포인트0/상한 도달은 false·invested0이며 이벤트/저장 요청/포인트 변경 없음. Lv1 시작3P/Lv당3P/Lv50 총150P/기존 경험치표 유지.
- 권한: 실제 투자 API와 저장 commit 양쪽에서 재검사. Main 활성 씬의 정상 준비 상태·현재 영구 manager 소유·플레이어 생존/활성·방0/준비/비전환·런 비추적/비결과/비재시작·저장 로드완료/비차단/비쓰기/트랜잭션0/비종료 상태만 허용. 선택 맵 테스트도 명시 거부. InventoryUI.CanEquip/SkillInventorySession.TryEquip을 참고했으나 UI 상태를 무조건 재사용하지 않음. 투자는 메인 씬 확인과 영구 저장 가능 여부가 추가로 필요하기 때문. 종료는 저장 서비스 OnApplicationQuit/OnDestroy에서 stopping 처리.
- 저장 실패 설계: 메모리를 먼저 차감하지 않음. 기존 Capture의 전체 영구 데이터로 후보 v3 생성→포인트/선택투자 변경→기존 원자적 Write 성공→메모리 투자 적용 및 Changed. 실패는 false/투자0/오류 반환과 Status 안내, 원래 포인트·이벤트·dirty 상태를 유지하며 롤백 자체가 불필요. 성공 후 기존 Changed 구독의 일반 debounce 재저장이 생길 수 있으나 이미 투자 후보는 디스크에 저장됨. 알림 뒤 발생한 다른 변경의 dirty를 덮어 지우지 않음. 재시도는 미적용 상태에서 시작하므로 중복 소비 없음.
- 능력치: MaximumHealthBonus=체력P×3, PhysicalAttackBonus/MagicAttackBonus=해당P×0.4f. PlayerSkillAttackPower의 기존 직렬화 기본값7/6과 virtual 공급 경로 유지, BasePhysicalAttack/BaseMagicAttack 제공 및 기존 PhysicalAttack/MagicAttack에 투자 보너스 합산. 중간 반올림 없음. 기존 스킬 Damage 공식/계수/에셋 및 곡괭이 피해 계산은 수정하지 않음. 기존 스킬 공식은 새 공급 스탯을 참조하지만 승인된 단일 유형 피해 공식 전환은 후속 작업이며 이번에 임시7/6을 확정 밸런스로 변경하지 않음. 장비 공급을 위한 기존 virtual 확장 지점 유지.
- HP 보류(승인되지 않은 경계): 기존 전투 HP는 int. 예:50/100에서 최대103이면 비율값51.5, 올림/내림/반올림 및 0HP/사망·부활 상태 적용 정책이 미확정. 요청 지침에 따라 현재HP·실제 MaxHealth를 자동 변경하는 연결은 보류. PlayerHealth.MaxHealthWithStatBonus로 기본HP+투자보너스 계산값만 제공하고 MaxHealth/CurrentHealth/Heal/부활 코드는 유지. 따라서 체력 투자 데이터는 저장되지만 이번 단계에서는 실제 최대HP가 증가하지 않음. 미확정 정책 승인 후 비율 유지 적용을 연결할 것. 보너스 계산을 Heal로 처리하지 않아 회복량·물약 기록을 만들지 않음.
- 스키마: version3에 healthPoints/physicalPoints/magicPoints 추가. v3는 세 필드 명시 존재/각0~100/미사용+세투자=Lv×3 검증, Capture/Restore/writer 동일 총량 검사. v1/v2의 새 투자 필드나 미지원 필드는 해석하지 않고 충돌 차단. 정상 v2는 투자0/기존포인트 그대로 v3로 확장, 정상 v1은 기존 무투자 검증·최초1회 Lv×3 보정 후 v3 저장(중간 v2 파일을 별도 기록해 이중 이전하지 않음). progressionV2Applied 표시는 기존 보정 완료를 계속 의미함. 반복로드 포인트 재지급 없음.
- 보호/복구: 기존 .pre-v2 보호 코드를 보존하고 이전 대상 버전 인자로 확장. v1 파일이 남아 있으면 기존 .pre-v2에 별도 보호, v1/v2에서 v3 작성 전에는 .pre-v3에 primary/backup SHA256별 불변 보호본 생성. 기존 보호본 덮어쓰기/삭제 없음. 기존 JSON payload/SHA256/.tmp Flush(true)/File.Replace/.bak/.corrupt 복구 경로 유지. 의미상 투자 오류는 clamp 또는 오래된 백업 자동 대체 없이 LoadBlocked.
- Edit 검증: 기존 ProgressionVerification를 현 스키마에 맞춰 최소 확장, 최종104개 통과. 성장1차85개 기반에 투자 보너스/배수/상한/부족량/비Play API 차단, 정상v2 Lv1/5/50→v3 무재지급·보호, 잘못된 투자범위·총량의 reader/writer 거부 등을 추가. 신규 스크립트 없이 기존 verifier의 임시 비활성 오브젝트1개를 finally에서 제거. 중간 verifier 지역변수 이름 충돌 컴파일 오류는 수정 후 재컴파일/104개 실행 성공, 이전 어셈블리의85개 실행을 최종 결과로 사용하지 않음.
- 실제 격리 Play 투자: 신규Lv1 3P에서 각1P→체력1/물리1/마법1/미사용0, Changed3회 및 즉시 디스크 일치. 공격7.4/6.4, HP계산103/실제100 유지 확인. Lv3 총9P에서 .tmp를 FileShare.None으로 잠그고 물리×5 요청→false/0P/이벤트0/메모리·주파일·.bak 동일; 잠금 해제 후 ×5는 한 번만 적용, 마법×10은 남은1P만 적용. 재실행에서 체력1/물리6/마법2와 전체 영구 스냅샷 동일.
- 실제 상한/권한: Lv50 미사용141+투자9=150. 체력×10을9회 후 마지막9P 부분 투자→100P 상한, 물리/마법×5 후 체력100/물리11/마법7/미사용32=150. HP계산400, 공격11.4/8.8 소수 유지. 상한 추가EXP 무보상. 트랜잭션/재시작/결과/종료/stopping/방전환/사망 상태를 검사 중만 지정해 API 거부·쓰기/투자불변 확인 후 finally 원복. 실제 던전에서도32P가 남은 상태의 투자 거부·저장불변 확인.
- 기존 기능 회귀: 일반 방 실제10마리 처치와 EXP47 지급, 기존4스킬 EstimatedDamage 공급 경로가 각 SkillData.Damage와 일치. 기존 PlayerHealth 실제 치명타/부활2회 후 투자 상태 보존. FailRun 결과EXP47·부활2 기록 일치, 결과 상태 투자 거부. RetryDungeon 후 물약3/부활2 초기화·투자100/11/7/미사용32 유지·던전 투자 금지. 기존 선택 Map01 runner에서도 별도 Library/SelectedMapTests/<GUID>/character.json의 v3/Lv1 3P/투자0/10마리/EXP47/성공/일반 정산없음 및 투자 거부 확인. 이번 선택 테스트 성공 이력1건 추가, 기존 이력 삭제 없음.
- 실제 이전/복구: 손상 주파일+투자된 정상v3 backup에서 전체 스냅샷 동일 복구/.corrupt 보존. v2 Lv7 EXP99 21P+v1 backup 혼재→v3 투자0/21P 유지, 돈·재료·스킬·장착·설정·정산ID 전체 동일, 양쪽 .pre-v3 원본 보호 및 기존 .pre-v2 sentinel 불변, 이후 마법10P 투자 성공. v1 Lv3 EXP17/기존2P→v3 9P/투자0/다른 영구정보 유지/.pre-v2·.pre-v3 보호, 실제 재실행에서9P 중복 지급 없음.
- 보존/마무리: 실제 사용자 build/Editor 주파일·.bak 및 기존 보호본 포함7경로의 SHA256/미존재 상태 작업 전후 동일. 테스트는 새 Library 고유 경로만 사용, 사용자 파일로 파괴적 검증 없음. Main 편집 모드/씬 dirty=false/선택 테스트 종료, SaveTestPath 원래 빈값·Play 옵션·start scene·runInBackground 원복. 몬스터8차 코드/EXP·HP·공격·드롭/프로필/씬/프리팹/스킬에셋/LevelProgression.asset 변경 없음. 컴파일 완료 및 최종 Console error0, 기존 Result Korean '갱' 글리프 누락 경고1건 범위 밖 유지. git diff --check 오류 없음.
- 근거: Library/ProgressionVerification/e5c4b4f8418e4b11a543f48748b06b00/edit-results.txt(104개), Library/StatInvestmentVerification/a913d8f689b949699fce3120bdf9d35f/{play-results.txt,expected.json,expected-cap.json,expected-recovery.json,expected-v1-reload.json,user-before.txt,user-after.txt} 및 격리 fixture/보호본. 미검증: HP 비율 정수화/사망 상태 실제 반영(의도적 보류), 스탯UI/패시브/초기화/새 피해 공식(이번 범위 제외), 실제 키보드·마우스 전투/모든스킬 시전·회피·물약 전체조합, 이번 세션의 일반3방→보스 전수 진행, 장시간/배포 빌드/전원·디스크·프로세스 강제 종료. 테스트 처치·피해는 기존 API 호출이며 수동 플레이 완료 주장이 아님.

## 2026-10-22 성장 2차 A 보완 — 체력 투자 실제 HP 적용
- 이번 추가 변경: Assets/Scripts/Player/PlayerHealth.cs, Assets/Scripts/DungeonRoomManager.cs, 기존 Assets/Editor/ProgressionVerification.cs, 이 WORK_STATUS. 시작 시 남아 있던 성장2차A의7파일 미커밋 변경을 보존하고 필요한 부분만 추가. 새 매니저/싱글톤/씬/프리팹/테스트 파일/GUID 변경 없음. Git 커밋/푸시 없음.
- 적용: 기존 직렬화 maxHealth는 기본HP로 그대로 보존, 별도 effectiveMaxHealth에 기본HP+HealthPoints×3을 적용. MaxHealth는 실제 적용값이며 기존 MaxHealthWithStatBonus도 같은 실제값을 반환. 기존 성장 Changed 이벤트에 구독, OnDisable 해제/재활성 재구독. 초기 Awake/OnEnable보다 영구 매니저 Awake가 늦을 수 있어 Start에서 로드된 성장 재확인. 던전 입장 직전에도 RefreshGrowthHealth를 호출해 올바른 최대HP 확인 후 기존 입장 Heal/초기화 진행.
- 초기 구현 정책: 이번 사용자가 지정한 ceil(기존현재HP×새최대HP/기존최대HP), 생존 양수HP 보존/최대HP 상한/사망0HP 유지 적용. float 오차 없이 long 정수 곱셈·나눗셈의 정확한 올림 사용. 동일 최대HP는 그대로 반환해 레벨/물리/마법/반복 로드 이벤트에서 재차 올림하지 않음. 사망 상태/부활 코루틴/생명 사용을 스탯 재계산이 변경하지 않음. 기본/기존/신규 최대HP의0·음수·오버플로, 음수현재HP/기존최대초과, 체력투자범위 오류는 HP를 임의 보정하지 않고 업데이트 차단/경고. 이번 올림·사망 규칙은 초기 구현 기준 승인으로 기록하며 GDD/Systems 최종 정책 확정으로 문서를 변경하지 않음. 앞선 2차A의 HP 보류는 이 보완으로 해소.
- 회복/저장: Heal/부활이 실제 MaxHealth를 사용하도록 연결, 물약 Q 입력·35%·수량·시전 로직은 미변경. 스탯 갱신은 Heal/RecordHealing/피격/물약/부활 API를 호출하지 않음. 기존 HUD의 성장 이벤트 및 매 LateUpdate의 CurrentHealth/MaxHealth 표시를 유지. 저장 스키마/이전/포인트 보정/보호본 코드는 이번 보완에서 변경하지 않음. 기존처럼 현재HP 자체는 영구 저장 대상이 아니며 새 플레이어는 복원된 최대HP의 full HP로 시작; 같은 성장값의 제자리 Restore는 현재HP를 바꾸지 않는 것을 확인.
- Edit 검증: 기존104개에 HP변환14개 추가, ProgressionVerification.Run 최종118개 통과. 50/100→52/103, 1/100→2/103, full100→130, 같은 최대값 불변, 양수1/400→1/100, 사망0 보존, int 최대치 부근 정수연산,0/음수 최대HP·음수/초과현재HP 거부 검증. 기존 Lv50/3P/총150P·투자상한·v1/v2/v3 이전·쓰기 실패·백업복구 검사도 포함.
- 실제 격리 Play 투자/재계산: 신규100/100→기존 TryTakeDamage50→50/100→체력1P 실제투자52/103.20회 재계산/물리·마법1P/레벨업에서52 유지. 추가 체력투자를 순차 실행해 매번 정확한 ceil을 대조, 최종 체력10P 최대130/현재68 확인. 같은 성장값 RestoreGrowth/비활성·재활성에서도68/130 불변. 이전 시점마다 실제 투자 단위별 올림을 적용하므로 한번의10P 투자와 반복1P 투자의 현재HP가 같다고 주장하지 않음(승인된 매 변경 ceil의 결과).
- 로드/입장: 실제 Play 재실행에서 영구 Capture 전체 동일, 체력10P 최대130/초기130/130 및 HUD130/130. 피격13→117 후 던전 입장 시 기존 초기화가130까지 회복, BeginTracking 이후 회복통계0. 별도 저장경로로만 테스트. 이번 사용자 Play는 시작 시 실제 Editor 경로/저장대기false를 확인한 뒤 정상 종료했으며 사용자 파일을 fixture로 사용하지 않음.
- 물약/사망: 기존 Q 분기의 TryStartUse를 reflection으로 호출(실제 키보드 Q 누름 테스트 아님).130HP 기준 피해65→65/130, 물약 완료 ceil130×0.35=46→111/130, 물약3→2/기록healed46.10회 HP재계산에서HP111/물약2/healed46 불변. 치명타 후0HP·사망·부활잔여1인 같은 호출 안에서 유효 성장 fixture를 잠시 복원해 최대133→130으로 변경, 두 경우 모두0HP/사망/생명/회복통계 유지. 원래 성장10P/미사용3으로 같은 호출에서 복구해 임시 투자값을 디스크에 기록하지 않음.
- 부활/방/실패: 첫 실제 부활130/130, 회복통계46 그대로. 피해17→113/130, 일반10마리 전멸 EXP48, 방2 이동에서113/130·물약2·잔여부활1 유지. 두 번째 부활은 runtime fixture의 reviveHealthRatio만25%로 잠시 지정해 ceil130×0.25=33 확인, 설정 즉시 원복(에셋 변경 없음). 회복통계46 유지. 세 번째 사망→실패0/130/usedRevives2/EXP48 결과 일치. 실패 상태10회 재계산해도 사망0 유지. 실제 RetryDungeon 후 새 플레이어130/130·체력10P·물약3·부활2 유지.
- 저장 실패/선택 테스트: 메인 귀환 후 .tmp 독점 잠금 상태의 체력1P 투자는 false/0P, 실제HP130/130·성장·주파일·.bak 전부 그대로. 잠금 해제 후 재시도1회만 체력11P/133/133 적용. 기존 선택 Map01 runner는 새 Library/SelectedMapTests/<GUID>/character.json에서 체력0P/100HP로 시작해 이전 테스트11P 유출 없음, 투자 거부/10마리/EXP47/성공/일반 정산없음 확인. 기존 Stop(false)/Cleanup 사용, 개발용 성공 이력1건 추가.
- 보존/마무리: 격리 검증 전후 실제 사용자 build/Editor 주파일·.bak·기존 .pre-v2/.pre-v3 보호본 포함11경로의 SHA256/미존재 상태 동일. 사용자 파일 삭제·복구·초기화 테스트 없음. 기존 성장/저장/공격/스킬/몬스터 EXP·보상·회피 코드와 기본 수치·스킬 에셋 변경 없음. Main 편집 모드/씬 dirty=false/선택 테스트 종료, SaveTestPath 빈값 및 원래 Play 옵션/start scene/runInBackground 복원. 최종 Unity 컴파일 완료/Console error0, 기존 결과창 Result Korean의 '갱' 글리프 누락 경고1건 유지. git diff --check 오류 없음.
- 근거: Library/ProgressionVerification/849af566fac1464397d077f1fb635350/edit-results.txt(118개), Library/HealthStatVerification/8a81c0187b26433d901829c0708c27bb/{play-results.txt,expected.json,user-before.txt,user-after.txt} 및 격리 character.json/.bak. 미검증: 실제 키보드 Q 입력 및 모든 스킬·평타·회피·물약 전체 조합, 일반3방→보스 전체 재주행, 모든맵/난이도, 장시간/배포 빌드/강제 전원·디스크·프로세스 종료. 물약 시작·몬스터 처치·피해는 기존 API 호출로 검증, 수동 플레이 완료 주장이 아님. 현재 확인된 미해결 기능 오류 없음.

## 2026-10-22 성장 2차 B — 곡괭이·4스킬 단일 유형 피해
- 이번 추가 변경: Assets/Scripts/Player/PlayerSkillAttackPower.cs, PlayerAttack.cs, SkillData.cs, Assets/Scripts/Inventory/InventoryPresentation.cs, 기존 Assets/Editor/ProgressionVerification.cs, Assets/Skills/{EarthSmash,DrillDash,TealBeam,OreBlast}.asset, 이 WORK_STATUS. 이전 성장2차A/HP 보완의 미커밋 변경을 보존. PlayerSkills.cs/PlayerSkillsMotion.cs, 공격 타이밍·범위·벽·이동·취소·입력·장착·쿨타임 코드는 수정하지 않음. 신규 매니저/싱글톤/씬/프리팹/테스트 파일 없음. Git 커밋/푸시 없음.
- 공식: 공유 PlayerSkillAttackPower.CalculateDamage(baseDamage,attackStat)에서 baseDamage×(1+attackStat/100)를 double로 계산하고 마지막 Floor(damage+0.5) 한 번만 적용. Mathf.RoundToInt 짝수 반올림에 의존하지 않으며 중간 스탯/피해 반올림 없음. 비정상 음수·NaN/Infinity 입력은0, int 상한 초과는 포화 처리. 최종 스탯은 기존 직렬화 기본 물리7/마법6+성장 투자P×0.4, 기존 virtual 공급 확장점 유지. 현재 연결된 별도 장비 보너스 공급은 없어 신규 장비 수치를 도입하지 않음. 기본7/6은 임시값 그대로이며 최종 밸런스로 확정하지 않음.
- 곡괭이: 기존 attackDamage10/모든 범위·각도·벽/중간 타격 타이밍을 유지하고 ApplyDamage 진입에서 최종 물리 스탯으로 정수 피해를 한 번 산출해 대상 전체에 사용. EstimatedDamage를 같은 함수로 제공. 원본 PlayerAttack.cs가 UTF-8이 아닌 기존 인코딩이므로 ASCII 코드 구간4곳만 바이트 치환해 주석/인코딩을 보존.
- 스킬/에셋: SkillData에 편집 가능한 baseDamage 추가, primaryType 한 가지만 선택해 공유 공식 적용. EarthSmash18/DrillDash13(물리), TealBeam15/OreBlast18(마법)은 이번 채택 초기 테스트값이며 최종 밸런스 아님. 과거 physicalCoefficient/magicCoefficient 값은 직렬화 호환을 위해 숨겨 보존하되 계산/툴팁에는 미사용. MCP로4에셋의 기본 피해만 갱신하고 변경 전후 JSON 비교에서 baseDamage 이외 전체 필드 동일, GUID/.meta 바이트 동일 확인. Unity 재직렬화로 기존 기본 등급 Common을 rarity:0으로 명시한 줄이 추가됐으나 실질 설정 변경 없음.
- 스냅샷/툴팁: 기존 PlayerSkills.TryCast의 Cast.damage 시전 시작 스냅샷과 후속 Hit 경로는 그대로 사용. EstimatedDamage/카드 정렬/툴팁도 SkillData.Damage 경유. 툴팁의 혼합 계수 표시를 기본 피해 표시로 교체, 현재 예상 피해는 실제 계산과 동일. 패시브 충격파/추가 폭발은 구현하지 않음.
- Edit 검증: 기존118개에 피해16개 추가한 ProgressionVerification.Run 최종134개 통과. 기본 곡괭이11,4스킬19/14/16/19; 스킬 ID·기본값·단일 유형, 물리100P(최종47)/마법100P(최종46)의 독립 영향 검사. 16.5→17,19.5→20,22.5→23, .5 직전/정확/직후, 중간 반올림하지 않는1×1.499 사례 확인. 기존 Lv50·3P·투자상한·HP비율·v1/v2/v3·보호/쓰기 실패/백업복구 검사 포함.
- 실제 기본 피해: Main의 별도 격리 저장에서 일시적 EnemyHealth1000+두 적중 Collider fixture1개 사용, 기존 공격/스킬 API로 실제 HP 감소를 측정. 곡괭이11/대지19/착암14/광선16/발파19 전부 툴팁 예상과 일치. 곡괭이 wall/cone/range 밖 타격 거부, 두 Collider 한 번 적중 확인. 대지 시전 직후 runtime 기본 물리만7→107로 잠시 바꿔도 실제19로 스냅샷 유지, 이후7 원복. 착암 실제3.8 이동·여러 프레임/Collider 중복 적중 없음, 광선 이동0.5/벽 차단, 발파 투척·지연 후19/즉시 CancelAll 후 무피해·효과0 확인. 쿨타임 설정8/7/9.5 및 R 대지6 확인.
- 실제 경계/투자: 광선 기본15에 runtime 마법10을 잠시 지정해16.5가 실제17로 적중/예상17 일치, 마법6으로 원복. Lv50에서 실제 마법50P 투자→곡괭이11/대지19/착암14 불변, 광선19/발파23. 이어 물리100P 투자→곡괭이15/대지26/착암19, 광선19/발파23 불변. 총투자150/미사용0. 모든 툴팁 값 대조, 실제 투자 후 곡괭이15/광선19/R 대지26 타격 확인. 일시 변경은 runtime fixture에서만 수행, 기본 스탯·스킬 에셋 튜닝 없음.
- 행동/회귀: TryCast가 공격 중/물약 사용 중/TimeScale0에서 거부됨을 테스트 상태를 지정한 같은 호출 내 확인 후 finally 원복. 실제 R 대지 수락/E 중복 시전 거부. 실제 치명타가 예약 발파·효과를 즉시 취소. 실제 부활2회 후 투자/곡괭이15 유지. 실제 재실행에서 Capture 전체(돈·재료·보유/장착 스킬·설정·성장) 동일, 투자100물리/50마법 유지. 임시 플레이어 장착은 기존 inventory Bind로 원복, fixture 적/벽 제거.
- 일반 던전 회귀: 기존 입장 API로 격리된 정규 던전 시작, 실제 EnemyHealth.TakeDamage로 일반3방→보스 진행. 첫 방 처치 및 같은 적 중복 치명타의 보상 재지급 없음 확인. 방2→3에서도 스탯/피해/남은 부활 유지. 기존 보스HP240 유지. 보스 확정 사망 후 클리어 정산EXP202=결과창202, 사용부활2, Lv50/총150P, 디스크의 투자량·정산ID 정상. 전체 주행의 처치는 API 강제 피해이며 수동 전투/보스 패턴 전체 검증 주장이 아님.
- 보존/최종: 사용자 build/Editor character.json/.bak 및 기존 .pre-v2/.pre-v3 보호본 포함11경로의 SHA256/미존재 상태 작업 전후 동일. 사용자 저장을 fixture로 사용/삭제/초기화/강제복구하지 않음. 몬스터 수치·EXP·드롭·프로필/씬/프리팹/장착ID/GUID 변경 없음. Main 편집 모드/씬 dirty=false/임시 target·wall 없음, 저장 override 빈값 및 원래 Play 옵션/start scene/runInBackground 복원. 컴파일 완료 및 최종 Console error0, 기존 Result Korean '갱' 글리프 누락 경고1건 범위 밖 유지. git diff --check 오류 없음.
- 근거: Library/ProgressionVerification/b6886719e9eb4377ad1f33c62c5729f4/edit-results.txt(134), Library/SingleTypeDamageVerification/5dac8a5fe10b47db862e87e99f4a398b/{asset-preservation.txt,*-before.json,*-after.json,play-results.txt,expected.json,user-before.txt,user-after.txt}. 미검증: 실제 키보드 E/R/Q·마우스/회피 입력 전체 조합, 모든 위치·맵·난이도·보스 패턴, 장시간/배포 빌드/강제 디스크·전원·프로세스 장애, 이번 세션에서 선택 맵 runner 재실행. HP·저장 스키마·투자 권한 및 선택 맵 격리 코드는 이번 단계에서 변경하지 않았으며 기존 회귀 검사와 구분. 확인된 미해결 기능 오류 없음.

## 2026-10-22 성장 3차 A — 20P 패시브 3종
- 변경 파일: Assets/Scripts/Player/PlayerProgression.cs, PlayerPotion.cs, PlayerAttack.cs, PlayerSkills.cs, PlayerSkillsMotion.cs, Assets/Scripts/Inventory/InventoryPresentation.cs, 기존 Assets/Editor/ProgressionVerification.cs, 이 WORK_STATUS. 관련 문서/코드만 확인. 스킬·몬스터·성장 에셋/씬/프리팹/.meta/GUID 변경 없음. 신규 매니저/싱글톤/패시브 저장 필드/해금 UI/50P·100P 패시브 없음. Git 커밋/푸시 없음.
- 공통: HasHealth20Passive/HasPhysical20Passive/HasMagic20Passive는 각 투자P>=20의 계산 속성. 별도 해금 상태를 저장하거나 캐시하지 않아 기존 투자 Changed/Restore와 일치하고 재실행·방·부활·재시도에서 현재 투자량으로 계산. 기존 v3 스키마/총150P/투자API/상한100P/저장 성공 후 메모리 적용 구조는 유지.
- 체력20: 물약 완료 지점에만 CalculateHealAmount 적용. 비활성은 기존 Mathf.CeilToInt(MaxHealth×healRatio), 활성은 중간 정수화 없이 double MaxHealth×기존비율×1.2를 마지막에 한 번 Ceiling. 기본35%/시전0.35초/수량·Q 경로·취소 규칙 미변경. Heal/입장/부활/일반회복에는 보너스를 넣지 않음. 실제 증가량 기록과 최대HP 제한은 기존 PlayerHealth.Heal 유지. PlayerPotion/PlayerAttack은 원본 인코딩을 유지한 ASCII 구간 변경.
- 물리20: EffectiveAttackRange=기존 attackRange×(해금시1.15, 그외1), 실제 OverlapCircleAll과 Gizmo에 동일 속성 사용. 기본1.5→1.725, 기존 hitAngle120과 타이밍·벽·피해·중복 방지 불변. 직렬화 원본1.5와 각도120을 바꾸지 않음. 향후 충격파에 대한 코드/확대 경로 없음.
- 마법20: 현재 유효 치수 API EffectiveRange/Width/Radius를 제공. 마법 유형 TealBeam만 거리8→9.6/폭1→1.2, 마법 유형 OreBlast만 반경2.4→2.88. 물리 스킬·Ore 투척4.5는 그대로. Cast에 range/width/radius를 시전 시작 스냅샷해 모든 예고/발사/벽검사/적중/효과에 사용. 물리 스킬도 원래 치수를 그대로 스냅샷하며 보너스 없음. Ore 투사체0.25/0.28와 투척 안전0.12 원본 코드는 유지. 툴팁 범위 설명도 현재 유효 치수를 사용. 공유 SkillData를 수정하지 않음.
- Edit 검증: 기존134개에 19/20/21 독립 해금9개와 물약 계산5개 추가, ProgressionVerification.Run 최종148개 통과. 세스탯 각각 다른 투자0 조건에서 경계 판정, MaxHP163 강화69(중간 ceil이면70이 되는 구간), 비활성58, Health19 Max157/기본55, Health20 Max160/강화68, 정수 결과42 검증. 기존 Lv50/3P/총량·투자상한·HP비율·단일유형 피해·.5 올림·v1/v2/v3/보호·백업·쓰기실패 검증 포함.
- 실제 경계/기본동작: 격리 경로에서 Lv50까지 EXP 지급 후 각 스탯19P를 실제 투자API로 투자. 세효과 비활성/Max157/사거리1.5/광선8×1/발파2.4 확인. 곡괭이는1.65 거리의 작은 적을 못 맞힘. Health19의 기존 물약은1HP→56HP(55회복), 수량3→2. 각20/21 단계도 실제 투자로 진행해 즉시 해금/유지 확인. 최종 각21P·투자63/미사용87=150, Max163/v3 저장에 패시브 필드 없음.
- 실제 광선/스냅샷: Magic19로 시작한 Cast와 예고8×1 확인→같은 호출의 선딜레이 중 실제20P 투자→현재유효9.6×1.2여도 기존 Cast8×1 유지, 거리8.8/옆방향0.57의 fixture 적은 무피해. 다음20P 시전에서는 두 적 모두 실제17 피해. 예고 치수9.6×1.2 일치. 폭에만 걸리는 벽(중심2,0.57)을 두면 폭1의 StripLength8은 통과하지만 폭1.2는1.905로 줄며 예고 길이도1.905/폭1.2, 실제 두 적 모두 차단 확인. 기존 피해 스냅샷/취소/쿨타임 경로 유지.
- 실제 곡괭이/발파: Physical20에서1.65의 같은 적을 실제12 피해로 맞힘, 기본필드1.5/각도120 불변, 중간 벽은 여전히 차단. Magic19의 발파는 투척목표4.5/반경2.4, 중심에서2.7인 적 무피해/효과지름4.8. Magic20 투자 후 같은 투척목표4.5/반경2.88/초기 투사체0.25, 같은 적 실제21 피해/효과지름5.76. 효과 크기는 Editor update 관찰 callback으로 폭발 예고 단계에서 읽고 즉시 구독 해제. 물리 스킬 유효값 Earth 반경2.2/Drill 거리3.8·폭1.4 불변과 광선9.6·1.2/발파2.88 툴팁 확인. 임시 fixture 적·벽 제거 및 inventory Bind로 실제 E/R 연결 원복.
- 실제 로드/회복 통계: Play 재실행 Capture 전체 동일/각21P에서 효과 즉시 복원, 입장HP163·회복통계0. 일반 Heal10은1→11/기록10으로 보너스 없음. 다시1HP에서 강화물약69→70/163/누적회복79. 일반 Heal88→158/누적167 후 강화물약 요청69는 실제5만 회복해163/기록172. full HP 물약 사용은 거부. 세 번째 물약 시작 직후 실제 사망하면 회복 취소, 부활 뒤에도 기록172 그대로. 물약 시작은 기존 Q 분기의 TryStartUse를 reflection 호출한 것으로 실제 키보드 Q 입력 시험은 아님.
- 실제 유지/보상 회귀: 실제 부활2회 각각HP163, 각21P/패시브/사거리 유지. 일반12마리 전멸 EXP57 및 중복 치명타 재보상 없음. 방2 이동 후 최대HP·사거리·광선치수·총150P 유지, 던전 투자 요청 거부. 세 번째 사망→실패0/163, 부활기록2/회복172/결과EXP57=실제57, v3 투자21/21/21 저장 확인. RetryDungeon 후163/163·물약3·부활2·세효과 유지.
- 선택 맵 격리: 기존 Map01 runner의 별도 Library/SelectedMapTests/<GUID>/character.json에서 신규투자0/미사용3/패시브 전부off/HP100·사거리1.5·광선8로 시작, 이전21P 효과 유출 없음.10마리/EXP47/성공/일반 정산없음. 기존 Stop(false)/Cleanup 사용, 테스트 성공 이력1건 추가(기존 기록 삭제 없음).
- 보존/최종: 사용자 build/Editor 주저장·.bak·보호본11경로 및 공유4스킬 asset/meta8개, 총19경로 SHA256/미존재 상태 작업 전후 동일. 실제 사용자 저장으로 파괴 테스트하지 않음. 기존 저장·HP·피해 공식·몬스터EXP/수치/드롭 에셋은 수정하지 않음. Main 편집 모드/씬 dirty=false/선택 테스트 종료, 저장 override 빈값 및 원래 Play 옵션/start scene/background 복원. 최종 Unity 컴파일 완료/Console error0, 기존 Result Korean '갱' 글리프 경고1건 범위 밖 유지. git diff --check 오류 없음.
- 근거: Library/ProgressionVerification/5af47a8e5f6445e5b93fe787eafefb3f/edit-results.txt(148), Library/Passive20Verification/6f663e9293d34cbbbf3c6cff1cbb207d/{play-results.txt,expected.json,before.txt,after.txt}. 미검증: 실제 키보드/마우스 입력 전체 조합, 전맵·난이도·보스 전체 주행과 패턴, 이번 단계의 스킬4종 전체 수동 시전/회피 조합, 장시간·배포 빌드·강제 디스크/전원/프로세스 장애, Gizmo의 수동 Scene 뷰 시각 비교. 공격/피해/물약 시작은 기존 API 호출이며 수동 플레이 완료 주장이 아님. 확인된 미해결 기능 오류 없음.
