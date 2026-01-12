using UnityEngine;
using AutoChess.Core;
using AutoChess.View;

public class UnitFactory : MonoBehaviour
{
    [Header("Shell (optional)")]
    public GameObject unitShellPrefab;        // 里面挂 UnitView + ModelRoot（可为空）

    [Header("Default Model")]
    public GameObject cylinderModelPrefab;    // 圆柱占位模型（可为空）

    public UnitView CreateU(string unitId, Team team, Vector3 spawnPos)
    {
        GameObject go;

        if (unitShellPrefab != null)
        {
            go = Instantiate(unitShellPrefab);
        }
        else
        {
            // 动态创建壳
            go = new GameObject("UnitShell");
            var root = new GameObject("ModelRoot");
            root.transform.SetParent(go.transform, false);
        }

        go.name = $"Unit_{unitId}";

        var view = go.GetComponent<UnitView>();
        if (view == null) view = go.AddComponent<UnitView>();

        view.unitId = unitId;
        view.team = team;

        // 确保 modelRoot 存在
        if (view.modelRoot == null)
        {
            var mr = go.transform.Find("ModelRoot");
            view.modelRoot = (mr != null) ? mr : go.transform;
        }

        // 默认模型：圆柱
        if (cylinderModelPrefab != null)
        {
            view.SetModel(cylinderModelPrefab);
        }
        else
        {
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "CylinderModel";
            cyl.transform.SetParent(view.modelRoot, false);
            view.CacheRenderers();
            view.ApplyTeamColor();
        }

        view.SetPos(spawnPos);

        return view;
    }
}
