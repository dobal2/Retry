using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    
    [SerializeField] private float updateInterval = 0.3f; // 경로 업데이트 주기 (0.2~0.5 추천)
    private Vector3 lastPlayerPosition;
    private float distanceThreshold = 0.5f; // 플레이어가 이 거리 이상 움직이면 경로를 다시 계산

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found!");

        StartCoroutine(UpdatePathRoutine());
    }

    IEnumerator UpdatePathRoutine()
    {
        while (true)
        {
            if (player != null && agent.isOnNavMesh && agent.enabled)
            {
                // 플레이어가 일정 거리 이상 이동했을 때만 경로 갱신
                if (Vector3.Distance(lastPlayerPosition, player.position) > distanceThreshold)
                {
                    agent.SetDestination(player.position);
                    lastPlayerPosition = player.position;
                }
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }
}