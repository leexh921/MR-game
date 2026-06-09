using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Room-level weapon parameters supplied by Manager and enforced by Server.
    /// </summary>
    [Serializable]
    public sealed class WeaponConfig
    {
        public string weapon_id = "energy_gun";
        public int damage = 25;
        public float range = 15f;
        public float cooldown = 0.5f;
    }
}
