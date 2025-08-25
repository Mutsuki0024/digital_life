using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Rewards")]
    public float foodReward = 1.0f;
    public float trapPenalty = -1.0f;
    public float surviveRewardPerSecond = 0.01f;
    public float getCatched = -1.0f;
    public float deadPenalty = -1.0f;

    [Header("Agent")]
    public int agentMaxHP = 100;
    public float iFrameSeconds = 0.7f;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    [Header("GameInitial")]
    public int foodCount = 8;
    public int trapCount = 4;
    public float foodHeight = 1f;
    public float trapHeight = 0f;
    public float agentHeight = 0.5f;


    [Header("Trap")]
    public int trapDamage = 25;

    [Header("Hunger system")]
    public bool hungerEnabled = true;
    public float hungerGraceSec = 15f;          // 缓冲
    public float hungerNegRewardPerSec = -0.01f;// 缓冲后每秒RL负奖励（柔性）
    public bool hardStarvation = false;        // 是否启用硬性饿死
    public float hardStarveTimeoutSec = 30f;    // 硬性超时阈值
    public float starvationTerminalReward = -1f;  // 饿死终止惩罚
    public bool hungerResetsOnFood = true;
    public float hungerExtraGraceOnFood = 30f;  //吃到食物后的奖励时间

    
    [Header("Tags/Layers")]
    public string agentTag = "Agent";
}
