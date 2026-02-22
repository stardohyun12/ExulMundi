# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Exul Mundi is a Unity 6 (version 6000.3.5f2) 2D card-based companion battle game. It uses the Universal Render Pipeline (URP) with 2D renderer and the modern Input System package.

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

### 핵심 루프
쇼츠처럼 스와이프로 스테이지(배경/몬스터 환경)를 넘겨서 이동. 각 스테이지에서 전투 발생.

### 스테이지 이동 비용
- 스와이프로 이동 시 **식량(Food)** 소모
- 식량이 부족하면 이동 불가 또는 패널티 발생
- 식량은 전투 승리 등으로 획득

### 도주 시스템 (기존)
- 전투 중 스와이프 → 도주
- 패널티 3종 중 선택: HP 감소 / 동료 이탈 / 난이도 증가

### 미정 항목
- 카드 배치 수량 및 방식 (전투 중 스킬 카드 vs 동료 편성 카드)

## 현재 진행 상황 (2026-02-22 기준)

### 구현 완료 — 하스스톤 스타일 카드 배치 시스템

**수정된 스크립트 3개:**
- `Assets/Scripts/UI/CardDragHandler.cs` — `isPlayed` 플래그, `ConfirmPlay()` 추가. 한 번 슬롯에 놓으면 드래그 불가
- `Assets/Scripts/UI/PartySlot.cs` — 불필요한 UI 필드 제거, `OnDrop`에서 `drag.ConfirmPlay()` 호출. 슬롯 중복 배치 차단
- `Assets/Scripts/UI/PartyManager.cs` — `OnCardPlayed()` 추가 (손 목록에서 제거), `PlaceInSlot`의 `SetOccupant` 호출 제거

**씬:** `Assets/Scenes/CardTest.unity` 사용 중

### 씬 구성 진행 상황

**완료 여부 불명 (다음 대화 시작 시 확인 필요):**
- [ ] Canvas 생성 (Screen Space Overlay, 1920×1080)
- [ ] FieldArea (HorizontalLayoutGroup, Pos Y:80, 820×200)
- [ ] Slot1~4 각각 생성 + PartySlot 컴포넌트 + EmptyState 자식 연결
- [ ] HandArea (HorizontalLayoutGroup, Pos Y:90, 900×180, Force Expand OFF)
- [ ] DragLayer (stretch, Canvas 맨 마지막 자식)
- [ ] Managers > PartyManager 오브젝트 + 컴포넌트

### 카드 프리팹 진행 상황

- [ ] `Assets/Prefabs/` 폴더 생성
- [ ] Card 오브젝트 (130×180, Image + CanvasGroup + CardDisplay + CardDragHandler + CardHover)
- [ ] 자식: CardImage / NameText / HPText / ATKText / SkillText (전부 top-stretch 앵커)
- [ ] CardDisplay 필드 연결 (NameText, HPText, ATKText, SkillText, CardImage)
- [ ] Prefabs 폴더에 프리팹 저장
- [ ] PartyManager에 Card Prefab 연결
- [ ] PartyManager Owned Companions에 Companion_Test, Companion_Test2 연결

### 다음에 할 일
1. 위 체크리스트에서 완료되지 않은 항목 마저 세팅
2. Play 모드로 검증:
   - HandArea에 카드 2장 자동 생성되는지
   - 카드 드래그 → 슬롯 드롭 → 카드가 슬롯 안으로 이동, 손에서 사라지는지
   - 이미 찬 슬롯에 드롭 시 거부되는지
   - 필드에 배치된 카드 재드래그 불가한지

### 기존 ScriptableObject
- `Assets/ScriptableObjects/Companion_Test.asset`
- `Assets/ScriptableObjects/Companion_Test2.asset`

---

## Build & Development

- **Unity Version**: 6000.3.5f2 — open the project in Unity Hub with this version
- **IDE**: Rider or Visual Studio (both supported via editor packages)
- **Solution File**: `Exul_Mundi.sln` / `Assembly-CSharp.csproj`
- **Test Framework**: Unity Test Framework is included in packages (`com.unity.test-framework` 1.6.0) — run tests via Unity Test Runner window
