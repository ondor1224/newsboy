using UnityEngine;
using System.Collections.Generic;

public class MapLoopManager : MonoBehaviour
{
    [Header("맵 블록 설정")]
    public GameObject[] mapBlockPrefabs; // 바닥 및 건물이 포함된 맵 블록 프리팹들
    public float blockLength = 50f;      // 맵 블록 하나의 Z축 길이
    public int initialBlockCount = 3;    // 화면에 유지할 맵 블록 개수
    public float spawnZOffset = 0f;      // 시작 위치 조정용

    private List<GameObject> activeBlocks = new List<GameObject>();
    private float nextSpawnZ = 0f;

    void Start()
    {
        nextSpawnZ = spawnZOffset;

        // 게임 시작 시 초기 맵 블록들을 연속으로 배치
        for (int i = 0; i < initialBlockCount; i++)
        {
            SpawnMapBlock();
        }
    }

    void Update()
    {
        if (activeBlocks.Count == 0) return;

        // 가장 뒤에 있는(플레이어를 한참 지나친) 맵 블록 확인
        GameObject firstBlock = activeBlocks[0];

        // 플레이어 기준 뒤쪽으로 블록 길이너머까지 갔다면 삭제 후 앞에 새 블록 배치
        if (firstBlock.transform.position.z < -blockLength)
        {
            activeBlocks.RemoveAt(0);
            Destroy(firstBlock);

            SpawnMapBlock();
        }
    }

    void SpawnMapBlock()
    {
        if (mapBlockPrefabs == null || mapBlockPrefabs.Length == 0) return;

        // 랜덤으로 맵 블록 디자인 선택
        int randomIndex = Random.Range(0, mapBlockPrefabs.Length);
        GameObject selectedPrefab = mapBlockPrefabs[randomIndex];

        // 새로운 맵 블록 생성 및 스크립트 자동 부착
        GameObject newBlock = Instantiate(selectedPrefab, new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity);

        if (newBlock.GetComponent<MapBlock>() == null)
        {
            newBlock.AddComponent<MapBlock>();
        }

        activeBlocks.Add(newBlock);

        // 다음 블록이 생성될 Z 위치를 블록 길이만큼 앞으로 이동
        nextSpawnZ += blockLength;
    }
}