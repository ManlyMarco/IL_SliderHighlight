using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace SliderHighlight
{
    [BepInPlugin(GUID, "SliderHighlight", Version)]
    [BepInDependency(KKABMX_Core.GUID, KKABMX_Core.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class SliderHighlightPlugin : BaseUnityPlugin
    {
        public const string GUID = "SliderHighlight";
        public const string Version = "2.0";

        private static Material _mat, _matSolid;
        private static SkinnedMeshRenderer _smrBod;
        private static SkinnedMeshRenderer _smrFac;
        private static Harmony _hi;
        private static Dictionary<Slider, Func<IEnumerable<Transform>>> _boneSliderLookup;

        private static bool _isHighlightCleared;
        private static readonly Color _highlightColor = Color.green;

        private void Start()
        {
            MakerAPI.MakerBaseLoaded += (s, e) => StartCoroutine(LoadPlugin());
            MakerAPI.MakerExiting += (s, e) => Dispose();

            MakerAPI.ReloadCustomInterface += (s, e) => StartCoroutine(
                CoroutineUtils.CreateCoroutine(
                    CoroutineUtils.WaitForEndOfFrame,
                    () => LoadHighlightBody(MakerAPI.GetCharacterControl())));
        }

        private IEnumerator LoadPlugin()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _boneSliderLookup = new Dictionary<Slider, Func<IEnumerable<Transform>>>();
                
                LoadShaders();

                LoadHighlightBody(MakerAPI.GetCharacterControl());

                _hi = Harmony.CreateAndPatchAll(typeof(Hooks));

                InitializeBodySliders();
                InitializeFaceSliders();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
            sw.Stop();

            // Need to wait for ABMX sliders to get instantiated
            yield return new WaitUntil(() => MakerAPI.InsideAndLoaded);

            sw.Start();
            InitializeAbmxSliders();
            Logger.LogDebug($"Initialized in {sw.ElapsedMilliseconds}ms");
        }

        private static void Dispose()
        {
            if (_smrBod) Destroy(_smrBod.gameObject);
            if (_smrFac) Destroy(_smrFac.gameObject);
            _hi?.UnpatchAll(_hi.Id);
            _boneSliderLookup = null;
            _isHighlightCleared = false;
        }

        private static void LoadShaders()
        {
            if (_mat != null) return;

            AssetBundle ab = null;
            try
            {
                var res = ResourceUtils.GetEmbeddedResource("bonelyfans.unity3d") ?? throw new ArgumentNullException("GetEmbeddedResource");
                ab = AssetBundle.LoadFromMemory(res) ?? throw new ArgumentNullException("LoadFromMemory");
                var assetName = ab.GetAllAssetNames().First(x => x.Contains("bonelyfans"));
                var sha = ab.LoadAsset<Shader>(assetName) ?? throw new ArgumentNullException("LoadAsset");
                ab.Unload(false);

                _mat = new Material(sha);
                _mat.SetInt("_UseMaterialColor", 0);

                _matSolid = new Material(sha);
                _matSolid.SetInt("_UseMaterialColor", 1);
                _matSolid.SetColor("_Color", _highlightColor);
            }
            catch (Exception)
            {
                if (ab != null) ab.Unload(true);
                throw;
            }
        }
    }
}