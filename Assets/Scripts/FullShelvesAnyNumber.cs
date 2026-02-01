using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class FullShelvesAnyNumber : MonoBehaviour
{
    [System.Serializable]
    class ShelfDefinition
    {
        public GameObject ShelfTypePrefab;
        public Vector3 UpOffsetFromBottom = new Vector3(-0.0110001564f, 1.774544f, 0.0320000052f);
    }

    [SerializeField]
    List<ShelfDefinition> Shelves = new List<ShelfDefinition>();

    public int ItemsPerShelf = 20;

    public float ShelfScale = 1.0f;
    public float ShelfLocalYRotation = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (ShelfDefinition shelfDefinition in Shelves)
        {
            SetupShelf(shelfDefinition.ShelfTypePrefab, shelfDefinition.UpOffsetFromBottom);
        }
    }

    void SetupShelf(GameObject shelf, Vector3 pos)
    {
        GameObject newShelf = Instantiate(shelf, this.transform);
        newShelf.transform.localPosition = pos;
        newShelf.GetComponent<Shelf>().PropCount = ItemsPerShelf;
        newShelf.transform.localRotation = Quaternion.AngleAxis(ShelfLocalYRotation, new Vector3(0.0f, 1.0f, 0.0f));
        newShelf.transform.localScale = new Vector3(ShelfScale, ShelfScale, ShelfScale);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
