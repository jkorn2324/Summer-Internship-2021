using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BaseHP), typeof(BaseDestroyHPListener))]
public class GameOutpostTent : MonoBehaviour
{

    [System.Serializable, System.Flags]
    public enum GameOutpostTentType
    {
        // TODO: Add Tent Types
        Default = 1 << 0,
        Shielded = 1 << 1
    }

    public delegate void GameOutpostTentDestroyedSignature(GameOutpostTent tent);
    public GameOutpostTentDestroyedSignature GameOutpostTentDestroyedEventDelegate;

    [SerializeField]
    private GameOutpostTentType tentType;
    [SerializeField]
    private GameOutpost outpost;
    [SerializeField]
    private EnemySpawnPositionData enemySpawnPositionData;

    private BaseDestroyHPListener _destroyHPListener;

    public GameOutpost Outpost => outpost;

    private void Start()
    {
        _destroyHPListener = GetComponent<BaseDestroyHPListener>();
        HookEvents();
    }

    private void OnEnable()
    {
        HookEvents();
    }

    private void OnDisable()
    {
        UnHookEvents();
    }

    private void HookEvents()
    {
        if(HRNetworkManager.Get
            && HRNetworkManager.Get.bIsServer)
        {
            if (_destroyHPListener)
            {
                _destroyHPListener.OnDestroyDelegate += HandleDestroyed;
            }
        }
    }

    private void UnHookEvents()
    {
        if (_destroyHPListener)
        {
            _destroyHPListener.OnDestroyDelegate -= HandleDestroyed;
        }
    }

    private void LateUpdate()
    {
        enemySpawnPositionData.UpdateOffsets(this.transform.position);
    }

    private void HandleDestroyed(BaseDestroyHPListener listener)
    {
        GameOutpostTentDestroyedEventDelegate?.Invoke(this);
    }

    public Vector3 GetEnemySpawnPosition()
    {
        return enemySpawnPositionData.GetSpawnPosition(this.transform.position);
    }

    public bool IsTentType(GameOutpostTentType tentType)
    {
        return this.tentType.HasFlag(tentType);
    }

    private void OnDrawGizmos()
    {
        DrawPositionData(enemySpawnPositionData);
    }

    private void DrawPositionData(EnemySpawnPositionData spawnPositionData)
    {
        spawnPositionData.UpdateOffsets(this.transform.position);

        if (spawnPositionData.SpawnPositionType ==
            EnemySpawnPositionData.EnemySpawnPositionType.TYPE_RANDOM_DEFINED_POSITIONS)
        {
            if (spawnPositionData.RandomDefinedPositions == null
                || spawnPositionData.RandomDefinedPositions.Length <= 0)
            {
                return;
            }

            foreach (EnemySpawnPositionData.WeightedDefinedSpawnPosition definedSpawnPosition in
                spawnPositionData.RandomDefinedPositions)
            {
                Gizmos.color = Color.Lerp(Color.red, Color.green, definedSpawnPosition.weight);
                Vector3 spawnPosition = definedSpawnPosition.SpawnPosition.HasValue
                    ? definedSpawnPosition.SpawnPosition.Value : transform.position;
                Gizmos.DrawSphere(spawnPosition, 0.5f);
            }
        }
    }
}
