using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOutpostListener : MonoBehaviour
{
    #region delegates

    public delegate void GameOutpostSignature(GameOutpostListener listener, GameOutpost outpost);
    public GameOutpostSignature GameOutpostEnterDelegate;
    public GameOutpostSignature GameOutpostExitDelegate;

    public delegate void GameOutpostListenerDestroySignature(GameOutpostListener listener);
    public GameOutpostListenerDestroySignature GameOutpostListenerDestroyDelegate;

    #endregion

    private List<GameOutpost> _currentOutposts;

    public GameOutpostManager OutpostManager
        => ((HRGameManager)BaseGameManager.Get).OutpostManager;

    public List<GameOutpost> CurrentOutposts
        => _currentOutposts;

    private void Start()
    {
        _currentOutposts = new List<GameOutpost>();
    }

    private void OnDestroy()
    {
        GameOutpostListenerDestroyDelegate?.Invoke(this);
    }

    private void LateUpdate()
    {
        List<GameOutpost> currentOutposts = new List<GameOutpost>(_currentOutposts);
        OutpostManager.GetOutpostsFromPosition(
            this.transform.position, ref _currentOutposts);

        foreach (GameOutpost foundOutpost in _currentOutposts)
        {
            if(currentOutposts.Contains(foundOutpost))
            {
                continue;
            }
            foundOutpost.OnListenerEnter(this);
            GameOutpostEnterDelegate?.Invoke(this, foundOutpost);
        }
        
        foreach(GameOutpost currentOutpost in currentOutposts)
        {
            if(_currentOutposts.Contains(currentOutpost))
            {
                continue;
            }
            currentOutpost.OnListenerExit(this);
            GameOutpostExitDelegate?.Invoke(this, currentOutpost);
        }
    }
}
