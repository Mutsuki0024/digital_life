using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Assertions.Must;


public class MyAgent : Agent
{
    private int hp;
    private float timeSinceLastFood;
    GameConfig cfg;

    public override void Initialize()
    {
        cfg = Config.Instance;
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
        // 执行动作（移动/转向），略…

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
