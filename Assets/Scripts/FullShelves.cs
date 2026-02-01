using UnityEngine;

public class FullShelves : MonoBehaviour
{
    public GameObject Shelf4;
    public GameObject Shelf3;
    public GameObject Shelf2;
    public GameObject Shelf1;

    public int ItemsPerShelf = 20;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupShelf(Shelf4, new Vector3(-0.0110001564f, 3.52654433f, 0.0320000052f));
        SetupShelf(Shelf3, new Vector3(-0.0110001564f, 2.652544f, 0.0320000052f));
        SetupShelf(Shelf2, new Vector3(-0.0110001564f, 1.774544f, 0.0320000052f));
        SetupShelf(Shelf1, new Vector3(-0.0110001564f, 0.9005442f, 0.0320000052f));
    }

    void SetupShelf(GameObject shelf, Vector3 pos)
    {
        GameObject newShelf = Instantiate(shelf, this.transform);
        newShelf.transform.localPosition = pos;
        newShelf.GetComponent<Shelf>().PropCount = ItemsPerShelf;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
