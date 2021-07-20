using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOutpostEnemyWavePrefabData
{
    public HashSet<int> spawnedEnemyPrefabs;
    public List<GameOutpostEnemyPrefabData> allocatedPrefabs;
    public int currentWave;

    public GameOutpostEnemyWavePrefabData(int currentWave)
    {
        this.spawnedEnemyPrefabs = new HashSet<int>();
        this.allocatedPrefabs = new List<GameOutpostEnemyPrefabData>();
        this.currentWave = currentWave;
    }
}

public class GameOutpostSpawnerTent
{

    private GameOutpostTent _tent;
    private List<GameOutpostEnemyWavePrefabData> _allocatedWavePrefabs;
    public GameOutpostTent Tent => _tent;

    public GameOutpostSpawnerTent(GameOutpostTent tent)
    {
        _tent = tent;
        _allocatedWavePrefabs = new List<GameOutpostEnemyWavePrefabData>();
    }

    public bool AddEnemy(int currentWave, GameOutpostEnemyPrefabData prefabData)
    {
        GameOutpostEnemyWavePrefabData wavePrefabs;
        if (currentWave >= _allocatedWavePrefabs.Count)
        {
            wavePrefabs = new GameOutpostEnemyWavePrefabData(currentWave);
            _allocatedWavePrefabs.Add(wavePrefabs);
        }
        else
        {
            wavePrefabs = _allocatedWavePrefabs[currentWave];
        }

        if(!prefabData.CanSpawn(_tent))
        {
            return false;
        }
        wavePrefabs.allocatedPrefabs.Add(prefabData);
        return true;
    }

    public bool HasSpawnedEnemiesForWave(int currentWave)
    {
        if (currentWave >= _allocatedWavePrefabs.Count)
        {
            return true;
        }
        GameOutpostEnemyWavePrefabData prefabData = _allocatedWavePrefabs[currentWave];
        if (prefabData.allocatedPrefabs.Count <= 0
            || prefabData.spawnedEnemyPrefabs.Count >= prefabData.allocatedPrefabs.Count)
        {
            return true;
        }
        return false;
    }

    public bool SpawnEnemy(int currentWave, out GameObject gameObject)
    {
        if(HasSpawnedEnemiesForWave(currentWave))
        {
            gameObject = null;
            return false;
        }

        GameOutpostEnemyWavePrefabData prefabData = _allocatedWavePrefabs[currentWave];
        int randomizedIndex;
        do 
        {
            randomizedIndex = Random.Range(0, prefabData.allocatedPrefabs.Count);
        } while (prefabData.spawnedEnemyPrefabs.Contains(randomizedIndex));

        GameOutpostEnemyPrefabData enemyPrefabData = prefabData.allocatedPrefabs[randomizedIndex];
        Vector3 enemySpawnPosition = _tent.GetEnemySpawnPosition()
            + enemyPrefabData.spawnPositionOffset;
        gameObject = MirrorUtils.SpawnPrefab(enemyPrefabData.prefab, enemySpawnPosition);
        prefabData.spawnedEnemyPrefabs.Add(randomizedIndex);
        return true;
    }
}

public class GameOutpostFlexibleEnemyDataSet
{
    public class GameOutpostFlexibleEnemy
    {
        public List<GameOutpostSpawnerTent> availableTents;
        public GameOutpostEnemyPrefabData prefabData;
        public int currentTentIndex;

        public GameOutpostFlexibleEnemy(GameOutpostEnemyPrefabData pfData, List<GameOutpostSpawnerTent> tents, int currentIndex)
        {
            currentTentIndex = currentIndex;
            prefabData = pfData;
            availableTents = new List<GameOutpostSpawnerTent>(tents);
        }
    }

    private List<GameOutpostFlexibleEnemy> _flexibleEnemies;
    private HashSet<int> _spawnedEnemies;
    private int _currentWave;
    public int Count => _flexibleEnemies.Count;
    public int CurrentWave => _currentWave;

