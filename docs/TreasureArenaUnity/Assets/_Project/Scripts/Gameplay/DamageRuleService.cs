using TreasureArenaMR.Shared;

namespace TreasureArenaMR.Gameplay
{
    /// <summary>
    /// Pure damage rules. It assumes hit detection has already been decided by the caller.
    /// </summary>
    public sealed class DamageRuleService
    {
        public bool CanAttack(PlayerRuntimeState attacker)
        {
            return attacker != null
                && attacker.state == PlayerState.Alive
                && attacker.team != TeamType.None
                && attacker.hp > 0;
        }

        public bool CanBeAttacked(PlayerRuntimeState attacker, PlayerRuntimeState target)
        {
            return CanAttack(attacker)
                && target != null
                && target.state == PlayerState.Alive
                && target.team != TeamType.None
                && target.team != attacker.team
                && target.player_id != attacker.player_id
                && target.hp > 0;
        }

        public int CalculateDamage(WeaponConfig weaponConfig)
        {
            if (weaponConfig == null || weaponConfig.damage <= 0)
            {
                return 0;
            }

            return weaponConfig.damage;
        }

        public DamageRuleResult ApplyDamage(
            PlayerRuntimeState attacker,
            PlayerRuntimeState target,
            WeaponConfig weaponConfig)
        {
            DamageRuleResult result = new DamageRuleResult
            {
                attackerPlayerId = attacker != null ? attacker.player_id : null,
                targetPlayerId = target != null ? target.player_id : null
            };

            if (!CanAttack(attacker))
            {
                result.failureReason = "attacker_not_allowed";
                return result;
            }

            if (!CanBeAttacked(attacker, target))
            {
                result.failureReason = "target_not_attackable";
                return result;
            }

            int damage = CalculateDamage(weaponConfig);
            if (damage <= 0)
            {
                result.failureReason = "invalid_damage";
                return result;
            }

            result.ok = true;
            result.damage = damage;
            result.targetHpBefore = target.hp;
            target.hp = System.Math.Max(0, target.hp - damage);
            result.targetHpAfter = target.hp;

            if (target.hp <= 0)
            {
                result.targetEnteredGhostRetreat = true;
                result.droppedTreasureId = target.carried_treasure_id;
                target.state = PlayerState.GhostRetreat;
                target.carried_treasure_id = null;
            }

            return result;
        }

        public sealed class DamageRuleResult
        {
            public bool ok;
            public string failureReason;
            public string attackerPlayerId;
            public string targetPlayerId;
            public int damage;
            public int targetHpBefore;
            public int targetHpAfter;
            public bool targetEnteredGhostRetreat;
            public string droppedTreasureId;
        }
    }
}
