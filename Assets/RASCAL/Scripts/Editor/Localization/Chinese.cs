using System.Collections.Generic;

namespace RASCAL.Localization {
    public class Chinese : Locale {

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

        internal new static string isoCodeStatic => "zh";
        internal override string isoCode => isoCodeStatic;
        internal override Dictionary<string, string> names => _names;
        internal override Dictionary<string, string> tooltips => _tooltips;
        internal override Dictionary<string, string> messages => _messages;


        static readonly Dictionary<string, string> _names = new Dictionary<string, string> {
            {"clear", "清除"},
            {"generation", "生成"},
            {"updating", "更新"},
            {"materialAssociationsAndExclusions", "材质关联和排除"},
            {"materialAssociationList", "材质关联"},
            {"addExclusions", "添加排除项"},
            {"clearAllMeshColliders", "所有网格碰撞器"},
            {"generateOnStart", "启动时生成"},
            {"immediateStartupCollision", "立即启动碰撞"},
            {"onlyUniqueTriangles", "仅唯一三角形"},
            {"splitCollisionMeshesByMaterial", "按材质拆分碰撞网格"},
            {"zeroBoneMeshAlternateTransform", "零骨骼网格备用变换"},
            {"convexMeshColliders", "凸面网格碰撞器"},
            {"boneWeightThreshold", "骨骼权重阈值"},
            {"maxColliderTriangles", "最大碰撞器三角形"},
            {"physicsMaterial", "物理材质"},
            {"enableUpdatingOnStart", "在开始时启用更新"},
            {"useThreadedColMeshBaking", "使用线程化碰撞网格烘焙"},
            {"idleCpuBudget", "空闲 CPU 预算"},
            {"activeCpuBudget", "活动 CPU 预算"},
            {"meshUpdateThreshold", "网格更新阈值"}
        };