    public bool HasSpawnedAllEnemies
        => _spawnedEnemies.Count >= _flexibleEnemies.Count;

    public GameOutpostFlexibleEnemy this[int index]
    {
        get
        {
            if(_flexibleEnemies.Count <= 0
                || index >= _flexibleEnemies.Count)
            {
                return null;
            }
            return _flexibleEnemies[index];
        }
    }

    public GameOutpostFlexibleEnemyDataSet(int currentWave)
    {
        _currentWave = currentWave;
        _spawnedEnemies = new HashSet<int>();
        _flexibleEnemies = new List<GameOutpostFlexibleEnemy>();
    }

    public bool AddEnemy(GameOutpostEnemyPrefabData enemy, List<GameOutpostSpawnerTent> tents, int currentTentIndex)
    {
        if(!enemy.SpawnAtAvailableTent)
        {
            return false;
        }
        GameOutpostFlexibleEnemy flexibleEnemy = new GameOutpostFlexibleEnemy(
            enemy, tents, currentTentIndex);
        _flexibleEnemies.Add(flexibleEnemy);
        return true;
    }

    public bool SpawnEnemy(out GameObject gameObject, ref GameOutpostSpawnerTent spawnedTent)
    {
        if(_flexibleEnemies.Count <= 0
            || _spawnedEnemies.Count >= _flexibleEnemies.Count)
        {
            gameObject = null;
            return false;
        }

        int randomEnemyIndex;
        do
        {
            randomEnemyIndex = Random.Range(0, _flexibleEnemies.Count);
        } while (_spawnedEnemies.Contains(randomEnemyIndex));

        GameOutpostFlexibleEnemy enemy = _flexibleEnemies[randomEnemyIndex];
        if(enemy.availableTents.Count <= 0)
        {
            gameObject = null;
            _spawnedEnemies.Add(randomEnemyIndex);
            return true;
        }

        spawnedTent = enemy.availableTents[enemy.currentTentIndex];
        Vector3 spawnPosition = spawnedTent.Tent.GetEnemySpawnPosition()
            + enemy.prefabData.spawnPositionOffset;
        GameObject spawnedEnemy = MirrorUtils.SpawnPrefab(enemy.prefabData.prefab,
            spawnPosition);
        gameObject = spawnedEnemy;
        _spawnedEnemies.Add(randomEnemyIndex);
        return true;
    }

    public void HandleTentDestroyed(GameOutpostSpawnerTent tent)
    {
        // Loops through each of the flexible enemies and removes the tent from
        // the available tents that the enemy can spawn to.
        for (int i = 0; i < _flexibleEnemies.Count; i++)
        {
            GameOutpostFlexibleEnemy flexibleEnemy = _flexibleEnemies[i];
            if (flexibleEnemy.availableTents.Contains(tent))
            {
                int indexOfTent = flexibleEnemy.availableTents.IndexOf(tent);
                flexibleEnemy.availableTents.Remove(tent);
                
                if (flexibleEnemy.currentTentIndex == indexOfTent
                    || indexOfTent > flexibleEnemy.availableTents.Count)
                {
                    flexibleEnemy.currentTentIndex = Random.Range(0, flexibleEnemy.availableTents.Count);
                }
            }
        }
    }
}

public class GameOutpostWaveSpawner : IHRWaveSpawner
{

    public AHREnemySpawner.EnemySpawnEventSignature EnemySpawnEventDelegate;

    private int _currentWave;
    private float _currentTimeBetweenNextSpawn = 0.0f;

    private GameOutpost _gameOutpost;
    
    private Dictionary<GameOutpostTent, GameOutpostSpawnerTent> _tents;
    private List<GameOutpostTent> _completedSpawningTents;
    private List<GameOutpostFlexibleEnemyDataSet> _nonTentEnemies;

    private INextWaveTrigger _nextWaveTrigger;

    private bool _startedSpawning = false;
    private bool _isBetweenWaves = false;
    private bool _enabled = false;

