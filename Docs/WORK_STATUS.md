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