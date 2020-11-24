﻿using System.Collections.Generic;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine;

namespace SliderHighlight
{
    public partial class SliderHighlightPlugin
    {
        private static readonly List<KeyValuePair<Renderer, Material[]>> _toRemove = new List<KeyValuePair<Renderer, Material[]>>(5);

        private static void SetAccessoryHighlight(int slotId)
        {
            ClearAccessoryHighlight();

            if(slotId < 0) return;
            var acc = MakerAPI.GetCharacterControl().GetAccessory(slotId);
            if (acc == null) return;
            foreach (var renderer in acc.GetComponentsInChildren<Renderer>())
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    _toRemove.Add(new KeyValuePair<Renderer, Material[]>(renderer, renderer.sharedMaterials));
                    renderer.sharedMaterials = renderer.sharedMaterials.AddToArray(_matSolid);
                }
            }
        }

        private static void ClearAccessoryHighlight()
        {
            if (_toRemove.Count == 0) return;
            foreach (var renderer in _toRemove)
            {
                if (renderer.Key != null)
                    renderer.Key.sharedMaterials = renderer.Value;
            }
            _toRemove.Clear();
        }
    }
}