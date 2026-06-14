using System.Collections.Generic;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Validates the minimum map data needed by TeamTreasure MVP.
    /// </summary>
    public sealed class MapValidator
    {
        public MapValidationResult Validate(MapJsonModels.MapJson map, PrefabRegistry prefabRegistry = null)
        {
            MapValidationResult result = new MapValidationResult();
            if (map == null)
            {
                result.errors.Add("map_is_null");
                return result;
            }

            ValidateTeamBases(result, map.team_bases);
            ValidateTreasureSpawnPoints(result, map.treasure_spawn_points);
            result.ok = result.errors.Count == 0;
            return result;
        }

        private static void ValidateTeamBases(MapValidationResult result, List<MapJsonModels.TeamBaseJson> bases)
        {
            if (bases == null || bases.Count == 0)
            {
                result.errors.Add("missing_team_bases");
                return;
            }

            bool hasRed = false;
            bool hasBlue = false;
            for (int i = 0; i < bases.Count; i++)
            {
                MapJsonModels.TeamBaseJson teamBase = bases[i];
                if (teamBase == null)
                {
                    continue;
                }

                if (teamBase.team == "Red")
                {
                    hasRed = true;
                }
                else if (teamBase.team == "Blue")
                {
                    hasBlue = true;
                }
            }

            if (!hasRed)
            {
                result.errors.Add("missing_red_base");
            }

            if (!hasBlue)
            {
                result.errors.Add("missing_blue_base");
            }
        }

        private static void ValidateTreasureSpawnPoints(MapValidationResult result, List<MapJsonModels.TreasureSpawnPointJson> points)
        {
            if (points == null || points.Count == 0)
            {
                result.errors.Add("missing_treasure_spawn_points");
                return;
            }
        }
    }

    public sealed class MapValidationResult
    {
        public bool ok;
        public readonly List<string> errors = new List<string>();

        public string GetErrorText()
        {
            return string.Join(", ", errors.ToArray());
        }
    }
}