        static readonly Dictionary<string, string> _tooltips = new Dictionary<string, string> {
            {"clear", "销毁此脚本生成的所有网格碰撞器和数据。这不再需要了，但我会把它留在这里以防万一。"},
            {"clearAllMeshColliders", "调用清除功能时，清除组件下的所有网格碰撞器，而不仅仅是当前与此组件关联的碰撞器。请小心使用。"},
            {"materialAssociationList", "使用此列表将蒙皮网格中的材质与将添加到网格碰撞器的物理材质相关联。如果您的网格有多种材质，请确保选中按材质拆分碰撞网格的选项。"},
            {"addExclusions", "将蒙皮网格、骨骼变换或材质拖到下面的插槽中，以将其添加到排除列表。这样就不会为给定的网格、骨骼变换和材质生成碰撞网格。"},
            {"generateOnStart", "这将处理并生成创建用于碰撞的骨骼网格所需的所有数据，并在游戏开始时生成一些初始碰撞形状。"},
            {"immediateStartupCollision", "这会在启动时立即生成碰撞，而不是使用可能需要一两秒钟才能完全生成的异步方法。这显然是以更长的初始游戏加载时间为代价的。如果您在游戏加载后的几秒钟内不需要立即发生碰撞，建议不要启用此功能。"},
            {"onlyUniqueTriangles", "启用后，所有骨骼网格之间将仅使用唯一的三角形。这可以防止网格重叠，但是由于三角形的选择方式不考虑三角形对哪个骨骼更重要，因此可能会导致混乱的骨骼网格，从而影响某些碰撞的结果。它速度稍快，使用的内存更少，除非您注意到因此生成了一些不良的碰撞网格，否则该选项可能应该打开。"},
            {"splitCollisionMeshesByMaterial", "按材质拆分每个碰撞网格。例如，如果您的蒙皮网格有两种材质，它将为具有第一种材质的所有三角形创建一个碰撞器，并为第二种材质创建另一个碰撞器。这主要用于根据材质将不同的物理材质应用于碰撞器或根据材质排除网格部分。为此，请使用材质关联列表和排除列表。"},
            {"zeroBoneMeshAlternateTransform", "您几乎肯定不需要这个。但为了以防万一，还是包含了它。它基本上使没有骨骼且只有混合形状的网格以不同的方式进行变换。但默认情况下应该没问题，这应该是最后的故障排除步骤。"},
            {"convexMeshColliders", "启用网格碰撞器的凸面设置。此选项显然会导致整个网格不准确，您可能需要降低每个网格的最大多边形数以避免错误，但凸面网格应允许使用非运动学刚体（如果您需要的话）。"},
            {"boneWeightThreshold", "生成碰撞器时，顶点的骨骼权重必须高于此值才能将顶点添加到碰撞器。"},
            {"maxColliderTriangles", "每个碰撞形状中允许的最大三角形/多边形数。如果它包含的数量超过此数量，网格将被拆分为多个网格碰撞器。拆分大型网格有助于异步碰撞生成帧速率稳定性，因为大型网格在更新网格碰撞器时可能会导致卡顿。但是，拆分网格会使更新所有碰撞形状的时间稍长一些，但这可能是微不足道的。"},
            {"physicsMaterial", "应用于网格碰撞器的材质。如果您需要对骨骼上的材质进行更精细的控制，则需要在骨骼变换或蒙皮网格变换中添加 RASCALProperties 组件。优先级从高到低依次为：材质关联列表 -> 骨骼变换 -> 蒙皮网格变换 -> 物理材质变量"},
            {"enableUpdatingOnStart", "这将在游戏开始时启用碰撞形状的连续异步更新。您可能会发现初始碰撞形状效果很好，甚至不需要更新它们，这肯定会节省性能，但是如果网格因骨骼移动而变形，形状将不匹配并可能导致一些非常大的不准确性。或者，如果您知道您的网格只需要在某些点进行更新，您可以手动调用脚本中提供的函数来立即或异步重建碰撞形状。"},
            {"useThreadedColMeshBaking", "使用 Unity 中的一项新功能在单独的线程上手动烘焙碰撞网格数据。烘焙步骤是更新网格中成本最高的步骤，因此启用此功能可以使主线程可以自由地执行更多需要它的操作。但是，这带来了一个适度的权衡，即由于线程开销，每个单独的碰撞器更新所需的时间会更长。在现代计算机上，这可能仍然总体上更快，因为可以同时在单独的线程上处理多个不同的碰撞器。"},
            {"idleCpuBudget", "当网格变化不足以保证重建碰撞形状时，允许异步生成每帧运行的时间（以毫秒为单位）。（此设置不影响立即更新）"},
            {"activeCpuBudget", "当任何碰撞形状正在被主动重建时，允许异步生成每帧运行的时间（以毫秒为单位）。这允许更多的时间来重建碰撞形状，这意味着它们将以性能为代价更快地更新。（此设置不影响立即更新）"},
            {"meshUpdateThreshold", "骨骼网格需要更改的数量才能重建其碰撞。这样做的目的是在完全重建之前允许网格发生微小变化，从而以牺牲准确性为代价略微提高性能。此值可能应该很小，但您应该尝试一下，看看什么最适合您。"}
        };

        static readonly Dictionary<string, string> _messages = new Dictionary<string, string> {
            {"changeVariableUndo", "RASCAL 更改变量"},
            {"addMaterialAssociationUndo", "添加 RASCAL 材质关联"},
            {"removeMaterialAssociationUndo", "删除 RASCAL 材质关联"},
            {"setMaterialUndo", "RASCAL MA 设置材质"},
            {"setPhysMatUndo", "RASCAL MA 设置物理材质"},
            {"addExcludedSkinnedMeshUndo", "添加排除的蒙皮网格"},
            {"addExcludedMaterialUndo", "添加排除的材质"},
            {"addExcludedBoneUndo", "添加排除的骨骼"},
            {"removeExcludedSkinnedMeshUndo", "删除排除的蒙皮网格"},
            {"removeExcludedBoneUndo", "删除排除的骨骼"},
            {"removeExcludedMaterialUndo", "删除排除的材质"},

            //--------------------------------------------------------------------------------
            //MISC---------------------------------------------------------------
            //--------------------------------------------------------------------------------
            { "languageSelectorTitle", "RASCAL 语言选择器" },
            { "selectLanguagePrompt", "请选择一种语言：" },
            { "languageLabel", "语言：" },
            { "confirmSelectionButton", "确认选择" },
        };
    }
}