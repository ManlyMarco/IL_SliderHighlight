using System;
using HarmonyLib;
using UnityEngine.EventSystems;
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
            private static void OnSliderUpdateSelectionState(Selectable __instance, BaseEventData eventData, int ___m_CurrentSelectionState)
            {
                if (_smrBod == null || !_enabled.Value) return;

                try
                {
                    //Console.WriteLine($"focus state={___m_CurrentSelectionState} name={__instance.FullPath()}\n{eventData}");
                    if (___m_CurrentSelectionState == 1 || ___m_CurrentSelectionState == 2)
                    {
                        if (__instance is Slider sld && _boneSliderLookup.TryGetValue(sld, out var bones))
                            HighlightBones(bones());
                        else
                            HighlightBones();

                        if (__instance is Toggle tgl && IsAccessoryButton(tgl, eventData))
                            SetAccessoryHighlight(GettAccId(tgl));
                        else
                            ClearAccessoryHighlight();
                    }
                    else
                    {
                        HighlightBones();
                        ClearAccessoryHighlight();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            private static bool IsAccessoryButton(Toggle tgl, BaseEventData eventData)
            {
                if (!tgl.transform.name.StartsWith("tglSlot")) return false;
                if (eventData is PointerEventData pointerEventData)
                {
                    var sourceObj = pointerEventData.pointerCurrentRaycast.gameObject;
                    return sourceObj == null || sourceObj.name == "imgOff";
                }
                return true;
            }

            private static int GettAccId(Toggle tgl)
            {
                return int.Parse(tgl.transform.name.Substring(7)) - 1;
            }

            /// <summary>
            /// When dragging the slider turn off the highlight so user can see
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Slider), "UpdateVisuals")]
            private static void OnUpdateVisuals(Slider __instance)
            {
                if (_smrBod == null || !_enabled.Value) return;

                try
                {
                    HighlightBones();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}