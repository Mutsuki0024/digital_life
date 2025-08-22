using UnityEngine;

public class FoodTrigger : MonoBehaviour
{
    /// <summary>
    /// Send reward when agent ate food
    /// </summary>
    /// <param name="other">get collider to check if it's agent entering</param>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Config.Instance.agentTag))
        {
            Debug.Log("Agent ate food!");
            
            // 通知 Agent 增加奖励
            var agent = other.GetComponent<MyAgent>();
            agent.OnAteFood();

            // inactivate collider to prevent multiple triggering
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;

            GameManager.Instance.ReturnFood(gameObject);
            GameManager.Instance.SpawnFood();

        }
    }

    // Update is called once per frame
    //void OnTriggerEnter()
}
