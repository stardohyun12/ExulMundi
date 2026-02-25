# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Exul Mundi is a Unity 6 (version 6000.3.5f2) 2D card-based battle game. It uses the Universal Render Pipeline (URP) with 2D renderer and the modern Input System package.

## Architecture

**Data Model**: `CompanionData` ScriptableObjects define companion stats (name, HP, card image). New companions are created via Unity menu: Assets > Create > Exul Mundi > Companion.

**Card System**: `CardDisplay` reads from `CompanionData` and renders card UI using TextMeshPro. `CardSwipe` handles mouse/touch swipe-down input to dismiss cards.

**Scene**: The main scene is `Assets/Scenes/Battle.unity`.

## Key Conventions

- Scripts live in `Assets/Scripts/`
- ScriptableObject assets go in `Assets/ScriptableObjects/`
- Uses C# 9.0 targeting .NET Standard 2.1
- Input is handled via the new Input System (`UnityEngine.InputSystem`), not the legacy `Input` class
- Comments may be in Korean

## Game Design (확정된 기획)

### 전투 구조 (핵심)
- **주인공 1명**이 적과 자동 전투 (플레이어는 관전하며 개입)
- 플레이어는 **카드를 사용**해서 주인공에게 실시간으로 스킬/버프를 적용
- 카드 사용 즉시 주인공 수치에 반영 (지연 없음)
- 전투 종료 → 다음 스테이지로 이동
- **동료 파티 편성 시스템은 현재 범위 밖** (추후 검토)

### 핵심 루프
쇼츠처럼 스와이프로 스테이지(배경/몬스터 환경)를 넘겨서 이동. 각 스테이지에서 전투 발생.

### 스테이지 이동 비용
- 스와이프로 이동 시 **식량(Food)** 소모
- 식량이 부족하면 이동 불가 또는 패널티 발생
- 식량은 전투 승리 등으로 획득

### 도주 시스템
- 전투 중 스와이프 → 도주
- 패널티 3종 중 선택: HP 감소 / 난이도 증가 / (동료 이탈은 동료 시스템 추가 후 재검토)

### 플레이어(주인공) 수치 시스템
- **스탯 3종**: HP / ATK / DEF (SPD는 카드 효과로만 조정)
- **기준값**: HP 100 / ATK 10 / DEF 2 / 자동공격 간격 1.5초
- **스탯 3계층 구조**:
  - 1계층: 영구 성장 (BaseStats) — 스테이지 클리어 보상으로 증가, 런 전체 유지
  - 2계층: 전투 지속 버프 (PersistentBuff) — 전투 종료 시 초기화
  - 3계층: 즉발 효과 (InstantEffect) — 카드 사용 즉시 적용, 지속 없음
- 데미지 계산: `받는피해 = max(0, 적ATK - EffectiveDEF)`

### 카드 시스템 (확정)
- **비용 방식**: 에너지 방식 (최대 10, 초당 1.5 회복). 카드마다 비용 1~4
- **카드 지속성**: 사용 후 소멸 (Slay the Spire 방식). 버린 카드는 덱 바닥으로
- **카드 등급**: 일반(흰색/비용1~2) / 희귀(파란색/비용2~3) / 전설(금색/비용3~4)
- **카드 카테고리 4종**:
  - 공격형: 적에게 즉시 데미지 (빠른 일격, 폭발 강타, 연속 베기, 관통 일격, 처형 등)
  - 방어형: 생존 지원 (응급 치료, 방어 자세, 철벽 방어, 불사 의지, 역전의 기회 등)
  - 유틸형: 전투 흐름 변환 (카드 뽑기, 약점 탐지, 시간 가속, 에너지 폭발 등)
  - 특수형: 리스크/리워드 (피의 계약, 도박사의 패, 완전 연소, 극한 집중 등)
- **밸런싱 원칙**: 강한 카드 = 높은 비용 OR 조건 OR 영구 대가. 등급 = 복잡도로 구분

### 적(Enemy) 시스템
- **스탯**: maxHP / atk / def / atkSpeed
- **행동 패턴 타입**: SimpleAttacker / Enrager(HP 절반 이하 강화) / Defender / Debuffer / Berserker
- **난이도 스케일링**: `배율 = 1 + (difficulty - 1) * 0.15`

