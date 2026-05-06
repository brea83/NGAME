using UnityEngine;
using NGAME;
using System.Collections.Generic;

public class SpawnerDemo : MonoBehaviour, ISpawnPoint
{
    [SerializeField]
    private List<SO_SpawnTypeTag> m_AllowedSpawnTypes;
    public List<SO_SpawnTypeTag> AllowedSpawnableTypes { get => m_AllowedSpawnTypes; set => m_AllowedSpawnTypes = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public SpawnerData GetSpawnerData()
    {
        SpawnerData result = new SpawnerData() { Name = name, Position = transform.position, ValidTypes = m_AllowedSpawnTypes };
        return result;
    }
}
