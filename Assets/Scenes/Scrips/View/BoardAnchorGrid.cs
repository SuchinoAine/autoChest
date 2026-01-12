using System;
using System.Collections.Generic;
using UnityEngine;
using AutoChess.Core;

public class BoardAnchorGrid : MonoBehaviour
{
    [Header("Auto Find (optional)")]
    public Transform boardRing;                // 如果拖了 BoardRing, 这里会自动找子节点
    public string selfPath = "BoardPosSelf";   // BoardRing 下的名字
    public string enePath  = "BoardPosEne";

    [Header("Or Assign Directly (optional)")]
    public Transform boardPosSelf;
    public Transform boardPosEne;

    [Header("Layout")]
    public int rows = 4;
    public int cols = 7;

    private Vector3[,] _self;
    private Vector3[,] _ene;

    void Awake()
    {
        TryAutoBind();
        Try2Build();
    }

    public void TryAutoBind()
    {
        // 已经直接拖了就不自动找
        if (boardPosSelf != null && boardPosEne != null) return;
        // 默认用挂载对象当 root
        if (boardRing == null) boardRing = transform; 
        if (boardPosSelf == null) boardPosSelf = boardRing.Find(selfPath);
        if (boardPosEne == null) boardPosEne = boardRing.Find(enePath);
    }

    public bool Try2Build()
    {
        TryAutoBind();

        if (boardPosSelf == null || boardPosEne == null)
        {
            Debug.LogError($"[BoardAnchorGrid] Missing anchors. boardPosSelf={(boardPosSelf ? boardPosSelf.name : "null")}, boardPosEne={(boardPosEne ? boardPosEne.name : "null")}");
            return false;
        }
        if (!TryBuild(boardPosSelf, out _self)) return false;
        if (!TryBuild(boardPosEne,  out _ene))  return false;

        return true;
    }

    public Vector3 GetDeployWorldPos(Team team, int row, int col)
    {
        row = Mathf.Clamp(row, 0, rows - 1);
        col = Mathf.Clamp(col, 0, cols - 1);

        return team == Team.A ? _self[row, col] : _ene[row, col];
    }

    private bool TryBuild(Transform root, out Vector3[,] grid)
    {
        grid = new Vector3[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            var rowT = root.Find($"Row{r}");
            if (rowT == null)
            {
                Debug.LogError($"[BoardAnchorGrid] Missing {root.name}/Row{r}");
                return false;
            }

            var pipes = new List<Transform>();
            foreach (Transform c in rowT)
            {
                if (c != null && c.name.StartsWith("Pipe", StringComparison.OrdinalIgnoreCase))
                    pipes.Add(c);
            }

            if (pipes.Count < cols)
            {
                Debug.LogError($"[BoardAnchorGrid] {root.name}/Row{r} pipes={pipes.Count}, expected>={cols}");
                return false;
            }

            pipes.Sort((a, b) => ExtractPipeIndex(a.name).CompareTo(ExtractPipeIndex(b.name)));

            for (int c = 0; c < cols; c++)
                grid[r, c] = pipes[c].position;
        }

        return true;
    }

    private int ExtractPipeIndex(string name)
    {
        int i = 0;
        for (int k = name.Length - 1; k >= 0; k--)
        {
            if (!char.IsDigit(name[k]))
            {
                int.TryParse(name.Substring(k + 1), out i);
                return i;
            }
        }
        int.TryParse(name, out i);
        return i;
    }
}
