using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(GameArea), typeof(Mirror.NetworkIdentity))]
public class GameOutpost : Mirror.NetworkBehaviour
{

    public delegate void GameOutpostListenerSignature(GameOutpostListener listener, GameOutpost outpost);
    public GameOutpostListenerSignature ListenerEnterEventDelegate;
    public GameOutpostListenerSignature ListenerExitEventDelegate;

    public delegate void GameOutpostSignature(GameOutpost outpost);
    public GameOutpostSignature GameOutpostDestroyedEventDelegate;

    [Header("Outpost References")]

    [SerializeField]
    private GameOutpostBomb bomb;
    [SerializeField]
    private List<GameOutpostTent> tents;

    [Header("Enemy Spawner Data")]

    [SerializeField]
    private List<GameOutpostWaveData> waves;
    [SerializeField]
    private NextWaveTriggerData nextWaveTriggerData;


    private HashSet<GameOutpostListener> _listeners;
    private GameOutpostWaveSpawner _spawner;
    private GameArea _gameArea;

    public HashSet<GameOutpostListener> Listeners
        => _listeners;

    public List<GameOutpostTent> Tents => tents;

    public List<GameOutpostWaveData> Waves => waves;

    public NextWaveTriggerData NxtWaveTriggerData
        => nextWaveTriggerData;
         
    private void Start()
    {
        _gameArea = this.GetComponent<GameArea>();
        _listeners = new HashSet<GameOutpostListener>();

        if(HRNetworkManager.Get
            && HRNetworkManager.Get.bIsServer)
        {
            _spawner = new GameOutpostWaveSpawner(this);
        }
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
            if(bomb)
            {
                bomb.BombDetonatedEventDelegate += HandleBombDetonated;
            }
        }

        _spawner?.HookEvents();

        if (_spawner != null)
        {
            _spawner.EnemySpawnEventDelegate += HandleEnemySpawn;
        }

        foreach (GameOutpostTent tent in tents)
        {
            tent.GameOutpostTentDestroyedEventDelegate += HandleOutpostTentDestroyed;
        }
    }

    private void UnHookEvents()
    {
        if (bomb)
        {
            bomb.BombDetonatedEventDelegate -= HandleBombDetonated;
        }

        _spawner?.UnHookEvents();

        if (_spawner != null)
        {
            _spawner.EnemySpawnEventDelegate -= HandleEnemySpawn;
        }

        foreach (GameOutpostTent tent in tents)
        {
            tent.GameOutpostTentDestroyedEventDelegate -= HandleOutpostTentDestroyed;
        }
    }

    private void Update()
    {
        _spawner?.Update(Time.deltaTime);
    }

    private void OnDestroy()
    {
        GameOutpostDestroyedEventDelegate?.Invoke(this);    
    }

    public bool IsWithinOutpost(Vector3 point)
    {
        return _gameArea.CurrentShape.IsWithinShape(point);
    }

    private void HandleOutpostTentDestroyed(GameOutpostTent tent)
    {
        if(tents.Contains(tent))
        {
            tents.Remove(tent);
        }
    }

    public void OnListenerEnter(GameOutpostListener listener)
    {
        if(!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
            listener.GameOutpostListenerDestroyDelegate += HandleListenerDestroyed;
            HandleListenerEnter(listener);
            ListenerEnterEventDelegate?.Invoke(listener, this);
        }
    }

    public void OnListenerExit(GameOutpostListener listener)
    {
        if(_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
            listener.GameOutpostListenerDestroyDelegate -= HandleListenerDestroyed;
            HandleListenerExit(listener);
            ListenerExitEventDelegate?.Invoke(listener, this);
        }
    }

    private void HandleListenerEnter(GameOutpostListener listener)
    {
        HeroPlayerCharacter character = listener?.GetComponentInParent<HeroPlayerCharacter>();
        if(character
            && character.IsPossessedByPlayer)
        {
            _spawner?.SetEnabled(true, true);
        }
    }

    private void HandleListenerExit(GameOutpostListener listener)
    {
        HeroPlayerCharacter character = listener?.GetComponentInParent<HeroPlayerCharacter>();
        if(character
            && character.IsPossessedByPlayer
            && _listeners.Count <= 0)
        {
            _spawner?.SetEnabled(false);
            return;
        }

        foreach(GameOutpostListener lListener in _listeners)
        {
            HeroPlayerCharacter foundCharacter = lListener?.GetComponentInParent<HeroPlayerCharacter>();
            if(foundCharacter
                && foundCharacter.IsPossessedByPlayer)
            {
                return;
            }
        }
        _spawner?.SetEnabled(false);
    }

    private void HandleListenerDestroyed(GameOutpostListener listener)
    {
        if(_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
            HandleListenerExit(listener);
        }
    }

    private void HandleBombDetonated(GameOutpostBomb bomb) 
    {
        if(HRNetworkManager.Get
            && HRNetworkManager.Get.bIsServer)
        {
            // TODO: Check if we need the clientrpc function
            Destroy(this.gameObject, 3.0f);
        }
    }

    [Mirror.ClientRpc]
    private void DestroyOutpost_ClientRPC(float timeDelay)
    {
        Destroy(this.gameObject, timeDelay);
    }

    private void HandleEnemySpawn(GameObject spawnedObject) { }
}
