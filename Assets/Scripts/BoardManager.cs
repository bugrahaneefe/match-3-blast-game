using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public GameObject[] blockPrefabs;
    public Node[,] blockBoard;
    public int width = 9;
    public int height = 9;
    public float spacingX;
    public float spacingY;
    private SpriteRenderer boardRenderer;

    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    private void Start()
    {
        boardRenderer = GetComponent<SpriteRenderer>();
        ValidateBoard();
        GenerateBlocks();
        CenterCamera();
    }

    #region If mouse clicked on to block
    public void HandleBlockClick(Block clickedBlock)
    {
        if (clickedBlock == null) return;

        List<Block> connectedBlocks = GetConnectedBlocks(clickedBlock);

        // If match found remove the blocks
        if (connectedBlocks.Count >= 2 && connectedBlocks.Count <= 5)
        {
            RemoveBlocks(connectedBlocks);
        }
        // No match
        else { }
    }
    #endregion

    #region Set limit to block generation & handle board background 'border sprite'
    private void ValidateBoard()
    {
        width = Mathf.Clamp(width, 5, 12);
        height = Mathf.Clamp(height, 5, 12);
        if (boardRenderer != null)
        {
            boardRenderer.size = new Vector2(width + 0.2f, height + 0.4f);
        }
    }
    #endregion

    #region Sync camera to board
    private void CenterCamera()
    {
        Camera.main.transform.position = new Vector3(0, 0, -10);
        float aspectRatio = (float)Screen.width / Screen.height;
        Camera.main.orthographicSize = Mathf.Max(height / 2f + 1, (width / 2f) / aspectRatio + 1);
    }
    #endregion

    #region Board generation
    private void GenerateBlocks()
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
                GameObject blockObj = Instantiate(blockPrefabs[randomIndex], position, Quaternion.identity);

                float zPosition = -(float)y / height; 
                blockObj.transform.position = new Vector3(position.x, position.y, zPosition);

                Block block = blockObj.GetComponent<Block>();
                block.SetIndicies(x, y);

                blockBoard[x, y] = new Node(blockObj);
            }
        }
    }
    #endregion

    #region Each match should be unique & stored in list of blocks
    private List<Block> GetConnectedBlocks(Block startBlock)
    {
        List<Block> result = new List<Block>();
        Queue<Block> queue = new Queue<Block>();
        HashSet<Block> visited = new HashSet<Block>();

        queue.Enqueue(startBlock);
        visited.Add(startBlock);

        while (queue.Count > 0)
        {
            Block current = queue.Dequeue();
            result.Add(current);

            foreach (Block neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor) && neighbor.blockType == startBlock.blockType)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }
    #endregion

    #region Determine matches
    private List<Block> GetNeighbors(Block block)
    {
        List<Block> neighbors = new List<Block>();

        int x = block.xIndex;
        int y = block.yIndex;

        // Check up
        if (y + 1 < height && blockBoard[x, y + 1]?.block != null)
        {
            neighbors.Add(blockBoard[x, y + 1].block.GetComponent<Block>());
        }
        // Check down
        if (y - 1 >= 0 && blockBoard[x, y - 1]?.block != null)
        {
            neighbors.Add(blockBoard[x, y - 1].block.GetComponent<Block>());
        }
        // Check left
        if (x - 1 >= 0 && blockBoard[x - 1, y]?.block != null)
        {
            neighbors.Add(blockBoard[x - 1, y].block.GetComponent<Block>());
        }
        // Check right
        if (x + 1 < width && blockBoard[x + 1, y]?.block != null)
        {
            neighbors.Add(blockBoard[x + 1, y].block.GetComponent<Block>());
        }

        return neighbors;
    }
    #endregion

    #region Removing blocks
    private void RemoveBlocks(List<Block> blocksToRemove)
    {
        foreach (Block b in blocksToRemove)
        {
            if (b == null) continue;
            blockBoard[b.xIndex, b.yIndex] = null;
            Destroy(b.gameObject);
        }
    }
    #endregion
}
