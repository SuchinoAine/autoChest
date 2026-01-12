using System;
using System.Collections.Generic;
using UnityEngine;
using AutoChess.Core;

/// <summary>
/// Builds a deploy grid from the scene hierarchy:
/// BoardRing/BoardPosSelf/Row{r}/Pipe{c}
/// BoardRing/BoardPosEne /Row{r}/Pipe{c}
///
/// IMPORTANT:
/// We DO NOT use the Pipe transform pivot as the spawn point.
/// Instead, we use the Pipe *model* center:
/// - Prefer Renderer.bounds.center (world-space)
/// - Fallback to Collider.bounds.center (world-space)
/// - Fallback to transform.position
///
/// By default we project the center onto the board plane (y = boardY) so units sit on the board.
/// </summary>
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

    [Header("Spawn Point Rule")]
    [Tooltip("Use Pipe model center from Renderer/Collider bounds instead of transform pivot.")]
    public bool usePipeModelCenter = true;

    [Tooltip("Project the spawn point onto a fixed board Y plane.")]
    public bool projectToBoardPlane = true;

    [Tooltip("If projectToBoardPlane is true, this Y value will be used. If left as NaN, will use this.transform.position.y.")]
    public float boardY = float.NaN;

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

        float finalBoardY = boardY;
        if (float.IsNaN(finalBoardY))
            finalBoardY = transform.position.y;

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
            {
                Vector3 p = usePipeModelCenter ? GetPipeModelCenterWorld(pipes[c]) : pipes[c].position;

                if (projectToBoardPlane)
                    p.y = finalBoardY;

                grid[r, c] = p;
            }
        }

        return true;
    }

    private Vector3 GetPipeModelCenterWorld(Transform pipe)
    {
        if (pipe == null) return Vector3.zero;

        // Prefer Renderer bounds center
        var r = pipe.GetComponentInChildren<Renderer>(true);
        if (r != null)
            return r.bounds.center;

        // Fallback to Collider bounds center
        var col = pipe.GetComponentInChildren<Collider>(true);
        if (col != null)
            return col.bounds.center;

        // Last resort: pivot
        return pipe.position;
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
