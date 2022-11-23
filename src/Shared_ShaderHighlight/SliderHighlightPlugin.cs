using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Utilities;
using Screencap;
using UnityEngine;
using UnityEngine.UI;

namespace SliderHighlight
{
    [BepInPlugin(GUID, "SliderHighlight", Version)]
    [BepInDependency(KKABMX_Core.GUID, KKABMX_Core.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ScreenshotManager.GUID, ScreenshotManager.Version)]
    public partial class SliderHighlightPlugin : BaseUnityPlugin
    {
        public const string GUID = "SliderHighlight";
        public const string Version = "2.1.2";

        private static Material _mat, _matSolid;
        private static SkinnedMeshRenderer _smrBod;
        private static SkinnedMeshRenderer _smrFac;
        private static Harmony _hi;
        private static Dictionary<Slider, Func<IEnumerable<Transform>>> _boneSliderLookup;

        private static bool _isHighlightCleared;

        private static ConfigEntry<bool> _enabled;
        private static ConfigEntry<bool> _enabledAccessory;
        private static ConfigEntry<Color> _highlightColor;

        private static SliderHighlightPlugin _instance;

        private void Start()
        {
            _instance = this;

            _enabled = Config.Bind("Maker Highlights", "Slider Highlights", true, "Enable showing highlights when hovering over sliders in character maker.");
            _enabled.SettingChanged += (sender, args) =>
            {
                if (_matSolid == null) return;
                HighlightBones();
            };
            _enabledAccessory = Config.Bind("Maker Highlights", "Accessory Highlights", true, "Enable showing highlights when hovering over accessory slots in character maker.");
            _enabledAccessory.SettingChanged += (sender, args) =>
            {
                if (_matSolid == null) return;
                ClearAccessoryHighlight();
            };

            _highlightColor = Config.Bind("Maker Highlights", "Color", Color.green, "Color of the highlight. Avoid using transparent colors.");
            _highlightColor.SettingChanged += (sender, args) =>
            {
                if (_matSolid == null) return;
                HighlightBones();
                ClearAccessoryHighlight();
                _matSolid.SetColor("_Color", _highlightColor.Value);
            };

            MakerAPI.MakerBaseLoaded += (s, e) => StartCoroutine(LoadPlugin(e));
            MakerAPI.MakerExiting += (s, e) => Dispose();

            MakerAPI.ReloadCustomInterface += (s, e) => StartCoroutine(
                CoroutineUtils.CreateCoroutine(
                    CoroutineUtils.WaitForEndOfFrame,
                    () => LoadHighlightBody(MakerAPI.GetCharacterControl())));
        }

        private IEnumerator LoadPlugin(RegisterCustomControlsEvent e)
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

                // todo waste of space on sidebar?
                // var toggle = e.AddSidebarControl(new SidebarToggle("Highlight on hover", _enabled.Value, this));
                // toggle.ValueChanged.Subscribe(b => _enabled.Value = b);
                // _enabled.SettingChanged += (sender, args) =>
                // {
                //     if (!toggle.IsDisposed) toggle.Value = _enabled.Value;
                // };
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
            
            ScreenshotManager.OnPreCapture += OnScreenshotPreCapture;
            ScreenshotManager.OnPostCapture += OnScreenshotPostCapture;

            Logger.LogDebug($"Initialized in {sw.ElapsedMilliseconds}ms");
        }

        private static void Dispose()
        {
            if (_smrBod) Destroy(_smrBod.gameObject);
            if (_smrFac) Destroy(_smrFac.gameObject);
            _hi?.UnpatchSelf();
            _boneSliderLookup = null;
            _isHighlightCleared = false;
            _accMaterialsToRestore.Clear();
            
            ScreenshotManager.OnPreCapture -= OnScreenshotPreCapture;
            ScreenshotManager.OnPostCapture -= OnScreenshotPostCapture;
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
                _matSolid.SetColor("_Color", _highlightColor.Value);
            }
            catch (Exception)
            {
                if (ab != null) ab.Unload(true);
                throw;
            }
        }

        private static void OnScreenshotPreCapture()
        {
            if (MakerAPI.InsideAndLoaded && _smrBod != null)
            {
                ClearAccessoryHighlight();
                _smrBod.enabled = false;
                _smrFac.enabled = false;
            }
        }

        private static void OnScreenshotPostCapture()
        {
            if (MakerAPI.InsideAndLoaded && _smrBod != null)
            {
                _smrBod.enabled = true;
                _smrFac.enabled = true;
            }
        }
    }
}