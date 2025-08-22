using UnityEngine;

public class Config
{
    private static GameConfig _instance;

    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("configs/config");
                if (_instance == null)
                {
                    Debug.LogError("GameConfig not found! Make sure it's in Resources/Configs/GameConfig.asset");
                }
            }
            return _instance;
        }
    }

}
