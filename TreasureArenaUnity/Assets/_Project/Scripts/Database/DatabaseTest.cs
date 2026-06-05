using UnityEngine;

/// <summary>
/// Quick test: MUST start db_api server first, then Play.
/// Run: dotnet run --project db_api -p:CodePage=65001
/// </summary>
public class DatabaseTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[DatabaseTest] Connecting to DB API...");
        TreasureArenaMR.Database.DatabaseManager.Instance.Initialize();
    }
}