    private GameOutpostWaveData OutpostWaveData
    {
        get
        {
            if(_currentWave >= _gameOutpost.Waves.Count)
            {
                return null;
            }
            return _gameOutpost.Waves[_currentWave];
        }
    }

    public NextWaveTriggerData NxtWaveTriggerData
        => _gameOutpost.NxtWaveTriggerData;

    private bool IsSpawningEnemies
        => HasWavesLeft && !HasFinishedSpawningForWave() && !_isBetweenWaves;

    public bool HasWavesLeft => _currentWave < _gameOutpost.Waves.Count;

    public bool IsEnabled => _enabled;

    public GameOutpostWaveSpawner(GameOutpost outpost)
    {
        _currentWave = 0;
        _gameOutpost = outpost;
        _nextWaveTrigger = CreateNextWaveTrigger();
        _nonTentEnemies = new List<GameOutpostFlexibleEnemyDataSet>();
        _tents = new Dictionary<GameOutpostTent, GameOutpostSpawnerTent>();
        _completedSpawningTents = new List<GameOutpostTent>();
        InitializeSpawner();
    }

    #region initialization

    private INextWaveTrigger CreateNextWaveTrigger()
    {
        switch (NxtWaveTriggerData.NextWaveTriggerType)
        {
            case NextWaveTriggerData.NextWaveTrigger.TYPE_TIMER:
                return new BetweenWavesTimer(this);
            case NextWaveTriggerData.NextWaveTrigger.TYPE_SPAWNED_ENEMIES_DEAD:
                return new SpawnedEnemiesDeadDelay(this);
        }
        return null;
    }

    private void InitializeSpawner()
    {
        foreach (GameOutpostTent tent in _gameOutpost.Tents)
        {
            GameOutpostSpawnerTent spawnerTent = new GameOutpostSpawnerTent(tent);
            _tents.Add(tent, spawnerTent);
        }

        // Pre-allocates enemies to each tent.
        int totalNumberOfWaves = _gameOutpost.Waves.Count;
        int currentNumberOfWaves = 0;
        while(currentNumberOfWaves < totalNumberOfWaves)
        {
            AllocateEnemies(currentNumberOfWaves);
            currentNumberOfWaves++;
        }
    }

    private bool AllocateEnemies(int currentWave)
    {
        GameOutpostWaveData currentWaveData = _gameOutpost.Waves[currentWave];
        if (currentWaveData == null)
        {
            return false;
        }

        GameOutpostSpawnerTent[] tents;
        ConvertTentsDictToArray(out tents, true);

        int previousOutpostTentIndex = -1;
        for (int prefabIndex = 0; prefabIndex < currentWaveData.EnemiesList.PrefabsCount; prefabIndex++)
        {
            GameOutpostEnemyPrefabData prefabData = currentWaveData.EnemiesList[prefabIndex];
            if(AllocateFlexibleEnemy(currentWave, prefabData))
            {
                continue;
            }

            int currentTentIndex = Mathf.Max(0, previousOutpostTentIndex % _tents.Count);
            for (int currentSpawnAmount = 0; currentSpawnAmount < prefabData.SpawnAmount; currentSpawnAmount++)
            {
                GameOutpostSpawnerTent spawnerTent = tents[currentTentIndex];
                int searchedTents = 0;
                while (!spawnerTent.AddEnemy(currentWave, prefabData)
                    && searchedTents < tents.Length)
                {
                    currentTentIndex = (currentTentIndex + 1) % _tents.Count;
                    spawnerTent = tents[currentTentIndex];
                    searchedTents++;
                }
                currentTentIndex = (currentTentIndex + 1) % _tents.Count;
            }
            previousOutpostTentIndex = currentTentIndex;
        }
        return true;
    }

