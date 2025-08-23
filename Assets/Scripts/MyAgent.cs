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
    [SerializeField] private float moveSpeed = 5f;     // 前进速度 m/s
    [SerializeField] private float turnSpeed = 180f;   // 转向速度 度/s
    private Rigidbody rb;
    // === 用于保存动作输入 ===
    private float turnInput;
    private float throttleInput;

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
        // 读取动作（-1~1）
        turnInput = (actions.ContinuousActions.Length > 0)
                    ? Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f) : 0f;
        throttleInput = (actions.ContinuousActions.Length > 1)
                    ? Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f) : 0f;

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

        float turn = 0f;   // 左右
        float throttle = 0f; // 上下

        if (Keyboard.current != null)
        {
            // 左右转向
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) turn -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) turn += 1f;
            // 前后油门
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) throttle += 1f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) throttle -= 1f;
        }

        ca[0] = Mathf.Clamp(turn, -1f, 1f);
        ca[1] = Mathf.Clamp(throttle, -1f, 1f);

    }

    //
    private void FixedUpdate()
    {
        // === 转向 ===
        float yawDelta = turnInput * turnSpeed * Time.fixedDeltaTime;
        if (Mathf.Abs(yawDelta) > 0f)
        {
            Quaternion deltaRot = Quaternion.Euler(0f, yawDelta, 0f);
            rb.MoveRotation(rb.rotation * deltaRot);
        }

        // === 前进/后退 ===
        Vector3 forwardVel = transform.forward * (throttleInput * moveSpeed);
        rb.linearVelocity = new Vector3(forwardVel.x, rb.linearVelocity.y, forwardVel.z);
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
