using UnityEngine;
using System.Collections.Generic;

public class IntegratedSpawner : MonoBehaviour
{
    public static IntegratedSpawner Instance { get; private set; }

    [Header("프리팹 등록")]
    public GameObject[] obstaclePrefabs; // 장애물 (마차, 화살표 박스 등)
    public GameObject[] customerPrefabs; // 손님
    public GameObject[] itemPrefabs;     // 코인, 쉴드 프리팹
    public GameObject coinPrefab;        // 코인 프리팹

    [Header("🌟 전체 속도 및 웨이브 간격 (기본값)")]
    public float globalMoveSpeed = 10f;  // 현재 적용 중인 전체 이동 속도
    public float waveInterval = 2.2f;    // 현재 적용 중인 웨이브 생성 간격
    public float spawnZ = 25f;           // 생성되는 앞쪽 거리
    public float[] laneX = { -5f, 0f, 5f }; // 3차선 X좌표

    [Header("🔥 코인 10개당 난이도 상승 설정")]
    public int coinsPerLevelUp = 10;          // 몇 개 모을 때마다 난이도가 오를지 (기본 10개)
    public int currentDifficultyLevel = 0;    // 현재 난이도 단계 (인스펙터 확인용)
    public float speedUpPerLevel = 1.5f;      // 1단계당 빨라지는 속도 (+1.5)
    public float maxMoveSpeed = 25f;          // 최고 속도 제한 (너무 빨라서 못 피하는 현상 방지)
    public float intervalReducePerLevel = 0.12f; // 1단계당 줄어드는 스폰 시간 간격 (-0.12초)
    public float minWaveInterval = 0.9f;      // 최소 스폰 간격 제한

    [Header("🌟 코인 줄 & 장애물 위 코인 설정")]
    public int coinCount = 5;            // 한 줄에 나오는 코인 개수 (5개)
    public float coinSpacing = 2.5f;     // 코인 앞뒤 간격
    public float groundCoinY = 1.0f;     // 바닥 코인 높이
    public float obstacleRoofY = 3.8f;   // 장애물 위 코인 높이
    [Range(0f, 1f)]
    public float coinOnObstacleChance = 0.5f; // 장애물 위에 코인이 나올 확률

    [Header("보스전 연동 제어")]
    public bool isObstacleSpawnPaused = false;
    public bool isCustomerSpawnEnabled = true;

    private float baseMoveSpeed;
    private float baseWaveInterval;
    private float waveTimer = 0f;
    private int waveCount = 0;

    private void Awake()
    {
        Instance = this;

        // 게임 시작 시 원래 설정해 둔 기본 속도와 간격을 기억해 둠
        baseMoveSpeed = globalMoveSpeed;
        baseWaveInterval = waveInterval;

        if (coinPrefab == null && itemPrefabs != null)
        {
            foreach (var item in itemPrefabs)
            {
                if (item != null) { coinPrefab = item; break; }
            }
        }
    }

