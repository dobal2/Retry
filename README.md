# Retry

Procedural 생성 지형 위에서 끊임없이 밀려오는 적을 처치하고, 업그레이드를 쌓아 10분 30초간 생존하는 3인칭 슈터 로그라이크 게임입니다. 시간 정지와 타임 스킵 스킬을 전략적으로 활용해 위기를 극복합니다.

## 플레이 영상

> 플레이 영상 링크를 여기에 추가하세요

## 개요

| 항목 | 내용 |
|------|------|
| 엔진 | Unity 2024 (URP) |
| 언어 | C# |
| 개발 인원 | 1인 개발 |
| 개발 기간 | 2025.10.24 ~ 2025.11.22 (약 4주) |
| 렌더링 | Universal Render Pipeline |
| 해상도 | 1920x1080, 적응형 스케일링 |

## 게임 플레이

- Procedural 생성된 지형에서 시작 → 적이 웨이브로 스폰 → 처치하여 에너지 수집 → 업그레이드 카드 선택 → 난이도 상승 반복
- 25초마다 난이도가 상승하며, 25단계(10분 30초)를 생존하면 클리어
- 포스필드 밖으로 나가면 지속 데미지를 받으며, 에너지를 소비해 포스필드를 확장할 수 있습니다
- 사망 시 게임오버, Retry로 재시작

### 난이도 단계

| 레벨 | 난이도 | 특징 |
|------|--------|------|
| 0 | Easy | 기본 스폰 간격 3초, 최대 적 10마리 |
| 1~6 | Normal | 스폰 간격 감소, 적 체력/데미지 증가 |
| 7~12 | Hard | 가속적 난이도 스케일링 |
| 13~18 | Insane | 최대 적 50마리까지 증가 |
| 19~24 | Extreme | 최소 스폰 간격 0.5초 |
| 25 | Clear | 게임 클리어 |

## 조작법

| 키 | 동작 |
|----|------|
| WASD | 이동 |
| Space | 점프 (업그레이드로 더블 점프 가능) |
| 마우스 | 조준 |
| 좌클릭 | 사격 |
| E | 시간 정지 스킬 |
| Q | 타임 스킵 (대시) |
| F | 상호작용 (상자 열기 / 포스필드 업그레이드, 대상을 바라보고 사용) |

## 담당 기술

### Procedural 지형 생성

Perlin Noise 기반의 청크 시스템으로 무한 지형을 생성합니다.

- **멀티스레드 청크 생성**: `Task.WhenAll`로 모든 청크 데이터를 병렬 계산 후 메인 스레드에서 메시 생성
- **Falloff Map**: 맵 가장자리가 자연스럽게 낮아지도록 Falloff 함수를 적용하고, 중심부는 보호 계수로 평탄하게 유지
- **옥타브 노이즈**: 4개 옥타브를 중첩하여 자연스러운 지형 굴곡 생성 (Lacunarity 2, Persistence 0.5)
- **View Distance Culling**: 플레이어 위치 기반으로 200m 이내 청크만 활성화
- **동적 풀 생성**: 초기 로딩 시 중심 150m만 생성 후, 플레이어 이동에 따라 120m 반경 내 청크에 풀을 점진적으로 생성
- **시드 기반 월드**: FNV-1a 해시로 문자열 시드를 숫자로 변환, 동일 시드로 재현 가능한 지형 생성
- **로딩 시스템**: 지형 생성 → 메시 빌드 → 풀 생성 → 오브젝트 배치 단계별 진행률 UI 표시

### 시간 정지 스킬 시스템

E키로 발동하는 시간 정지 스킬에 다층 포스트프로세싱 연출을 구현했습니다.

