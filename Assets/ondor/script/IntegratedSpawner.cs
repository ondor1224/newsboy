using UnityEngine;

public class IntegratedSpawner : MonoBehaviour
{
    public static IntegratedSpawner Instance { get; private set; }

    [Header("프리팹 등록")]
    public GameObject[] obstaclePrefabs; // 장애물 (Low, H_l, H_r 등)
    public GameObject[] customerPrefabs; // 손님
    public GameObject[] itemPrefabs;     // 🌟 코인, 쉴드 프리팹을 여기에 넣으세요!

    [Header("스폰 시간 간격")]
    public float obstacleSpawnInterval = 2f;
    public float customerSpawnInterval = 3.5f;
    public float itemSpawnInterval = 1.5f; // 코인은 자주 나오게 설정

    [Header("스폰 위치 설정")]
    public float spawnZ = 20f;
    public float[] laneX = { -2.5f, 0f, 2.5f };

    [Header("보스전 연동 제어")]
    public bool isObstacleSpawnPaused = false;
    public bool isCustomerSpawnEnabled = true;

    private float obstacleTimer = 0f;
    private float customerTimer = 0f;
    private float itemTimer = 0f;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        float dt = Time.deltaTime;

        // 1. 장애물 스폰
        if (!isObstacleSpawnPaused)
        {
            obstacleTimer += dt;
            if (obstacleTimer >= obstacleSpawnInterval)
            {
                SpawnObject(obstaclePrefabs);
                obstacleTimer = 0f;
            }
        }

        // 2. 손님 스폰
        if (isCustomerSpawnEnabled)
        {
            customerTimer += dt;
            if (customerTimer >= customerSpawnInterval)
            {
                SpawnObject(customerPrefabs);
                customerTimer = 0f;
            }
        }

        // 3. 코인/아이템 스폰
        itemTimer += dt;
        if (itemTimer >= itemSpawnInterval)
        {
            SpawnObject(itemPrefabs);
            itemTimer = 0f;
        }
    }

    void SpawnObject(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        int randomPrefab = Random.Range(0, prefabs.Length);
        int randomLane = Random.Range(0, laneX.Length);

        Vector3 spawnPos = new Vector3(laneX[randomLane], 0.5f, spawnZ);
        Instantiate(prefabs[randomPrefab], spawnPos, Quaternion.identity);
    }
}