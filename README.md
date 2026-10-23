# MaxStation

[GitHub 仓库](https://github.com/Serein-YuFeng/DSP-MaxStation)

戴森球计划（Dyson Sphere Program）小 mod：一键把物流运输站里**所有已配置物品**的“货物上限”拉满，不用再一个个拖滑条。

## 功能

打开行星物流塔 / 星际物流塔的窗口，或打开总控面板并选中一个物流站后：

- **Ctrl + F7**：把所有已放置物品的槽位上限设为当前科技允许的最大值。
- **Ctrl + 鼠标左键点击任意槽位的上限滑条**：同上，鼠标操作版。
- **Alt + 鼠标左键点击某个槽位的上限滑条**：只把该槽位拉满。

总控面板右侧的槽位列表同样支持以上三种操作。

总控面板底部还提供三个范围按钮：

- **当前星球拉满**：拉满当前所在星球上的所有物流站。
- **当前星系拉满**：拉满当前所在星系内所有星球上的物流站。
- **全星区拉满**：拉满所有已创建工厂星球上的物流站。

范围操作会遍历目标范围内的物流站，忽略当前物品/状态/搜索筛选，并**跳过轨道采集器**。

单站操作的上限取自游戏自己给滑条设置的最大值（建筑基础仓储 + 科技升级）。范围操作会把请求值交给游戏和其他扩容 mod 自行 clamp，因此不会把 SpaciousStations 等 mod 提高后的上限（例如 200000）压回原版。

## 安装

需要 BepInEx 5.4.x。

### 从 Release 安装（推荐）

1. 下载 `MaxStation-<version>.zip`。
2. 解压到游戏根目录（`Dyson Sphere Program\`），或者解压到 Gale / r2modman 对应 profile 的根目录。
3. 启动游戏。

zip 内的结构是：

```
BepInEx/plugins/MaxStation/MaxStation.dll
```

### 手动安装

把 `MaxStation.dll` 放到：

```
Dyson Sphere Program/BepInEx/plugins/MaxStation/MaxStation.dll
```

如果你用 **Gale** 或 **r2modman**，请放到对应 profile 的 `BepInEx/plugins/MaxStation/`，而不是 Steam 游戏根目录。不同管理器的 profile 目录结构可能不同，以实际 profile 为准。

## 配置

首次启动游戏后生成：

```
BepInEx/config/com.maxstation.dsp.cfg
```

可以改 `MaxAllHotkey`，格式是 BepInEx 的 `KeyboardShortcut`，例如 `LeftControl + F7`、`LeftAlt + K`。

## 构建

需要：

- Windows
- .NET Framework 4.x（Windows 自带 `csc.exe` 即可）
- 游戏本体，用于引用 `Assembly-CSharp.dll`、`UnityEngine.*.dll`

设置游戏目录后编译：

```powershell
$env:DSP_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program"
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

也可以显式传参：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Dyson Sphere Program"
```

安装到游戏目录：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install
```

安装到 Gale / r2modman profile：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install -ProfileDir "<profile 目录>"
```

如果装了 .NET SDK / Visual Studio，也可以：

```powershell
dotnet build -p:DspDir="C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program"
```

## 兼容性与限制

- 适用游戏版本：**0.10.35**（Unity 2022.3.62f3c1，Assembly-CSharp 未混淆）。
- 同时支持**物流运输站窗口**、**总控面板（Control Panel）**右侧的槽位列表，以及总控面板底部的三个范围按钮。
- 范围按钮只覆盖已创建工厂星球上的物流站；轨道采集器不纳入范围。
- 不新增物品/配方/建筑，不需要 DSPModSave；`StationStore.max` 是原版存档字段，改完会随原版存档保存。
- 与 `StationAssistant`（双击改数值）功能不冲突：它改的是双击输入，本 mod 改的是 Ctrl/Alt + 点击和快捷键。
- 如果装着 `SpaciousStations` 之类扩展槽位的 mod，本 mod 直接遍历 `StationComponent.storage`，所以超出原版 UI 数组的槽位也会被处理；容量上限也由游戏和该 mod 共同钳制。

## 实现要点

- Harmony patch `Slider.OnPointerDown`，判断点中的是不是物流塔槽位的 `maxSlider`，再处理 Ctrl/Alt；这样点在滑条手柄上也能触发。
- 通过 `AccessTools.Field` 拿 `UIStationWindow.storageUIs` 和 `UIControlPanelStationInspector.storageUIs`（两者都是私有字段）。
- 总控面板的槽位 UI 是 `UIControlPanelStationStorage`，通过其 `masterInspector.transport` 调用同一套 `SetStationStorage`。
- 范围按钮遍历 `GameData.factories` 和 `PlanetFactory.transport.stationPool`，按 `planet.astroId` / `planet.star.astroId` 过滤；按钮克隆自总控面板的 `filterPanel.mainTypeButtons`。
- 调用 `PlanetTransport.SetStationStorage(stationId, storageIdx, itemId, max, localLogic, remoteLogic, player)` 写入上限，游戏内部会 clamp 到真实上限；范围操作传入足够大的请求值，让 SpaciousStations 等 mod 的 clamp 生效。
- 用 `Slider.SetValueWithoutNotify` 刷新 UI，避免递归触发 `onValueChanged`。


## License

[MIT](LICENSE)
