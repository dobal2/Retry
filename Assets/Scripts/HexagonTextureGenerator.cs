// using UnityEngine;
// using UnityEditor;
//
// public class HexagonTextureGenerator : MonoBehaviour
// {
//     [MenuItem("Tools/Generate Hexagon Texture")]
//     static void GenerateHexTexture()
//     {
//         int size = 512;
//         Texture2D tex = new Texture2D(size, size);
//         
//         float hexSize = 32f;
//         
//         for (int y = 0; y < size; y++)
//         {
//             for (int x = 0; x < size; x++)
//             {
//                 float hexValue = HexPattern(x, y, hexSize);
//                 Color col = new Color(hexValue, hexValue, hexValue, 1f);
//                 tex.SetPixel(x, y, col);
//             }
//         }
//         
//         tex.Apply();
//         
//         byte[] bytes = tex.EncodeToPNG();
//         System.IO.File.WriteAllBytes(Application.dataPath + "/HexagonPattern.png", bytes);
//         AssetDatabase.Refresh();
//         
//         Debug.Log("Hexagon texture created!");
//     }
//     
//     static float HexPattern(float x, float y, float size)
//     {
//         float sqrt3 = 1.732f;
//         
//         // Hexagon grid coordinates
//         float q = (x * sqrt3/3f - y / 3f) / size;
//         float r = y * 2f/3f / size;
//         
//         // Convert to axial coordinates
//         float qRound = Mathf.Round(q);
//         float rRound = Mathf.Round(r);
//         float sRound = Mathf.Round(-q - r);
//         
//         float qDiff = Mathf.Abs(qRound - q);
//         float rDiff = Mathf.Abs(rRound - r);
//         float sDiff = Mathf.Abs(sRound - (-q - r));
//         
//         if (qDiff > rDiff && qDiff > sDiff)
//             qRound = -rRound - sRound;
//         else if (rDiff > sDiff)
//             rRound = -qRound - sRound;
//         
//         // Distance to center of hexagon
//         float dx = q - qRound;
//         float dy = r - rRound;
//         float dist = Mathf.Sqrt(dx * dx + dy * dy);
//         
//         // Edge thickness
//         float edge = 1f - Mathf.Clamp01((dist - 0.4f) * 10f);
//         
//         return edge;
//     }
// }