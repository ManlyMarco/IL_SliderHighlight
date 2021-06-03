using System.Collections.Generic;
using System.Linq;
using KKABMX.GUI;
using KKAPI.Maker;
using UnityEngine;
using UnityEngine.UI;

namespace SliderHighlight
{
    public partial class SliderHighlightPlugin
    {
        private static void InitializeAbmxSliders()
        {
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
        }
    }
}
