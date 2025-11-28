using UnityEngine;
using System.Collections.Generic;

public class InfiniteTerrainManager : MonoBehaviour
{
    [Header("설정")]
    public GameObject chunkPrefab; // TerrainChunk 스크립트가 붙은 프리팹
    public int chunkCount = 5; // 화면에 보여줄 지형 개수
    public float scrollSpeed = 10f; // 이동 속도 (자동차 속도)
    public float zOffsetIncrement = 0f; // 노이즈 연속성을 위한 누적값

    [Header("초기 위치")]
    public Vector3 startPos = new Vector3(-10, -5, 0); // 차창 밖 위치에 맞게 조절

    private List<GameObject> chunks = new List<GameObject>();
    private float chunkLength; // 지형 조각 하나의 실제 길이

    void Start()
    {
        // 1. 청크 길이 계산
        TerrainChunk tempChunk = chunkPrefab.GetComponent<TerrainChunk>();
        chunkLength = tempChunk.zSize * tempChunk.cellSize;

        // 2. 초기 청크 생성
        for (int i = 0; i < chunkCount; i++)
        {
            SpawnChunk(i * chunkLength);
        }
    }

    void Update()
    {
        // 3. 지형 이동 (모든 청크를 뒤로 이동)
        MoveChunks();
    }

    void SpawnChunk(float zPos)
    {
        GameObject newChunk = Instantiate(chunkPrefab, transform);
        newChunk.transform.position = startPos + new Vector3(0, 0, zPos);
        
        // 지형 생성 (펄린 노이즈 적용)
        TerrainChunk terrainScript = newChunk.GetComponent<TerrainChunk>();
        
        // zOffsetIncrement 값을 기반으로 노이즈 생성 (연속성 보장)
        // 월드 좌표 기반으로 노이즈 오프셋을 계산
        float noiseOffset = (zPos / chunkLength) * terrainScript.zSize;
        terrainScript.GenerateMesh(noiseOffset);

        chunks.Add(newChunk);
    }

    void MoveChunks()
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            GameObject chunk = chunks[i];
            
            // 뒤로 이동 (Vector3.back)
            chunk.transform.Translate(Vector3.back * scrollSpeed * Time.deltaTime);

            // 4. 화면 뒤로 넘어갔는지 확인 (재활용)
            // 카메라보다 충분히 뒤로 갔을 때 (-chunkLength)
            if (chunk.transform.localPosition.z < startPos.z - chunkLength)
            {
                RecycleChunk(chunk);
            }
        }
    }

    void RecycleChunk(GameObject chunk)
    {
        // 리스트의 맨 마지막 청크(가장 멀리 있는 청크) 찾기
        // (현재 chunk는 이미 뒤로 넘어갔으므로 제외하고 생각해야 함)
        // 가장 Z값이 큰 녀석을 찾아야 함
        float maxZ = -99999f;
        foreach(var c in chunks)
        {
            if (c.transform.localPosition.z > maxZ) maxZ = c.transform.localPosition.z;
        }

        // 맨 뒤로 이동 (약간의 오차 보정)
        Vector3 newPos = chunk.transform.localPosition;
        newPos.z = maxZ + chunkLength;
        chunk.transform.localPosition = newPos;

        // 펄린 노이즈 새로 생성 (새로운 지형처럼 보이게)
        TerrainChunk terrainScript = chunk.GetComponent<TerrainChunk>();
        
        // 현재 위치에 맞는 노이즈 오프셋 계산 (중요: 연속성을 위해)
        float noiseOffset = (newPos.z - startPos.z) / terrainScript.cellSize;
        terrainScript.GenerateMesh(noiseOffset);
    }
}