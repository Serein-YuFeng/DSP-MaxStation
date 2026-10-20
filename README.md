# MaxStation

[GitHub 仓库](https://github.com/Serein-YuFeng/DSP-MaxStation)

戴森球计划（Dyson Sphere Program）小 mod：一键把物流运输站里**所有已配置物品**的“货物上限”拉满，不用再一个个拖滑条。

## 功能

打开行星物流塔 / 星际物流塔的窗口后：

- **Ctrl + F7**：把所有已放置物品的槽位上限设为当前科技允许的最大值。
- **Ctrl + 鼠标左键点击任意槽位的上限滑条**：同上，鼠标操作版。
- **Alt + 鼠标左键点击某个槽位的上限滑条**：只把该槽位拉满。

上限值取自游戏自己给滑条设置的最大值（建筑基础仓储 + 科技升级），不会超过游戏允许的范围。

## 安装

需要 BepInEx 5.4.x（DSP 是 Unity Mono 游戏）。

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
- 只处理**物流运输站窗口**。总控面板（Control Panel）里的槽位暂不支持。
- 不新增物品/配方/建筑，不需要 DSPModSave；`StationStore.max` 是原版存档字段，改完会随原版存档保存。
- 与 `StationAssistant`（双击改数值）功能不冲突：它改的是双击输入，本 mod 改的是 Ctrl/Alt + 点击和快捷键。
- 如果装着 `SpaciousStations` 之类扩展槽位的 mod，本 mod 直接遍历 `StationComponent.storage`，所以超出原版 UI 数组的槽位也会被处理。

## 实现要点

- Harmony patch `Slider.OnPointerDown`，判断点中的是不是物流塔槽位的 `maxSlider`，再处理 Ctrl/Alt；这样点在滑条手柄上也能触发。
- 通过 `AccessTools.Field` 拿 `UIStationWindow.storageUIs`（私有字段）。
- 调用 `PlanetTransport.SetStationStorage(stationId, storageIdx, itemId, max, localLogic, remoteLogic, player)` 写入上限，游戏内部会 clamp 到科技上限。
- 用 `Slider.SetValueWithoutNotify` 刷新 UI，避免递归触发 `onValueChanged`。


## License

[MIT](LICENSE)
