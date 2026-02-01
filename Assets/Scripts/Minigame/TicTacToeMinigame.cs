using UnityEngine;
using UnityEngine.Serialization;

public class TicTacToeMinigame : MonoBehaviour, IMinigame
{
    [Header("Board")]
    [SerializeField] private SpriteRenderer boardRenderer;
    [SerializeField] private Transform inputAreaTransform;
    [SerializeField] private Transform marksRoot;
    [SerializeField] private bool debugDrawInput = false;
    [SerializeField] private float aiMoveDelaySeconds = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float aiMistakeChance = 0.3f;
    [SerializeField] private float resultDelaySeconds = 0.6f;
    [SerializeField] private GameObject winVfxPrefab;
    [SerializeField] private GameObject loseVfxPrefab;
    [SerializeField] private GameObject drawVfxPrefab;
    [SerializeField] private AudioClip winSfx;
    [SerializeField] private AudioClip loseSfx;
    [SerializeField] private AudioClip drawSfx;
    [SerializeField] private AudioSource sfxSource;
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
    private float aiMoveTimer;
    private bool aiMovePending;
    private bool resultPending;

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
            ScheduleAiMove();
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
        aiMovePending = false;
        aiMoveTimer = 0f;
        resultPending = false;
    }

    private void Update()
    {
        if (!aiMovePending)
            return;

        aiMoveTimer -= Time.deltaTime;
        if (aiMoveTimer > 0f)
            return;

        aiMovePending = false;
        MakeAiMove();
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
            ShowResult(mark == playerMark, false);
            return;
        }

        if (IsBoardFull())
        {
            ShowResult(false, true);
            return;
        }

        if (mark == playerMark)
        {
            playerTurn = false;
            ScheduleAiMove();
        }
        else
        {
            playerTurn = true;
        }
    }

    private void MakeAiMove()
    {
        if (resultPending)
            return;

        int chosen = FindBestAiMove();
        if (chosen < 0)
        {
            ShowResult(false, true);
            return;
        }

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

    private void ScheduleAiMove()
    {
        if (aiMoveDelaySeconds <= 0f)
        {
            MakeAiMove();
            return;
        }

        aiMoveTimer = aiMoveDelaySeconds;
        aiMovePending = true;
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

        aiMovePending = false;
        aiMoveTimer = 0f;
        resultPending = false;
    }

    private void ShowResult(bool playerWon, bool isDraw)
    {
        if (resultPending)
            return;

        resultPending = true;
        aiMovePending = false;
        aiMoveTimer = 0f;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        GameObject vfxPrefab = null;
        AudioClip clip = null;

        if (isDraw)
        {
            vfxPrefab = drawVfxPrefab;
            clip = drawSfx;
        }
        else if (playerWon)
        {
            vfxPrefab = winVfxPrefab;
            clip = winSfx;
        }
        else
        {
            vfxPrefab = loseVfxPrefab;
            clip = loseSfx;
        }

        if (vfxPrefab != null)
        {
            var root = marksRoot != null ? marksRoot : transform;
            Instantiate(vfxPrefab, root);
        }

        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);

        if (resultDelaySeconds <= 0f)
        {
            MinigameManager.Instance?.EndMinigame(playerWon);
            return;
        }

        StartCoroutine(EndAfterDelay(playerWon, resultDelaySeconds));
    }

    private System.Collections.IEnumerator EndAfterDelay(bool success, float delay)
    {
        yield return new WaitForSeconds(delay);
        MinigameManager.Instance?.EndMinigame(success);
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

    private int FindBestAiMove()
    {
        if (aiMistakeChance > 0f && Random.value < aiMistakeChance)
            return PickRandomEmpty();

        int winMove = FindWinningMove(aiMark);
        if (winMove >= 0)
            return winMove;

        int blockMove = FindWinningMove(playerMark);
        if (blockMove >= 0)
            return blockMove;

        if (cells[4] == 0)
            return 4;

        int corner = PickRandomEmpty(new int[] { 0, 2, 6, 8 });
        if (corner >= 0)
            return corner;

        int side = PickRandomEmpty(new int[] { 1, 3, 5, 7 });
        if (side >= 0)
            return side;

        return PickRandomEmpty();
    }

    private int FindWinningMove(int mark)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != 0)
                continue;

            cells[i] = mark;
            bool win = CheckWin(mark);
            cells[i] = 0;

            if (win)
                return i;
        }

        return -1;
    }

    private int PickRandomEmpty()
    {
        int emptyCount = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == 0)
                emptyCount++;
        }

        if (emptyCount == 0)
            return -1;

        int pick = Random.Range(0, emptyCount);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != 0)
                continue;

            if (pick == 0)
                return i;

            pick--;
        }

        return -1;
    }

    private int PickRandomEmpty(int[] candidates)
    {
        int emptyCount = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (cells[candidates[i]] == 0)
                emptyCount++;
        }

        if (emptyCount == 0)
            return -1;

        int pick = Random.Range(0, emptyCount);
        for (int i = 0; i < candidates.Length; i++)
        {
            int index = candidates[i];
            if (cells[index] != 0)
                continue;

            if (pick == 0)
                return index;

            pick--;
        }

        return -1;
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
            // Compute bounds in minigame-local space to avoid non-uniform scale + rotation distortion.
            Vector3 half = box.size * 0.5f;
            Vector3 center = box.center;
            Matrix4x4 localToWorld = box.transform.localToWorldMatrix;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 localCorner = center + new Vector3(half.x * sx, half.y * sy, half.z * sz);
                        Vector3 worldCorner = localToWorld.MultiplyPoint3x4(localCorner);
                        Vector3 rootLocal = worldToLocal.MultiplyPoint3x4(worldCorner);
                        min = Vector3.Min(min, rootLocal);
                        max = Vector3.Max(max, rootLocal);
                    }
                }
            }

            boardWidth = Mathf.Abs(max.x - min.x);
            boardHeight = Mathf.Abs(max.y - min.y);
            areaCenter = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
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
