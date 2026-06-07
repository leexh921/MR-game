using TreasureArenaMR.Shared;

namespace TreasureArenaMR.Mock
{
    /// <summary>
    /// Placeholder for creating red/blue mock players in LocalTest.
    /// </summary>
    public sealed class MockPlayerFactory
    {
        public PlayerInfo CreatePlayer(string playerId, string nickname, TeamType team, int maxHp)
        {
            return new PlayerInfo
            {
                player_id = playerId,
                nickname = nickname,
                team = team,
                state = PlayerState.Alive,
                hp = maxHp,
                max_hp = maxHp,
                is_connected = true,
                carried_treasure_id = null
            };
        }
    }
}
