using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.AnimatedValues;
using System.Reflection;
using System.Linq.Expressions;
using RASCAL.Localization;

namespace RASCAL {
    [CustomEditor(typeof(RASCALSkinnedMeshCollider))]
    public class RASCALInspector : Editor {

        GUIStyle bold = new GUIStyle();
        GUIStyle center = new GUIStyle();
        GUIStyle label = new GUIStyle();

        private void Awake() {
            SetStyles();
        }

        void SetStyles() {
            bold = new GUIStyle(EditorStyles.boldLabel);
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 12;

            center = new GUIStyle(EditorStyles.label);
            center.alignment = TextAnchor.LowerCenter;
            center.fontStyle = FontStyle.Bold;
            center.fontSize = 12;

            label = new GUIStyle(EditorStyles.label);
            label.padding = new RectOffset(4, 0, 0, 0);
            label.alignment = TextAnchor.UpperLeft;
            label.fontSize = 12;
        }

        Vector2 excludeScrollPos;
        Vector2 matAssScrollPos;

        [SerializeField]
        static AnimBool assocAndExcludeAnim;

        SerializedProperty maxTris = null;

        private void OnEnable() {
            maxTris = serializedObject.FindProperty(nameof(RASCALSkinnedMeshCollider.maxColliderTriangles));

            if (assocAndExcludeAnim == null)
                assocAndExcludeAnim = new AnimBool(false);
            assocAndExcludeAnim.valueChanged.AddListener(Repaint);
        }


        public override void OnInspectorGUI() {
            serializedObject.Update();

            RASCALSkinnedMeshCollider rascal = (RASCALSkinnedMeshCollider)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Locale.GetName("generation"), bold);

            //if(GUILayout.Button(new GUIContent("Generate", "Generate collision meshes based on the current settings. THIS WILL UPDATE YOUR PREFAB if it is part of one, otherwise everything breaks."))) {
            //    rascal.ProcessMesh();
            //    rascal.ImmediateUpdateColliders(true);

            //    return;
            //}


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLocalContent("clear"),
                new GUIStyle(EditorStyles.miniButton) { fixedWidth = 120 })) {
                rascal.CleanUpMeshes();
                return;
            }

            EditorGUI.BeginChangeCheck();
            bool clearAllMeshColliders = EditorGUILayout.Toggle(GetLocalContent("clearAllMeshColliders"), rascal.clearAllMeshColliders);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(rascal, Locale.GetMsg("changeVariableUndo"));
                rascal.clearAllMeshColliders = clearAllMeshColliders;
                Undo.FlushUndoRecordObjects();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();


            bool generateOnStart = EditorGUILayout.ToggleLeft(GetContent(() => rascal.generateOnStart), rascal.generateOnStart, label);
            bool immediateStartupCollision = rascal.immediateStartupCollision;
            if (rascal.generateOnStart) {
                immediateStartupCollision = EditorGUILayout.ToggleLeft(GetContent(() => rascal.immediateStartupCollision), rascal.immediateStartupCollision, label);
            }

            EditorGUILayout.Space();

            bool onlyUniqueTriangles = EditorGUILayout.ToggleLeft(GetContent(() => rascal.onlyUniqueTriangles), rascal.onlyUniqueTriangles, label);
            bool splitCollisionMeshesByMaterial = EditorGUILayout.ToggleLeft(GetContent(() => rascal.splitCollisionMeshesByMaterial), rascal.splitCollisionMeshesByMaterial, label);
            bool zeroBoneMeshAlternateTransform = EditorGUILayout.ToggleLeft(GetContent(() => rascal.zeroBoneMeshAlternateTransform), rascal.zeroBoneMeshAlternateTransform, label);
            bool convexMeshColliders = EditorGUILayout.ToggleLeft(GetContent(() => rascal.convexMeshColliders), rascal.convexMeshColliders, label);

            EditorGUILayout.Space();

            float boneWeightThreshold = EditorGUILayout.Slider(GetContent(() => rascal.boneWeightThreshold), rascal.boneWeightThreshold, 0f, 1f);

            EditorGUILayout.PropertyField(maxTris, GetContent(() => rascal.maxColliderTriangles));
            PhysicsMaterial physicsMaterial = EditorGUILayout.ObjectField(GetContent(() => rascal.physicsMaterial), rascal.physicsMaterial, typeof(PhysicsMaterial), true) as PhysicsMaterial;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbar) { fixedHeight = 1 }, GUILayout.Height(2));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Locale.GetName("updating"), bold);

            bool enableUpdatingOnStart = EditorGUILayout.ToggleLeft(GetContent(() => rascal.enableUpdatingOnStart), rascal.enableUpdatingOnStart, label);
#if UNITY_2019_3_OR_NEWER
            bool useThreadedColMeshBaking = EditorGUILayout.ToggleLeft(GetContent(() => rascal.useThreadedColMeshBaking), rascal.useThreadedColMeshBaking, label);
