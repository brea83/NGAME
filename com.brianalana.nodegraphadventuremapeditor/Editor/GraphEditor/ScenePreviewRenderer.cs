using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NGAME.Editor
{
    public class ScenePreviewRenderer
    {
        public static Texture2D WriteTexture(Scene aScene, SceneData sceneData, int renderTextureHeight)
        {
            SceneBounds bounds = sceneData.Bounds;

            Camera camera = InitPreviewCamera(aScene, bounds);

            Texture2D result = DrawScene(bounds, camera, renderTextureHeight);
            Color[] pixels = result.GetPixels();
            Vector2Int size = new Vector2Int(result.width, result.height);
            DrawSpawners(sceneData, sceneData.SpawnPoints, result, size, camera);
            DrawConnections(sceneData, sceneData.UniqueConnectionObjects, result, size, camera);
            result.Apply(false);
            return result;
        }

        private static Camera InitPreviewCamera(Scene aScene, SceneBounds bounds)
        {
            
            Camera camera = aScene.GetRootGameObjects()[0].GetComponentInChildren<Camera>();
            if(camera == null)
            {
                return null;
            }
            camera.cameraType = CameraType.Preview;
            camera.orthographic = true;

            Vector2 position2d = bounds.CenterPoint;
            Vector3 camPosition = new Vector3(position2d.x, 25.0f, position2d.y);
            Quaternion camRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            camera.scene = aScene;
            camera.transform.SetPositionAndRotation(camPosition, camRotation);
            camera.aspect = bounds.AspectRatio;

            camera.orthographicSize = bounds.Height();

            

            return camera;
        }

        private static Texture2D DrawScene(SceneBounds bounds, Camera camera, int renderTextureHeight)
        {
            float aspectRatio = bounds.AspectRatio;
            RenderTexture renderTexture = new RenderTexture(Mathf.RoundToInt(aspectRatio * renderTextureHeight), renderTextureHeight, 16);

            if(camera == null)
            {
                
                Texture2D nullResult = Texture2D.blackTexture;
                nullResult.Reinitialize(renderTexture.width, renderTexture.height);
                return nullResult;
            }

            camera.targetTexture = renderTexture;
            camera.Render();

            Texture2D textureResult = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

            RenderTexture oldActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            textureResult.ReadPixels(new Rect(0,0, renderTexture.width, renderTexture.height), 0, 0);

            RenderTexture.active = oldActive;

            // add the visible world min and max points to the scene bounds just in case they aren't already in it.
            Vector3 worldMin3D = camera.ScreenToWorldPoint(new Vector3(0, 0, 10));
            Vector3 worldMax3D = camera.ScreenToWorldPoint(new Vector3(textureResult.width - 1, textureResult.height - 1, 10));

            Vector2 worldMin = new Vector2(worldMin3D.x, worldMin3D.z);
            Vector2 worldMax = new Vector2(worldMax3D.x, worldMax3D.z);

            bounds.AddPointToBounds(worldMin);
            bounds.AddPointToBounds(worldMax);

            return textureResult;

        }

        private static void DrawSpawners(SceneData data, List<SpawnerData> spawnPoints, Texture2D texture, Vector2Int actualTextureSize, Camera camera)
        {

            Color[] pixels = texture.GetPixels();
            
            Vector2 worldMin = data.Bounds.MinPoint;
            Vector2 worldMax = data.Bounds.MaxPoint;

            // drawing keying pixels to make sure I can successfully find the min and max of the image via world coord
            Vector2 worldMaxRemaped = RemapVector2(worldMax, worldMin/*bounds.MinPoint*/, worldMax/*bounds.MaxPoint*/, new Vector2(0.0f, 0.0f), new Vector2(actualTextureSize.x - 1, actualTextureSize.y - 1));
            int indexMax = Mathf.FloorToInt(worldMaxRemaped.y) * (actualTextureSize.x) + Mathf.FloorToInt(worldMaxRemaped.x);
            if (indexMax < pixels.Length || indexMax > 0)
            {
                pixels[indexMax] = Color.red;
            }
            pixels[0] = Color.blue;

            // actually start drawing markers for the spawners
            foreach (SpawnerData spawner in spawnPoints)
            {
                Vector2 position = new Vector2(spawner.Position.x, spawner.Position.z);

                position = RemapVector2(position, worldMin/*bounds.MinPoint*/, worldMax/*bounds.MaxPoint*/, new Vector2(0.0f, 0.0f), new Vector2(actualTextureSize.x - 1, actualTextureSize.y -1));

                int x = Mathf.FloorToInt(position.x);
                int y = Mathf.FloorToInt(position.y);
                int originalIndex = y * (actualTextureSize.x) + x;
                List<int> neighboringPixels = GetNeighborPixelIndexes(x, y, actualTextureSize, pixels.Length);
                foreach(int index in neighboringPixels)
                {
                    pixels[index] = Color.magenta;
                }
            }
            texture.SetPixels(pixels);
        }

        
        private static void DrawConnections(SceneData data, List<RegionConnectionData> connections, Texture2D texture, Vector2Int actualTextureSize, Camera camera)
        {
            
            Color[] pixels = texture.GetPixels();
            
            Vector2 worldMin = data.Bounds.MinPoint;
            Vector2 worldMax = data.Bounds.MaxPoint;

            foreach (RegionConnectionData connection in connections)
            {
                Vector2 position = new Vector2(connection.Position.x, connection.Position.z);

                position = RemapVector2(position, worldMin/*bounds.MinPoint*/, worldMax/*bounds.MaxPoint*/, new Vector2(0.0f, 0.0f), new Vector2(actualTextureSize.x - 1, actualTextureSize.y - 1));

                int x = Mathf.FloorToInt(position.x);
                int y = Mathf.FloorToInt(position.y);
                int originalIndex = y * (actualTextureSize.x) + x;
                List<int> neighboringPixels = GetNeighborPixelIndexes(x, y, actualTextureSize, pixels.Length);
                foreach (int index in neighboringPixels)
                {
                    pixels[index] = Color.greenYellow;
                }
            }
            texture.SetPixels(pixels);
        }

        private static List<int> GetNeighborPixelIndexes(int x, int y, Vector2Int size, int ArrayLength)
        {
            List<int> indexes = new();
            for(int i = y-1; i <= y+1 && i < size.y; i++)
            {
                if(i< 0)
                {
                    continue;
                }
                
                
                for(int j = x - 1; j <= x+ 1 && j < size.x; j++)
                {
                    if (j < 0)
                    {
                        continue;
                    }
                    int index = i * size.x + j;

                    if (index >= ArrayLength)
                    {
                        break;
                    }

                    indexes.Add(index);
                }
            }

            return indexes;
        }

        public static float Remap(float input, float inputRangeMin, float inputRangeMax, float outputRangeMin, float outputRangeMax)
        {
            float t = Mathf.InverseLerp(inputRangeMin, inputRangeMax, input);
            float output = Mathf.Lerp(outputRangeMin, outputRangeMax, t);
            return output;
        }

        public static Vector2 RemapVector2(Vector2 input, Vector2 inputRangeMin, Vector2 inputRangeMax, Vector2 outputRangeMin, Vector2 outputRangeMax)
        {
            float newX = Remap(input.x, inputRangeMin.x, inputRangeMax.x, outputRangeMin.x, outputRangeMax.x);
            float newY = Remap(input.y, inputRangeMin.y, inputRangeMax.y, outputRangeMin.y, outputRangeMax.y);
            return new Vector2(newX, newY);
        }
    }
}