- **이펙트 시퀀스**: Lens Distortion(팽창→수축→복귀) → Hue Shift(0→180→0) → Saturation(-100) → Chromatic Aberration 순차 실행
- **Lens Distortion 3단계**: 0→0.3(팽창, 0.2초) → -0.7(수축, 0.3초) → 0(복귀, 0.3초)
- **시간 정지 트리거 타이밍**: Lens Distortion 종료 0.4초 전에 실제 시간 정지를 발동하여 연출과 게임플레이를 동기화
- **Unscaled Time**: 모든 이펙트가 `Time.unscaledDeltaTime`으로 동작해 Time.timeScale 영향 없이 재생
- **앵커박스 색상 전환**: 시간 정지 중 모든 적의 UI 표시기가 회색으로 전환되어 정지 상태를 시각적으로 표현
- **Volumetric Fog 연동**: `_ColorBlend` 프로퍼티를 통해 시간 정지 중 안개 색상 전환
- **시계 사운드 루프**: 시간 정지 동안 일정 간격으로 시계 틱 사운드 반복 재생

### 타임 스킵 (대시) 시스템

Q키로 발동하는 대시 스킬을 구현했습니다.

- **Time.timeScale 조작**: 대시 중 timeScale을 3배로 가속하여 플레이어 이동 속도 증가 효과
- **Saturation 시퀀스**: 대시 발동 시 채도를 -100으로 급격히 낮춘 뒤 서서히 복원
- **시간 정지 충돌 방지**: 시간 정지 활성화 중에는 Saturation 시퀀스를 자동으로 건너뛰어 이펙트 충돌 방지
- **쿨다운**: 1초 쿨타임, `Time.unscaledDeltaTime`으로 계산해 시간 정지 중에도 쿨다운 진행

### 적 AI 및 웨이브 시스템

적 AI는 Rigidbody 기반 물리 이동과 장애물 회피를 구현했습니다.

- **감지 → 추적 → 장애물 회피**: 감지 범위(15m) 내 플레이어를 추적하되, 전방 장애물 감지 시 ±45도 회피 방향 탐색
- **수중 이동**: 워터 레이어 감지 시 중력 비활성화 + Drag 증가로 수영 물리 전환
- **난이도 스케일링**: 25초마다 `scalingFactor = 1 + (level * 0.1)`로 가속적 난이도 증가 — 스폰 간격 감소, 최대 적 수/체력/데미지 배율 증가
- **시간 정지 동기화**: `TimeStopManager`가 관리하는 EntityType 기반 동결 — Rigidbody velocity를 캐시 후 kinematic 전환, 해제 시 velocity 복원
- **Object Pool**: 적 생성/파괴 대신 풀링으로 GC 스파이크 방지
- **사망 연출**: Material `_Transparency` 프로퍼티 페이드 아웃 + 앵커박스 UI 알파 동시 감소
- **에너지 드랍**: 난이도 레벨에 비례하여 드랍량 증가 (기본 2~3개 + 난이도 보너스)

### 포스필드 시스템

맵 중앙의 보호 구역을 관리하는 레벨 기반 포스필드를 구현했습니다.

- **레벨 시스템**: 최대 10레벨, 레벨당 크기 5 증가 (기본 크기 10)
- **에너지 비용 곡선**: `baseEnergyCost + (level-1) * baseEnergyCost * 1.3` — 레벨이 올라갈수록 비용 증가
- **영역 판정**: 매 프레임 플레이어와 포스필드 중심 거리 계산, 밖에서는 초당 10 데미지
- **Fog 연동**: 포스필드 내부에서는 안개 밀도 0, 외부에서는 1.88로 전환하여 시야 제한
- **테레인 스캐너**: 레벨업 시 ParticleSystem 기반 스캔 웨이브 발사, 시간 정지 중에는 일시정지
- **성장 애니메이션**: AnimationCurve 기반 크기 보간 + 레벨업 시 Pulse 이펙트

### 업그레이드 카드 시스템

적 처치 후 3장의 카드 중 1장을 선택하는 로그라이크 업그레이드 시스템을 구현했습니다.

| 업그레이드 | 수치 |
|-----------|------|
| Max Health | +25~40 HP |
| Attack Speed | +25~40% |
| Projectile Damage | +25~40% |
| Projectile Speed | +25~40% |
| Projectile Size | +32~52% |
| Move Speed | +25~40% |
| Jump Force | +25~40% |
| Extra Jump | +1 점프 횟수 |

