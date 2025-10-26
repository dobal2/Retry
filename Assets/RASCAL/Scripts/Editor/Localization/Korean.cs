using System.Collections.Generic;

namespace RASCAL.Localization {
    public class Korean : Locale {

        new public static string GetTooltip(string memberName) {
            _tooltips.TryGetValue(memberName, out var tip);
            return tip;
        }

        new public static string GetName(string memberName) {
            _names.TryGetValue(memberName, out var name);
            return name;
        }

        new public static string GetMsg(string memberName) {
            _messages.TryGetValue(memberName, out var msg);
            return msg;
        }

        internal new static string isoCodeStatic => "ko";
        internal override string isoCode => isoCodeStatic;
        internal override Dictionary<string, string> names => _names;
        internal override Dictionary<string, string> tooltips => _tooltips;
        internal override Dictionary<string, string> messages => _messages;


        static readonly Dictionary<string, string> _names = new Dictionary<string, string> {
            {"clear", "지우기"},
            {"generation", "생성"},
            {"updating", "업데이트 중"},
            {"materialAssociationsAndExclusions", "재질 연관 및 제외"},
            {"materialAssociationList", "재질 연관"},
            {"addExclusions", "제외 추가"},
            {"clearAllMeshColliders", "모든 메시 콜라이더"},
            {"generateOnStart", "시작 시 생성"},
            {"immediateStartupCollision", "즉시 시작 충돌"},
            {"onlyUniqueTriangles", "고유 삼각형만"},
            {"splitCollisionMeshesByMaterial", "재질별 충돌 메시 분할"},
            {"zeroBoneMeshAlternateTransform", "제로 본 메시 대체 변환"},
            {"convexMeshColliders", "볼록 메시 콜라이더"},
            {"boneWeightThreshold", "본 가중치 임계값"},
            {"maxColliderTriangles", "최대 콜라이더 삼각형"},
            {"physicsMaterial", "물리 재질"},
            {"enableUpdatingOnStart", "시작 시 업데이트 활성화"},
            {"useThreadedColMeshBaking", "스레드 컬 메시 베이킹 사용"},
            {"idleCpuBudget", "유휴 CPU 예산"},
            {"activeCpuBudget", "활성 CPU 예산"},
            {"meshUpdateThreshold", "메시 업데이트 임계값"}
        };

