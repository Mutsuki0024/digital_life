using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;


public class MyAgent : Agent
{
    private int hp;
    private float timeSinceLastFood;
    GameConfig cfg;

    // ==== 新增：运动参数 ====
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;   // 米/秒
    private Rigidbody rb;
    private Vector2 lastMove; // 保存上一次决策的输入

    public override void Initialize()
    {
        cfg = Config.Instance;

        // ==== 新增：抓刚体并防侧翻 ====
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // 确保不是 Kinematic
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void OnEpisodeBegin()
    {
        GameManager.Instance.ResetEnvironment();

        hp = cfg.agentMaxHP;
        timeSinceLastFood = 0f;

        //出生点
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((float)hp / cfg.agentMaxHP);  // observe the remaining hp
        sensor.AddObservation(timeSinceLastFood / cfg.hungerGraceSec);  // observe the left time to be hunger
        sensor.AddObservation(timeSinceLastFood / cfg.hardStarveTimeoutSec);  // observe the left time to starve to death
        sensor.AddObservation(cfg.hardStarvation ? 1f : 0f); // tell agent if the hardStarvation is on

        //To DO: add other observation
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 读入动作（-1~1）
        var ca = actions.ContinuousActions;
        float h = ca.Length > 0 ? ca[0] : 0f;
        float v = ca.Length > 1 ? ca[1] : 0f;

        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 1f) move.Normalize();
        lastMove = move; // 存起来，物理帧里用

        // 饥饿机制（柔性/硬性）——按帧给 shaping
        if (cfg.hungerEnabled)
        {
            float dt = Time.deltaTime;
            timeSinceLastFood += dt;

            if (cfg.hungerEnabled)
            {
                // 软性惩罚
                if (timeSinceLastFood > cfg.hungerGraceSec)
                    AddReward(cfg.hungerNegRewardPerSec * dt);

                // 硬性饿死
                if (cfg.hardStarvation && timeSinceLastFood >= cfg.hardStarveTimeoutSec)
                {
                    AddReward(cfg.starvationTerminalReward);
                    EndEpisode();                       // 通知训练器
                    GameManager.Instance.EndEpisode(); // 可选：让 GM 统计
                }
            }

        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;

        float h = 0f;
        float v = 0f;

        // 键盘 WASD / 方向键
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
        }

        // 归一化，避免对角线超过 1
        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (ca.Length >= 2)
        {
            ca[0] = move.x; // 水平
            ca[1] = move.y; // 垂直
        }
        
    }


    //
    private void FixedUpdate()
    {
        Debug.Log("running!");
        // 把二维输入映射到世界 xz
        Vector3 wishVel = new Vector3(lastMove.x, 0f, lastMove.y) * moveSpeed;

        // 简单：直接设速度（保留y速度以受重力影响）
        rb.linearVelocity = new Vector3(wishVel.x, rb.linearVelocity.y, wishVel.z);
    }

    //=========与食物、陷阱、捕食者交互逻辑===========
    public void OnAteFood()
    {
        AddReward(cfg.foodReward);
        if (cfg.hungerEnabled && cfg.hungerResetsOnFood)
        {
            timeSinceLastFood = 0f;
        }
        else
        {
            timeSinceLastFood = Mathf.Max(0f, timeSinceLastFood - cfg.hungerExtraGraceOnFood);
        }
    }

    public void OnHitTrap()
    {
        AddReward(cfg.trapPenalty);
        TakeDamage(cfg.trapDamage);
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;

        if (hp <= 0)
        {
            AddReward(cfg.deadPenalty);
            EndEpisode();
            GameManager.Instance.EndEpisode();
        }
    }



}
