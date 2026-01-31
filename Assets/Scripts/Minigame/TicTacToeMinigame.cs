using UnityEngine;

public class TicTacToeMinigame : MonoBehaviour, IMinigame
{
    [Header("Board")]
    [SerializeField] private float boardWidth = 1f;
    [SerializeField] private float boardHeight = 1f;

    private readonly int[] cells = new int[9];
    private MinigameContext context;

    public void Begin(MinigameContext ctx)
    {
        context = ctx;
        ClearBoard();
    }

    public void HandlePointerDown(Vector3 localPos)
    {
        if (!TryPlace(localPos))
            return;

        if (CheckWin(1))
        {
            MinigameManager.Instance?.EndMinigame(true);
            return;
        }

        if (IsBoardFull())
            MinigameManager.Instance?.EndMinigame(false);
    }

    public void HandlePointerDrag(Vector3 localPos)
    {
    }

    public void HandlePointerUp(Vector3 localPos)
    {
    }

    public void Cancel()
    {
    }

    private bool TryPlace(Vector3 localPos)
    {
        if (boardWidth <= 0f || boardHeight <= 0f)
            return false;

        float halfW = boardWidth * 0.5f;
        float halfH = boardHeight * 0.5f;

        if (localPos.x < -halfW || localPos.x > halfW || localPos.y < -halfH || localPos.y > halfH)
            return false;

        float cellW = boardWidth / 3f;
        float cellH = boardHeight / 3f;

        int col = Mathf.FloorToInt((localPos.x + halfW) / cellW);
        int row = Mathf.FloorToInt((localPos.y + halfH) / cellH);

        col = Mathf.Clamp(col, 0, 2);
        row = Mathf.Clamp(row, 0, 2);

        int index = row * 3 + col;
        if (cells[index] != 0)
            return false;

        cells[index] = 1;
        Debug.Log($"TicTacToe: placed at {row},{col}", this);
        return true;
    }

    private void ClearBoard()
    {
        for (int i = 0; i < cells.Length; i++)
            cells[i] = 0;
    }

    private bool IsBoardFull()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == 0)
                return false;
        }

        return true;
    }

    private bool CheckWin(int player)
    {
        return (cells[0] == player && cells[1] == player && cells[2] == player) ||
               (cells[3] == player && cells[4] == player && cells[5] == player) ||
               (cells[6] == player && cells[7] == player && cells[8] == player) ||
               (cells[0] == player && cells[3] == player && cells[6] == player) ||
               (cells[1] == player && cells[4] == player && cells[7] == player) ||
               (cells[2] == player && cells[5] == player && cells[8] == player) ||
               (cells[0] == player && cells[4] == player && cells[8] == player) ||
               (cells[2] == player && cells[4] == player && cells[6] == player);
    }
}
