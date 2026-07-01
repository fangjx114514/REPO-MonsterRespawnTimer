using UnityEngine;

namespace MonsterRespawnTimer;

internal sealed class MonsterRespawnTimerRunner : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        MonsterRespawnTimerController.Tick("Runner.Update");
    }
}
