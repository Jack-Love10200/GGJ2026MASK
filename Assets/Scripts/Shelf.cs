using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Shelf : MonoBehaviour
{
    // public ////////////////////////////////////////////////
    public List<GameObject> propPrefabs;
    public int propCount = 5;

    // private ///////////////////////////////////////////////
    private BoxCollider spawnArea;
    private List<Bounds> placedBounds = new List<Bounds>();


    void Start()
    {
        spawnArea = GetComponent<BoxCollider>();
        int maxAttempts = 30;

        for (int i = 0; i < propCount; i++)
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

                float halfX = spawnArea.size.x / 2f;
                float halfZ = spawnArea.size.z / 2f;

                Vector3 localPos = new Vector3(
                    Random.Range(-halfX, halfX),
                    0,
                    Random.Range(-halfZ, halfZ)
                );
                localPos += spawnArea.center;
                Vector3 worldPos = transform.TransformPoint(localPos);

                propBounds.center = worldPos + (propBounds.center - propPrefab.transform.position);

                // check against all of the bounds we have placed
                bool overlaps = false;
                foreach (var p in placedBounds)
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
                        placedBounds.Add(instanceRenderer.bounds);
                    placed = true;
                }
            }
        }
    }

    void Update()
    {

    }
}