using UnityEngine;
using TreasureArenaMR.Gameplay;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Builds primitive runtime map placeholders from validated map JSON.
    /// </summary>
    public sealed class MapRuntimeBuilder
    {
        public GameObject Build(MapJsonModels.MapJson map)
        {
            GameObject root = new GameObject("MapRuntimeRoot");
            if (map == null)
            {
                return root;
            }

            BuildObjects(root.transform, map);
            BuildTeamBases(root.transform, map);
            BuildTreasurePoints(root.transform, map);
            BuildSupplyBoxes(root.transform, map);
            BuildMapBoundary(root.transform, map);
            return root;
        }

        private static void BuildObjects(Transform root, MapJsonModels.MapJson map)
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

                PrimitiveType type = IsFloor(item.prefab_id) ? PrimitiveType.Plane : PrimitiveType.Cube;
                GameObject go = CreatePrimitive(item.object_id, type, parent);
                go.transform.position = ToVector3(item.position);
                go.transform.eulerAngles = ToVector3(item.rotation);
                go.transform.localScale = ToVector3(item.scale, Vector3.one);
                SetColor(go, ObjectColor(item.object_type));

                Collider collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = item.has_collider;
                }

                ConfigureRuntimeObject(go, item);
            }
        }

        private static void BuildTeamBases(Transform root, MapJsonModels.MapJson map)
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

                GameObject go = CreatePrimitive(item.base_id, PrimitiveType.Cylinder, parent);
                go.transform.position = ToVector3(item.position) + Vector3.up * 0.03f;
                go.transform.localScale = new Vector3(item.radius * 2f, 0.06f, item.radius * 2f);
                SetColor(go, item.team == "Red" ? new Color(0.9f, 0.15f, 0.12f, 0.85f) : new Color(0.1f, 0.3f, 0.95f, 0.85f));

                Collider collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }
        }

        private static void BuildTreasurePoints(Transform root, MapJsonModels.MapJson map)
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

                GameObject go = CreatePrimitive(item.point_id, PrimitiveType.Sphere, parent);
                go.transform.position = ToVector3(item.position);
                go.transform.localScale = Vector3.one * item.radius * 2f;
                SetColor(go, TreasureColor(item.treasure_type));
            }
        }

        private static void BuildSupplyBoxes(Transform root, MapJsonModels.MapJson map)
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

                GameObject go = CreatePrimitive(item.box_id, PrimitiveType.Cube, parent);
                go.transform.position = ToVector3(item.position);
                go.transform.localScale = Vector3.one * 0.7f;
                SetColor(go, new Color(0.15f, 0.8f, 0.25f, 1f));
            }
        }

        private static void BuildMapBoundary(Transform root, MapJsonModels.MapJson map)
        {
            if (map.map_boundary == null || map.map_boundary.points == null || map.map_boundary.points.Count < 3)
            {
                return;
            }

            GameObject go = new GameObject("map_boundary");
            go.transform.SetParent(root, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.widthMultiplier = 0.04f;
            line.positionCount = map.map_boundary.points.Count;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.material.color = new Color(0.1f, 0.8f, 1f, 0.85f);

            for (int i = 0; i < map.map_boundary.points.Count; i++)
            {
                line.SetPosition(i, ToVector3(map.map_boundary.points[i]) + Vector3.up * 0.02f);
            }
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

        private static bool IsFloor(string prefabId)
        {
            return !string.IsNullOrEmpty(prefabId) && prefabId.ToLowerInvariant().Contains("floor");
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

        private static Color ObjectColor(string objectType)
        {
            if (objectType == MapObjectType.StaticFloor.ToString())
            {
                return new Color(0.22f, 0.45f, 0.35f, 1f);
            }

            if (objectType == MapObjectType.PhysicsProp.ToString())
            {
                return new Color(0.75f, 0.45f, 0.18f, 1f);
            }

            if (objectType == MapObjectType.OpenableObject.ToString())
            {
                return new Color(0.65f, 0.35f, 0.8f, 1f);
            }

            return new Color(0.28f, 0.3f, 0.32f, 1f);
        }

        private static void ConfigureRuntimeObject(GameObject go, MapJsonModels.MapObjectJson item)
        {
            MapRuntimeObject runtimeObject = go.AddComponent<MapRuntimeObject>();
            runtimeObject.object_id = item.object_id;
            runtimeObject.prefab_id = item.prefab_id;
            runtimeObject.object_type = ParseObjectType(item.object_type);
            runtimeObject.interaction_type = ParseInteractionType(item.interaction_type);
            runtimeObject.is_movable = item.is_movable;
            runtimeObject.is_grabbable = item.is_grabbable;
            runtimeObject.is_openable = item.is_openable;
            runtimeObject.is_shootable = item.is_shootable;
            runtimeObject.blocks_bullet = item.blocks_bullet;
            runtimeObject.decal_enabled = item.decal_enabled;
            runtimeObject.mass = item.mass;

            if (item.is_shootable)
            {
                BulletSurface surface = go.AddComponent<BulletSurface>();
                surface.blocks_bullet = item.blocks_bullet;
                surface.decal_enabled = item.decal_enabled;
            }

            if (item.is_movable || item.is_grabbable)
            {
                Rigidbody body = go.AddComponent<Rigidbody>();
                body.mass = Mathf.Max(0.01f, item.mass);
            }

            if (item.is_openable || item.object_type == MapObjectType.OpenableObject.ToString())
            {
                OpenableBox openable = go.AddComponent<OpenableBox>();
                openable.object_id = item.object_id;
            }
        }

        private static MapObjectType ParseObjectType(string value)
        {
            if (System.Enum.TryParse(value, true, out MapObjectType parsed))
            {
                return parsed;
            }

            return MapObjectType.StaticObstacle;
        }

        private static MapInteractionType ParseInteractionType(string value)
        {
            if (System.Enum.TryParse(value, true, out MapInteractionType parsed))
            {
                return parsed;
            }

            return MapInteractionType.None;
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
