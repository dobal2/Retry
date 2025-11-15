using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 범용 오브젝트 풀 매니저
/// SetActive(false)로 비활성화된 오브젝트를 재사용합니다
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public bool autoExpand = true;
    }
    
    [Header("Pooled Object")]
    [SerializeField] private List<Pool> pools = new List<Pool>();
    
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolSettings;
    
    public static ObjectPool Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolSettings = new Dictionary<string, Pool>();
        
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            poolSettings[pool.tag] = pool;
            
            // 초기 풀 생성
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab);
                objectPool.Enqueue(obj);
            }
            
            poolDictionary.Add(pool.tag, objectPool);
        }
    }
    
    GameObject CreateNewObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }
    
    /// <summary>
    /// 풀에서 오브젝트를 가져와 활성화합니다
    /// </summary>
    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return null;
        }
        
        GameObject obj = null;
        
        // 비활성화된 오브젝트 찾기
        if (poolDictionary[tag].Count > 0)
        {
            obj = poolDictionary[tag].Dequeue();
        }
        else if (poolSettings[tag].autoExpand)
        {
            // 풀이 비어있으면 자동 확장
            obj = CreateNewObject(poolSettings[tag].prefab);
        }
        else
        {
            return null;
        }
        
        // 활성화 및 위치 설정
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        
        // 초기화 메서드가 있다면 호출
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }
        
        return obj;
    }
    
    /// <summary>
    /// 오브젝트를 풀로 반환합니다 (SetActive(false) 호출)
    /// </summary>
    public void Despawn(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return;
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        poolDictionary[tag].Enqueue(obj);
    }
    
    /// <summary>
    /// 모든 활성 오브젝트를 풀로 반환
    /// </summary>
    public void DespawnAll(string tag)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.activeSelf && obj.name.Contains(poolSettings[tag].prefab.name))
            {
                Despawn(tag, obj);
            }
        }
    }
}

/// <summary>
/// 풀에서 스폰될 때 초기화가 필요한 오브젝트가 구현하는 인터페이스
/// </summary>
public interface IPoolable
{
    void OnSpawn();
}