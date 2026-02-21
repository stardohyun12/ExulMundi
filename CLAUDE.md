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

## Build & Development

- **Unity Version**: 6000.3.5f2 — open the project in Unity Hub with this version
- **IDE**: Rider or Visual Studio (both supported via editor packages)
- **Solution File**: `Exul_Mundi.sln` / `Assembly-CSharp.csproj`
- **Test Framework**: Unity Test Framework is included in packages (`com.unity.test-framework` 1.6.0) — run tests via Unity Test Runner window
