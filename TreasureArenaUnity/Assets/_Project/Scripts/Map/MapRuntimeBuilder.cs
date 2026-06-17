using System;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Builds runtime map objects from validated map JSON.
    /// </summary>
    public sealed class MapRuntimeBuilder
    {
        public GameObject Build(MapJsonModels.MapJson map)
        {
            throw new InvalidOperationException("MapRuntimeBuilder requires a PrefabRegistry for runtime object instantiation.");
        }

        public GameObject Build(MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            GameObject root = new GameObject("MapRuntimeRoot");
            if (map == null)
            {
                return root;
            }

            BuildObjects(root.transform, map, prefabRegistry);
            BuildTeamBases(root.transform, map, prefabRegistry);
            BuildTreasurePoints(root.transform, map, prefabRegistry);
            BuildSupplyBoxes(root.transform, map, prefabRegistry);
            BuildBounds(root.transform, map, prefabRegistry);
            return root;
        }

        private static void BuildObjects(Transform root, MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (map.objects == null)
            {
                return;
            }

            Transform parent = CreateGroup(root, "Objects");
            for (int i = 0; i < map.objects.Count; i++)
            {
                MapJsonModels.MapObjectJson item = map.objects[i];
                if (item == null)
                {
                    continue;
                }

                GameObject go = CreateMapObject(item, prefabRegistry, parent);
                go.transform.position = ToVector3(item.position);
                go.transform.eulerAngles = ToVector3(item.rotation);
                go.transform.localScale = ToVector3(item.scale, Vector3.one);

                Collider collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = item.has_collider;
                }
            }
        }

        private static GameObject CreateMapObject(
            MapJsonModels.MapObjectJson item,
            PrefabRegistry prefabRegistry,
            Transform parent)
        {
            if (prefabRegistry == null)
            {
                throw new InvalidOperationException("Cannot instantiate map object without MapPrefabRegistry: " + item.object_id);
            }

            if (!prefabRegistry.TryGetPrefab(item.prefab_id, out GameObject prefab) || prefab == null)
            {
                throw new InvalidOperationException("Map prefab id is not registered: " + item.prefab_id);
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = string.IsNullOrEmpty(item.object_id) ? prefab.name : item.object_id;

            MapRuntimeObject runtimeObject = go.GetComponent<MapRuntimeObject>();
            if (runtimeObject == null)
            {
                runtimeObject = go.AddComponent<MapRuntimeObject>();
            }

            runtimeObject.object_id = item.object_id;
            runtimeObject.prefab_id = item.prefab_id;
            runtimeObject.is_shootable = true;
            runtimeObject.blocks_bullet = true;
            runtimeObject.decal_enabled = true;
            return go;
        }

        private static void BuildTeamBases(Transform root, MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (map.team_bases == null)
            {
                return;
            }

            Transform parent = CreateGroup(root, "TeamBases");
            for (int i = 0; i < map.team_bases.Count; i++)
            {
                MapJsonModels.TeamBaseJson item = map.team_bases[i];
                if (item == null)
                {
                    continue;
                }

                string prefabId = item.team == "Red" ? "TeamBase_Red" : "TeamBase_Blue";
                GameObject go = TryCreateFromPrefab(prefabRegistry, prefabId, item.base_id, parent)
                    ?? CreatePrimitive(item.base_id, PrimitiveType.Cylinder, parent);
                go.transform.position = ToVector3(item.position) + Vector3.up * 0.03f;
                go.transform.localScale = new Vector3(item.radius * 2f, 0.06f, item.radius * 2f);

                // Only apply runtime color/material to primitives (prefabs already have their own materials).
                if (go.GetComponent<MapRuntimeObject>() == null)
                {
                    SetColor(go, item.team == "Red" ? new Color(0.9f, 0.15f, 0.12f, 0.85f) : new Color(0.1f, 0.3f, 0.95f, 0.85f));
                }

                Collider collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }
        }

        private static void BuildTreasurePoints(Transform root, MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (map.treasure_spawn_points == null)
            {
                return;
            }

            Transform parent = CreateGroup(root, "TreasureSpawnPoints");
            for (int i = 0; i < map.treasure_spawn_points.Count; i++)
            {
                MapJsonModels.TreasureSpawnPointJson item = map.treasure_spawn_points[i];
                if (item == null)
                {
                    continue;
                }

                string prefabId = item.treasure_type switch
                {
                    "Rare" => "Treasure_Rare",
                    "Final" => "Treasure_Final",
                    _ => "Treasure_Normal"
                };
                GameObject go = TryCreateFromPrefab(prefabRegistry, prefabId, item.point_id, parent)
                    ?? CreatePrimitive(item.point_id, PrimitiveType.Sphere, parent);
                go.transform.position = ToVector3(item.position);
                go.transform.localScale = Vector3.one * item.radius * 2f;

                if (go.GetComponent<MapRuntimeObject>() == null)
                {
                    SetColor(go, TreasureColor(item.treasure_type));
                }
            }
        }

        private static void BuildSupplyBoxes(Transform root, MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (map.supply_boxes == null)
            {
                return;
            }

            Transform parent = CreateGroup(root, "SupplyBoxes");
            for (int i = 0; i < map.supply_boxes.Count; i++)
            {
                MapJsonModels.SupplyBoxJson item = map.supply_boxes[i];
                if (item == null)
                {
                    continue;
                }

                GameObject go = TryCreateFromPrefab(prefabRegistry, "SupplyBox", item.box_id, parent)
                    ?? CreatePrimitive(item.box_id, PrimitiveType.Cube, parent);
                go.transform.position = ToVector3(item.position);
                go.transform.localScale = Vector3.one * 0.7f;

                if (go.GetComponent<MapRuntimeObject>() == null)
                {
                    SetColor(go, new Color(0.15f, 0.8f, 0.25f, 1f));
                }
            }
        }

        private static void BuildBounds(Transform root, MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (map.bounds == null)
            {
                return;
            }

            GameObject go = TryCreateFromPrefab(prefabRegistry, "Bounds", "bounds", root)
                ?? new GameObject("bounds");
            go.transform.SetParent(root, false);
            go.transform.position = ToVector3(map.bounds.center);
            go.transform.localScale = ToVector3(map.bounds.size, Vector3.one);
        }

        private static Transform CreateGroup(Transform root, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(root, false);
            return group.transform;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = string.IsNullOrEmpty(name) ? type.ToString() : name;
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>
        /// Tries to instantiate a gameplay marker from PrefabRegistry.
        /// Returns null if the prefab_id is not registered, so callers can fall back to CreatePrimitive.
        /// </summary>
        private static GameObject TryCreateFromPrefab(PrefabRegistry registry, string prefabId, string fallbackName, Transform parent)
        {
            if (registry != null && registry.TryGetPrefab(prefabId, out GameObject prefab) && prefab != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
                go.name = string.IsNullOrEmpty(fallbackName) ? prefab.name : fallbackName;

                MapRuntimeObject runtimeObject = go.GetComponent<MapRuntimeObject>();
                if (runtimeObject == null)
                {
                    runtimeObject = go.AddComponent<MapRuntimeObject>();
                }

                runtimeObject.object_id = fallbackName;
                runtimeObject.prefab_id = prefabId;
                runtimeObject.is_shootable = true;
                runtimeObject.blocks_bullet = true;
                runtimeObject.decal_enabled = true;
                return go;
            }

            return null;
        }

        private static Color TreasureColor(string treasureType)
        {
            if (treasureType == "Rare")
            {
                return new Color(0.55f, 0.2f, 0.95f, 1f);
            }

            if (treasureType == "Final")
            {
                return new Color(0.05f, 0.85f, 0.65f, 1f);
            }

            return new Color(1f, 0.72f, 0.08f, 1f);
        }

        private static void SetColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static Vector3 ToVector3(MapJsonModels.Vector3Json value)
        {
            return ToVector3(value, Vector3.zero);
        }

        private static Vector3 ToVector3(MapJsonModels.RotationJson value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(MapJsonModels.ScaleJson value, Vector3 defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(MapJsonModels.Vector3Json value, Vector3 defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            return new Vector3(value.x, value.y, value.z);
        }
    }
}
