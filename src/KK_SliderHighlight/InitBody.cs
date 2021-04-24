using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChaCustom;
using HarmonyLib;
using KKABMX.Core;
using KKAPI.Maker;
using StrayTech;
using UnityEngine;
using UnityEngine.UI;

namespace SliderHighlight
{
    public partial class SliderHighlightPlugin
    {
        private void InitializeBodySliders()
        {
            var makerBase = MakerAPI.GetMakerBase();
            var sibBodyTv = Traverse.Create(makerBase.chaCtrl.GetSibBody());
            var dictCategory = sibBodyTv.Field<Dictionary<int, List<ShapeInfoBase.CategoryInfo>>>("dictCategory").Value;
            var dictDst = sibBodyTv.Field<Dictionary<int, ShapeInfoBase.BoneInfo>>("dictDst").Value;

            var idLookup = BoneIdLookups.BoneIdLookupBodyF;
            HookCvsSliders(makerBase.GetComponentInChildren<CvsArm>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsBodyAll>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsBodyLower>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsBodyShapeAll>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsBodyUpper>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsBreast>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsLeg>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
        }

        private void InitializeFaceSliders()
        {
            var makerBase = MakerAPI.GetMakerBase();
            var sibFaceTv = Traverse.Create(makerBase.chaCtrl.GetSibFace());
            var dictCategory = sibFaceTv.Field<Dictionary<int, List<ShapeInfoBase.CategoryInfo>>>("dictCategory").Value;
            var dictDst = sibFaceTv.Field<Dictionary<int, ShapeInfoBase.BoneInfo>>("dictDst").Value;

            // Everything goes through straight so can be ignored, might be needed if any manual edits are needed
            var idLookup = (Dictionary<int, int[]>)null; //BoneIdLookups.BoneIdLookupFaceF;
            HookCvsSliders(makerBase.GetComponentInChildren<CvsCheek>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsChin>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsEar>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsEye01>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsEyebrow>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsFaceAll>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsFaceShapeAll>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsMouth>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
            HookCvsSliders(makerBase.GetComponentInChildren<CvsNose>(true), _boneSliderLookup, idLookup, dictCategory, dictDst);
        }

        private void HookCvsSliders(MonoBehaviour cvsMb,
            Dictionary<Slider, Func<IEnumerable<Transform>>> sliderLookup,
            Dictionary<int, int[]> boneIdLookup,
            Dictionary<int, List<ShapeInfoBase.CategoryInfo>> dictCategory,
            Dictionary<int, ShapeInfoBase.BoneInfo> dictDst)
        {
            var cvsTv = Traverse.Create(cvsMb);
            var sliders = cvsTv.Field<Slider[]>("sliders").Value;
            var arrIndex = cvsTv.Field<int[]>("arrIndex").Value;
            for (var i = 0; i < sliders.Length; i++)
            {
                var sld = sliders[i];
                var ari = arrIndex[i];
                if (dictCategory.TryGetValue(ari, out var ids))
                {
                    var affectedBones = ids
                        .SelectMany(categoryInfo =>
                        {
                            if (boneIdLookup == null)
                            {
                                dictDst.TryGetValue(categoryInfo.id, out var boneInfo);
                                return boneInfo?.trfBone == null ? Enumerable.Empty<Transform>() : new[] { boneInfo.trfBone };
                            }

                            if (boneIdLookup.TryGetValue(categoryInfo.id, out var dstIds))
                            {
                                return dstIds.Select(did =>
                                    {
                                        dictDst.TryGetValue(did, out var boneInfo);
                                        return boneInfo?.trfBone;
                                    })
                                    .Where(x => x != null);
                            }

                            Logger.LogDebug($"Bone ID={categoryInfo.id} Name={categoryInfo.name} missing in lookup for slider {sld.transform.FullPath()}");
                            return Enumerable.Empty<Transform>();
                        })
                        .ToList();

                    if (affectedBones.Count > 0) sliderLookup[sld] = () => affectedBones;
                }
            }
        }

        private static void LoadHighlightBody(ChaControl chaControl)
        {
            var renderers = chaControl.objRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            var body = renderers.First(x => x.name == "o_body_a");
            _smrBod = CreateHighlightRenderer(body, _mat);

            var face = renderers.First(x => x.name == "cf_O_face");
            _smrFac = CreateHighlightRenderer(face, _mat);

            // Clear the colors, might be set to something already
            _isHighlightCleared = false;
            HighlightBones();
        }

        private static SkinnedMeshRenderer CreateHighlightRenderer(SkinnedMeshRenderer body, Material mat)
        {
            // Guard against plugins using StartsWith to find renderers
            var namae = "Highlight_" + body.name + "_rend";
            DestroyImmediate(body.transform.Find(namae)?.gameObject);

            var rootGo = new GameObject(namae);
            rootGo.layer = body.gameObject.layer;
            rootGo.transform.SetParent(body.transform, false);

            var smr = rootGo.AddComponent<SkinnedMeshRenderer>();
            smr.rootBone = body.rootBone;
            smr.bones = body.bones;
            // Need to make a copy to avoid overwriting stock mesh colors since they are used for some thing by the game
            smr.sharedMesh = Instantiate(body.sharedMesh);
            smr.sharedMaterial = mat;
            return smr;
        }

        private static void HighlightBones(IEnumerable<Transform> bones = null)
        {
            if (bones != null)
            {
                if (!(bones is ICollection))
                    bones = bones.ToList();

                if (!bones.Any())
                {
                    if (_isHighlightCleared) return;
                    _isHighlightCleared = true;
                }
                else
                {
                    _isHighlightCleared = false;
                }
            }
            else
            {
                if (_isHighlightCleared) return;
                _isHighlightCleared = true;
                bones = Enumerable.Empty<Transform>();
            }

            if(_smrFac == null || _smrBod == null) LoadHighlightBody(MakerAPI.GetCharacterControl());
            HighlightSingleRendBones(bones, _smrBod);
            HighlightSingleRendBones(bones, _smrFac);

            // for the shader that has the waves in it. this sets the origin point of the waves. ignore for the flat colour shader
            // mat.SetVector("_Pos", smr.transform.InverseTransformPoint(bones.First().position));
        }

        //https://stackoverflow.com/questions/34460587/unity-changing-only-certain-part-of-3d-models-color
        private static void HighlightSingleRendBones(IEnumerable<Transform> bones, SkinnedMeshRenderer targetRend)
        {
            var boneIndexes = new List<int>(25);
            foreach (var bone in bones)
            {
                var smrBones = targetRend.bones;
                for (var i = 0; i < smrBones.Length; ++i)
                {
                    if (smrBones[i] == bone)
                    {
                        boneIndexes.Add(i);
                        break;
                    }
                }
            }

            var mesh = targetRend.sharedMesh;
            var weights = mesh.boneWeights;
            var colors = new Color32[weights.Length];

            for (var i = 0; i < colors.Length; ++i)
            {
                float sum = 0;

                for (var j = 0; j < boneIndexes.Count; j++)
                {
                    var idx = boneIndexes[j];
                    var boneWeight = weights[i];
                    if (boneWeight.boneIndex0 == idx && boneWeight.weight0 > 0)
                        sum += boneWeight.weight0;
                    if (boneWeight.boneIndex1 == idx && boneWeight.weight1 > 0)
                        sum += boneWeight.weight1;
                    if (boneWeight.boneIndex2 == idx && boneWeight.weight2 > 0)
                        sum += boneWeight.weight2;
                    if (boneWeight.boneIndex3 == idx && boneWeight.weight3 > 0)
                        sum += boneWeight.weight3;
                }

                colors[i] = Color32.Lerp(Color.black, _highlightColor.Value, sum);
            }

            mesh.colors32 = colors;
        }
    }
}
