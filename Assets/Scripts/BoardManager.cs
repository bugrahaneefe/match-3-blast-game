using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int width = 9;
    public int height = 9;
    public float spacingX;
    public float spacingY;
    public GameObject[] blockPrefabs;

    public Node[,] blockBoard;

    private SpriteRenderer boardRenderer;

    public static BoardManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        boardRenderer = GetComponent<SpriteRenderer>();
        ValidateGridSize();
        AdjustBoardSize();
        GenerateBlocks();
        CenterCamera();
    }

    #region Validate Grid Size
    void ValidateGridSize()
    {
        width = Mathf.Clamp(width, 5, 12);
        height = Mathf.Clamp(height,5, 12);
    }
    #endregion

    #region Adjust Board Size
    void AdjustBoardSize()
    {
        if (boardRenderer != null)
        {
            boardRenderer.size = new Vector2(width + 0.2f, height + 0.4f);
            boardRenderer.transform.position = new Vector3(0, -1, 1);
        }
    }
    #endregion

    #region Generate Blocks
    void GenerateBlocks()
    {

        blockBoard = new Node[width, height];

        spacingX = (float)(width - 1) / 2;
        spacingY = (float)((height - 1) / 2) + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY);

                int randomIndex = Random.Range(0, blockPrefabs.Length);

                GameObject block = Instantiate(blockPrefabs[randomIndex], position, Quaternion.identity);
                block.GetComponent<Block>().SetIndicies(x, y);
                blockBoard[x, y] = new Node(block);
            }
        }
    }
    #endregion

    #region Center Camera
    void CenterCamera()
    {
        Camera.main.transform.position = new Vector3(0, 0, -10);
        float aspectRatio = (float)Screen.width / Screen.height;
        Camera.main.orthographicSize = Mathf.Max(height / 2f + 1, (width / 2f) / aspectRatio + 1);
    }
    #endregion
}
