# MapEditor Prefab 导入说明

## 唯一导入目录

所有可摆放的地图素材 prefab 必须放在：

```
Assets/_Project/Prefabs/MapEditor/Palette/Prefabs/
```

不放到 `MapObjects`、`GameplayMarkers` 等其他目录。那些目录存放编辑器内置标记物（如 TeamBase、Bounds），不接受用户素材。

## Prefab 要求

### 根节点挂 MapExportMarker

每个 prefab 根节点必须挂 `MapExportMarker` 组件，配置如下：

| 字段 | 值 |
|------|-----|
| `marker_type` | `MapObject` |
| `prefab_id` | prefab 文件名去掉 `.prefab`，稳定唯一（如 `book_1`、`antique_light`） |
| `has_collider` | `true` |

`prefab_id` 会被写入地图 JSON 的 `objects[].prefab_id` 字段，Pico 客户端通过 `MapPrefabRegistry.asset` 用这个 ID 查找 prefab 实例化。**prefab_id 一旦确定不要改，否则已导出的旧地图 JSON 无法加载。**

### 必须带 Collider

prefab 必须至少有一个 Collider（BoxCollider / MeshCollider 等），用于：
- Pico 端射线点击选中
- 编辑器表面放置检测
- Game 模式运行时碰撞

### 不写入 JSON 的内容

缩略图路径、模型路径、材质路径都不写入地图 JSON。地图文件只记录 `prefab_id` + transform。

## 缩略图

编辑器菜单 `Tools > TreasureArena > 地图编辑器 > 重新生成 Brush 缩略图` 会扫描 `Palette/Prefabs/` 下所有 prefab，按 `prefab_id` 生成缩略图 PNG 到：

```
Assets/_Project/Textures/MapEditor/BrushThumbnails/<prefab_id>.png
```

新增或替换 prefab 后需要重新执行此菜单。

## Prefab Registry

菜单 `Tools > TreasureArena > 地图编辑器 > 重建 Map Prefab Registry` 扫描 `Palette/Prefabs/` 生成：

```
Assets/_Project/ScriptableObjects/Map/MapPrefabRegistry.asset
```

此 asset 会被加入 PlayerSettings Preloaded Assets，确保 Pico 打包时包含。Registry 是 `prefab_id -> prefab` 查找表，运行时加载地图 JSON 时用。

## Pico 真机导出路径

导出菜单点击后，地图 JSON 写入两个位置：

| 优先级 | 路径 | 说明 |
|--------|------|------|
| 主路径 | `/storage/emulated/0/Android/data/com.DefaultCompany.TreasureArenaUnity/files/TreasureArenaMR/MapExports/<map_id>.json` | 必定写入，app private |
| 辅助路径 | `/storage/emulated/0/Download/TreasureArenaMR/MapExports/<map_id>.json` | 可能因系统权限失败，不影响主导出 |

导出后 UI 状态栏会显示实际写入的路径。Console 日志也会打印完整路径。

## 拷贝导出文件到 PC

Pico 连接 PC 后：
1. 主路径文件在 `Android/data/com.DefaultCompany.TreasureArenaUnity/files/TreasureArenaMR/MapExports/` 下
2. 辅助路径文件在 `Download/TreasureArenaMR/MapExports/` 下
3. 拷贝到 Unity 项目的 `Assets/_Project/StreamingAssets/Maps/` 即可被服务端加载
