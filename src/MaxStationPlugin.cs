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
    [BepInPlugin(Guid, Name, Version)]
    public class MaxStationPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.maxstation.dsp";
        public const string Name = "MaxStation";
        public const string Version = "1.0.2";

        internal static ConfigEntry<KeyboardShortcut> MaxAllKey;
        internal static ManualLogSource Log;

        private static readonly FieldInfo StorageUIsField =
            AccessTools.Field(typeof(UIStationWindow), "storageUIs");

        private static readonly FieldInfo ControlPanelStorageUIsField =
            AccessTools.Field(typeof(UIControlPanelStationInspector), "storageUIs");

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

        internal static UIControlPanelStationInspector GetControlPanelStationInspector()
        {
            if (UIRoot.instance == null || UIRoot.instance.uiGame == null)
            {
                return null;
            }

            UIControlPanelWindow panel = UIRoot.instance.uiGame.controlPanelWindow;
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
