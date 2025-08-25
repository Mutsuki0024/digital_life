using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject foodPrefab;
    public GameObject trapPrefab;

    public Transform foodPoolParent;  // 食物对象池的parent
    public Transform trapPoolParent;  // 陷阱对象池的parent
    private ObjectPool _foodPool;  //食物对象池
    private ObjectPool _trapPool;  //陷阱对象池
    //ptivate 

    private GameConfig _cfg;

    private void Awake()
    {
        _cfg = Config.Instance;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //DontDestroyOnLoad(gameObject); // 跨场景可选
    }

    private void Start()
    {
        Debug.Log("Game start.");
        _foodPool = new ObjectPool(foodPrefab, _cfg.foodCount, foodPoolParent);
        _trapPool = new ObjectPool(trapPrefab, _cfg.trapCount, trapPoolParent);
        ResetEnvironment();
    }

    private void Update()
    {

    }


    // ================食物陷阱管理================
    public void SpawnFood()
    {
        var go = _foodPool.Get();
        if (!go) return;

        go.transform.position = gameUtils.GetRandomSpawnPosition(_cfg.foodHeight, 1.5f);
    }

    public void ReturnFood(GameObject go) => _foodPool.Return(go);

    public void SpawnTrap()
    {
        var go = _trapPool.Get();
        if (!go) return;

        go.transform.position = gameUtils.GetRandomSpawnPosition(_cfg.trapHeight, 2.5f);
    }

    public void ReturnTrap(GameObject go) => _trapPool.Return(go);

    // ================游戏阶段管理================
    public void EndEpisode()
    {
        ResetEnvironment();
    }

    public void ResetEnvironment()
    {
        Debug.Log("RESET!!");
        // delete current spawns
        _foodPool.ReturnAllActive();
        _trapPool.ReturnAllActive();

        //initial spawns
        SpawnInitialObjects();
    }

    private void SpawnInitialObjects()
    {
        // 取出并铺点
        for (int i = 0; i < _cfg.foodCount; i++) SpawnFood();
        for (int i = 0; i < _cfg.trapCount; i++) SpawnTrap();
    }
}
