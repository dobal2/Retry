using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RASCAL {
    [RequireComponent(typeof(RASCALSkinnedMeshCollider))]
    public class RetargetSkeleton : MonoBehaviour {

        public Transform targetRoot;
        public bool destroyIfNoMatch = true;

        RASCALSkinnedMeshCollider rascal;

        private void Awake() {
            rascal = GetComponent<RASCALSkinnedMeshCollider>();
            rascal.generateOnStart = false;
        }

        private void Start() {
            Retarget();
        }

        async void Retarget() {

            await rascal.ProcessMeshAsync();

            foreach (var skin in rascal.skinfos) {
                foreach (var bone in skin.bones) {

                    Transform newT;
                    if (newT = RecursiveFindChild(targetRoot, bone.transform.name)) {

                        bone.transform = newT;

                        foreach (var col in bone.boneMeshes) {
                            var newMC = newT.gameObject.AddComponent<MeshCollider>();
                            newMC.convex = col.meshCol.convex;
                            newMC.sharedMaterial = col.meshCol.sharedMaterial;

                            Destroy(col.meshCol);
                            col.meshCol = newMC;
                        }

                    } else if (destroyIfNoMatch) {
                        foreach (var col in bone.boneMeshes) {
                            Destroy(col.meshCol);
                        }
                        bone.boneMeshes.Clear();
                    }
                }
            }

            rascal.StartAsyncUpdating(rascal.enableUpdatingOnStart);
        }


        Transform RecursiveFindChild(Transform parent, string childName) {
            foreach (Transform child in parent) {
                if (child.name == childName) {
                    return child;
                } else {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null) {
                        return found;
                    }
                }
            }
            return null;
        }

    }
}
