using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class MyAgent : Agent
{
    private int hp;
    private float timeSinceLastFood;
    GameConfig cfg;

    // ====运动参数====
    [Header("Movement")]
    private float moveSpeed;     // 前进速度 m/s
    private float turnSpeed;   // 转向速度 度/s
    private Rigidbody rb;

    // ===用于保存动作输入===
    private float turnInput;
    private float throttleInput;

    // ===sensor===
    [Header("Ray Sensors")]
    [SerializeField] private LayerMask rayLayerMask;
    private RayPerceptionSensorComponent3D sensorForward; // 远距前向扇形
    private RayPerceptionSensorComponent3D sensorNearRing; // 近距环形

    public override void Initialize()
    {
        cfg = Config.Instance;

        // エージェントの運動パラメータを初期化
        moveSpeed = cfg.moveSpeed;
        turnSpeed = cfg.turnSpeed;

        // About sensor
        CreateRaySensors();

        // About Rigid
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

    // =======观测、输入、输出处理=========
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

    //============sensor======================
    private void CreateRaySensors()
    {
        var detectableTags = new List<string> { "Food", "Trap", "Wall" };

        // 前向远距扇形sensor
        sensorForward = gameObject.AddComponent<RayPerceptionSensorComponent3D>();
        sensorForward.SensorName = "Ray_Forward";
        sensorForward.DetectableTags = detectableTags;
        sensorForward.RaysPerDirection = 6;  //両側に六本のレイがあり、中央の一本を加えて全部十三本
        sensorForward.MaxRayDegrees = 90f;  //前方扇形の角度
        sensorForward.RayLength = 12f;  //レイの長さ
        sensorForward.SphereCastRadius = 0.05f; //レイの半径
        sensorForward.RayLayerMask = rayLayerMask; //探索されるlayerを指定
        sensorForward.StartVerticalOffset = 0.1f; //地面と間違えて接するのをさけるために少し上げる
        sensorForward.EndVerticalOffset = 0.1f;


        // ---------- B: 近距环形短射线 ----------
        sensorNearRing = gameObject.AddComponent<RayPerceptionSensorComponent3D>();
        sensorNearRing.SensorName = "Ray_B_NearRing";
        sensorNearRing.DetectableTags = detectableTags;
        sensorNearRing.RaysPerDirection = 8;       // 环形密一点
        sensorNearRing.MaxRayDegrees = 180f;       // 180° + 两侧 = 360° 环形
        sensorNearRing.RayLength = 1.25f;          // 贴身防漏
        sensorNearRing.SphereCastRadius = 0.05f;
        sensorNearRing.RayLayerMask = rayLayerMask;
        sensorNearRing.StartVerticalOffset = 0.1f;
        sensorNearRing.EndVerticalOffset = 0.1f;

        
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
