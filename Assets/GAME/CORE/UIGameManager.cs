using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIGameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [Range(2, 8)]
    public int gridDimension = 4;
    public Vector2 gridSize = new Vector2(120f, 120f);
    public float spacing = 5f;
    public float padding = 10f;

    [Header("UI References")]
    public RectTransform gridContainer;
    public GameObject cellPrefab;
    public GameObject tilePrefab;
    public GameObject obstaclePrefab;
    [Space]
    public GameObject WinPanel;

    [Space]
    public GameObject LosePanel;

    [Space]
    public TextMeshProUGUI timeText;
    public GameObject timeBoard;

    [Header("Obstacle Placement")]
    public List<Vector2Int> obstaclePositions = new List<Vector2Int>();


    [Header("Game Settings")]
    public Sprite[] tileSprites = new Sprite[4];
    private float time = 45f;
    private RectTransform[,] cells;
    private RectTransform[,] tiles;
    private bool[,] obstacles;
    private bool isAnimating = false;
    private float cellSize;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private float minSwipeDistance = 50f;
    private bool isTouching = false;

    private int[,] tileNumbers; 
    private bool gameEnd = false;

    private Level level;

    void Start()
    {
        level = GameManager.Instance.levels[GameManager.Instance.indexOfThisLevel];
        time = 45f;
        InitializeGrid();
        PlaceObstacles();
        SpawnGameTiles();
    }

    void Update()
    {
        if (gameEnd)
            return;
        time -= Time.deltaTime;
        timeText.text = ((int)time).ToString();
        if (time < 0) 
        {
            EndGame(false);
        }
        if (!isAnimating && !gameEnd)
        {
            HandleTouchInput();
        }
    }

    private void EndGame(bool v)
    {
        gameEnd = true;
        if(v==true)
        {
            WinPanel.SetActive(true);
            timeBoard.SetActive(false);
            gridContainer.gameObject.SetActive(false);
        }
        else
        {
            LosePanel.SetActive(true);
            timeBoard.SetActive(false);
            gridContainer.gameObject.SetActive(false);
        }
    }

    
    void InitializeGrid()
    {
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
        gridDimension = level.dimension;
        cells = new RectTransform[gridDimension, gridDimension];
        tiles = new RectTransform[gridDimension, gridDimension];
        obstacles = new bool[gridDimension, gridDimension];
        tileNumbers = new int[gridDimension, gridDimension];

        gridContainer.sizeDelta = new Vector2(gridSize.x + padding * 2, gridSize.y + padding * 2);

        cellSize = (gridSize.x - (gridDimension - 1) * spacing) / gridDimension;

        Image background = gridContainer.GetComponent<Image>();
        if (background == null)
        {
            background = gridContainer.gameObject.AddComponent<Image>();
        }

        for (int x = 0; x < gridDimension; x++)
        {
            for (int y = 0; y < gridDimension; y++)
            {
                GameObject cellObject = Instantiate(cellPrefab, gridContainer);
                RectTransform cellRect = cellObject.GetComponent<RectTransform>();

                float posX = padding + x * (cellSize + spacing);
                float posY = padding + y * (cellSize + spacing);

                cellRect.anchorMin = new Vector2(0, 0);
                cellRect.anchorMax = new Vector2(0, 0);
                cellRect.pivot = new Vector2(0, 0);
                cellRect.anchoredPosition = new Vector2(posX, posY);
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);

                Image cellImage = cellObject.GetComponent<Image>();

                cells[x, y] = cellRect;
                cellObject.name = "Cell_" + x + "_" + y;

                obstacles[x, y] = false;
                tileNumbers[x, y] = 0;
            }
        }
    }

    void PlaceObstacles()
    {
        obstaclePositions = level.positions;
        foreach (Vector2Int pos in obstaclePositions)
        {
            if (IsValidPosition(pos))
            {
                CreateObstacleAt(pos.x, pos.y);
            }
        }
    }

    void CreateObstacleAt(int x, int y)
    {
        if (!IsValidPosition(new Vector2Int(x, y)) || obstacles[x, y] || tiles[x, y] != null)
            return; 

        GameObject obstacleObject = Instantiate(obstaclePrefab, gridContainer);
        RectTransform obstacleRect = obstacleObject.GetComponent<RectTransform>();

        obstacleRect.anchorMin = new Vector2(0, 0);
        obstacleRect.anchorMax = new Vector2(0, 0);
        obstacleRect.pivot = new Vector2(0, 0);
        obstacleRect.anchoredPosition = cells[x, y].anchoredPosition;
        obstacleRect.sizeDelta = new Vector2(cellSize, cellSize);

        Image obstacleImage = obstacleObject.GetComponent<Image>();

        obstacles[x, y] = true;
        obstacleObject.name = "Obstacle_" + x + "_" + y;
    }

    void SpawnGameTiles()
    {
        List<Vector2Int> emptyCells = GetAllEmptyCells();

        if (emptyCells.Count < 4)
        {
            Debug.LogError("Not enough empty cells to place all 4 tiles!");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (emptyCells.Count == 0) break;

            int randomIndex = Random.Range(0, emptyCells.Count);
            Vector2Int pos = emptyCells[randomIndex];
            emptyCells.RemoveAt(randomIndex);

            SpawnNumberedTileAt(pos.x, pos.y, i + 1); 
        }
    }

    void SpawnNumberedTileAt(int x, int y, int number)
    {
        GameObject tileObject = Instantiate(tilePrefab, gridContainer);
        RectTransform tileRect = tileObject.GetComponent<RectTransform>();

        tileRect.anchorMin = new Vector2(0, 0);
        tileRect.anchorMax = new Vector2(0, 0);
        tileRect.pivot = new Vector2(0, 0);
        tileRect.anchoredPosition = cells[x, y].anchoredPosition;
        tileRect.sizeDelta = new Vector2(cellSize, cellSize);

        Image tileImage = tileObject.GetComponent<Image>();

        if (number >= 1 && number <= 4 && tileSprites[number - 1] != null)
        {
            tileImage.sprite = tileSprites[number - 1];
        }

       
        tiles[x, y] = tileRect;
        tileNumbers[x, y] = number;
        tileObject.name = "Tile_" + number + "_" + x + "_" + y;

        tileRect.localScale = Vector3.zero;
        StartCoroutine(ScaleTileIn(tileRect));
    }

    IEnumerator ScaleTileIn(RectTransform tile)
    {
        float duration = 0.2f;
        float elapsedTime = 0f;
        Vector2 originalPosition = tile.anchoredPosition;
        Vector2 centerOffset = new Vector2(cellSize / 2, cellSize / 2);

        Vector2 originalPivot = tile.pivot;
        Vector2 originalAnchorMin = tile.anchorMin;
        Vector2 originalAnchorMax = tile.anchorMax;

        tile.pivot = new Vector2(0.5f, 0.5f);
        tile.anchorMin = new Vector2(0, 0);
        tile.anchorMax = new Vector2(0, 0);
        tile.anchoredPosition = originalPosition + centerOffset;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            tile.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        tile.localScale = Vector3.one;

        tile.pivot = originalPivot;
        tile.anchorMin = originalAnchorMin;
        tile.anchorMax = originalAnchorMax;
        tile.anchoredPosition = originalPosition;
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStartPos = touch.position;
                        isTouching = true;
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (isTouching)
                        {
                            touchEndPos = touch.position;
                            ProcessSwipe();
                            isTouching = false;
                        }
                        break;
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
                isTouching = true;
            }
            else if (Input.GetMouseButtonUp(0) && isTouching)
            {
                touchEndPos = Input.mousePosition;
                ProcessSwipe();
                isTouching = false;
            }
        }
    }

    void ProcessSwipe()
    {
        Vector2 swipeDelta = touchEndPos - touchStartPos;

        if (swipeDelta.magnitude < minSwipeDistance)
            return;

        Vector2Int direction;

        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            direction = swipeDelta.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            direction = swipeDelta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        StartCoroutine(MoveTiles(direction));
    }

    IEnumerator MoveTiles(Vector2Int direction)
    {
        isAnimating = true;
        bool moved = false;

        int startX, startY, endX, endY, incrementX, incrementY;

        if (direction.x > 0)
        {
            startX = gridDimension - 1; endX = -1; incrementX = -1;
        }
        else
        {
            startX = 0; endX = gridDimension; incrementX = 1;
        }

        if (direction.y > 0)
        {
            startY = gridDimension - 1; endY = -1; incrementY = -1;
        }
        else
        {
            startY = 0; endY = gridDimension; incrementY = 1;
        }

        List<(RectTransform tile, Vector2 startPos, Vector2 endPos, int tileNumber)> movesToAnimate =
            new List<(RectTransform, Vector2, Vector2, int)>();

        for (int x = startX; x != endX; x += incrementX)
        {
            for (int y = startY; y != endY; y += incrementY)
            {
                if (tiles[x, y] != null)
                {
                    Vector2Int newPos = GetSingleCellMovePosition(new Vector2Int(x, y), direction);

                    if (newPos.x != x || newPos.y != y)
                    {
                        movesToAnimate.Add((
                            tiles[x, y],
                            tiles[x, y].anchoredPosition,
                            cells[newPos.x, newPos.y].anchoredPosition,
                            tileNumbers[x, y]
                        ));

                        tiles[newPos.x, newPos.y] = tiles[x, y];
                        tiles[x, y] = null;

                        tileNumbers[newPos.x, newPos.y] = tileNumbers[x, y];
                        tileNumbers[x, y] = 0;

                        tiles[newPos.x, newPos.y].name = "Tile_" + tileNumbers[newPos.x, newPos.y] + "_" + newPos.x + "_" + newPos.y;

                        moved = true;
                    }
                }
            }
        }

        if (movesToAnimate.Count > 0)
        {
            float duration = 0.15f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;

                foreach (var move in movesToAnimate)
                {
                    move.tile.anchoredPosition = Vector2.Lerp(move.startPos, move.endPos, t);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            foreach (var move in movesToAnimate)
            {
                move.tile.anchoredPosition = move.endPos;
            }
        }

        if (moved)
        {
            CheckWinCondition();
        }

        isAnimating = false;
        yield return null;
    }

    Vector2Int GetSingleCellMovePosition(Vector2Int position, Vector2Int direction)
    {
        Vector2Int nextPos = position + direction;

        if (IsValidPosition(nextPos) && tiles[nextPos.x, nextPos.y] == null && !obstacles[nextPos.x, nextPos.y])
        {
            return nextPos;
        }

        return position;
    }

    bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridDimension && position.y >= 0 && position.y < gridDimension;
    }

    List<Vector2Int> GetAllEmptyCells()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();

        for (int x = 0; x < gridDimension; x++)
        {
            for (int y = 0; y < gridDimension; y++)
            {
                if (tiles[x, y] == null && !obstacles[x, y])
                {
                    emptyCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return emptyCells;
    }

    void CheckWinCondition()
    {
        for (int x = 0; x < gridDimension - 1; x++)
        {
            for (int y = 0; y < gridDimension - 1; y++)
            {
                if (tileNumbers[x, y] == 1 &&
                    tileNumbers[x + 1, y] == 2 &&
                    tileNumbers[x, y + 1] == 3 &&
                    tileNumbers[x + 1, y + 1] == 4)
                {
                    HandleWin();
                    return;
                }
            }
        }
    }

    void HandleWin()
    {
        gameEnd = true;
        Debug.Log("Puzzle Solved! You won!");
        StartCoroutine(WinAnimation());
    }

    IEnumerator WinAnimation()
    {
        List<RectTransform> winningTiles = new List<RectTransform>();

        for (int x = 0; x < gridDimension; x++)
        {
            for (int y = 0; y < gridDimension; y++)
            {
                if (tileNumbers[x, y] >= 1 && tileNumbers[x, y] <= 4)
                {
                    winningTiles.Add(tiles[x, y]);
                }
            }
        }

        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float scale = 1.0f + 0.2f * Mathf.Sin(elapsed * 6f);

            foreach (var tile in winningTiles)
            {
                Vector2 originalPos = tile.anchoredPosition;
                Vector2 centerOffset = new Vector2(cellSize / 2, cellSize / 2);

                tile.pivot = new Vector2(0.5f, 0.5f);
                tile.anchorMin = new Vector2(0, 0);
                tile.anchorMax = new Vector2(0, 0);
                tile.anchoredPosition = originalPos + centerOffset;

                tile.localScale = new Vector3(scale, scale, 1f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var tile in winningTiles)
        {
            tile.localScale = Vector3.one;
        }
        EndGame(true);
    }

    public void ResetGrid()
    {
        StopAllCoroutines();
        isAnimating = false;
        gameEnd = false;
        InitializeGrid();
        PlaceObstacles();
        SpawnGameTiles();
    }

    public void Reset()
    {
        GameManager.Instance.LoadLevel(GameManager.Instance.indexOfThisLevel);
    }
    public void LoadHome()
    {
        GameManager.Instance.Home();
    }
    public void NextLevel()
    {
        GameManager.Instance.NextLevel();   
    }
}