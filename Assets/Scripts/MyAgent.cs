using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using System.Collections.Generic;
//using System.Numerics;


public class MyAgent : Agent
{
    private int hp;
    private float timeSinceLastFood;
    GameConfig cfg;
    private float episodeElapsedSec = 0f;    // 本回合已流逝的时间（秒）

    // ====运动参数====
    [Header("Movement")]
    private float moveSpeed;     // 前进速度 m/s
    private float turnSpeed;   // 转向速度 度/s
    public bool useLowPassOnApplied = true;

    private Rigidbody rb;

    // ===用于保存动作输入===
    private float turnInput;
    private float throttleInput;

    // ===动作连贯性===
    private Vector2 appliedAction = Vector2.zero;  // 实际用于控制的动作（平滑后）
    private bool hasPre = false;
    private Vector2 prevAction = Vector2.zero; // 上一步动作
    [Range(0f, 1f)] public float actionEMA = 0.2f; // 越大越平滑但更“钝”
    public float lambdaActionChange;

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
        lambdaActionChange = cfg.lambdaActionChange;

        // About sensor
        CreateRaySensors();

        // About Rigid
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // 确保不是 Kinematic
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    }

    public override void OnEpisodeBegin()
    {

        hp = cfg.agentMaxHP;
        timeSinceLastFood = 0f;
        hasPre = false;
        prevAction = Vector2.zero;
        appliedAction = Vector2.zero;
        episodeElapsedSec = 0;

        //出生点
        ResetAgentPos();
    }

    private void ResetAgentPos()
    {
        // 清空速度，避免带入上回合惯性
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 spawnPos = gameUtils.GetRandomSpawnPosition(cfg.agentHeight, 0.7f);

        // 随机朝向（只绕 Y 轴）
        float yaw = Random.Range(0f, 360f);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        // 应用位置与方向
        transform.SetPositionAndRotation(spawnPos, rot);

        // 清零上帧动作缓存，避免第一帧残留输入
        turnInput = 0f;
        throttleInput = 0f;
    }

    // =======观测、输入、输出处理=========
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((float)hp / cfg.agentMaxHP);  // observe the remaining hp
        sensor.AddObservation(timeSinceLastFood / cfg.hungerGraceSec);  // observe the left time to be hunger
        sensor.AddObservation(timeSinceLastFood / cfg.hardStarveTimeoutSec);  // observe the left time to starve to death
        sensor.AddObservation(cfg.hardStarvation ? 1f : 0f); // tell agent if the hardStarvation is on

        // 1) 朝向（用 sin/cos，而不是角度，避免2π不连续）
        Vector3 fwd = transform.forward;
        sensor.AddObservation(fwd.x);
        sensor.AddObservation(fwd.z); // 地面场景只要XZ平面

        // 2) 线速度（转到局部坐标），并做归一化
        Vector3 vLocal = transform.InverseTransformDirection(rb.linearVelocity);
        float vNorm = moveSpeed > 0f ? 1f / moveSpeed : 1f;
        sensor.AddObservation(Mathf.Clamp(vLocal.x * vNorm, -1f, 1f)); // 侧滑
        sensor.AddObservation(Mathf.Clamp(vLocal.z * vNorm, -1f, 1f)); // 前后速度

        // 3) 角速度（只要绕Y），归一化
        float yawRate = rb.angularVelocity.y;             // rad/s
        float yawRateNorm = Mathf.Deg2Rad * turnSpeed;    // 期望上界（把度/s转成rad/s）
        if (yawRateNorm <= 0f) yawRateNorm = 1f;
        sensor.AddObservation(Mathf.Clamp(yawRate / yawRateNorm, -1f, 1f));

        // 4) （可选）上一帧动作，有助于稳定与 credit assignment
        sensor.AddObservation(turnInput);     // [-1, 1]
        sensor.AddObservation(throttleInput); // [-1, 1]
        //Debug.Log("Collecting...");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 读取动作（-1~1）
        turnInput = (actions.ContinuousActions.Length > 0)
                    ? Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f) : 0f;
        throttleInput = (actions.ContinuousActions.Length > 1)
                    ? Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f) : 0f;

        Vector2 a = new Vector2(turnInput, throttleInput);

        if (hasPre)
        {
            float delta = (a - prevAction).sqrMagnitude; // L2^2
            AddReward(-lambdaActionChange * delta);
        }
        prevAction = a;
        hasPre = true;

        // 对输入动作做低通，减少物理抖动
        if (useLowPassOnApplied)
            appliedAction = actionEMA * a + (1f - actionEMA) * appliedAction;
        else
            appliedAction = a;


        // 饥饿机制（柔性/硬性）—— 按帧给 shaping
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
        //sensor debug
        //#if UNITY_EDITOR
        //if (Time.frameCount % 15 == 0)  // 每隔几帧
        //{
        //    var foods = GameObject.FindGameObjectsWithTag("Food");
        //    foreach (var f in foods)
        //        Debug.DrawLine(transform.position + Vector3.up * 1.0f,  // 与 StartVerticalOffset 对齐
        //                    f.transform.position, Color.yellow, 0.3f);
        //}
        //#endif

        // 超时检查
        episodeElapsedSec += Time.fixedDeltaTime;
        if (episodeElapsedSec >= cfg.episodeTimeLimitSec)
        {
            EndEpisode();
            GameManager.Instance.EndEpisode();
            // 一般无需再调用 GameManager.Instance.EndEpisode()，避免重复重置；
            // 因为 OnEpisodeBegin() 已经会调用 ResetEnvironment()。
        }
        float turnInput = appliedAction.x;
        float throttleInput = appliedAction.y;
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
        sensorForward.RaysPerDirection = 10;  //両側に10本のレイがあり、中央の一本を加えて全部21本
        sensorForward.MaxRayDegrees = 90f;  //前方扇形の角度
        sensorForward.RayLength = 25f;  //レイの長さ
        sensorForward.SphereCastRadius = 0.05f; //レイの半径
        sensorForward.RayLayerMask = rayLayerMask; //探索されるlayerを指定
        sensorForward.StartVerticalOffset = 0.1f; //地面と間違えて接するのをさけるために少し上げる
        sensorForward.EndVerticalOffset = 0.1f;


        // // ---------- B: 近距环形短射线 ----------
        // sensorNearRing = gameObject.AddComponent<RayPerceptionSensorComponent3D>();
        // sensorNearRing.SensorName = "Ray_B_NearRing";
        // sensorNearRing.DetectableTags = detectableTags;
        // sensorNearRing.RaysPerDirection = 15;       // 环形密一点
        // sensorNearRing.MaxRayDegrees = 180f;       // 180° + 两侧 = 360° 环形
        // sensorNearRing.RayLength = 5f;          // 贴身防漏
        // sensorNearRing.SphereCastRadius = 0.05f;
        // sensorNearRing.RayLayerMask = rayLayerMask;
        // sensorNearRing.StartVerticalOffset = 0.1f;
        // sensorNearRing.EndVerticalOffset = 0.1f;

        
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
