using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hagusen;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshTest : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uv;

    


    Grid<bool> grid;

    // Start is called before the first frame update
    void Start()
    {
        grid = new Grid<bool>(1000, 1000, 1, Vector3.zero);

        for (int i = 0; i < grid.GetHeight(); i++)
        {
           grid.SetValue(3 ,i, true);
        }

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateDiscreteMeshGrid();
        UpdateMesh();
    }


    // loop trough grid to create Mesh
    // true => wall (a little higher)
    // false => floor 

    void CreateDiscreteMeshGrid()
    {
        float vertexOffset = grid.GetCellSize() * .5f;
        int xSize = grid.GetWidth(), zSize = grid.GetHeight();

        vertices = new Vector3[xSize * zSize * 4];
        triangles = new int[xSize * zSize * 6];
        uv = new Vector2[vertices.Length];


        int v, t;
        v = t = 0;

        for (int x = 0, i = 0; x < xSize; x++)
        {
            for (int z = 0; z < zSize; z++, i++)
            {
                var obj = grid.GetValue(x, z);


                    Vector3 cellOffset = new Vector3(x, 0, z) * grid.GetCellSize();

                    vertices[v] = new Vector3(-vertexOffset, 0, -vertexOffset) + cellOffset;
                    vertices[v + 1] = new Vector3(-vertexOffset, 0, vertexOffset) + cellOffset;
                    vertices[v + 2] = new Vector3(vertexOffset, 0, -vertexOffset) + cellOffset;
                    vertices[v + 3] = new Vector3(vertexOffset, 0, vertexOffset) + cellOffset;


                    triangles[t] = v;
                    triangles[t + 1] = triangles[t + 4] = v + 1;
                    triangles[t + 2] = triangles[t + 3] = v + 2;
                    triangles[t + 5] = v + 3;

                    uv[v] = new Vector2(0, 1);
                    uv[v+1] = new Vector2(.333f, 1);
                    uv[v+2] = new Vector2(0, 1-.333f);
                    uv[v+3] = new Vector2(.333f, 1- .333f);
                 

                if (obj == true)
                {
                    vertices[v] = vertices[v] + Vector3.up;
                    vertices[v+1] = vertices[v+1] + Vector3.up;
                    vertices[v+2] = vertices[v+2] + Vector3.up;
                    vertices[v+3] = vertices[v+3] + Vector3.up;
                    uv[v] = new Vector2(.5f, .5f);
                    uv[v+1] = new Vector2(.5f, 1);
                    uv[v+2] = new Vector2(1, .5f);
                    uv[v+3] = new Vector2(1, 1);
                }

                v += 4;
                t += 6;

            }
        }
    }

    void UpdateMesh()
    {

        mesh.Clear();


        vertices[0] = new Vector3(0,1,0);
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        
        mesh.RecalculateNormals();


    }
     void Update() {
        
        //mesh.RecalculateNormals();

    }

    private void OnDrawGizmos()
    {
        if (!(vertices is null))
        { 
            Gizmos.color = Color.red;
            foreach (var item in vertices)
            {

                //Gizmos.DrawSphere(item, .1f);
            }
        }
    }


}
