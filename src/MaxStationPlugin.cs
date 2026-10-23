using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaxStation
{
    internal enum MaxScope
    {
        Planet = 0,
        System = 1,
        Galaxy = 2
    }

    [BepInPlugin(Guid, Name, Version)]
    public class MaxStationPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.maxstation.dsp";
        public const string Name = "MaxStation";
        public const string Version = "1.0.3";

        internal static ConfigEntry<KeyboardShortcut> MaxAllKey;
        internal static ManualLogSource Log;

        private static readonly FieldInfo StorageUIsField =
            AccessTools.Field(typeof(UIStationWindow), "storageUIs");

        private static readonly FieldInfo ControlPanelStorageUIsField =
            AccessTools.Field(typeof(UIControlPanelStationInspector), "storageUIs");

        private static readonly FieldInfo UiButtonOnClickField =
            typeof(UIButton).GetField(
                "onClick",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private const int RequestedMax = 1000000000;

        private static readonly string[] ScopeButtonLabels =
        {
            "当前星球拉满",
            "当前星系拉满",
            "全星区拉满"
        };

        private static GameObject ScopeButtonRoot;

        internal static bool Applying;

        private void Awake()
        {
            Log = Logger;
            MaxAllKey = Config.Bind(
                "General",
                "MaxAllHotkey",
                new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl),
                "Open a logistics station and press this to set every occupied slot's limit to max.");

            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(SliderPointerPatch));

            Logger.LogInfo(Name + " " + Version + " loaded. Max-all hotkey: " + MaxAllKey.Value);
        }

        private void Update()
        {
            UIControlPanelWindow panel = GetControlPanelWindow();
            if (panel != null && panel.active)
            {
                EnsureScopeButtons(panel);
            }

            if (!MaxAllKey.Value.IsDown())
            {
                return;
            }

            UIControlPanelStationInspector inspector = GetControlPanelStationInspector();
            if (inspector != null)
            {
                MaxAll(inspector);
                return;
            }

            UIStationWindow window = GetStationWindow();
            if (window != null && window.active)
            {
                MaxAll(window);
            }
        }

        internal static UIStationWindow GetStationWindow()
        {
            if (UIRoot.instance == null || UIRoot.instance.uiGame == null)
            {
                return null;
            }

            return UIRoot.instance.uiGame.stationWindow;
        }

        internal static UIControlPanelWindow GetControlPanelWindow()
        {
            if (UIRoot.instance == null || UIRoot.instance.uiGame == null)
            {
                return null;
            }

            return UIRoot.instance.uiGame.controlPanelWindow;
        }

        internal static UIControlPanelStationInspector GetControlPanelStationInspector()
        {
            UIControlPanelWindow panel = GetControlPanelWindow();
            if (panel == null || !panel.active)
            {
                return null;
            }

            UIControlPanelStationInspector inspector = panel.stationInspector;
            if (inspector == null || !inspector.active || inspector.station == null)
            {
                return null;
            }

            return inspector;
        }

        internal static void MaxAll(UIStationWindow window)
        {
            if (Applying || window == null || window.transport == null || StorageUIsField == null)
            {
                return;
            }

            UIStationStorage[] uis = StorageUIsField.GetValue(window) as UIStationStorage[];
            if (uis == null || uis.Length == 0)
            {
                return;
            }

            StationComponent station = null;
            int cap = 0;
            for (int i = 0; i < uis.Length; i++)
            {
                if (IsValid(uis[i]))
                {
                    station = uis[i].station;
                    cap = GetCap(uis[i]);
                    break;
                }
            }

            if (station == null || cap <= 0)
            {
                return;
            }

            Applying = true;
            try
            {
                for (int index = 0; index < station.storage.Length; index++)
                {
                    StationStore store = station.storage[index];
                    if (store.itemId <= 0 || store.max >= cap)
                    {
                        continue;
                    }

                    window.transport.SetStationStorage(
                        station.id,
                        index,
                        store.itemId,
                        cap,
                        store.localLogic,
                        store.remoteLogic,
                        GameMain.mainPlayer);
                }

                for (int i = 0; i < uis.Length; i++)
                {
                    UIStationStorage ui = uis[i];
                    if (!IsValid(ui) || ui.station != station)
                    {
                        continue;
                    }

                    if (ui.maxValueText != null)
                    {
                        ui.maxValueText.text = cap.ToString();
                    }

                    if (ui.maxSlider != null)
                    {
                        ui.maxSlider.SetValueWithoutNotify(cap / 100f);
                    }
                }
            }
            finally
            {
                Applying = false;
            }
        }

        internal static void MaxAll(UIControlPanelStationInspector inspector)
        {
            if (Applying || inspector == null || inspector.transport == null || ControlPanelStorageUIsField == null)
            {
                return;
            }

            UIControlPanelStationStorage[] uis =
                ControlPanelStorageUIsField.GetValue(inspector) as UIControlPanelStationStorage[];
            if (uis == null || uis.Length == 0)
            {
                return;
            }

            StationComponent station = null;
            int cap = 0;
            for (int i = 0; i < uis.Length; i++)
            {
                if (IsValid(uis[i]))
                {
                    station = uis[i].station;
                    cap = GetCap(uis[i]);
                    break;
                }
            }

            if (station == null || cap <= 0)
            {
                return;
            }

            Applying = true;
            try
            {
                for (int index = 0; index < station.storage.Length; index++)
                {
                    StationStore store = station.storage[index];
                    if (store.itemId <= 0 || store.max >= cap)
                    {
                        continue;
                    }

                    inspector.transport.SetStationStorage(
                        station.id,
                        index,
                        store.itemId,
                        cap,
                        store.localLogic,
                        store.remoteLogic,
                        GameMain.mainPlayer);
                }

                for (int i = 0; i < uis.Length; i++)
                {
                    UIControlPanelStationStorage ui = uis[i];
                    if (!IsValid(ui) || ui.station != station)
                    {
                        continue;
                    }

                    SetUiValue(ui.maxValueText, ui.maxSlider, cap);
                }
            }
            finally
            {
                Applying = false;
            }
        }

        internal static void MaxOne(UIStationStorage ui)
        {
            if (Applying || !IsValid(ui) || ui.stationWindow == null || ui.stationWindow.transport == null)
            {
                return;
            }

            StationComponent station = ui.station;
            StationStore store = station.storage[ui.index];
            if (store.itemId <= 0)
            {
                return;
            }

            int cap = GetCap(ui);
            if (cap <= 0 || store.max >= cap)
            {
                return;
            }

            Applying = true;
            try
            {
                ui.stationWindow.transport.SetStationStorage(
                    station.id,
                    ui.index,
                    store.itemId,
                    cap,
                    store.localLogic,
                    store.remoteLogic,
                    GameMain.mainPlayer);

                if (ui.maxValueText != null)
                {
                    ui.maxValueText.text = cap.ToString();
                }

                if (ui.maxSlider != null)
                {
                    ui.maxSlider.SetValueWithoutNotify(cap / 100f);
                }
            }
            finally
            {
                Applying = false;
            }
        }

        internal static void MaxOne(UIControlPanelStationStorage ui)
        {
            if (Applying || !IsValid(ui) || ui.masterInspector == null || ui.masterInspector.transport == null)
            {
                return;
            }

            StationComponent station = ui.station;
            StationStore store = station.storage[ui.index];
            if (store.itemId <= 0)
            {
                return;
            }

            int cap = GetCap(ui);
            if (cap <= 0 || store.max >= cap)
            {
                return;
            }

            Applying = true;
            try
            {
                ui.masterInspector.transport.SetStationStorage(
                    station.id,
                    ui.index,
                    store.itemId,
                    cap,
                    store.localLogic,
                    store.remoteLogic,
                    GameMain.mainPlayer);

                SetUiValue(ui.maxValueText, ui.maxSlider, cap);
            }
            finally
            {
                Applying = false;
            }
        }

        internal static void EnsureScopeButtons(UIControlPanelWindow window)
        {
            if (ScopeButtonRoot != null || window == null || window.filterPanel == null)
            {
                return;
            }

            UIButton template = null;
            UIButton[] templates = window.filterPanel.mainTypeButtons;
            if (templates != null)
            {
                for (int i = 0; i < templates.Length; i++)
                {
                    if (templates[i] != null)
                    {
                        template = templates[i];
                        break;
                    }
                }
            }

            if (template == null)
            {
                return;
            }

            GameObject root = new GameObject("MaxStationScopeButtons", typeof(RectTransform));
            root.layer = window.gameObject.layer;
            root.transform.SetParent(window.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 52f);
            rootRect.sizeDelta = new Vector2(720f, 40f);
            root.transform.SetAsLastSibling();

            for (int i = 0; i < ScopeButtonLabels.Length; i++)
            {
                UIButton button = UnityEngine.Object.Instantiate(template, root.transform, false) as UIButton;
                if (button == null)
                {
                    continue;
                }

                button.name = "MaxStationScope" + i;
                button.transform.SetParent(root.transform, false);

                RectTransform rect = button.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(220f, 36f);
                    rect.anchoredPosition = new Vector2((i - 1) * 236f, 0f);
                }

                button.gameObject.SetActive(true);
                ApplyScopeButtonLabel(button, ScopeButtonLabels[i]);

                ClearScopeButtonHandler(button);
                button.data = i;
                button.onClick += OnScopeButtonClick;
            }

            ScopeButtonRoot = root;
            Log.LogInfo("Scope buttons created.");
        }

        private static void ApplyScopeButtonLabel(UIButton button, string value)
        {
            if (button == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = button.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == "Localizer")
                {
                    behaviour.enabled = false;
                }
            }

            Text[] labels = button.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                Text label = labels[i];
                if (label == null)
                {
                    continue;
                }

                label.text = value;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            ScopeLabelKeeper keeper = button.gameObject.GetComponent<ScopeLabelKeeper>();
            if (keeper == null)
            {
                keeper = button.gameObject.AddComponent<ScopeLabelKeeper>();
            }

            keeper.targets = labels;
            keeper.value = value;
        }

        private static void ClearScopeButtonHandler(UIButton button)
        {
            if (button == null)
            {
                return;
            }

            if (UiButtonOnClickField != null)
            {
                UiButtonOnClickField.SetValue(button, null);
            }

            if (button.button != null)
            {
                button.button.onClick.RemoveAllListeners();
            }
        }

        private static void OnScopeButtonClick(int scope)
        {
            MaxAllInScope((MaxScope)scope);
        }

        internal static void MaxAllInScope(MaxScope scope)
        {
            if (Applying)
            {
                return;
            }

            GameData data = GameMain.data;
            if (data == null)
            {
                return;
            }

            PlanetData localPlanet = data.localPlanet;
            StarData localStar = data.localStar;

            if (scope == MaxScope.Planet && localPlanet == null)
            {
                ShowTip("MaxStation: 当前不在星球上");
                return;
            }

            if (scope == MaxScope.System && localStar == null)
            {
                ShowTip("MaxStation: 当前不在星系中");
                return;
            }

            int stationCount = 0;
            int slotCount = 0;

            Applying = true;
            try
            {
                PlanetFactory[] factories = data.factories;
                if (factories == null)
                {
                    return;
                }

                for (int f = 0; f < factories.Length; f++)
                {
                    PlanetFactory factory = factories[f];
                    if (factory == null)
                    {
                        continue;
                    }

                    PlanetData planet = factory.planet;
                    if (planet == null)
                    {
                        continue;
                    }

                    if (scope == MaxScope.Planet && planet.astroId != localPlanet.astroId)
                    {
                        continue;
                    }

                    if (scope == MaxScope.System
                        && (planet.star == null || planet.star.astroId != localStar.astroId))
                    {
                        continue;
                    }

                    PlanetTransport transport = factory.transport;
                    if (transport == null || transport.stationPool == null)
                    {
                        continue;
                    }

                    StationComponent[] pool = transport.stationPool;
                    for (int i = 1; i < pool.Length; i++)
                    {
                        StationComponent station = pool[i];
                        if (station == null || station.id != i)
                        {
                            continue;
                        }

                        if (station.isCollector)
                        {
                            continue;
                        }

                        if (station.storage == null)
                        {
                            continue;
                        }

                        bool changed = false;
                        for (int s = 0; s < station.storage.Length; s++)
                        {
                            StationStore store = station.storage[s];
                            if (store.itemId <= 0)
                            {
                                continue;
                            }

                            transport.SetStationStorage(
                                station.id,
                                s,
                                store.itemId,
                                RequestedMax,
                                store.localLogic,
                                store.remoteLogic,
                                GameMain.mainPlayer);

                            slotCount++;
                            changed = true;
                        }

                        if (changed)
                        {
                            stationCount++;
                        }
                    }
                }
            }
            finally
            {
                Applying = false;
            }

            RefreshControlPanelScopeUI();

            if (stationCount > 0)
            {
                ShowTip("MaxStation: " + stationCount + " 个物流站 / " + slotCount + " 个槽位已拉满");
            }
            else
            {
                ShowTip("MaxStation: 范围内没有需要处理的物流站");
            }

            Log.LogInfo(
                "Scope " + scope + ": " + stationCount + " stations, " + slotCount + " slots");
        }

        private static void RefreshControlPanelScopeUI()
        {
            UIControlPanelStationInspector inspector = GetControlPanelStationInspector();
            if (inspector == null || ControlPanelStorageUIsField == null)
            {
                return;
            }

            UIControlPanelStationStorage[] uis =
                ControlPanelStorageUIsField.GetValue(inspector) as UIControlPanelStationStorage[];
            if (uis == null)
            {
                return;
            }

            for (int i = 0; i < uis.Length; i++)
            {
                UIControlPanelStationStorage ui = uis[i];
                if (!IsValid(ui))
                {
                    continue;
                }

                int cap = GetCap(ui);
                if (cap > 0)
                {
                    SetUiValue(ui.maxValueText, ui.maxSlider, cap);
                }
            }
        }

        private static void ShowTip(string message)
        {
            UIRealtimeTip.Popup(message, false, 0);
        }

        internal static bool IsValid(UIStationStorage ui)
        {
            return ui != null
                && ui.station != null
                && ui.station.storage != null
                && ui.index >= 0
                && ui.index < ui.station.storage.Length;
        }

        internal static UIStationStorage FindStorageBySlider(UIStationWindow window, Slider slider)
        {
            if (StorageUIsField == null || window == null || slider == null)
            {
                return null;
            }

            UIStationStorage[] uis = StorageUIsField.GetValue(window) as UIStationStorage[];
            if (uis == null)
            {
                return null;
            }

            for (int i = 0; i < uis.Length; i++)
            {
                if (uis[i] != null && uis[i].maxSlider == slider)
                {
                    return uis[i];
                }
            }

            return null;
        }

        internal static int GetCap(UIStationStorage ui)
        {
            if (ui.maxSlider == null)
            {
                return 0;
            }

            return (int)(ui.maxSlider.maxValue * 100f + 0.5f);
        }

        internal static bool IsValid(UIControlPanelStationStorage ui)
        {
            return ui != null
                && ui.station != null
                && ui.station.storage != null
                && ui.index >= 0
                && ui.index < ui.station.storage.Length;
        }

        internal static UIControlPanelStationStorage FindStorageBySlider(
            UIControlPanelStationInspector inspector,
            Slider slider)
        {
            if (ControlPanelStorageUIsField == null || inspector == null || slider == null)
            {
                return null;
            }

            UIControlPanelStationStorage[] uis =
                ControlPanelStorageUIsField.GetValue(inspector) as UIControlPanelStationStorage[];
            if (uis == null)
            {
                return null;
            }

            for (int i = 0; i < uis.Length; i++)
            {
                if (uis[i] != null && uis[i].maxSlider == slider)
                {
                    return uis[i];
                }
            }

            return null;
        }

        internal static int GetCap(UIControlPanelStationStorage ui)
        {
            if (ui == null || ui.maxSlider == null)
            {
                return 0;
            }

            return (int)(ui.maxSlider.maxValue * 100f + 0.5f);
        }

        internal static void SetUiValue(Text text, Slider slider, int cap)
        {
            if (text != null)
            {
                text.text = cap.ToString();
            }

            if (slider != null)
            {
                slider.SetValueWithoutNotify(cap / 100f);
            }
        }
    }

    internal sealed class ScopeLabelKeeper : MonoBehaviour
    {
        internal Text[] targets;
        internal string value;

        private void LateUpdate()
        {
            if (targets == null || value == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Text target = targets[i];
                if (target != null && target.text != value)
                {
                    target.text = value;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Slider), "OnPointerDown")]
    internal static class SliderPointerPatch
    {
        private static bool Prefix(Slider __instance, PointerEventData eventData)
        {
            if (MaxStationPlugin.Applying || __instance == null || eventData == null)
            {
                return true;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return true;
            }

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (!ctrl && !alt)
            {
                return true;
            }

            UIStationWindow window = MaxStationPlugin.GetStationWindow();
            if (window != null && window.active)
            {
                UIStationStorage target = MaxStationPlugin.FindStorageBySlider(window, __instance);
                if (target != null)
                {
                    MaxStationPlugin.Log.LogInfo("Station slider click: " + (ctrl ? "max all" : "max one"));

                    if (ctrl)
                    {
                        MaxStationPlugin.MaxAll(window);
                    }
                    else
                    {
                        MaxStationPlugin.MaxOne(target);
                    }

                    return false;
                }
            }

            UIControlPanelStationInspector inspector = MaxStationPlugin.GetControlPanelStationInspector();
            if (inspector != null)
            {
                UIControlPanelStationStorage target = MaxStationPlugin.FindStorageBySlider(inspector, __instance);
                if (target != null)
                {
                    MaxStationPlugin.Log.LogInfo("Control panel slider click: " + (ctrl ? "max all" : "max one"));

                    if (ctrl)
                    {
                        MaxStationPlugin.MaxAll(inspector);
                    }
                    else
                    {
                        MaxStationPlugin.MaxOne(target);
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
