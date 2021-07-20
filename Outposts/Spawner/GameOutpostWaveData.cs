using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class GameOutpostEnemyPrefabData
{
    [SerializeField, Tooltip("Doesn't do anything within the code, just labels the element.")]
    private string enemyName;
    [SerializeField]
    public GameObject prefab;
    [SerializeField]
    public Vector3 spawnPositionOffset;
    [SerializeField, Min(1)]
    private int spawnAmount;
    [SerializeField]
    private GameOutpostTent.GameOutpostTentType tentsToSpawn;
    [SerializeField, Tooltip("If Enabled, should spawn enemy at any available tent out of its given tentsToSpawn type.")]
    private bool spawnAtAvailableTent = false;

    public int SpawnAmount => spawnAmount;

    public bool SpawnAtAvailableTent
        => spawnAtAvailableTent;

    public bool CanSpawn(GameOutpostTent tent)
    {
        return tent
            && tent.IsTentType(tentsToSpawn);
    }
}

[System.Serializable]
public class GameOutpostEnemyPrefabs
{
    [SerializeField]
    private List<GameOutpostEnemyPrefabData> prefabs;

    public GameOutpostEnemyPrefabData this[int index]
        => prefabs[index];

    public int PrefabsCount => prefabs.Count;

    public int EnemiesCount
    {
        get
        {
            int numEnemies = 0;
            foreach(GameOutpostEnemyPrefabData prefab in prefabs)
            {
                numEnemies += prefab.SpawnAmount;
            }
            return numEnemies;
        }
    }
}

[CreateAssetMenu(fileName = "OutpostWaveData", menuName = "Waves/Outpost Wave Data")]
public class GameOutpostWaveData : ScriptableObject
{
    [SerializeField]
    private EnemySpawnOrderData waveSpawnOrder;
    [SerializeField]
    private GameOutpostEnemyPrefabs enemiesList;

    public GameOutpostEnemyPrefabs EnemiesList
        => enemiesList;

    public EnemySpawnOrderData WaveSpawnOrder => waveSpawnOrder;
}
