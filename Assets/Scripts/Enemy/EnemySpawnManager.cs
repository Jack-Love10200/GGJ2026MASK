using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class EnemySpawnManager : MonoBehaviour
{
    public EnemySpawningSettings EnemySettings;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetEnemyTypes();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetEnemyTypes()
    {
        EnemyMaskStackVisual[] allEnemies = FindObjectsByType<EnemyMaskStackVisual>(FindObjectsSortMode.None);

        Dictionary<int, int> changedEnemyIndicies = new Dictionary<int, int>(); // For saving enemies that have already been set

        int numIterations = 0;

        // For each type of special enemy to make
        foreach (EnemySpawningSettings.EnemyTypeSettings specialEnemyType in EnemySettings.SpecialEnemyTypes)
        {
            // Decide how many enemies of that type
            int EnemiesToChange = Random.Range(specialEnemyType.desiredNumberMin, specialEnemyType.desiredNumberMax);

            // Randomly set which enemies should change to that type
            for (int i = 0; i < EnemiesToChange; i++)
            {
                int indexToChange = 0;

                do 
                {
                    indexToChange = Random.Range(0, allEnemies.Length);

                    numIterations++;

                    if (numIterations > 1000)
                    {
                        print("EnemySpawnManager: SetEnemyTypes: Hit too long loop, aborting");
                        return;
                    }
                }
                while (changedEnemyIndicies.ContainsKey(indexToChange) == true); //While this enemy is already used


                // Set enemy type
                allEnemies[indexToChange].ClearRuntimeLayersNoDestroy();
                foreach (MaskDef mask in specialEnemyType.initialMasksBottomToTop)
                {
                    allEnemies[indexToChange].AddMask(mask);
                }


                // Set index as used so we don't change it to another type
                changedEnemyIndicies.Add(indexToChange, 1);

            }
        }
    }
}
