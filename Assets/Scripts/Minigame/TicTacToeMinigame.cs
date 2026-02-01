using UnityEngine;
using UnityEngine.Serialization;

public class TicTacToeMinigame : MonoBehaviour, IMinigame
{
    [Header("Board")]
    [SerializeField] private SpriteRenderer boardRenderer;
    [SerializeField] private Transform inputAreaTransform;
    [SerializeField] private Transform marksRoot;
    [SerializeField] private bool debugDrawInput = false;
    [FormerlySerializedAs("markPrefab")]
    [FormerlySerializedAs("playerMarkPrefab")]
    [SerializeField] private GameObject xMarkPrefab;
    [FormerlySerializedAs("aiMarkPrefab")]
    [SerializeField] private GameObject oMarkPrefab;

    private readonly int[] cells = new int[9];
    private readonly GameObject[] marks = new GameObject[9];
    private MinigameContext context;
    private int playerMark;
    private int aiMark;
    private bool playerTurn;
    private const int XMark = 1;
    private const int OMark = 2;
    private bool loggedMissingBoardRenderer;
    private bool hasLastInput;
    private Vector3 lastLocalInput;

    private void Awake()
    {
        if (boardRenderer == null)
            boardRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Begin(MinigameContext ctx)
    {
        context = ctx;
        ClearBoard();
        RandomizeSides();
        if (!playerTurn)
            MakeAiMove();
    }

    public void HandlePointerDown(Vector3 localPos)
    {
        lastLocalInput = localPos;
        hasLastInput = true;

        if (!playerTurn)
            return;

        if (!TryPlace(localPos, playerMark))
            return;

        ResolveAfterMove(playerMark);
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

    private bool TryPlace(Vector3 localPos, int mark)
    {
        if (!TryGetInputArea(out float boardWidth, out float boardHeight, out Vector2 areaCenter))
            return false;

        float halfW = boardWidth * 0.5f;
        float halfH = boardHeight * 0.5f;

        float localX = localPos.x - areaCenter.x;
        float localY = localPos.y - areaCenter.y;
        if (localX < -halfW || localX > halfW || localY < -halfH || localY > halfH)
            return false;

        float cellW = boardWidth / 3f;
        float cellH = boardHeight / 3f;

        int col = Mathf.FloorToInt((localX + halfW) / cellW);
        int row = Mathf.FloorToInt((localY + halfH) / cellH);

        col = Mathf.Clamp(col, 0, 2);
        row = Mathf.Clamp(row, 0, 2);

        int index = row * 3 + col;
        return TryPlaceIndex(index, mark);
    }

    private bool TryPlaceIndex(int index, int mark)
    {
        if (cells[index] != 0)
            return false;

        cells[index] = mark;
        SpawnMark(index, mark);
        return true;
    }

    private void ResolveAfterMove(int mark)
    {
        if (CheckWin(mark))
        {
            MinigameManager.Instance?.EndMinigame(mark == playerMark);
            return;
        }

        if (IsBoardFull())
        {
            MinigameManager.Instance?.EndMinigame(false);
            return;
        }

        if (mark == playerMark)
        {
            playerTurn = false;
            MakeAiMove();
        }
        else
        {
            playerTurn = true;
        }
    }

    private void MakeAiMove()
    {
        int emptyCount = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == 0)
                emptyCount++;
        }

        if (emptyCount == 0)
        {
            MinigameManager.Instance?.EndMinigame(false);
            return;
        }

        int pick = Random.Range(0, emptyCount);
        int chosen = -1;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != 0)
                continue;

            if (pick == 0)
            {
                chosen = i;
                break;
            }

