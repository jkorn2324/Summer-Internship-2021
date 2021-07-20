using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOutpostManager : MonoBehaviour
{

    [SerializeField]
    private List<GameOutpost> outposts;

    private void Start()
    {
        HookEvents();
    }

    private void HookEvents()
    {
        foreach(GameOutpost outpost in outposts)
        {
            if(outpost)
            {
                outpost.GameOutpostDestroyedEventDelegate += HandleOutpostDestroyed;
            }
        }
    }

    private void UnHookEvents()
    {
        foreach (GameOutpost outpost in outposts)
        {
            if(outpost)
            {
                outpost.GameOutpostDestroyedEventDelegate -= HandleOutpostDestroyed;
            }
        }
    }

    private void OnEnable()
    {
        HookEvents();
    }

    private void OnDisable()
    {
        UnHookEvents();
    }

    private void HandleOutpostDestroyed(GameOutpost outpost)
    {
        if(outposts.Contains(outpost))
        {
            outposts.Remove(outpost);
        }
    }

    public void GetOutpostsFromPosition(Vector3 pos, 
        ref List<GameOutpost> referenceOutposts)
    {
        referenceOutposts.Clear();

        if(this.outposts.Count <= 0)
        {
            return;
        }

        foreach(GameOutpost outpost in outposts)
        {
            if(outpost.IsWithinOutpost(pos))
            {
                referenceOutposts.Add(outpost);
            }
        }
    }
}