### 세계 이동 시스템
- **월드 타입 6종**: Normal(55%) / Elite(15%) / Shop(10%) / Healing(8%) / Mystery(7%) / Boss(5%)
- **이동 비용**: difficulty 1~3: Food 1 / difficulty 4~6: Food 2 / difficulty 7~10: Food 3
- **승리 보상**: 카드 획득 / 수치 성장 / 식량 대량 획득 중 3개 택1
- **난이도 곡선**: 긴장(고난도) → 이완(숨 고르기) 사인파 구조. 10~11스테이지마다 보스

### 미결 항목 (다음 논의 시 결정 필요)
1. 핸드 최대 카드 수 (3장 vs 5장 vs 무제한)
2. 덱 구성 방식 — 고정 덱 vs 매 런 새로 구성 (로그라이크 강도 결정)
3. 보스 씬 분리 여부
4. 게임오버 후 리트라이 구조 (완전 리셋 vs 부분 유지)

## 현재 진행 상황 (2026-02-22 기준)

### 구현 완료 — 핵심 시스템 전체 구조

#### 신규 생성 스크립트
- `Assets/Scripts/Data/HeroData.cs` — 주인공 기본 스탯 SO (baseHP/ATK/DEF, attackInterval, startingDeck)
- `Assets/Scripts/Data/CardData.cs` — 전투 카드 SO (energyCost, CardEffectType, effectValue, rarity)
- `Assets/Scripts/Battle/HeroUnit.cs` — 주인공 런타임 (3계층 스탯, 자동 공격, 버프 타이머, 이벤트)
- `Assets/Scripts/Battle/EnergySystem.cs` — 에너지 바 (최대 10, 초당 1.5 회복, 회복 배율 버프)
- `Assets/Scripts/Battle/CardEffectApplier.cs` — 카드 효과 적용 정적 헬퍼 (모든 CardEffectType 처리)
- `Assets/Scripts/UI/HandManager.cs` — 핸드 관리 (덱/핸드/버림 더미, DrawCards, BurnAllHand, RefreshHand)
- `Assets/Scripts/UI/CardClickHandler.cs` — 카드 클릭→사용 (에너지 체크, 흔들기 피드백)

#### 수정된 스크립트
- `EnemyData.cs` — def, EnemyBehaviorType, foodReward/goldReward 추가
- `EnemyUnit.cs` — HeroUnit 타겟, DEF 적용, TakeDamageIgnoreDef(), 행동 패턴 (Enrager/Defender)
- `WorldData.cs` — WorldType enum, MoveCost 프로퍼티 추가
- `BattleManager.cs` — HeroUnit 기반 전면 재작성, 난이도 스케일링, 에너지/핸드 시작 연결
- `CardDisplay.cs` — SetupCard(CardData) 추가, 등급 테두리 색상 지원
- `BattleUI.cs` — 주인공 HP 바 갱신, 동료 카드 생성 제거
- `WorldManager.cs` — 식량 시스템, WorldType별 분기, IncreaseDifficulty(), 보상 패널
- `PenaltyManager.cs` — 동료 이탈 제거 → HP 감소/난이도 증가 2종으로 단순화

### 씬 구성 — 다음 진행 시 체크 필요
- [ ] 씬에 HeroUnit 오브젝트 배치 + HeroUnit 컴포넌트 + HP 슬라이더/텍스트 연결
- [ ] BattleManager 오브젝트에 heroData, HeroUnit, energySystem, handManager 연결
- [ ] EnergySystem 오브젝트 생성 + 에너지 슬라이더/텍스트 UI 연결
- [ ] HandManager 오브젝트 생성 + handArea(HorizontalLayoutGroup), cardPrefab 연결
- [ ] 카드 프리팹: Image + CardDisplay + CardClickHandler + CardHover (CardDragHandler 대신)
- [ ] HeroData SO 생성 (Assets/ScriptableObjects/Hero_Default.asset)
- [ ] CardData SO 샘플 카드 최소 5장 생성 (공격/방어/유틸 각 1~2장)

### 기존 ScriptableObject (유지)
- `Assets/ScriptableObjects/Companion_Test.asset`
- `Assets/ScriptableObjects/Companion_Test2.asset`

---

## Build & Development

- **Unity Version**: 6000.3.5f2 — open the project in Unity Hub with this version
- **IDE**: Rider or Visual Studio (both supported via editor packages)
- **Solution File**: `Exul_Mundi.sln` / `Assembly-CSharp.csproj`
- **Test Framework**: Unity Test Framework is included in packages (`com.unity.test-framework` 1.6.0) — run tests via Unity Test Runner window