- **가중치 기반 랜덤**: 각 업그레이드 타입별 가중치를 설정할 수 있어 출현 확률 조정 가능
- **Time.timeScale 0**: 카드 선택 중 게임 시간을 정지시키고 마우스 커서를 해제
- **오디오 Low Pass 필터**: 카드 선택 UI 등장 시 BGM에 Low Pass 필터를 적용해 몰입감 강화

### 피격 이펙트 시스템

플레이어 피격 시 카메라 셰이크, Vignette, Material Blur를 복합적으로 연출합니다.

- **Camera Shake**: SmoothCameraShaker 기반 흔들림
- **Vignette Pulse**: AnimationCurve로 Vignette 강도를 0→0.5→0으로 펄스
- **Material Blur**: `_BlurOffset`과 `_Intensity` 프로퍼티를 AnimationCurve에 맞춰 동시에 보간
- **자동 회복**: 일정 간격으로 체력 1씩 자연 회복

### 커스텀 셰이더

HLSL 직접 작성과 Shader Graph를 활용하여 게임의 시각 연출을 구현했습니다.

#### HLSL 셰이더

| 셰이더 | 설명 |
|--------|------|
| **VolumetricFog** | Ray Marching 기반 볼류메트릭 안개 — 3D 노이즈 텍스처 샘플링, Henyey-Greenstein 위상 함수로 광산란 계산, 시간 정지 시 `_ColorBlend`로 색상 전환 |
| **TerrainShader** | 높이 기반 그라디언트 지형 — 월드 Y좌표로 그라디언트 텍스처를 샘플링하여 높이별 색상 적용, URP PBR 라이팅 + ShadowCaster/DepthOnly/DepthNormals 패스 포함 |
| **ForceField** | Fresnel + 헥사곤 패턴 보호막 — Depth 기반 교차선 감지, 헥사곤 텍스처 스크롤, `_AnimationTime`으로 시간 정지 중 애니메이션 독립 제어 |
| **UI_Glow** | UI Additive Blending 글로우 — HDR `_GlowColor`와 `_GlowStrength`로 UI 요소 발광 효과 |

#### Shader Graph

| 셰이더 | 설명 |
|--------|------|
| **Dissolve / VerticalDissolve** | 노이즈 기반 디졸브 이펙트 |
| **TerrainScanner** | 포스필드 스캔 웨이브 시각 효과 |
| **SafeZoneField** | 안전 구역 경계 표시 |
| **CRT** | CRT 모니터 스캔라인 효과 |
| **Fresnel / HeatDistortion / GalaxyShader** | 프레넬 림라이트, 열 왜곡, 갤럭시 이펙트 |

## 기술 사항

### 디자인 패턴

- **Singleton** — GameManager, PlayerStats, SoundManager, ForceFieldManager, TimeStopManager, UpgradeCardManager를 씬 내 상태 관리에 사용
- **Object Pool** — 적 스폰/디스폰 시 Instantiate/Destroy 대신 풀링으로 메모리 최적화

### 렌더링

- Universal Render Pipeline (URP)
- Post Processing Volume (ColorAdjustments, ChromaticAberration, Vignette, LensDistortion)
- 커스텀 HLSL 셰이더 5개 + Shader Graph 8개
- Minimap Render Texture

### 오디오

