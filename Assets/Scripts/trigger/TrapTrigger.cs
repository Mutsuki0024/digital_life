using UnityEngine;

public class TrapTrigger : MonoBehaviour
{
    /// <summary>
    /// Send negative reward when agent entered trap
    /// </summary>
    /// <param name="other">get collider to check if it's agent entering</param>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Config.Instance.agentTag))
        {
            Debug.Log("Agent triggered trap!");

            // 通知 Agent 踩到陷阱
            var agent = other.GetComponent<MyAgent>();
            agent.OnHitTrap();

            // inactivate collider to prevent multiple triggering
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;

            GameManager.Instance.ReturnTrap(gameObject);
            GameManager.Instance.SpawnTrap();

        }
    }
}
