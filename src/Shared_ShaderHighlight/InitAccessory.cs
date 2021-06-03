using System.Collections.Generic;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine;

namespace SliderHighlight
{
    public partial class SliderHighlightPlugin
    {
        private static readonly List<KeyValuePair<Renderer, Material[]>> _accMaterialsToRestore = new List<KeyValuePair<Renderer, Material[]>>(5);

        private static void SetAccessoryHighlight(int slotId)
        {
            ClearAccessoryHighlight();

            if (slotId < 0) return;
            var acc = MakerAPI.GetCharacterControl().GetAccessory(slotId);
            if (acc == null) return;
            foreach (var renderer in acc.GetComponentsInChildren<Renderer>())
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    var materials = renderer.sharedMaterials;

                    var mask = materials[materials.Length - 1].GetTexture("_AlphaMask");
                    _matSolid.SetTexture("_AlphaMask", mask);

                    var tex = materials[materials.Length - 1].GetTexture("_MainTex");
                    _matSolid.SetTexture("_MainTex", tex);

                    _accMaterialsToRestore.Add(new KeyValuePair<Renderer, Material[]>(renderer, materials));
                    renderer.sharedMaterials = materials.AddToArray(_matSolid);
                }
            }
        }

        private static void ClearAccessoryHighlight()
        {
            if (_accMaterialsToRestore.Count == 0) return;
            foreach (var renderer in _accMaterialsToRestore)
            {
                if (renderer.Key != null)
                    renderer.Key.sharedMaterials = renderer.Value;
            }
            _accMaterialsToRestore.Clear();
        }
    }
}
