using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(TerrainDetailsController))]
public class TerrainMeshGeneration : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private TerrainDetailsController detailController;
    [SerializeField] private GameObject snowEmitter; //Move into own script

    //[[Constants]]
    private const int TERRAIN_WIDTH = 200;
    private const int TERRAIN_DEPTH = 200;
    private const int TERRAIN_AREA = TERRAIN_WIDTH * TERRAIN_DEPTH;
    private const int TERRAIN_VERTEX_COUNT = (TERRAIN_WIDTH + 1) * (TERRAIN_DEPTH + 1);
    private const int TERRAIN_SCALE = 1;

    private const int MOUNTAIN_PEAK_LEVEL = 30;
    private const int WATER_LEVEL = -1;

    //[[HeightMap]]
    //HM Noise
    private const float HM_PERLIN_POWER = 1f;
    private const float HM_PERLIN_REFINEMENT = 0.8f;
    private const float HM_PERLIN_NOISE_BEGIN_HEIGHT = 0f;
    private const float HM_PERLIN_NOISE_END_HEIGHT = 10f;

    //[[non-const constants]]
    private Vector2Int min_vertex_index = new(0, 0);
    private Vector2Int max_vertex_index = new(TERRAIN_WIDTH, TERRAIN_DEPTH);
    private MinMaxInt min_max_vertex_y = new(-15,50);


    //[[Variables]]
    [SerializeField] private TerrainBumpInfo[] allTerrainBumps;

    //Mesh
    private Dictionary<Vector2Int, int> verticiesDictionary;
    private Vector3[] vertices;
    private int[] triangles;
    private float[] heightMap;
    private Mesh terrainMesh;

    //Colour
    [SerializeField] private Gradient terrainColourGradient;
    private Color[] colorMap;


    // Start is called before the first frame update
    void Awake()
    {
        terrainMesh = new Mesh();
        terrainMesh.name = "Terrain " + name;
        meshFilter.sharedMesh = terrainMesh;
    }

    private void OnEnable()
    {
        //Run after awake
        ClearTerrain();
        GenerateTerrain();
    }

    private void ClearTerrain()
    {
        verticiesDictionary = new Dictionary<Vector2Int, int>();
        vertices = new Vector3[TERRAIN_VERTEX_COUNT];
        triangles = new int[TERRAIN_AREA * 6];
        heightMap = new float[TERRAIN_VERTEX_COUNT];

        colorMap = new Color[TERRAIN_VERTEX_COUNT];
    }

    private void GenerateTerrain()
    {

        //Set basic mesh data
        GenerateMeshArray();

        //Update heights
        GenerateHeightMap();
        ApplyHeightMap();

        //Change vert colours
        GenerateColourMap();

        //Calc triangles
        GenerateTriangles();

        //Change mesh
        UpdateTerrainMesh();

        //Add in veg
        detailController.GenerateVegetation(vertices);

        //Add weather
        GenerateWeather();

    }

    private void GenerateMeshArray()
    {
        
        for(int i = 0, z = 0; z <= TERRAIN_DEPTH; z++)
        {
            for(int x = 0; x <= TERRAIN_WIDTH; x++, i++)
            {
                //Generate individual points
                Vector3 point = new (x, 0, z);
                Vector2Int pointV2 = new (x, z);
                verticiesDictionary.Add(pointV2, i);
                vertices[i] = point;
            }
        }

    }

    private void ApplyHeightMap()
    {

        foreach(KeyValuePair<Vector2Int,int> pair in verticiesDictionary)
        {

            vertices[pair.Value].y += heightMap[pair.Value];

        }

    }

    //Move this later into its own script
    private void GenerateWeather()
    {

        foreach (KeyValuePair<Vector2Int, int> pair in verticiesDictionary)
        {
            Vector3 offsetVert = vertices[pair.Value] + transform.position;
            if (offsetVert.y >= MOUNTAIN_PEAK_LEVEL)
            {
                Instantiate(snowEmitter, offsetVert, new(), transform);
            }

        }

    }
    //

    private void GenerateHeightMap()
    {

        //Generate each type of bump
        foreach(TerrainBumpInfo bump in allTerrainBumps)
        {

            //Draw bumps and return their positional data
            CircleInfo[] createdBumps = GenerateCircularTerrainBumps(bump.minMaxBumpsHeight.max, bump.numberOfBumps, bump.yDirection);

            if (bump.linkToSameTypeOfBump)
            {

                //Bumps want to link together eg a river
                GenerateBumpLinks(createdBumps, bump);

            }

        }

        GeneratePerilinNoise();

    }

    private void GeneratePerilinNoise()
    {
        
        for(int i = 0; i <= TERRAIN_AREA; i++)
        {
            if (heightMap[i] >= HM_PERLIN_NOISE_BEGIN_HEIGHT)
            {
                //Calculate height map
                float multiplier = Mathf.Clamp(heightMap[i] / HM_PERLIN_NOISE_END_HEIGHT, 0, 1);
                float noise = GetPerlinNoiseHeight(i);
                float changeBy = noise * HM_PERLIN_POWER * multiplier;

                //Update height map
                heightMap[i] += changeBy;

            }

        }

    }

    private float GetPerlinNoiseHeight(int vertIndex)
    {

        float xNoise = vertices[vertIndex].x / HM_PERLIN_REFINEMENT;
        float zNoise = vertices[vertIndex].z / HM_PERLIN_REFINEMENT;

        return Mathf.PerlinNoise(xNoise, zNoise);

    }

    private void GenerateBumpLinks(CircleInfo[] bumps, TerrainBumpInfo bump)
    {

        //Compare each same type of bump to one another to determine if they can be linked
        for(int bumpIndex = 0;  bumpIndex < bumps.Length; bumpIndex++)
        {

            for(int secondaryBumpIndex =  0; secondaryBumpIndex < bumps.Length; secondaryBumpIndex++)
            {

                if (CheckIfBumpLinkIsValid(bumps, bumpIndex, secondaryBumpIndex, bump))
                {

                    DrawBumpLink(bumps[bumpIndex], bumps[secondaryBumpIndex], bump);

                }

            }

        }

    }

    private bool CheckIfBumpLinkIsValid(CircleInfo[] bumps, int bumpIndex, int secondaryBumpIndex, TerrainBumpInfo bump)
    {

        if (bumpIndex != secondaryBumpIndex)
        {

            //Distance between 2 bumps is close enough
            if (Vector2.Distance(bumps[bumpIndex].Centre, bumps[secondaryBumpIndex].Centre) < bump.maxLinkDistance)
            {

                //Bump link is valid
                return true;

            }

        }

        //Bump link is invalid
        return false;

    }

    private void DrawBumpLink(CircleInfo startBump, CircleInfo targetBump, TerrainBumpInfo bump)
    {

        Vector2Int AB = targetBump.Centre - startBump.Centre;

        Vector2 point;

        //Draw a line of bumps from start to target to link bumps of the same type (such as bumps)
        for(float i = 0.1f; i < 1f; i += 0.025f)
        {

            //Calculate how far along the line we are
            point = startBump.Centre + (Vector2)AB * i;

            //Draw
            DrawCircularTerrainBump(new(new Vector2Int((int)point.x, (int)point.y), bump.minMaxBumpsHeight.max), bump.minMaxBumpsHeight.min * -1, -1);

        }

    }

    private CircleInfo[] GenerateCircularTerrainBumps(int maxHeight, int bumpCount, float multiplier)
    {

        CircleInfo[] bumpInfos = new CircleInfo[bumpCount];

        for (int i = 0; i < bumpCount; i++)
        {

            //Randomise bump height
            int maxBumpHeight = maxHeight + UnityEngine.Random.Range(-maxHeight / 2, maxHeight + maxHeight / 4);

            //Get bump position
            int xPosRdm = UnityEngine.Random.Range(maxBumpHeight + 1, TERRAIN_WIDTH - maxBumpHeight - 1);
            int zPosRdm = UnityEngine.Random.Range(maxBumpHeight, TERRAIN_DEPTH - maxBumpHeight - 1);
            Vector2Int mountainPositionAsVector2Int = new(xPosRdm, zPosRdm);
            
            //Generate circle info for the bump and store it for later
            CircleInfo bumpInfo = new(mountainPositionAsVector2Int, maxBumpHeight);
            bumpInfos[i] = bumpInfo;

            //Draw the bump
            DrawCircularTerrainBump(bumpInfo, maxBumpHeight, multiplier);

        }

        return bumpInfos;

    }

    private void DrawCircularTerrainBump(CircleInfo circle, int maxHeight, float multiplier)
    {

        Vector2Int Centre = circle.Centre;
        int Radius = circle.Radius;
        double rad2 = System.Math.Pow(Radius, 2);

        //Calculate top and left of the circle
        int left = Centre.x - Radius;
        int top = Centre.y - Radius;

        //Iterate over each point in the range of far left/top to centre
        for (int x = left; x <= Centre.x; x++)
        {
            for (int y = top; y <= Centre.y; y++)
            {

                //Calculate left and top 1 whole circle away
                int farLeft = x - Centre.x;
                int farTop = y - Centre.y;

                //x2 + y2 <= r2 (in circle) [Pythagorus] 
                double location = System.Math.Pow(farLeft, 2) + System.Math.Pow(farTop, 2);
                if (location <= rad2)
                {
                    int xFlipped = Centre.x - farLeft;
                    int yFlipped = Centre.y - farTop;

                    //Flip quadrant in each direction, convert points into positions in the vertices array
                    HashSet<int> UniqueCoords = new HashSet<int>(4) { };
                    UniqueCoords.Add(PointToVertexPosition(new(x, y)));
                    UniqueCoords.Add(PointToVertexPosition(new(xFlipped, yFlipped)));
                    UniqueCoords.Add(PointToVertexPosition(new(x, yFlipped)));
                    UniqueCoords.Add(PointToVertexPosition(new(xFlipped, y)));

                    //Calculate change in height based on distance from center
                    float distanceFromCenterAsPercent = (float) ((rad2 - location) / rad2);
                    float changeInHeight = maxHeight * distanceFromCenterAsPercent * multiplier;

                    //Update the XY coords, ensuring same coord isn't updated twice
                    foreach(int index in  UniqueCoords)
                    {

                        heightMap[index] += changeInHeight;
                        heightMap[index] = Mathf.Clamp(heightMap[index], min_max_vertex_y.min, min_max_vertex_y.max);

                    }

                }
            }
        }
    }

    private void GenerateColourMap()
    {

        for(int i = 0; i <= TERRAIN_AREA; i++)
        {

            //height = y coord as percent between water and mountain level
            float height = Mathf.InverseLerp(WATER_LEVEL, MOUNTAIN_PEAK_LEVEL, heightMap[i]);
            colorMap[i] = terrainColourGradient.Evaluate(Mathf.Clamp(height, 0f, 1f));

        }

    }

    private int PointToVertexPosition(Vector2Int point)
    {

        point.Clamp(min_vertex_index, max_vertex_index);
        return verticiesDictionary[point];

    }

    private void GenerateTriangles()
    {
        for(int z = 0, tris = 0, vert = 0; z < TERRAIN_DEPTH; z++, vert++)
        {
            for(int x = 0; x < TERRAIN_WIDTH; x++, tris += 6, vert++)
            {
                //1 square = 2 triangles, so requires 6 verts
                triangles[tris] = vert;
                triangles[tris + 1] = vert + TERRAIN_WIDTH + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + TERRAIN_WIDTH + 1;
                triangles[tris + 5] = vert + TERRAIN_WIDTH + 2;
            }
        }

    }

    private void UpdateTerrainMesh()
    {

        terrainMesh.Clear();

        //Update geom
        terrainMesh.SetVertices(vertices);
        terrainMesh.SetTriangles(triangles, 0);

        terrainMesh.colors = colorMap;

        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateTangents();

        //reassign mesh
        meshCollider.sharedMesh = terrainMesh;

        transform.localScale = Vector3.one * TERRAIN_SCALE;

    }
}