    private bool AllocateFlexibleEnemy(int currentWaveIndex, GameOutpostEnemyPrefabData prefabData)
    {
        GameOutpostFlexibleEnemyDataSet enemyDataSet;
        if(currentWaveIndex >= _nonTentEnemies.Count)
        {
            enemyDataSet = new GameOutpostFlexibleEnemyDataSet(currentWaveIndex);
            _nonTentEnemies.Add(enemyDataSet);
        }
        else
        {
            enemyDataSet = _nonTentEnemies[currentWaveIndex];
        }

        if(!prefabData.SpawnAtAvailableTent)
        {
            return false;
        }

        List<GameOutpostSpawnerTent> availableTents = new List<GameOutpostSpawnerTent>();
        foreach (GameOutpostTent tent in _tents.Keys)
        {
            GameOutpostSpawnerTent spawnerTent = _tents[tent];
            if (!prefabData.CanSpawn(tent))
            {
                continue;
            }
            availableTents.Add(spawnerTent);
        }

        // If there are no tents which the prefab can spawn at, don't do anything.
        if (availableTents.Count <= 0)
        {
            return true;
        }

        // Sets the available tents that the enemy can spawn at.
        int currentTentIndex = 0;
        for(int currentSpawnAmount = 0; currentSpawnAmount < prefabData.SpawnAmount; currentSpawnAmount++)
        {
            enemyDataSet.AddEnemy(prefabData, availableTents, currentTentIndex);
            currentTentIndex = (currentTentIndex + 1) % availableTents.Count;
        }
        return true;
    }

    #endregion

    private void ConvertTentsDictToArray(out GameOutpostSpawnerTent[] tents, bool includeSearched)
    {
        int currentTentIndex = 0;
        tents = new GameOutpostSpawnerTent[_tents.Count];
        foreach (GameOutpostTent tent in _tents.Keys)
        {
            if(!includeSearched
                && _completedSpawningTents.Contains(tent))
            {
                continue;
            }
            tents[currentTentIndex] = _tents[tent];
            currentTentIndex++;
        }
    }

    public void SetEnabled(bool enabled, bool forceSpawnEnemies = false)
    {
        if(_enabled != enabled)
        {
            if(forceSpawnEnemies && !_startedSpawning)
            {
                SpawnEnemies();
            }
        }
        _enabled = enabled;
    }

    public void HookEvents() 
    {  
        foreach(GameOutpostSpawnerTent tent in _tents.Values)
        {
            tent.Tent.GameOutpostTentDestroyedEventDelegate 
                += HandleTentDestroyed;
        }

        if (_nextWaveTrigger != null)
        {
            _nextWaveTrigger.HookNextWaveDelegate(HandleNextWaveBegin);
        }
    }

    public void UnHookEvents() 
    {
        foreach(GameOutpostSpawnerTent tent in _tents.Values)
        {
            tent.Tent.GameOutpostTentDestroyedEventDelegate 
                -= HandleTentDestroyed;
        }

        if(_nextWaveTrigger != null)
        {
            _nextWaveTrigger.UnHookNextWaveDelegate(HandleNextWaveBegin);
        }
    }

    public void HookActionToSpawnEvent(AHREnemySpawner.EnemySpawnEventSignature spawnEventSignature)
    {
        EnemySpawnEventDelegate += spawnEventSignature;
    }

    public void Update(float deltaTime)
    {
        if(!IsEnabled)
        {
            return;
        }

        if(!HasWavesLeft)
        {
            return;
        }

        if(IsSpawningEnemies)
        {
            _currentTimeBetweenNextSpawn -= Time.deltaTime;

            if(_currentTimeBetweenNextSpawn <= 0.0f)
            {
                if (SpawnEnemy())
                {
                    _currentTimeBetweenNextSpawn =
                        OutpostWaveData.WaveSpawnOrder.ConsecutiveSpawnTimeDiff;
                }
                else
                {
                    _currentWave++;
                    _currentTimeBetweenNextSpawn = 0.0f;
                }
            }
            return;
        }
        _nextWaveTrigger?.Update(deltaTime);
    }

