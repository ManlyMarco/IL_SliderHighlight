using BepInEx;

namespace SliderHighlight
{
    [BepInIncompatibility("madevil.kk.AccGotHigh")]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
    public partial class SliderHighlightPlugin { }
}