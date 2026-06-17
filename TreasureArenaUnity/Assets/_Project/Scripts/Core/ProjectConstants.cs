namespace TreasureArenaMR.Core
{
    /// <summary>
    /// Shared project constants for scene names, config paths, and runtime settings.
    /// </summary>
    public static class ProjectConstants
    {
        public const string ProjectRootFolder = "Assets/_Project";

        public const string SceneBoot = "Boot";
        public const string SceneHome = "Home";
        public const string SceneGame = "Game";
        public const string SceneMapPreview = "MapPreview";

        public const string MapFolder = "Assets/_Project/StreamingAssets/Maps/";
        public const string ConfigFolder = "Assets/_Project/StreamingAssets/Configs/";

        public const string DefaultMapId = "test_map_01";
        public const string DefaultWeaponId = "energy_gun";

        public const string DbFileName = "treasure_arena.db";
    }
}