            pick--;
        }

        if (chosen < 0)
            return;

        TryPlaceIndex(chosen, aiMark);
        ResolveAfterMove(aiMark);
    }

    private void SpawnMark(int index, int mark)
    {
        GameObject prefab = mark == XMark ? xMarkPrefab : oMarkPrefab;
        if (prefab == null)
            prefab = xMarkPrefab;
        if (prefab == null)
            return;

        if (!TryGetInputArea(out float boardWidth, out float boardHeight, out Vector2 areaCenter))
            return;

        var root = marksRoot != null ? marksRoot : transform;
        if (marks[index] != null)
            Destroy(marks[index]);

        int row = index / 3;
        int col = index % 3;
        var markObj = Instantiate(prefab, root);
        markObj.transform.localPosition = GetCellCenter(row, col, boardWidth, boardHeight, areaCenter);
        markObj.transform.localRotation = Quaternion.identity;
        marks[index] = markObj;
    }

    private void RandomizeSides()
    {
        playerMark = Random.value < 0.5f ? XMark : OMark;
        aiMark = playerMark == XMark ? OMark : XMark;
        playerTurn = Random.value < 0.5f;
    }

    private void ClearBoard()
    {
        for (int i = 0; i < cells.Length; i++)
            cells[i] = 0;

        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] != null)
                Destroy(marks[i]);
            marks[i] = null;
        }
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

    private Vector3 GetCellCenter(int row, int col, float boardWidth, float boardHeight, Vector2 areaCenter)
    {
        float halfW = boardWidth * 0.5f;
        float halfH = boardHeight * 0.5f;
        float cellW = boardWidth / 3f;
        float cellH = boardHeight / 3f;

        float x = -halfW + (cellW * 0.5f) + col * cellW + areaCenter.x;
        float y = -halfH + (cellH * 0.5f) + row * cellH + areaCenter.y;
        return new Vector3(x, y, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!TryGetInputArea(out float boardWidth, out float boardHeight, out Vector2 areaCenter))
            return;

        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;

        float halfW = boardWidth * 0.5f;
        float halfH = boardHeight * 0.5f;
        Vector3 topLeft = new Vector3(areaCenter.x - halfW, areaCenter.y + halfH, 0f);
        Vector3 topRight = new Vector3(areaCenter.x + halfW, areaCenter.y + halfH, 0f);
        Vector3 bottomLeft = new Vector3(areaCenter.x - halfW, areaCenter.y - halfH, 0f);
        Vector3 bottomRight = new Vector3(areaCenter.x + halfW, areaCenter.y - halfH, 0f);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        float cellW = boardWidth / 3f;
        float cellH = boardHeight / 3f;
        Gizmos.DrawLine(new Vector3(areaCenter.x - halfW + cellW, areaCenter.y - halfH, 0f), new Vector3(areaCenter.x - halfW + cellW, areaCenter.y + halfH, 0f));
        Gizmos.DrawLine(new Vector3(areaCenter.x - halfW + cellW * 2f, areaCenter.y - halfH, 0f), new Vector3(areaCenter.x - halfW + cellW * 2f, areaCenter.y + halfH, 0f));
        Gizmos.DrawLine(new Vector3(areaCenter.x - halfW, areaCenter.y - halfH + cellH, 0f), new Vector3(areaCenter.x + halfW, areaCenter.y - halfH + cellH, 0f));
        Gizmos.DrawLine(new Vector3(areaCenter.x - halfW, areaCenter.y - halfH + cellH * 2f, 0f), new Vector3(areaCenter.x + halfW, areaCenter.y - halfH + cellH * 2f, 0f));

        if (debugDrawInput && hasLastInput)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.TransformPoint(lastLocalInput), 0.03f);
        }
    }

    private bool TryGetInputArea(out float boardWidth, out float boardHeight, out Vector2 areaCenter)
    {
        if (inputAreaTransform != null && TryGetAreaFromTransform(inputAreaTransform, out boardWidth, out boardHeight, out areaCenter))
            return true;

        if (boardRenderer == null)
        {
            if (!loggedMissingBoardRenderer)
            {
                Debug.LogWarning($"{nameof(TicTacToeMinigame)}: boardRenderer is not assigned.", this);
                loggedMissingBoardRenderer = true;
            }
            boardWidth = 0f;
            boardHeight = 0f;
            areaCenter = Vector2.zero;
            return false;
        }

        var bounds = boardRenderer.bounds.size;
        Vector3 centerWorld = boardRenderer.bounds.center;
        Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);
        var scale = transform.lossyScale;
        float scaleX = Mathf.Abs(scale.x);
        float scaleY = Mathf.Abs(scale.y);
        boardWidth = scaleX > 0f ? bounds.x / scaleX : bounds.x;
        boardHeight = scaleY > 0f ? bounds.y / scaleY : bounds.y;
        areaCenter = new Vector2(centerLocal.x, centerLocal.y);
        return boardWidth > 0f && boardHeight > 0f;
    }

    private bool TryGetAreaFromTransform(Transform areaTransform, out float boardWidth, out float boardHeight, out Vector2 areaCenter)
    {
        areaCenter = Vector2.zero;

        if (areaTransform == null)
        {
            boardWidth = 0f;
            boardHeight = 0f;
            return false;
        }

        Vector3 centerLocal = transform.InverseTransformPoint(areaTransform.position);
        areaCenter = new Vector2(centerLocal.x, centerLocal.y);

        var box = areaTransform.GetComponent<BoxCollider>();
        if (box != null)
        {
            Vector3 worldSize = Vector3.Scale(box.size, areaTransform.lossyScale);
            Vector3 localSize = transform.InverseTransformVector(worldSize);
            boardWidth = Mathf.Abs(localSize.x);
            boardHeight = Mathf.Abs(localSize.y);
            return boardWidth > 0f && boardHeight > 0f;
        }

        var renderer = areaTransform.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Vector3 worldSize = renderer.bounds.size;
            Vector3 localSize = transform.InverseTransformVector(worldSize);
            boardWidth = Mathf.Abs(localSize.x);
            boardHeight = Mathf.Abs(localSize.y);
            return boardWidth > 0f && boardHeight > 0f;
        }

        Vector3 localScale = areaTransform.localScale;
        boardWidth = Mathf.Abs(localScale.x);
        boardHeight = Mathf.Abs(localScale.y);
        return boardWidth > 0f && boardHeight > 0f;
    }
}
