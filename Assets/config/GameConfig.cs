using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Rewards")]
    public float foodReward = 3.0f;
    public float forwardReward = 0.05f;
    public float trapPenalty = -5.0f;
    public float surviveRewardPerSecond = 0.01f;
    public float getCatched = -1.0f;
    public float deadPenalty = -7.0f;

    [Header("Agent")]
    public int agentMaxHP = 100;
    public float iFrameSeconds = 0.7f;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    public float lambdaActionChange = 0.005f;   // 连贯性系数，先小一点
    public float eps = 0.01f; //正向位移奖励阈值

    [Header("GameInitial")]
    public int foodCount = 30;
    public int trapCount = 5;
    public float foodHeight = 0.5f;
    public float trapHeight = 0f;
    public float agentHeight = 0.5f;


    [Header("Trap")]
    public int trapDamage = 25;

    [Header("Wall/Corner")]
    public float wallContactPenaltyPerSec = -0.02f;  // 轻微即可

    [Header("Hunger system")]
    public bool hungerEnabled = false;
    public float hungerGraceSec = 15f;          // 缓冲
    public float hungerNegRewardPerSec = -0.01f;// 缓冲后每秒RL负奖励（柔性）
    public bool hardStarvation = false;        // 是否启用硬性饿死
    public float hardStarveTimeoutSec = 30f;    // 硬性超时阈值
    public float starvationTerminalReward = -1f;  // 饿死终止惩罚
    public bool hungerResetsOnFood = true;
    public float hungerExtraGraceOnFood = 30f;  //吃到食物后的奖励时间


    [Header("Tags/Layers")]
    public string agentTag = "Agent";
    
    [Header("Episode Limit")]
    public float episodeTimeLimitSec = 1200f;  // 每回合上限（秒）

}