    private void Start()
    {
        // ★ GameManager의 코인 획득 이벤트에 난이도 체크 함수를 연결!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinUpdated += CheckDifficultyByCoins;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinUpdated -= CheckDifficultyByCoins;
        }
    }

    // ★ 코인을 먹을 때마다 자동으로 호출되는 함수
    private void CheckDifficultyByCoins(int totalCoins, int progressCoins)
    {
        if (coinsPerLevelUp <= 0) return;

        // 예: 코인 0~9개 = 0레벨, 10~19개 = 1레벨, 20~29개 = 2레벨...
        int targetLevel = totalCoins / coinsPerLevelUp;

        // 10개 단위를 돌파해서 레벨이 올랐을 때만 실행!
        if (targetLevel > currentDifficultyLevel)
        {
            currentDifficultyLevel = targetLevel;

            // 1. 이동 속도 증가 (최대 속도 maxMoveSpeed를 넘지 않게 제한)
            globalMoveSpeed = Mathf.Min(maxMoveSpeed, baseMoveSpeed + (currentDifficultyLevel * speedUpPerLevel));

            // 2. 스폰 시간 간격 단축 (최소 간격 minWaveInterval보다 작아지지 않게 제한)
            waveInterval = Mathf.Max(minWaveInterval, baseWaveInterval - (currentDifficultyLevel * intervalReducePerLevel));

            // 3. ★ 중요: 이미 화면에 나와서 달려오고 있는 장애물·코인·손님들의 속도도 즉시 동기화! (추돌 방지)
            SyncActiveObjectsSpeed();

            Debug.Log($"🔥 난이도 UP! [Lv.{currentDifficultyLevel}] (코인 {totalCoins}개 돌파!) -> 현재 속도: {globalMoveSpeed}, 스폰 간격: {waveInterval:F2}초");
        }
    }

    // 현재 맵에 떠 있는 모든 오브젝트의 속도를 새 속도로 즉시 맞춰주는 함수
    private void SyncActiveObjectsSpeed()
    {
        foreach (var obs in FindObjectsOfType<Obstacle>())
        {
            obs.speed = globalMoveSpeed;
        }
        foreach (var coin in FindObjectsOfType<Coin>())
        {
            coin.speed = globalMoveSpeed;
        }
        foreach (var cust in FindObjectsOfType<Customer>())
        {
            cust.moveSpeed = globalMoveSpeed;
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        waveTimer += Time.deltaTime;
        if (waveTimer >= waveInterval)
        {
            SpawnCleanWave();
            waveTimer = 0f;
        }
    }

    void SpawnCleanWave()
    {
        waveCount++;

        List<int> lanes = new List<int> { 0, 1, 2 };
        for (int i = 0; i < lanes.Count; i++)
        {
            int rnd = Random.Range(i, lanes.Count);
            int temp = lanes[i];
            lanes[i] = lanes[rnd];
            lanes[rnd] = temp;
        }

        int obstacleLane1 = lanes[0];
        int obstacleLane2 = lanes[1];
        int itemLane = lanes[2];

        if (!isObstacleSpawnPaused)
        {
            bool putCoinOnTop = (Random.value <= coinOnObstacleChance);
            SpawnObstacleInLane(obstacleLane1, putCoinOnTop);

            // ★ 난이도가 오를수록 장애물이 2차선에 동시에 나올 확률도 조금씩 증가! (기본 40% -> 최대 85%)
            float doubleObstacleChance = Mathf.Clamp(0.4f + (currentDifficultyLevel * 0.08f), 0.4f, 0.85f);
            if (Random.value < doubleObstacleChance)
            {
                SpawnObstacleInLane(obstacleLane2, false);
            }
        }

        if (isCustomerSpawnEnabled && waveCount % 3 == 0 && HasValidPrefab(customerPrefabs))
        {
            SpawnCustomerInLane(itemLane);
        }
        else
        {
            SpawnCoinLine(laneX[itemLane], groundCoinY);
        }
    }

    void SpawnObstacleInLane(int laneIdx, bool withCoinsOnTop)
    {
        GameObject prefab = GetRandomValidPrefab(obstaclePrefabs);
        if (prefab == null) return;

        float xPos = laneX[laneIdx];
        Vector3 spawnPos = new Vector3(xPos, prefab.transform.position.y, spawnZ);

        GameObject obsObj = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        Obstacle obsScript = obsObj.GetComponent<Obstacle>();
        if (obsScript != null)
        {
            obsScript.speed = globalMoveSpeed;
            obsScript.SetupObstacleAsset();
        }

        bool isLow = (obsScript == null || obsScript.type == ObstacleType.Low);
        if (withCoinsOnTop && isLow)
        {
            Physics.SyncTransforms();
            Collider col = obsObj.GetComponentInChildren<Collider>();
            float roofY = obstacleRoofY;
            if (col != null && col.bounds.max.y + 0.8f > roofY)
            {
                roofY = col.bounds.max.y + 0.8f;
            }

            SpawnCoinLine(xPos, roofY);
        }
    }

    void SpawnCoinLine(float xPos, float yPos)
    {
        GameObject targetCoin = coinPrefab != null ? coinPrefab : GetRandomValidPrefab(itemPrefabs);
        if (targetCoin == null) return;

        float startOffsetZ = -((coinCount - 1) * coinSpacing) * 0.5f;

        for (int i = 0; i < coinCount; i++)
        {
            float zPos = spawnZ + startOffsetZ + (i * coinSpacing);
            Vector3 coinPos = new Vector3(xPos, yPos, zPos);

            GameObject c = Instantiate(targetCoin, coinPos, targetCoin.transform.rotation);
            Coin coinScript = c.GetComponent<Coin>();
            if (coinScript == null) coinScript = c.AddComponent<Coin>();

            coinScript.speed = globalMoveSpeed;
        }
    }

    void SpawnCustomerInLane(int laneIdx)
    {
        GameObject prefab = GetRandomValidPrefab(customerPrefabs);
        if (prefab == null) return;

        Vector3 spawnPos = new Vector3(laneX[laneIdx], prefab.transform.position.y, spawnZ);
        GameObject custObj = Instantiate(prefab, spawnPos, prefab.transform.rotation);

        Customer custScript = custObj.GetComponent<Customer>();
        if (custScript != null)
        {
            custScript.moveSpeed = globalMoveSpeed;
        }
    }

    bool HasValidPrefab(GameObject[] prefabs)
    {
        return GetRandomValidPrefab(prefabs) != null;
    }

    GameObject GetRandomValidPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        int startIdx = Random.Range(0, prefabs.Length);
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject candidate = prefabs[(startIdx + i) % prefabs.Length];
            if (candidate != null) return candidate;
        }
        return null;
    }
}
