using UnityEngine;
using UnityEditor;
using AutoChess.Configs;

namespace AutoChess.EditorTools
{
    public class CardDataRadiusCalculator : Editor
    {
        // 在 Unity 顶部菜单栏生成一个按钮
        [MenuItem("Tools/AutoChess/一键自动计算所有卡牌的 Radius (碰撞半径)")]
        public static void CalculateAllRadiuses()
        {
            // 1. 找到项目里所有的 CardDataSO 配置文件
            string[] guids = AssetDatabase.FindAssets("t:CardDataSO");
            int updatedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);

                if (card != null && card.prefab != null)
                {
                    // 2. 临时在场景中生成这个预制体，用来精确测量
                    GameObject tempObj = (GameObject)PrefabUtility.InstantiatePrefab(card.prefab);
                    tempObj.transform.position = Vector3.zero;
                    tempObj.transform.rotation = Quaternion.identity;
                    
                    // 3. 获取身上所有的渲染器 (Renderer) 
                    Renderer[] renderers = tempObj.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        // 4. 计算合并后的包围盒 (Bounds)
                        Bounds combinedBounds = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++)
                        {
                            combinedBounds.Encapsulate(renderers[i].bounds);
                        }

                        // 5. 核心：半径取 X 轴和 Z 轴最大宽度的一半
                        // extents 是 bounds 的“半长”，刚好就是我们要的半径！
                        float calculatedRadius = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.z);
                        
                        // 为了防止模型太紧凑，稍微给一点点额外的空气墙（膨胀 10% 或加上 0.1f 也可以，看手感）
                        // 这里我们直接原样保留，保留 2 位小数
                        card.radius = (float)System.Math.Round(calculatedRadius, 2);

                        // 标记这个配置已经被修改过了，以便 Unity 保存它
                        EditorUtility.SetDirty(card);
                        updatedCount++;
                    }

                    // 6. 测完之后，立刻把临时生成的模型删掉（毁尸灭迹）
                    DestroyImmediate(tempObj);
                }
            }

            // 7. 保存所有修改过的 SO 数据资产
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[自动化工具] 成功计算并更新了 {updatedCount} 张卡牌的碰撞半径！</color>");
        }
    }
}