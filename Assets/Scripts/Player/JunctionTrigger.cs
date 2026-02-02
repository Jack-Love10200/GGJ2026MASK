using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
//using UnityEditor.Experimental.GraphView;
using System.Collections;

public class JunctionTrigger : MonoBehaviour
{

  [Serializable]
  public struct DirectionOption
  {
    public float angle;
    public InputDirection inputDirection;
  }

  public Transform Destination;
  public List<DirectionOption> OpenDirections;
  public List<JunctionTrigger> Siblings;

  public void Start()
  {
    
  }

  void OnTriggerEnter(Collider other)
  {

    Debug.Log("new trigger enter");

    GetComponent<BoxCollider>().enabled = false;
    foreach (JunctionTrigger tr in Siblings)
    {
      tr.GetComponent<BoxCollider>().enabled = false;
    }

    StartCoroutine(func());
  }

  IEnumerator func()
  {
    yield return new WaitForSeconds(10);

    GetComponent<BoxCollider>().enabled = true;
    foreach (JunctionTrigger tr in Siblings)
    {
      tr.GetComponent<BoxCollider>().enabled = true;
    }
  }

  public void Update()
  {
    
  }
}