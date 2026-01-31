using System;
using UnityEngine;
using UnityEngine.AI;

public class MinigameContext
{
    public EnemyMaskStackVisual enemy;
    public Transform anchor;
    public Action onSuccess;
    public Action onFail;
    public PlayerInteractor player;
    public NavMeshAgent agentToStop;

    public MinigameContext(EnemyMaskStackVisual enemy, Transform anchor, PlayerInteractor player = null, NavMeshAgent agentToStop = null)
    {
        this.enemy = enemy;
        this.anchor = anchor;
        this.player = player;
        this.agentToStop = agentToStop;
        onSuccess = PopTopMask;
    }

    public void PopTopMask()
    {
        if (enemy == null)
            return;

        enemy.PopTopMask();
    }
}
