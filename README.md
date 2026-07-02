# Monster Respawn Timer

[English](#english) | [中文](#中文)

## 中文

Monster Respawn Timer 是一个用于 R.E.P.O. 的 BepInEx HUD 模组。它会在游戏内显示当前关卡会出现哪些怪物、怪物距离复活还有多久，以及哪些事件正在影响复活时间。不改变怪物复活逻辑，也不会调整游戏难度。

![Monster Respawn Timer HUD](https://raw.githubusercontent.com/fangjx114514/REPO-MonsterRespawnTimer/main/assets/pic1.png)

### 显示内容

- 右下角显示当前关卡会出现的怪物。
- 橙色数字表示该怪物距离复活还有多久。
- 蓝色数字会短暂闪烁，表示刚刚怪物复活时间减少了多少秒（例如打碎物品、使用武器、物品发出的声音、激活或完成 extraction point 等）。

![Respawn timer reduction example](https://raw.githubusercontent.com/fangjx114514/REPO-MonsterRespawnTimer/main/assets/pic2.png)

### 安装

推荐使用 Thunderstore Mod Manager 或 r2modman 安装。

手动安装时，先安装 R.E.P.O. 的 BepInExPack，然后把 `MonsterRespawnTimer.dll` 放到：

```text
BepInEx/plugins/
```

如果你从 GitHub 下载，编译好的 DLL 在 `dist/MonsterRespawnTimer.dll`。

### 说明

这个模组只读取并显示游戏已经存在的怪物状态和复活计时，不会改变怪物生成、死亡、despawn 或复活逻辑。

## English

Monster Respawn Timer is a lightweight BepInEx HUD mod for R.E.P.O. It shows the monster lineup for the current level, tracks how long each monster has until it respawns, and highlights events that reduce respawn time. 

![Monster Respawn Timer HUD](https://raw.githubusercontent.com/fangjx114514/REPO-MonsterRespawnTimer/main/assets/pic1.png)

### What It Shows

- The monsters that can appear in the current level, shown in the bottom-right HUD.
- Orange numbers show how long that monster has until it respawns.
- Blue numbers briefly flash when a respawn timer has just been reduced, such as from broken objects, weapon use, valuable item noise, or extraction point events.

![Respawn timer reduction example](https://raw.githubusercontent.com/fangjx114514/REPO-MonsterRespawnTimer/main/assets/pic2.png)

### Installation

Thunderstore Mod Manager or r2modman is recommended.

For manual installation, install the R.E.P.O. BepInExPack first, then place `MonsterRespawnTimer.dll` in:

```text
BepInEx/plugins/
```

If you download from GitHub, the built DLL is available at `dist/MonsterRespawnTimer.dll`.

### Notes

This mod only reads and displays monster state and respawn timer information that already exists in the game. It does not change monster spawning, death, despawn, or respawn logic.
