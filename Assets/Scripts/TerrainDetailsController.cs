using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct VegetationGroup
{
    public GameObject[] objects;
    public MinMaxInt boundaries;
    public float density;
}

public class TerrainDetailsController : MonoBehaviour
{
    [SerializeField] private VegetationGroup[] vegetation;

    public void GenerateVegetation(Vector3[] vertices)
    {
        for(int i = 0; i < vertices.Length; i++)
        {
            for(int vegi = 0;  vegi < vegetation.Length; vegi++)
            {
                Vector3 offsetVert = vertices[i] + transform.position;
                if (offsetVert.y >= vegetation[vegi].boundaries.min && offsetVert.y < vegetation[vegi].boundaries.max && UnityEngine.Random.Range(0f, 1f) <= vegetation[vegi].density)
                {
                    PlaceVeg(offsetVert, vegetation[vegi]);
                }
            }
        }
    }

    private void PlaceVeg(Vector3 pos, VegetationGroup vegGroup)
    {
        Instantiate(vegGroup.objects[UnityEngine.Random.Range(0, vegGroup.objects.Length)], pos, new(0, UnityEngine.Random.Range(0, 1), 0, 0), transform);
    }
}