        static readonly Dictionary<string, string> _tooltips = new Dictionary<string, string> {
            {"clear", "이 스크립트로 생성된 모든 메시 콜라이더와 데이터를 파괴합니다. 더 이상 필요하지 않지만 만일을 위해 여기에 남겨 둡니다."},
            {"clearAllMeshColliders", "지우기 기능을 호출할 때 현재 이 구성 요소와 관련된 콜라이더뿐만 아니라 구성 요소 아래의 모든 메시 콜라이더를 지웁니다. 이 점에 주의하십시오."},
            {"materialAssociationList", "이 목록을 사용하여 스킨 메시의 재질을 메시 콜라이더에 추가될 물리 재질과 연결합니다. 메시에 여러 재질이 있는 경우 재질별로 충돌 메시를 분할하는 옵션을 선택해야 합니다."},
            {"addExclusions", "제외 목록에 스킨 메시, 본 변환 또는 재질을 추가하려면 아래 슬롯으로 드래그합니다. 이렇게 하면 지정된 메시, 본 변환 및 재질에 대한 충돌 메시가 생성되지 않습니다."},
            {"generateOnStart", "이렇게 하면 충돌에 사용되는 본 메시를 만드는 데 필요한 모든 데이터를 처리하고 생성하며 게임이 시작될 때 일부 초기 충돌 모양도 생성합니다."},
            {"immediateStartupCollision", "완전히 생성하는 데 1~2초가 걸릴 수 있는 비동기 방법을 사용하는 대신 시작 시 즉시 충돌을 생성합니다. 이는 분명히 초기 게임 로드 시간이 길어지는 단점이 있습니다. 게임 로드 후 몇 초 내에 충돌이 즉시 필요하지 않은 경우 이 기능을 활성화하지 않는 것이 좋습니다."},
            {"onlyUniqueTriangles", "활성화하면 모든 본 메시 간에 고유한 삼각형만 사용됩니다. 이렇게 하면 메시 중복을 방지할 수 있지만 삼각형이 어떤 본에 더 중요한지 고려하지 않고 선택되기 때문에 특정 충돌 결과에 영향을 줄 수 있는 지저분한 본 메시가 생성될 수 있습니다. 약간 더 빠르고 메모리를 덜 사용하므로 이로 인해 일부 잘못된 충돌 메시가 생성되는 것을 발견하지 않는 한 이 옵션을 켜두는 것이 좋습니다."},
            {"splitCollisionMeshesByMaterial", "각 충돌 메시를 재질별로 분할합니다. 예를 들어 스킨 메시에 2개의 재질이 있는 경우 첫 번째 재질이 있는 모든 삼각형에 대해 하나의 콜라이더를 만들고 두 번째 재질에 대해 다른 콜라이더를 만듭니다. 이는 재질에 따라 콜라이더에 다른 물리 재질을 적용하거나 재질에 따라 메시 부품을 제외하는 데 주로 유용하고 필요합니다. 이렇게 하려면 재질 연관 목록 및 제외 목록을 사용합니다."},
            {"zeroBoneMeshAlternateTransform", "거의 확실히 이것이 필요하지 않습니다. 하지만 만일을 위해 포함되어 있습니다. 기본적으로 본이 없고 블렌드 셰이프만 있는 메시가 다르게 변환되도록 합니다. 하지만 기본적으로는 괜찮을 것입니다. 이것은 최후의 문제 해결 단계여야 합니다."},
            {"convexMeshColliders", "메시 콜라이더의 볼록 설정을 활성화합니다. 이 옵션은 분명히 전체 메시의 부정확성을 유발하며 오류를 피하기 위해 메시당 최대 다각형 수를 낮춰야 할 수도 있지만 볼록 메시를 사용하면 필요한 경우 비운동학적 강체를 사용할 수 있습니다."},
            {"boneWeightThreshold", "콜라이더를 생성할 때 정점을 콜라이더에 추가하려면 정점의 본 가중치가 이 값보다 높아야 합니다."},
            {"maxColliderTriangles", "각 충돌 모양에서 허용되는 최대 삼각형/다각형 수입니다. 이보다 많은 양을 포함하면 메시는 여러 메시 콜라이더로 분할됩니다. 큰 메시를 분할하면 큰 메시가 메시 콜라이더를 업데이트할 때 히칭을 유발할 수 있으므로 비동기 충돌 생성 프레임 속도 안정성에 도움이 될 수 있습니다. 그러나 메시를 분할하면 모든 충돌 모양을 업데이트하는 데 시간이 약간 더 걸릴 수 있지만 이는 아마도 무시할 수 있을 것입니다."},
            {"physicsMaterial", "메시 콜라이더에 적용할 재질입니다. 본의 재질을 보다 세부적으로 제어해야 하는 경우 본 변환 또는 스킨 메시 변환에 RASCALProperties 구성 요소를 추가해야 합니다. 우선 순위는 재질 연관 목록 -> 본 변환 -> 스킨 메시 변환 -> 물리 재질 변수 순으로 높습니다."},
            {"enableUpdatingOnStart", "이렇게 하면 게임이 시작될 때 충돌 모양의 지속적인 비동기 업데이트가 활성화됩니다. 초기 충돌 모양이 충분히 잘 작동하여 업데이트할 필요조차 없다는 것을 알 수 있습니다. 이렇게 하면 성능이 확실히 절약되지만 본이 이동하여 메시가 변형되면 모양이 일치하지 않아 상당히 큰 부정확성을 유발할 수 있습니다. 또는 특정 지점에서만 메시를 업데이트해야 한다는 것을 알고 있는 경우 스크립트에서 제공하는 기능을 통해 충돌 모양의 즉시 또는 비동기 재구성을 수동으로 호출할 수 있습니다."},
            {"useThreadedColMeshBaking", "Unity의 새로운 기능을 사용하여 별도의 스레드에서 수동으로 충돌 메시 데이터를 베이킹합니다. 베이킹 단계는 메시 업데이트에서 가장 비용이 많이 드는 단계이므로 이 기능을 활성화하면 메인 스레드가 더 많은 작업을 자유롭게 수행할 수 있습니다. 그러나 이는 스레딩 오버헤드로 인해 각 개별 콜라이더를 업데이트하는 데 시간이 더 오래 걸리는 중간 정도의 절충안을 동반합니다. 최신 컴퓨터에서는 여러 다른 콜라이더를 별도의 스레드에서 한 번에 처리할 수 있으므로 전체적으로는 여전히 더 빠를 것입니다."},
            {"idleCpuBudget", "메시가 충돌 모양을 재구성할 만큼 충분히 변경되지 않을 때 비동기 생성을 프레임당 실행할 수 있는 시간(밀리초)입니다. (이 설정은 즉시 업데이트에 영향을 주지 않음)"},
            {"activeCpuBudget", "충돌 모양 중 하나가 활발하게 재구성될 때 비동기 생성을 프레임당 실행할 수 있는 시간(밀리초)입니다. 이렇게 하면 충돌 모양을 재구성하는 데 더 많은 시간을 할애할 수 있으므로 성능 저하를 감수하고 더 빨리 업데이트됩니다. (이 설정은 즉시 업데이트에 영향을 주지 않음)"},
            {"meshUpdateThreshold", "충돌을 재구성하기 위해 본 메시가 변경해야 하는 양입니다. 이것의 목적은 완전히 재구성하기 전에 메시를 약간 변경하여 정확도를 희생하면서 성능을 약간 향상시키는 것입니다. 이 값은 매우 작을 가능성이 높지만 가장 적합한 것을 찾기 위해 여러 가지를 시도해 보아야 합니다."}
        };

        static readonly Dictionary<string, string> _messages = new Dictionary<string, string> {
            {"changeVariableUndo", "RASCAL 변수 변경"},
            {"addMaterialAssociationUndo", "RASCAL 재질 연관 추가"},
            {"removeMaterialAssociationUndo", "RASCAL 재질 연관 제거"},
            {"setMaterialUndo", "RASCAL MA 재질 설정"},
            {"setPhysMatUndo", "RASCAL MA PhysMat 설정"},
            {"addExcludedSkinnedMeshUndo", "제외된 스킨 메시 추가"},
            {"addExcludedMaterialUndo", "제외된 재질 추가"},
            {"addExcludedBoneUndo", "제외된 본 추가"},
            {"removeExcludedSkinnedMeshUndo", "제외된 스킨 메시 제거"},
            {"removeExcludedBoneUndo", "제외된 본 제거"},
            {"removeExcludedMaterialUndo", "제외된 재질 제거"},
            //--------------------------------------------------------------------------------
            //MISC---------------------------------------------------------------
            //--------------------------------------------------------------------------------
            { "languageSelectorTitle", "RASCAL 언어 선택기" },
            { "selectLanguagePrompt", "언어를 선택하세요:" },
            { "languageLabel", "언어:" },
            { "confirmSelectionButton", "선택 확인" },
        };
    }
}