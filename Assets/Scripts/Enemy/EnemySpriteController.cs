using UnityEngine;
using UnityEditor;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;

public class EnemySpriteController : MonoBehaviour
{
  [Header("References")]
  public SpriteRenderer personSprite;
  //public SpriteRenderer shirtSprite;
  public Shader EnemyShader;
  public Transform FaceSocket;

  [Header("Texture/Color Data")]
  public List<Texture2D> PeopleTextures;
  public List<Texture2D> ShirtTextures;
  public List<Color> ShirtColors;
  public List<Vector3> FaceOffsets;

  [Header("Material Settings")]
  public float PixelationStrength = 1.0f;
  public float ColorBands = 10.0f;
  public float garbleSpeed = 1.0f;
  public float WaveSpeed = 1.0f;
  public float WaveIntensity = 1.0f;

  private Material enemyMaterial;
  //private Material shirtMaterial;

  void Start()
  {

    enemyMaterial = new Material(EnemyShader);

    int TexIndex = Random.Range(0, PeopleTextures.Count);

    enemyMaterial.SetTexture("_PersonTex", PeopleTextures[TexIndex]);
    enemyMaterial.SetTexture("_ShirtTex", ShirtTextures[TexIndex]);
    enemyMaterial.SetColor("_TintColor", ShirtColors[Random.Range(0, ShirtColors.Count)]);

    FaceSocket.Translate(FaceOffsets[TexIndex]);

    //basic ssettings
    enemyMaterial.SetFloat("_PixelationStrength", PixelationStrength);
    enemyMaterial.SetFloat("_ColorBands", ColorBands);
    enemyMaterial.SetFloat("_GarbleSpeed", garbleSpeed);
    enemyMaterial.SetFloat("_WaveSpeed", WaveSpeed);
    enemyMaterial.SetFloat("_WaveIntensity", WaveIntensity);

    personSprite.material = enemyMaterial;
    //shirtSprite.material = shirtMaterial;
  }
}