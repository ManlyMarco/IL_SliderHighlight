using HarmonyLib;
using UnityEngine.UI;

namespace SliderHighlight
{
    public partial class SliderHighlightPlugin
    {
        private static class Hooks
        {
            /// <summary>
            /// Show highlight when hovering over the slider or selecting it by navigating the UI with keyboard/gamepad
            /// hide it when the slider loses focus / mouse cursor leaves
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Selectable), "UpdateSelectionState")]
            private static void OnSliderUpdateSelectionState(Selectable __instance, int ___m_CurrentSelectionState)
            {
                if (_smrBod == null) return;

                if (___m_CurrentSelectionState == 1 || ___m_CurrentSelectionState == 2)
                {
                    //Console.WriteLine($"focus {___m_CurrentSelectionState} state={__instance.name} ");
                    if (__instance is Slider sld && _boneSliderLookup.TryGetValue(sld, out var bones))
                        HighlightBones(bones());
                    else
                        HighlightBones();
                }
                else
                {
                    HighlightBones();
                }
            }

            /// <summary>
            /// When dragging the slider turn off the highlight so user can see
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Slider), "UpdateVisuals")]
            private static void OnUpdateVisuals(Slider __instance)
            {
                if (_smrBod == null) return;

                HighlightBones();
            }
        }
    }
}