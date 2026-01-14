using UnityEngine;
using AutoChess.Core;
using AutoChess.View;

public class UnitFactory : MonoBehaviour
{
    [Header("Shell (optional)")]
    public GameObject unitShellPrefab;

    [Header("Default Model")]
    public GameObject cylinderModelPrefab; // Recommended: assign in inspector.

    [Header("HUD")]
    public GameObject unitHudPrefab; // UnitHud prefab

    public UnitView CreateU(string unitId, Team team, Vector3 spawnPos, float radius)
    {
        GameObject go = unitShellPrefab != null ? Instantiate(unitShellPrefab) : new GameObject("UnitShell");
        go.name = $"Unit_{unitId}";

        var view = go.GetComponent<UnitView>();
        if (view == null) view = go.AddComponent<UnitView>();

        view.unitId = unitId;
        view.team = team;

        // IMPORTANT: apply radius BEFORE grounding the model (grounding uses final scale).
        view.ApplyRadius(radius);

        // Spawn HUD under VisualRoot
        if (unitHudPrefab != null)
        {
            var hudGo = Instantiate(unitHudPrefab, view.transform);
            hudGo.name = "UnitHud";
            hudGo.transform.localPosition = Vector3.zero;
            hudGo.transform.localRotation = Quaternion.identity;
            hudGo.transform.localScale = Vector3.one;
        }

        if (cylinderModelPrefab != null)
        {
            view.SetModel(cylinderModelPrefab);
        }
        else
        {
            // Fallback: primitive cylinder
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "CylinderModel";
            cyl.transform.SetParent(view.modelRoot, false);
            cyl.transform.localPosition = Vector3.zero;
            cyl.transform.localRotation = Quaternion.identity;
            cyl.transform.localScale = Vector3.one;

            view.RegroundCurrentModel();
            view.CacheRenderers();
            view.ApplyTeamColor();
        }

        view.SetPos(spawnPos);
        return view;
    }
}
