using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hagusen
{
    //  Just a Generic Grid Class for XZ
    //Base Grid
    public class Grid<T>
    {

        private int width;
        private int height;
        private float cellSize;
        private Vector3 originPos;
        private T[,] grid;

        public Grid(int width, int height, float cellSize, Vector3 originPos)
        {

            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.originPos = originPos;

            grid = new T[width, height];

            ShowDebug();
        }



        private void ShowDebug()
        {

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(1); z++)
                {
                    Debug.DrawLine(GetWorldPos(x, z, true), GetWorldPos(x, z + 1, true), Color.white, 100f);
                    Debug.DrawLine(GetWorldPos(x, z, true), GetWorldPos(x + 1, z, true), Color.white, 100f);
                }
            }

        }



        private Vector3 GetWorldPos(int x, int z)
        {
            return new Vector3(x, 0, z) * cellSize + originPos; // XZ Dimensions
        }
        private Vector3 GetWorldPos(int x, int z, bool offset)
        {
            return new Vector3(x - .5f, 0, z - .5f) * cellSize + originPos; // XZ Dimensions
        }
        private Vector3 ToLocal(Vector3 worldPos){
            return worldPos - originPos;
        }

        ///////// Get sets 
        public void SetValue(int x, int z, T value)
        {
            if (x >= 0 && z >= 0 && x <= width && z <= height)
            {
                grid[x, z] = value;
            }
        }
        public void GetXZ(Vector3 worldpos, out int x, out int z)
        {
            worldpos = ToLocal(worldpos);
            x = Mathf.FloorToInt(worldpos.x / cellSize);
            z = Mathf.FloorToInt(worldpos.z / cellSize);
        }

        public void SetValue(Vector3 worldPos, T value)
        {
            int x, z;
            GetXZ(worldPos, out x, out z);
            SetValue(x, z, value);
        }

        public T GetValue(int x, int z)
        {
            if (x >= 0 && z >= 0 && x <= width && z <= height)
            {
                return grid[x, z];
            }
            else
            {
                return default(T);
            }
        }
        public T GetValue(Vector3 worldPos)
        {
            int x, z;
            GetXZ(worldPos, out x, out z);
            return GetValue(x, z);
        }
        public int GetWidth(){
            return width;
        }
        public int GetHeight(){
            return height;
        }
        public float GetCellSize(){
            return cellSize;
        }
        //

        public IEnumerable<T> LoopTrough(){

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(1); z++)
                {
                    yield return GetValue(x,z);
                }
            }

        }

        public void UpdateDebug()
        {
            ShowDebug();
        }

    }

}