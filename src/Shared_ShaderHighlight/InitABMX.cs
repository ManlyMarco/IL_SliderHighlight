using System;
using System.Collections.Generic;
using System.Linq;
using KKABMX.Core;
using KKABMX.GUI;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace SliderHighlight
{
    public partial class SliderHighlightPlugin
    {
        public static bool IsAdvancedWindowCurrentlyHovering => KKABMX_AdvancedGUI.BoneListMouseHoversOver.Key != null;

        private static void InitializeAbmxSliders()
        {
            //todo use new searcher, give location in abmx in case of future acc sliders
            var bdy = new FindAssist();
            bdy.Initialize(MakerAPI.GetCharacterControl().objAnim.transform);
            var boneDict = bdy.dictObjName.Values.Distinct().ToDictionary(x => x.name, x => x.GetComponentsInChildren<Transform>(true));

            foreach (var spawnedSlider in KKABMX_GUI.SpawnedSliders)
            {
                var sld = spawnedSlider;

                IEnumerable<Transform> GetBonesFunc()
                {
                    return sld.GetAffectedBones().SelectMany(x => boneDict[x]);
                }

                foreach (var makerSlider in spawnedSlider.Sliders)
                {
                    var slider = makerSlider.ControlObject.GetComponentInChildren<Slider>(true);
                    _boneSliderLookup[slider] = GetBonesFunc;
                }
            }

            KKABMX_AdvancedGUI.OnBoneListMouseHover += OnAbmxHover;

            // Cleanup
            MakerAPI.MakerExiting += OnMakerExit;
            void OnMakerExit(object sender, EventArgs args)
            {
                KKABMX_AdvancedGUI.OnBoneListMouseHover -= OnAbmxHover;
                MakerAPI.MakerExiting -= OnMakerExit;
            }
        }

        private static void OnAbmxHover(Transform transform, BoneLocation location)
        {
            _instance.StopAllCoroutines();
            _instance.StartCoroutine(CoroutineUtils.CreateCoroutine(new WaitForFixedUpdate(), () =>
            {
                var hoveredBones = transform != null ? transform.GetComponentsInChildren<Transform>() : null;
                HighlightBones(hoveredBones);
            }));
        }
    }
}
