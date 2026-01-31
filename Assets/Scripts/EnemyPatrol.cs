using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    private NavMeshAgent agent;
    private int index;
    private int direction = 1;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            enabled = false;
            return;
        }

        agent.SetDestination(waypoints[index].position);
    }

    private void Update()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance > agent.stoppingDistance)
            return;

        if (waypoints.Length == 1)
            return;

        if (index == waypoints.Length - 1)
            direction = -1;
        else if (index == 0)
            direction = 1;

        index += direction;
        agent.SetDestination(waypoints[index].position);
    }
}
