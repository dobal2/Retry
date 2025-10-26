using System.Collections.Generic;

namespace RASCAL.Localization {
    public class Japanese : Locale {

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

        internal new static string isoCodeStatic => "ja";
        internal override string isoCode => isoCodeStatic;
        internal override Dictionary<string, string> names => _names;
        internal override Dictionary<string, string> tooltips => _tooltips;
        internal override Dictionary<string, string> messages => _messages;


        static readonly Dictionary<string, string> _names = new Dictionary<string, string> {
            {"clear", "クリア"},
            {"generation", "生成"},
            {"updating", "更新"},
            {"materialAssociationsAndExclusions", "マテリアルの関連付けと除外"},
            {"materialAssociationList", "マテリアルの関連付け"},
            {"addExclusions", "除外を追加"},
            {"clearAllMeshColliders", "すべてのメッシュコライダー"},
            {"generateOnStart", "開始時に生成"},
            {"immediateStartupCollision", "起動時に即時衝突"},
            {"onlyUniqueTriangles", "一意の三角形のみ"},
            {"splitCollisionMeshesByMaterial", "マテリアルごとに衝突メッシュを分割"},
            {"zeroBoneMeshAlternateTransform", "ゼロボーンメッシュの代替トランスフォーム"},
            {"convexMeshColliders", "凸メッシュコライダー"},
            {"boneWeightThreshold", "ボーンウェイトのしきい値"},
            {"maxColliderTriangles", "最大コライダー三角形"},
            {"physicsMaterial", "物理マテリアル"},
            {"enableUpdatingOnStart", "開始時に更新を有効にする"},
            {"useThreadedColMeshBaking", "スレッド化されたコルメッシュベーキングを使用"},
            {"idleCpuBudget", "アイドルCPUバジェット"},
            {"activeCpuBudget", "アクティブCPUバジェット"},
            {"meshUpdateThreshold", "メッシュ更新のしきい値"}
        };