- BGM/SFX 분리 관리 (SoundManager)
- 채널 기반 다중 SFX 동시 재생
- 카드 선택 시 Audio Low Pass Filter 적용

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Player/
│   │   ├── ThirdPersonShooterController.cs  # 사격 및 조준
│   │   ├── PlayerStats.cs                   # 체력, 스탯, 피격 이펙트
│   │   ├── TimeStopSkill.cs                 # 시간 정지 스킬 + 포스트프로세싱
│   │   └── TimeSkip.cs                      # 대시 (타임 스킵)
│   ├── Enemy/
│   │   ├── EnemyAI.cs                       # 적 AI (추적, 회피, 수영, 동결)
│   │   ├── SpiderAI.cs                      # 스파이더 봇 AI
│   │   └── SpiderLegsIK.cs                  # 스파이더 다리 IK
│   ├── Terrain/
│   │   ├── OptimizedTerrainGenerator.cs     # Procedural 지형 생성 (멀티스레드)
│   │   ├── TerrainChunk.cs                  # 청크 메시 관리
│   │   └── ChunkData.cs                     # 청크 데이터 구조체
│   ├── Manager/
│   │   ├── GameManager.cs                   # 게임 초기화, 씬 전환
│   │   └── TimeStopManager.cs               # 시간 정지 상태 관리
│   ├── AnchorBox/
│   │   ├── AnchorBoxManager.cs              # 적 타겟 UI 관리
│   │   ├── UpgradeCardManager.cs            # 업그레이드 카드 스폰/선택
│   │   └── UpgradeCard.cs                   # 업그레이드 정의 및 적용
│   ├── UI/
│   │   ├── MenuUiFunction.cs                # 메뉴 씬 기능
│   │   ├── LoadingUI.cs                     # 로딩 화면
│   │   ├── PlayerHealthBar.cs               # 체력 바
│   │   └── TimeStopSkillCoolDownUi.cs       # 쿨다운 UI
│   ├── Grass/
│   │   ├── GrassGenerator.cs                # 풀 생성 (에디터/초기)
│   │   └── RuntimeGrassGenerator.cs         # 동적 풀 생성
│   ├── SoundManager/
│   │   └── SoundManager.cs                  # BGM/SFX 오디오 시스템
│   ├── ForceFieldManager.cs                 # 포스필드 레벨/보호/Fog
│   ├── PooledSpawner.cs                     # 웨이브 스폰 + 난이도 스케일링
│   ├── ObjectPool.cs                        # 오브젝트 풀링
│   ├── Energy.cs                            # 에너지 코어 수집/자석
│   ├── Box.cs                               # 루트 박스
│   ├── EnemyProjectile.cs                   # 적 투사체
│   └── MagicWeapon.cs                       # 무기 시스템
├── Scenes/
│   ├── Start.unity                          # 메뉴
│   ├── Main.unity                           # 메인 게임
│   └── Clear.unity                          # 클리어
├── Prefabs/
├── Models/
├── Sounds/
├── Sprites/
├── Materials/
└── Shaders/
```

## 사용 에셋

| 에셋 | 용도 |
|------|------|
| Infima Games - Low Poly Shooter Pack | 무기/사격 시스템 베이스 |
| StarterAssets - Third Person Controller | 3인칭 캐릭터 컨트롤러 |
| Hovl Studio - Toon Projectiles | 투사체 이펙트 |
| Stylized Water 3 | 물 렌더링 |
| Free Stylized Skybox | 스카이박스 |
| Scalable Grid Prototype Materials | 프로토타입 머티리얼 |
| Simple Crosshair Generator | 크로스헤어 UI |
| RASCAL - Skinned Mesh Collider | 스킨드 메시 콜라이더 |
| Rapa Motion - Mage Bundle Samples | 마법사 애니메이션 |
| Script_Wizard_AnimSet | 위자드 애니메이션 세트 |
| FirstGearGames - SmoothCameraShaker | 카메라 셰이크 |
| FreeFlyCamera | 에디터 프리플라이 카메라 |
| Compute Shader Grass | 풀 렌더링 (Compute Shader) |
| Evil Robot 3D Model (Sandman Shape) | 적 3D 모델 |
| LowPoly SpiderBot (Rzenn) | 스파이더 봇 3D 모델 |
| Energy Core 3D Model | 에너지 코어 3D 모델 |
| Power Box 3D Model | 루트 박스 3D 모델 |
| TextMesh Pro | 텍스트 렌더링 |

## 라이선스

이 프로젝트의 소스 코드는 [MIT License](./LICENSE)를 따릅니다.

프로젝트에 포함된 서드파티 에셋(스프라이트, 사운드, 모델, 플러그인 등)은 각 에셋의 원래 라이선스를 따릅니다.
