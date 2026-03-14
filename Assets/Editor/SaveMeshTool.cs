using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveMeshTool
{
    [MenuItem("Tools/一键保存程序化网格 (Bake Mesh)")]
    public static void SaveMeshes()
    {
        // 确保有一个文件夹用来存放这些生成的网格
        string folderPath = "Assets/BakedMeshes";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "BakedMeshes");
        }

        int count = 0;
        // 遍历你选中的所有物体
        foreach (GameObject obj in Selection.gameObjects)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                // 如果这个网格还不是一个实体资产
                if (!AssetDatabase.Contains(mf.sharedMesh))
                {
                    // 复制一份网格数据
                    Mesh savedMesh = Object.Instantiate(mf.sharedMesh);
                    string path = $"{folderPath}/{obj.name}_mesh.asset";
                    
                    // 保存为真正的 .asset 文件
                    AssetDatabase.CreateAsset(savedMesh, path);
                    
                    // 重新把保存好的实体网格赋给物体
                    mf.sharedMesh = savedMesh; 
                    
                    // (可选) 移除生成器脚本，因为网格已经固化了，不需要再生成了
                    // Object.DestroyImmediate(obj.GetComponent("PolyhedronGenerator")); 
                    
                    count++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"[SaveMeshTool] 成功烘焙并保存了 {count} 个网格到 {folderPath}！");
    }
}