        static readonly Dictionary<string, string> _tooltips = new Dictionary<string, string> {
            {"clear", "このスクリプトによって生成されたすべてのメッシュコライダーとデータを破棄します。これはもう必要ありませんが、念のためここに残しておきます。"},
            {"clearAllMeshColliders", "クリア機能を呼び出すときに、現在このコンポーネントに関連付けられているコライダーだけでなく、コンポーネントの下のすべてのメッシュコライダーをクリアします。これには注意してください。"},
            {"materialAssociationList", "このリストを使用して、スキンメッシュのマテリアルを、メッシュコライダーに追加される物理マテリアルに関連付けます。メッシュに複数のマテリアルがある場合は、マテリアルごとに衝突メッシュを分割するオプションを必ず確認してください。"},
            {"addExclusions", "スキンメッシュ、ボーントランスフォーム、またはマテリアルを除外リストに追加するには、以下のスロットにドラッグします。これにより、指定されたメッシュ、ボーントランスフォーム、およびマテリアルの衝突メッシュが生成されなくなります。"},
            {"generateOnStart", "これにより、衝突で使用されるボーンメッシュの作成に必要なすべてのデータが処理および生成され、ゲームの開始時にいくつかの初期衝突形状も生成されます。"},
            {"immediateStartupCollision", "これにより、完全に生成されるまでに1〜2秒かかる可能性のある非同期メソッドを使用するのではなく、起動時に即座に衝突が生成されます。これは明らかに、初期のゲーム読み込み時間が長くなるという犠牲を伴います。ゲームの読み込みから数秒以内に衝突がすぐに必要ない場合は、これを有効にしないことをお勧めします。"},
            {"onlyUniqueTriangles", "有効にすると、すべてのボーンメッシュ間ですべての一意の三角形のみが使用されます。これにより、メッシュの重複は防止されますが、三角形がどのボーンにとってより重要であるかを気にせずに選択されるため、特定の衝突の結果に影響を与える可能性のある乱雑なボーンメッシュになる可能性があります。少し速く、使用するメモリも少なくなります。このオプションは、結果としていくつかの悪い衝突メッシュが生成されていることに気付かない限り、オンにしておく必要があります。"},
            {"splitCollisionMeshesByMaterial", "各衝突メッシュをマテリアルごとに分割します。たとえば、スキンメッシュに2つのマテリアルがある場合、最初のマテリアルを持つすべての三角形に対して1つのコライダーを作成し、2番目のマテリアルに対して別のコライダーを作成します。これは、マテリアルに基づいてコライダーに異なる物理マテリアルを適用したり、マテリアルに基づいてメッシュパーツを除外したりするために、ほとんどの場合に役立ち、必要です。これを行うには、マテリアル関連付けリストと除外リストを使用します。"},
            {"zeroBoneMeshAlternateTransform", "これが必要になることはほとんどありません。しかし、念のため含まれています。基本的には、ボーンがなくブレンドシェイプのみのメッシュが異なる方法で変換されるようにします。しかし、デフォルトで問題ないはずです。これは最後の手段のトラブルシューティング手順である必要があります。"},
            {"convexMeshColliders", "メッシュコライダーの凸設定を有効にします。このオプションは明らかにメッシュ全体で不正確になり、エラーを回避するためにメッシュあたりの最大ポリゴン数を下げる必要がある場合がありますが、凸メッシュでは、それが必要な場合は非キネマティックリジッドボディを使用できるはずです。"},
            {"boneWeightThreshold", "コライダーを生成するとき、頂点をコライダーに追加するには、頂点のボーンウェイトがこれより上である必要があります。"},
            {"maxColliderTriangles", "各衝突形状で許可される三角形/ポリゴンの最大数。これより多くの量が含まれている場合、メッシュは複数のメッシュコライダーに分割されます。大きなメッシュを分割すると、大きなメッシュがメッシュコライダーを更新するときにヒッチングを引き起こす可能性があるため、非同期の衝突生成フレームレートの安定性に役立ちます。ただし、メッシュを分割すると、すべての衝突形状の更新にわずかに時間がかかる場合がありますが、これはおそらくごくわずかです。"},
            {"physicsMaterial", "メッシュコライダーに適用するマテリアル。ボーンのマテリアルをよりきめ細かく制御する必要がある場合は、ボーントランスフォームまたはスキンメッシュトランスフォームにRASCALPropertiesコンポーネントを追加する必要があります。優先度は、マテリアル関連付けリスト->ボーントランスフォーム->スキンメッシュトランスフォーム->物理マテリアル変数の順に高から低になります。"},
            {"enableUpdatingOnStart", "これにより、ゲームの開始時に衝突形状の継続的な非同期更新が有効になります。初期の衝突形状で十分に機能し、更新する必要さえないことがわかる場合があります。これにより、パフォーマンスが確実に節約されますが、ボーンが移動したためにメッシュが変形すると、形状が一致せず、かなり大きな不正確さが生じる可能性があります。または、特定の時点でメッシュを更新する必要があることがわかっている場合は、スクリプトで提供されている関数を介して、衝突形状の即時または非同期の再構築を手動で呼び出すことができます。"},
            {"useThreadedColMeshBaking", "Unityの新機能を使用して、別のスレッドで手動で衝突メッシュデータをベイク処理します。ベーキングステップはメッシュの更新で最もコストのかかるステップであるため、これを有効にすると、メインスレッドはそれを必要とするより多くのことを自由に行うことができます。ただし、これには、スレッド化のオーバーヘッドのために個々のコライダーの更新に時間がかかるという中程度のトレードオフが伴います。最新のコンピューターでは、複数の異なるコライダーを別々のスレッドで一度に処理できるため、全体的にはまだ高速である可能性があります。"},
            {"idleCpuBudget", "メッシュが衝突形状を再構築するほど十分に変化していないときに、非同期生成がフレームごとに実行されることを許可する時間（ミリ秒単位）。（この設定は即時更新には影響しません）"},
            {"activeCpuBudget", "衝突形状のいずれかがアクティブに再構築されているときに、非同期生成がフレームごとに実行されることを許可する時間（ミリ秒単位）。これにより、衝突形状を再構築するためにより多くの時間が割り当てられ、パフォーマンスを犠牲にしてより速く更新されることを意味します。（この設定は即時更新には影響しません）"},
            {"meshUpdateThreshold", "衝突を再構築するためにボーンメッシュが変更する必要がある量。これの目的は、完全に再構築する前にメッシュにわずかな変更を加え、精度を犠牲にしてパフォーマンスをわずかに向上させることです。この値は非常に小さい可能性がありますが、何が最適かを確認するために試してみる必要があります。"}
        };

        static readonly Dictionary<string, string> _messages = new Dictionary<string, string> {
            {"changeVariableUndo", "RASCAL変数を変更"},
            {"addMaterialAssociationUndo", "RASCALマテリアルの関連付けを追加"},
            {"removeMaterialAssociationUndo", "RASCALマテリアルの関連付けを削除"},
            {"setMaterialUndo", "RASCAL MAマテリアルを設定"},
            {"setPhysMatUndo", "RASCAL MA PhysMatを設定"},
            {"addExcludedSkinnedMeshUndo", "除外されたスキンメッシュを追加"},
            {"addExcludedMaterialUndo", "除外されたマテリアルを追加"},
            {"addExcludedBoneUndo", "除外されたボーンを追加"},
            {"removeExcludedSkinnedMeshUndo", "除外されたスキンメッシュを削除"},
            {"removeExcludedBoneUndo", "除外されたボーンを削除"},
            {"removeExcludedMaterialUndo", "除外されたマテリアルを削除"},

            //--------------------------------------------------------------------------------
            //MISC---------------------------------------------------------------
            //--------------------------------------------------------------------------------
            { "languageSelectorTitle", "RASCAL言語セレクター" },
            { "selectLanguagePrompt", "言語を選択してください：" },
            { "languageLabel", "言語：" },
            { "confirmSelectionButton", "選択を確定" },
        };
    }
}