#endif
            double idleCpuBudget = EditorGUILayout.DoubleField(GetContent(() => rascal.idleCpuBudget), rascal.idleCpuBudget);
            double activeCpuBudget = EditorGUILayout.DoubleField(GetContent(() => rascal.activeCpuBudget), rascal.activeCpuBudget);
            float meshUpdateThreshold = EditorGUILayout.FloatField(GetContent(() => rascal.meshUpdateThreshold), rascal.meshUpdateThreshold);

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(rascal, Locale.GetMsg("changeVariableUndo"));
                rascal.generateOnStart = generateOnStart;
                rascal.immediateStartupCollision = immediateStartupCollision;
                rascal.onlyUniqueTriangles = onlyUniqueTriangles;
                rascal.boneWeightThreshold = boneWeightThreshold;
                rascal.splitCollisionMeshesByMaterial = splitCollisionMeshesByMaterial;
                rascal.zeroBoneMeshAlternateTransform = zeroBoneMeshAlternateTransform;
                rascal.convexMeshColliders = convexMeshColliders;

#if UNITY_2019_3_OR_NEWER
                rascal.useThreadedColMeshBaking = useThreadedColMeshBaking;
#endif

                if (rascal.maxColliderTriangles < 2) rascal.maxColliderTriangles = 2;

                rascal.physicsMaterial = physicsMaterial;

                rascal.enableUpdatingOnStart = enableUpdatingOnStart;
                rascal.idleCpuBudget = idleCpuBudget;
                rascal.activeCpuBudget = activeCpuBudget;
                rascal.meshUpdateThreshold = meshUpdateThreshold;
                Undo.FlushUndoRecordObjects();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            assocAndExcludeAnim.target = EditorGUILayout.Foldout(assocAndExcludeAnim.target, Locale.GetName("materialAssociationsAndExclusions"), true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 12 });

            if (EditorGUILayout.BeginFadeGroup(assocAndExcludeAnim.faded)) {


                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(GetLocalContent("materialAssociationList"), new GUIStyle(center) { fixedHeight = 18 }, GUILayout.Width(156));
                if (GUILayout.Button("+", new GUIStyle(EditorStyles.miniButton) { fontSize = 13 }, GUILayout.Width(20), GUILayout.Height(20))) {
                    Undo.RecordObject(rascal, Locale.GetMsg("addMaterialAssociationUndo"));
                    rascal.materialAssociationList.Add(new RASCALSkinnedMeshCollider.PhysicsMaterialAssociation());
                    Undo.FlushUndoRecordObjects();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                matAssScrollPos = EditorGUILayout.BeginScrollView(matAssScrollPos, EditorStyles.helpBox, GUILayout.Height(rascal.materialAssociationList.Count * 26), GUILayout.MaxHeight(140));
                for (int i = 0; i < rascal.materialAssociationList.Count;) {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("X", new GUIStyle(EditorStyles.miniButton) { }, GUILayout.Width(19), GUILayout.Height(16))) {
                        Undo.RecordObject(rascal, Locale.GetMsg("removeMaterialAssociationUndo"));
                        rascal.materialAssociationList.Remove(rascal.materialAssociationList[i]);
                        Undo.FlushUndoRecordObjects();
                        continue;
                    }

                    EditorGUI.BeginChangeCheck();
                    Material material = EditorGUILayout.ObjectField(rascal.materialAssociationList[i].material, typeof(Material), true) as Material;
                    if (EditorGUI.EndChangeCheck()) {
                        Undo.RecordObject(rascal, Locale.GetMsg("setMaterialUndo"));
                        rascal.materialAssociationList[i].material = material;
                        Undo.FlushUndoRecordObjects();
                    }
                    EditorGUILayout.LabelField(" -> ", GUILayout.Width(28));

                    EditorGUI.BeginChangeCheck();
                    PhysicsMaterial physMat = EditorGUILayout.ObjectField(rascal.materialAssociationList[i].physicsMaterial, typeof(PhysicsMaterial), true) as PhysicsMaterial;
                    if (EditorGUI.EndChangeCheck()) {
                        Undo.RecordObject(rascal, Locale.GetMsg("setPhysMatUndo"));
                        rascal.materialAssociationList[i].physicsMaterial = physMat;
                        Undo.FlushUndoRecordObjects();
                    }


                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbar) { fixedHeight = 1 }, GUILayout.Height(2));
                    i++;
                }
                EditorGUILayout.EndScrollView();

                //exclusion stuff
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField(GetLocalContent("addExclusions"), center);
                EditorGUILayout.BeginHorizontal();
                SkinnedMeshRenderer exSkin = EditorGUILayout.ObjectField(null, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
                Transform exBone = EditorGUILayout.ObjectField(null, typeof(Transform), true) as Transform;
                Material exMat = EditorGUILayout.ObjectField(null, typeof(Material), true) as Material;
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) {
                    //New Obj was assigned

                    if (exSkin && !rascal.excludedSkins.Contains(exSkin)) {
                        Undo.RecordObject(rascal, Locale.GetMsg("addExcludedSkinnedMeshUndo"));
                        rascal.excludedSkins.Add(exSkin);
                        Undo.FlushUndoRecordObjects();
                    }
                    if (exMat && !rascal.excludedMaterials.Contains(exMat)) {
                        Undo.RecordObject(rascal, Locale.GetMsg("addExcludedMaterialUndo"));
                        rascal.excludedMaterials.Add(exMat);
                        Undo.FlushUndoRecordObjects();
                    }
                    if (exBone && !rascal.excludedBones.Contains(exBone)) {
                        Undo.RecordObject(rascal, Locale.GetMsg("addExcludedBoneUndo"));
                        rascal.excludedBones.Add(exBone);
                        Undo.FlushUndoRecordObjects();
                    }
                }

                const bool useDisabledLook = false;

                excludeScrollPos = EditorGUILayout.BeginScrollView(excludeScrollPos, EditorStyles.helpBox, GUILayout.Height(excludeCount(rascal) * 27), GUILayout.MaxHeight(160));
                for (int i = 0; i < rascal.excludedSkins.Count;) {
                    EditorGUILayout.BeginHorizontal(new GUIStyle() { margin = new RectOffset(0, 0, 0, 0) });

                    if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(16))) {
                        Undo.RecordObject(rascal, Locale.GetMsg("removeExcludedSkinnedMeshUndo"));
                        rascal.excludedSkins.Remove(rascal.excludedSkins[i]);
                        Undo.FlushUndoRecordObjects();
                        continue;
                    }

                    EditorGUI.BeginDisabledGroup(useDisabledLook);
                    rascal.excludedSkins[i] = EditorGUILayout.ObjectField(rascal.excludedSkins[i], typeof(SkinnedMeshRenderer), true, GUILayout.Height(17)) as SkinnedMeshRenderer;
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbar) { fixedHeight = 1 }, GUILayout.Height(2));

                    i++;
                }
                for (int i = 0; i < rascal.excludedBones.Count;) {
                    EditorGUILayout.BeginHorizontal(new GUIStyle() { margin = new RectOffset(0, 0, 0, 0) });

                    if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(16))) {
                        Undo.RecordObject(rascal, Locale.GetMsg("removeExcludedBoneUndo"));
                        rascal.excludedBones.Remove(rascal.excludedBones[i]);
                        Undo.FlushUndoRecordObjects();
                        continue;
                    }

                    EditorGUI.BeginDisabledGroup(useDisabledLook);
                    rascal.excludedBones[i] = EditorGUILayout.ObjectField(rascal.excludedBones[i], typeof(Transform), true, GUILayout.Height(17)) as Transform;
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbar) { fixedHeight = 1 }, GUILayout.Height(2));

                    i++;
                }
                for (int i = 0; i < rascal.excludedMaterials.Count;) {
                    EditorGUILayout.BeginHorizontal(new GUIStyle() { margin = new RectOffset(0, 0, 0, 0) });

                    if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(16))) {
                        Undo.RecordObject(rascal, Locale.GetMsg("removeExcludedMaterialUndo"));
                        rascal.excludedMaterials.Remove(rascal.excludedMaterials[i]);
                        Undo.FlushUndoRecordObjects();
                        continue;
                    }

                    EditorGUI.BeginDisabledGroup(useDisabledLook);
                    rascal.excludedMaterials[i] = EditorGUILayout.ObjectField(rascal.excludedMaterials[i], typeof(Material), true, GUILayout.Height(17)) as Material;
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbar) { fixedHeight = 1 }, GUILayout.Height(2));

                    i++;
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        static GUIContent GetLocalContent(string name) {
            return new GUIContent(Locale.GetName(name), Locale.GetTooltip(name));
        }

        GUIContent GetContent<T>(Expression<System.Func<T>> memberExpression) {
            MemberInfo member = ((MemberExpression)memberExpression.Body).Member;
            string fieldName = member.Name;


            string labelStr = Locale.GetName(fieldName);
            if (string.IsNullOrEmpty(labelStr)) {

                char[] label = fieldName.ToCharArray();
                labelStr = label[0].ToString().ToUpper();
                for (int i = 1; i < fieldName.Length; i++) {
                    if (char.IsUpper(label[i])) {
                        labelStr += " ";
                    }
                    labelStr += label[i];
                }
            }

            return new GUIContent(labelStr, GetTooltip(fieldName));
        }

        string GetTooltip(string fieldName) {

            string tooltip = Locale.GetTooltip(fieldName);
            if (string.IsNullOrEmpty(tooltip)) {

                var tip = (TooltipAttribute)typeof(RASCALSkinnedMeshCollider).GetField(fieldName).GetCustomAttribute(typeof(TooltipAttribute));
                if (tip == null) return "";
                tooltip = tip.tooltip;
            }

            return tooltip;
        }

        int excludeCount(RASCALSkinnedMeshCollider rascal) { return rascal.excludedSkins.Count + rascal.excludedBones.Count + rascal.excludedMaterials.Count; }

    }
}