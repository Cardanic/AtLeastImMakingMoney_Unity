using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavAgentScript : MonoBehaviour
{

    public Transform target1, target2;

    Transform currentTarget;
    NavMeshAgent navAgent;

    public float currentDistance;

    private void Start()
    {
        currentTarget = target1;
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.SetDestination(currentTarget.position);
    }

    void Update()
    {
        currentDistance = navAgent.remainingDistance;

        if (navAgent.remainingDistance < 1f)
        {
            if (currentTarget == target1)
                currentTarget = target2;
            else
                currentTarget = target1;  
        }

        navAgent.SetDestination(currentTarget.position);
    }
}
