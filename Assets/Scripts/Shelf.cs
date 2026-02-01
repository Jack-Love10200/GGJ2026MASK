using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Shelf : MonoBehaviour
{
    // public ////////////////////////////////////////////////
    public List<GameObject> propPrefabs;
    public int PropCount = 5;

    // private ///////////////////////////////////////////////
    private BoxCollider m_SpawnArea;
    private List<Bounds> m_PlacedBounds = new List<Bounds>();


    void Start()
    {
        m_SpawnArea = GetComponent<BoxCollider>();
        int maxAttempts = 30;
        float spawnAreaBottomY = m_SpawnArea.center.y - m_SpawnArea.size.y / 2f;
        float worldSpawnAreaBottomY = transform.TransformPoint(new Vector3(0, spawnAreaBottomY, 0)).y;

        for (int i = 0; i < PropCount; i++)
        {
            bool placed = false;
            int attempts = 0;

            while (!placed && attempts < maxAttempts) // keeps tryin to place until successful or max attempts reached
            {
                attempts++;

                int randomIndex = Random.Range(0, propPrefabs.Count);
                GameObject propPrefab = propPrefabs[randomIndex];
                Renderer prefabRenderer = propPrefab.GetComponentInChildren<Renderer>();
                Bounds propBounds = prefabRenderer.bounds;

                float halfX = m_SpawnArea.size.x / 2f;
                float halfZ = m_SpawnArea.size.z / 2f;

                Vector3 localPos = new Vector3(
                    Random.Range(-halfX, halfX),
                    spawnAreaBottomY,
                    Random.Range(-halfZ, halfZ)
                );
                localPos += m_SpawnArea.center;
                Vector3 worldPos = transform.TransformPoint(localPos);
                float propBottomOffset = prefabRenderer.bounds.min.y - propPrefab.transform.position.y;
                worldPos.y = worldSpawnAreaBottomY - propBottomOffset;
                propBounds.center = worldPos + (propBounds.center - propPrefab.transform.position);

                // check against all of the bounds we have placed
                bool overlaps = false;
                foreach (var p in m_PlacedBounds)
                {
                    if (propBounds.Intersects(p))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    Quaternion r = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    GameObject instance = Instantiate(propPrefab, worldPos, r, transform);
                    Renderer instanceRenderer = instance.GetComponentInChildren<Renderer>();

                    // adds the bounds to all of the bounds we have placed
                    if (instanceRenderer != null)
                        m_PlacedBounds.Add(instanceRenderer.bounds);
                    placed = true;
                }
            }
        }
    }

    void Update()
    {

    }
}