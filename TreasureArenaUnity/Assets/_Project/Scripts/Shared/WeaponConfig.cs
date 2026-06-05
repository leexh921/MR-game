using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Room-level weapon parameters supplied by Manager and enforced by Server.
    /// Matches json_protocol.md §4.1 weapon_config and database_design.md §9 weapon_config table.
    /// </summary>
    [Serializable]
    public sealed class WeaponConfig
    {
        public string weapon_id;
        public int damage;
        public float range;
        public float cooldown;

        public WeaponConfig()
        {
            weapon_id = "energy_gun";
            damage = 25;
            range = 15f;
            cooldown = 0.5f;
        }
    }
}
