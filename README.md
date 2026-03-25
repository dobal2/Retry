# WaSans

Undertale의 Sans 전투에서 영감을 받은 2D 픽셀아트 액션 플랫포머 보스러시 게임입니다. 스테이지를 클리어하며 다양한 적을 처리하고, 최종 보스 Sans와 전투합니다.

## 플레이 영상

[![WaSans 플레이 영상](https://img.youtube.com/vi/VIDEO_ID/0.jpg)](https://youtu.be/VIDEO_ID)

> 플레이 영상 링크를 여기에 추가하세요

## 개요

| 항목 | 내용 |
|------|------|
| 엔진 | Unity |
| 언어 | C# |
| 개발 인원 | 1인 개발 |
| 개발 기간 | 2025.04 ~ 2025.04 (약 2주) |
| 해상도 | 3840×2160 타겟, 적응형 스케일링 |

## 게임 플레이

- Stage 1 → Stage 2 → Stage 3 → Sans 보스전 → 클리어
- 각 스테이지의 적을 모두 처리하면 다음 스테이지로 진행됩니다
- 스테이지 간 체력이 회복됩니다
- 사망 시 현재 스테이지에서 재시작합니다

## 조작법

| 키 | 동작 |
|----|------|
| A/D 또는 ←/→ | 좌우 이동 |
| Space | 점프 (공중에서 더블 점프) |
| 좌클릭 | 근접 공격 (2히트 콤보) |
| 우클릭 | 대시 |

## 담당 기술

### 적 AI 패턴 구현

Sans 보스 AI에 9가지 공격 패턴을 구현했습니다.

- 거리에 따라 걷기와 순간이동을 전환하는 이동 AI가 적용되어 있습니다
- BlueBone(가만히 있으면 회피), OrangeBone(움직이면 회피) 등 색상별 투사체 회피 메카닉을 구현했습니다
- Gaster Blaster의 차징 애니메이션과 빔 확장 연출을 구현했습니다

### Sans 보스 공격 패턴

| 패턴 | 설명 |
|------|------|
| Bone | 단일 뼈 투사체 |
| MultiBone | 일반/파랑/주황 뼈 5개 랜덤 발사 |
| Teleport | 플레이어 뒤로 순간이동 |
| BlueBone | 파란 뼈 10개, 가만히 있으면 회피 가능 |
| OrangeBone | 주황 뼈 10개, 움직이면 회피 가능 |
| Blue & Orange | 혼합 뼈 10개, 고속 |
| Gaster Blaster | 단일 빔 공격 |
| Triple Gaster Blaster | 3연 빔 공격 |
| Tracking Gaster Blaster | 플레이어 위치 추적 빔 |

### 대시 시스템

쿨다운 기반 대시를 구현했습니다.

- 대시 중 무적 프레임이 적용됩니다

### 대시 잔상 이펙트

- 대시 시 플레이어의 스프라이트를 복제하여 잔상을 생성합니다
- 일정 거리마다 잔상 오브젝트를 생성하고, 알파값을 점차 감소시켜 페이드아웃 처리했습니다
- 완전히 투명해지면 자동으로 오브젝트가 파괴됩니다

### 스테이지 관리 시스템

- GameManager에서 스테이지 진행, 적 스폰, 클리어 판정을 관리합니다
- 스테이지별 적 배치와 자동 진행 로직을 구현했습니다
- 모든 적 처치 감지 후 다음 스테이지로 전환됩니다

### Tank 포물선 사격

탄도 계산 기반의 포물선 궤적 사격을 구현했습니다.

- 플레이어 위치를 기반으로 발사 각도(70도)를 계산합니다
- 착탄 시 폭발 이펙트가 발생합니다

## 기술 사항

### 디자인 패턴

- **Singleton** — GameManager, AudioManager를 씬 간 상태 유지에 사용했습니다

### 이펙트

- 대시 잔상 (스프라이트 복제 + 알파 페이드)
- 카메라 셰이크
- 플로팅 데미지/Miss 텍스트
- 피격 무적 프레임, 넉백

### 오디오

- BGM과 SFX를 분리하여 관리합니다
- 채널 기반 다중 SFX 동시 재생을 지원합니다

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Player/
│   │   ├── CharacterController2D.cs   # 플레이어 물리/이동
│   │   ├── PlayerMovement.cs          # 입력 처리
│   │   ├── Attack.cs                  # 콤보 공격
│   │   ├── CameraFollow.cs            # 카메라 추적/셰이크
│   │   ├── HealthDisplay.cs           # 하트 HP UI
│   │   └── DashAfterImage.cs          # 대시 잔상 이펙트
│   ├── Enemies/
│   │   ├── Sans.cs                    # 보스 AI (9가지 패턴)
│   │   ├── GasterBlaster.cs           # 보스 빔 공격
│   │   ├── Enemy.cs                   # 기본 적 AI
│   │   ├── Tank.cs                    # 포탑 적
│   │   ├── Ally.cs                    # 아군 AI
│   │   ├── Bone.cs                    # 기본 투사체
│   │   ├── BlueBone.cs                # 파란 뼈 (멈추면 회피)
│   │   └── OrangeBone.cs             # 주황 뼈 (움직이면 회피)
│   ├── GameManager.cs                 # 스테이지/게임 상태 관리
│   ├── AudioManager.cs                # 오디오 시스템
│   └── Environment/                   # 풀, 파괴 오브젝트, 킬존
├── Scenes/
│   ├── MainGame.unity                 # 메인 게임 씬
│   └── Clear.unity                    # 클리어 씬
├── Prefabs/
├── Sprites/
└── Audio/
```

## 사용 에셋

| 에셋 | 용도 |
|------|------|
| [Prototype Hero - Pixel Art](https://assetstore.unity.com/) | 플레이어 캐릭터 스프라이트 |
| [Bandits - Pixel Art](https://assetstore.unity.com/) | 적 캐릭터 스프라이트 |
| [Pixel Art Tanks](https://assetstore.unity.com/) | Tank 적 스프라이트 |
| [Industrial City](https://assetstore.unity.com/) | 배경 타일맵 |
| [MetroidvaniaController](https://assetstore.unity.com/) | 캐릭터 컨트롤러 베이스 |
| [19 Pixel Sprite effects](https://assetstore.unity.com/) | 이펙트 |
| [25 Pixel Sprite effects](https://assetstore.unity.com/) | 이펙트 |
| [Pixel UI](https://assetstore.unity.com/) | UI 스프라이트 |
| [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest) | 텍스트 렌더링 |
| [Minecraft Font](https://www.dafont.com/) | 게임 폰트 |

## 라이선스

이 프로젝트의 소스 코드는 [MIT License](./LICENSE)를 따릅니다.

프로젝트에 포함된 서드파티 에셋(스프라이트, 사운드, 폰트, 플러그인 등)은 각 에셋의 원래 라이선스를 따릅니다.