    private void SpawnEnemies()
    {
        if(!_startedSpawning)
        {
            _startedSpawning = true;
        }

        if(OutpostWaveData == null)
        {
            return;
        }

        EnemySpawnOrderData.EnemySpawnOrderType spawnOrderType = OutpostWaveData.WaveSpawnOrder.SpawnOrderType;
        if(spawnOrderType == EnemySpawnOrderData.EnemySpawnOrderType.TYPE_ALL)
        {
            while (SpawnEnemy())
            {
                continue;
            }
            return;
        }

        if (SpawnEnemy())
        {
            _currentTimeBetweenNextSpawn = OutpostWaveData.WaveSpawnOrder.ConsecutiveSpawnTimeDiff;
        }
    }

    private void ApplyTentTargetToEnemy(GameObject enemy, GameOutpostSpawnerTent tent)
    {
        HeroPlayerCharacter character = enemy?.GetComponentInParent<HeroPlayerCharacter>();
        if (character
            && !character.IsPossessedByPlayer)
        {
            character.OutpostTargetAI.SetTargetOutpostTent(tent.Tent);
        }
    }

    private bool HasFinishedSpawningForWave()
    {
        if (_tents.Count <= 0)
        {
            return true;
        }
        return _completedSpawningTents.Count >= _tents.Count
            && _nonTentEnemies[_currentWave].HasSpawnedAllEnemies;
    }

    private bool CanSpawnFlexibleEnemy()
    {
        // 1 in 4 chance that a flexible enemy spawns
        // if there are still enemies spawning from tents.
        int randomInteger = Random.Range(0, 4);
        return (_completedSpawningTents.Count >= _tents.Count || randomInteger >= 3)
            && !_nonTentEnemies[_currentWave].HasSpawnedAllEnemies;
    }

    private bool SpawnEnemy()
    {
        if(_tents.Count <= 0)
        {
            _isBetweenWaves = true;
            _currentTimeBetweenNextSpawn = 0.0f;
            _completedSpawningTents.Clear();
            _nextWaveTrigger?.HandleWaveFinished();
            return false;
        }

        GameObject enemyGameObject;
        GameOutpostSpawnerTent searchedTent = null;

        if(CanSpawnFlexibleEnemy())
        {
            _nonTentEnemies[_currentWave].SpawnEnemy(out enemyGameObject, ref searchedTent);
        }
        else
        {
            GameOutpostSpawnerTent[] tents;
            ConvertTentsDictToArray(out tents, false);

            searchedTent = tents[Random.Range(0, tents.Length)];
            searchedTent.SpawnEnemy(_currentWave, out enemyGameObject);
            
            if (searchedTent.HasSpawnedEnemiesForWave(_currentWave))
            {
                _completedSpawningTents.Add(searchedTent.Tent);
            }
        }
        
        if(enemyGameObject != null)
        {
            ApplyTentTargetToEnemy(enemyGameObject, searchedTent);
            EnemySpawnEventDelegate?.Invoke(enemyGameObject);
        }
        
        if(HasFinishedSpawningForWave())
        {
            _isBetweenWaves = true;
            _currentTimeBetweenNextSpawn = 0.0f;
            _completedSpawningTents.Clear();
            _nextWaveTrigger?.HandleWaveFinished();
            return false;
        }
        return true;
    }

    private void HandleNextWaveBegin(INextWaveTrigger trigger)
    {
        _isBetweenWaves = false;

        if(IsEnabled)
        {
            SpawnEnemies();
        }
    }

    private void HandleTentDestroyed(GameOutpostTent tent)
    {
        if(_tents.ContainsKey(tent))
        {
            int currentWave = _currentWave;
            if(currentWave < _nonTentEnemies.Count)
            {
                GameOutpostSpawnerTent currentTent = _tents[tent];
                for (int wave = currentWave; wave < _nonTentEnemies.Count; wave++)
                {
                    GameOutpostFlexibleEnemyDataSet flexibleEnemyDataSet = _nonTentEnemies[wave];
                    flexibleEnemyDataSet.HandleTentDestroyed(currentTent);
                }
            }
            _tents.Remove(tent);
        }

        if(_completedSpawningTents.Contains(tent))
        {
            _completedSpawningTents.Remove(tent);
        }
    }
}
