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
#if KK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Selectable), nameof(Selectable.UpdateSelectionState))]
            private static void OnSliderUpdateSelectionState(Selectable __instance, BaseEventData eventData)
            {
                HandleSelectionUpdate(__instance, eventData, __instance.m_CurrentSelectionState);
            }
#elif KKS
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Selectable), nameof(Selectable.EvaluateAndTransitionToSelectionState))]
            private static void OnSliderUpdateSelectionState(Selectable __instance)
            {
                // bug no eventdata available, need some other way to tell if cursor is hovering over an acc slot buttom
                HandleSelectionUpdate(__instance, null, __instance.currentSelectionState);
            }
#endif

            private static void HandleSelectionUpdate(Selectable __instance, BaseEventData eventData, Selectable.SelectionState currentSelectionState)
            {
                if (_smrBod == null || (!_enabled.Value && !_enabledAccessory.Value)) return;

                try
                {
                    //Console.WriteLine($"focus state={___m_CurrentSelectionState} name={__instance.FullPath()}\n{eventData}");
                    if (currentSelectionState == Selectable.SelectionState.Highlighted ||
                        currentSelectionState == Selectable.SelectionState.Pressed)
                    {
                        if (_enabled.Value)
                        {
                            if (__instance is Slider sld && _boneSliderLookup.TryGetValue(sld, out var bones))
                                HighlightBones(bones());
                            else
                                HighlightBones();
                        }

                        if (_enabledAccessory.Value)
                        {
                            if (__instance is Toggle tgl && IsAccessoryButton(tgl, eventData))
                                SetAccessoryHighlight(GettAccId(tgl));
                            else
                                ClearAccessoryHighlight();
                        }
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
            [HarmonyPatch(typeof(Slider), nameof(Slider.UpdateVisuals))]
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