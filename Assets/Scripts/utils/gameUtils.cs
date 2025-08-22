using UnityEngine;
using UnityEngine.UIElements;

public static class gameUtils
{
    // 复用缓冲区，零GC
    private static readonly Collider[] _buf = new Collider[8];

    

    public static Vector3 GetRandomSpawnPosition(float y = 0.5f, float checkRadius = 0.5f)
    {
        // 可选：做标签过滤防止检测到地面
        // Unity 平面(Plane) 默认大小是 10x10，所以 localScale = 5,5 就代表 50x50
        while (true)
        {
            float x = Random.Range(-23f, 23f);
            float z = Random.Range(-23f, 23f);
            Vector3 position = new Vector3(x, y, z);

            // 衝突検査用のボックスを作る
            Vector3 halfExtents = new Vector3(checkRadius, 3f, checkRadius);

            int layerMask = LayerMask.GetMask("block");

            // 衝突検査
            int hit = Physics.OverlapBoxNonAlloc(position, halfExtents, _buf, Quaternion.identity, layerMask);

            if (hit == 0)
            {
                return position;
            }
            Debug.Log("retry!");
        }
    }
}
