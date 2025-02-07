using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public GameObject[] blockPrefabs;
    public Node[,] blockBoard;
    public GameObject rocketBlockPrefab;
    public GameObject rocketLeftPrefab;
    public GameObject rocketRightPrefab;
    public int width = 9;
    public int height = 9;
    public float spacingX;
    public float spacingY;  
    private SpriteRenderer boardRenderer;
    private int currentDuckCount = 0;
    private int maxDucksAllowed = 0;
    private int currentBalloonCount = 0;
    private int maxBalloonAllowed = 0;

    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    private void Start()
    {
        GameManager.Instance.SetGameCondition(GameCondition.OnGoing);
        LevelManager.Instance.LoadLevel(1);
    }

    #region If mouse clicked on to block
    public void HandleBlockClick(Block clickedBlock)
    {
        if (clickedBlock == null) return;

        List<Block> connectedBlocks = GetConnectedBlocks(clickedBlock);

        // If match found remove the blocks
        if (connectedBlocks.Count >= 5)
        {
            UIManager.Instance.UpdateGoals(connectedBlocks);
            
            int xPos = clickedBlock.xIndex;
            int yPos = clickedBlock.yIndex;
            Vector3 worldPosition = clickedBlock.transform.position;

            connectedBlocks.Remove(clickedBlock);

            GameObject rocketObj = Instantiate(rocketBlockPrefab, worldPosition, Quaternion.identity);
            rocketObj.transform.SetParent(this.transform);

            Block rocketBlock = rocketObj.GetComponent<Block>();
            rocketBlock.SetIndicies(xPos, yPos);

            blockBoard[xPos, yPos] = new Node(rocketObj) { isClickable = true };

            RemoveBlocks(connectedBlocks);
            Destroy(clickedBlock.gameObject);

            GameManager.Instance.CheckRemainingMoves();
        }
        else if (connectedBlocks.Count >= 2)
        {
            UIManager.Instance.UpdateGoals(connectedBlocks);
            
            RemoveBlocks(connectedBlocks);

            GameManager.Instance.CheckRemainingMoves();
        }
    }
    #endregion

    public void SpawnRocket(Vector3 startPosition, Vector2 moveDirection)
    {
        GameObject rocketPrefab = (moveDirection.x != 0) ? rocketRightPrefab : rocketLeftPrefab;
        GameObject rocket = Instantiate(rocketPrefab, startPosition, Quaternion.identity);

        if (moveDirection == Vector2.right)
        {
            rocket.transform.rotation = Quaternion.Euler(0, 0, 0);  // Right (No rotation)
        }
        else if (moveDirection == Vector2.left)
        {
            rocket.transform.rotation = Quaternion.Euler(0, 0, 180); // Left (Flipped)
        }
        else if (moveDirection == Vector2.up)
        {
            rocket.transform.rotation = Quaternion.Euler(0, 0, -90);  // Up
        }
        else if (moveDirection == Vector2.down)
        {
            rocket.transform.rotation = Quaternion.Euler(0, 0, 90); // Down
        }

        Rocket rocketScript = rocket.AddComponent<Rocket>();
        rocketScript.direction = moveDirection;
    }

    #region Load level data from level manager and update UI accordingly
    public void LoadLevelData(int levelNumber)
    {
        ClearBoard();
        LevelData currentLevel = LevelManager.Instance.GetLevel(levelNumber);
        Debug.Log($"Level {currentLevel.level_number} has {currentLevel.move_count} moves.");

        width = currentLevel.grid_width;
        height = currentLevel.grid_height;
        GameManager.Instance.SetRemainingMoves(currentLevel.move_count);

        UIManager.Instance.LoadGoals(currentLevel);
        UIManager.Instance.UpdateMoveText(GameManager.Instance.GetRemainingMoves());

        boardRenderer = GetComponent<SpriteRenderer>();
        ValidateBoard();
        CenterCamera();
        GenerateBlocks();

        GameManager.Instance.SetGameCondition(GameCondition.OnGoing);
        UIManager.Instance.SetupGoalUI();
    }
    #endregion

    #region Clear board
    public void ClearBoard()
    {
        if (blockBoard != null)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (blockBoard[x, y] != null && blockBoard[x, y].block != null)
                    {
                        Destroy(blockBoard[x, y].block);
                        blockBoard[x, y] = null;
                    }
                }
            }
        }
    }
    #endregion

    #region Set limit to block generation & handle board background 'border sprite'
    private void ValidateBoard()
    {
        width = Mathf.Clamp(width, 4, 12);
        height = Mathf.Clamp(height, 4, 12);

        if (boardRenderer != null)
        {
            boardRenderer.size = new Vector2(width + 0.2f, height + 0.4f);
            float yPosition = (height % 2 == 0) ? -0.5f : -1f;
            // Adjust background position to center it with the board
            boardRenderer.transform.position = new Vector2(0, yPosition);
        }
    }
    #endregion

    #region Sync camera to board
    private void CenterCamera()
    {
        Camera.main.transform.position = new Vector3(0, 0, -30);
        float ortSize = Mathf.Max(height, width);
        // normalize
        float zoomFactor = Mathf.Lerp(1.13f, 1.1f, (ortSize - 4) / 8f);
        Camera.main.orthographicSize = ortSize * zoomFactor;
    }
    #endregion

    #region Board Generation
    public void GenerateBlocks()
    {
        blockBoard = new Node[width, height];

        spacingX = (float)(width - 1) / 2;
        spacingY = (float)((height - 1) / 2) + 1;

        LevelData currentLevel = LevelManager.Instance.GetLevel(LevelManager.Instance.GetCurrentLevelNumber());

        // Reset counters for ducks and balloons
        maxDucksAllowed = currentLevel.duck;
        currentDuckCount = 0;
        maxBalloonAllowed = currentLevel.balloon;
        currentBalloonCount = 0;

        // ---- NEW: Check if manual board generation is allowed
        if (currentLevel.allowManualBoardGeneration && currentLevel.boardBlocks != null)
        {
            // 1) Initialize entire board to null
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    blockBoard[x, y] = null;
                }
            }

            // 2) Place blocks for each coordinate specified in boardBlocks
            foreach (BoardBlock bb in currentLevel.boardBlocks)
            {
                // Safety checks
                if (bb.x < 0 || bb.x >= width || bb.y < 0 || bb.y >= height)
                {
                    Debug.LogWarning($"Skipping invalid board coordinate ({bb.x}, {bb.y})");
                    continue;
                }

                // Convert string blockType to your actual BlockType enum
                BlockType bt = GameManager.Instance.ParseBlockType(bb.blockType);

                // Find the corresponding prefab (assuming your 'blockPrefabs' array 
                // is ordered by block type or you have a helper method to map block type to prefab).
                GameObject prefabToSpawn = GetPrefabForBlockType(bt);

                if (prefabToSpawn == null)
                {
                    Debug.LogWarning($"No prefab found for blockType '{bb.blockType}' - skipping.");
                    continue;
                }

                // Instantiate at correct position
                Vector2 position = new Vector2(bb.x - spacingX, bb.y - spacingY);
                GameObject blockObj = Instantiate(prefabToSpawn, position, Quaternion.identity);

                // Adjust z-position (duck/balloon slightly forward)
                float zPosition = (bt == BlockType.Duck || bt == BlockType.Balloon) ? -2f : -(float)bb.y / height;
                blockObj.transform.position = new Vector3(position.x, position.y, zPosition);

                // Set up the Block's indices
                Block block = blockObj.GetComponent<Block>();
                block.SetIndicies(bb.x, bb.y);

                // Keep track of the newly created block
                blockBoard[bb.x, bb.y] = new Node(blockObj);

                // Update counters if needed
                if (bt == BlockType.Duck) currentDuckCount++;
                if (bt == BlockType.Balloon) currentBalloonCount++;
            }

            // Optionally, fill in the rest with random blocks if desired:
            FillEmptySpacesWithRandomBlocks(); // see snippet below if you want partial random

        }
        else
        {
            // ---- If manual generation is NOT allowed or boardBlocks is null, do your existing random approach:

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 position = new Vector2(x - spacingX, y - spacingY);

                    int randomIndex;

                    // 🚀 Ensure bottom row (y == 0) does not have Duck
                    do
                    {
                        randomIndex = GetValidRandomPrefabIndex();
                    } 
                    while (y == 0 && blockPrefabs[randomIndex].GetComponent<Block>().blockType == BlockType.Duck);

                    // ✅ Now safe to instantiate
                    GameObject blockObj = Instantiate(blockPrefabs[randomIndex], position, Quaternion.identity);
                    
                    Block block = blockObj.GetComponent<Block>();
                    float zPosition = (block.blockType == BlockType.Balloon || block.blockType == BlockType.Duck)
                        ? -2f
                        : -(float)y / height;
                    blockObj.transform.position = new Vector3(position.x, position.y, zPosition);

                    block.SetIndicies(x, y);
                    blockBoard[x, y] = new Node(blockObj);
                }
            }
        }
    }

    private GameObject GetPrefabForBlockType(BlockType blockType)
    {
        // 🚀 Handle Rockets Separately
        if (blockType == BlockType.Rocket)
        {
            return rocketBlockPrefab;
        }

        // 🎨 Handle Normal Blocks Using blockPrefabs[]
        int index = (int)blockType;
        if (index < 0 || index >= blockPrefabs.Length)
        {
            Debug.LogError($"Invalid block type index: {index} for {blockType}");
            return null;
        }

        return blockPrefabs[index];
    }


    private int GetValidRandomPrefabIndex()
    {
        int randomIndex;
        BlockType blockType;

        do
        {
            randomIndex = Random.Range(0, blockPrefabs.Length);
            blockType = blockPrefabs[randomIndex].GetComponent<Block>().blockType;

            // 🚀 If Duck limit is reached, ignore Ducks but allow other blocks
            if (blockType == BlockType.Duck && currentDuckCount >= maxDucksAllowed)
            {
                continue;
            }

            // 🎈 If Balloon limit is reached, ignore Balloons but allow other blocks
            if (blockType == BlockType.Balloon && currentBalloonCount >= maxBalloonAllowed)
            {
                continue;
            }

            // ✅ Found a valid block
            break;

        } while (true); // Keeps looping until a valid block is found

        // If Duck or Balloon is selected, update counts
        if (blockType == BlockType.Duck) currentDuckCount++;
        if (blockType == BlockType.Balloon) currentBalloonCount++;

        return randomIndex;
    }

    private void FillEmptySpacesWithRandomBlocks()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (blockBoard[x, y] == null)
                {
                    Vector2 position = new Vector2(x - spacingX, y - spacingY);

                    int randomIndex;

                    // 🚀 Ensure bottom row (y == 0) does not have Duck
                    do
                    {
                        randomIndex = GetValidRandomPrefabIndex();
                    } 
                    while (y == 0 && blockPrefabs[randomIndex].GetComponent<Block>().blockType == BlockType.Duck);

                    // ✅ Now safe to instantiate
                    GameObject blockObj = Instantiate(blockPrefabs[randomIndex], position, Quaternion.identity);

                    Block block = blockObj.GetComponent<Block>();
                    float zPosition = (block.blockType == BlockType.Balloon || block.blockType == BlockType.Duck)
                        ? -2f
                        : -(float)y / height;
                    blockObj.transform.position = new Vector3(position.x, position.y, zPosition);

                    block.SetIndicies(x, y);
                    blockBoard[x, y] = new Node(blockObj);
                }
            }
        }
    }
    #endregion
    
    #region Each matched pairs should be unique & stored in list of blocks
    public List<Block> GetConnectedBlocks(Block startBlock)
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
    public List<Block> GetNeighbors(Block block)
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

    #region Remove blocks & Apply fall implementations
    public void RemoveBlocks(List<Block> blocksToRemove, bool isTriggeredByRocket = false)
    {
        if (blocksToRemove.Count > 0)
        {
            SoundManager.Instance.PlayCubeExplode(); // Default block explosion sound
        }

        HashSet<Block> blocksToDestroy = new HashSet<Block>(blocksToRemove);

        foreach (Block b in blocksToRemove)
        {
            if (b == null) continue;

            if (!(isTriggeredByRocket)) {
                List<Block> adjacentBalloons = GetAdjacentBalloons(b);
                foreach (Block balloon in adjacentBalloons)
                {
                    if (!blocksToDestroy.Contains(balloon))
                    {
                        blocksToDestroy.Add(balloon);
                    }
                }
            }
        }

        foreach (Block b in blocksToDestroy)
        {
            if (b == null) continue;

            UIManager.Instance.ExplosionAffecting(b);

            if (b.blockType == BlockType.Balloon)
            {
                SoundManager.Instance.PlayBalloonExplode();
                UIManager.Instance.UpdateGoals(new List<Block> { b });
            }
            else if (b.blockType == BlockType.Duck)
            {
                SoundManager.Instance.PlayDuckExplode();
                UIManager.Instance.UpdateGoals(new List<Block> { b });
            }

            Destroy(b.gameObject);
            blockBoard[b.xIndex, b.yIndex] = null;
        }

        StartCoroutine(FallExistingBlocks());
    }
    #endregion

    #region Remove duck block
    private void RemoveDuckBlock(Block duckBlock)
    {
        if (duckBlock == null) return;

        // Update ui goals for a single block
        List<Block> singleDuck = new List<Block> { duckBlock };

        Debug.Log(duckBlock.blockType);

        // Remove from board
        RemoveBlocks(singleDuck);

        // Check if that completes all goals
        if (UIManager.Instance.CheckAllGoalsCompleted())
        {
            GameManager.Instance.LevelCompletePanelShowing();
        }
    }
    #endregion

    private List<Block> GetAdjacentBalloons(Block block)
    {
        List<Block> balloonsToRemove = new List<Block>();

        int x = block.xIndex;
        int y = block.yIndex;

        // Check up
        if (y + 1 < height && blockBoard[x, y + 1]?.block != null)
        {
            Block upBlock = blockBoard[x, y + 1].block.GetComponent<Block>();
            if (upBlock.blockType == BlockType.Balloon)
            {
                balloonsToRemove.Add(upBlock);
            }
        }

        // Check down
        if (y - 1 >= 0 && blockBoard[x, y - 1]?.block != null)
        {
            Block downBlock = blockBoard[x, y - 1].block.GetComponent<Block>();
            if (downBlock.blockType == BlockType.Balloon)
            {
                balloonsToRemove.Add(downBlock);
            }
        }

        // Check left
        if (x - 1 >= 0 && blockBoard[x - 1, y]?.block != null)
        {
            Block leftBlock = blockBoard[x - 1, y].block.GetComponent<Block>();
            if (leftBlock.blockType == BlockType.Balloon)
            {
                balloonsToRemove.Add(leftBlock);
            }
        }

        // Check right
        if (x + 1 < width && blockBoard[x + 1, y]?.block != null)
        {
            Block rightBlock = blockBoard[x + 1, y].block.GetComponent<Block>();
            if (rightBlock.blockType == BlockType.Balloon)
            {
                balloonsToRemove.Add(rightBlock);
            }
        }

        return balloonsToRemove;
    }

    #region Fall Implementation for existing blocks
    public IEnumerator FallExistingBlocks()
    {
        bool hasFallingBlocks = false;
        yield return new WaitForSeconds(0.2f);
        for (int x = 0; x < width; x++)
        {
            int writeIndex = 0;

            for (int y = 0; y < height; y++)
            {
                if (blockBoard[x, y] != null)
                {
                    if (writeIndex != y)
                    {
                        blockBoard[x, writeIndex] = blockBoard[x, y];
                        blockBoard[x, y] = null;

                        Block block = blockBoard[x, writeIndex].block.GetComponent<Block>();
                        float fallDistance = Mathf.Abs(y - writeIndex);

                        block.SetIndicies(x, writeIndex);

                        float finalY = writeIndex - spacingY;
                        StartCoroutine(MoveBlockDown(block, finalY, fallDistance));

                        hasFallingBlocks = true;
                    }

                    writeIndex++;
                }
            }
        }

        if (hasFallingBlocks)
        {
            yield return new WaitForSeconds(0.3f);
        }

        if (GameManager.Instance.GetGameCondition() == GameCondition.OnGoing) 
        {
            // Fall new blocks to emptied spaces
            StartCoroutine(FallNewBlocks());
        }
    }
    #endregion

    #region Fall Implementation for new blocks
    private IEnumerator FallNewBlocks()
    {
        bool hasNewBlocks = false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (blockBoard[x, y] == null)
                {
                    hasNewBlocks = true;
                    int randomIndex = Random.Range(0, blockPrefabs.Length);
                    Block blockComponent = blockPrefabs[randomIndex].GetComponent<Block>();

                    // Avoid extra balloons or ducks if we are over the limit
                    if (blockComponent != null)
                    {
                        bool isDuck = blockComponent.blockType == BlockType.Duck;
                        bool isBalloon = blockComponent.blockType == BlockType.Balloon;

                        if ((isDuck && currentDuckCount >= maxDucksAllowed) || (isBalloon && currentBalloonCount >= maxBalloonAllowed))
                        {
                            do
                            {
                                randomIndex = Random.Range(0, blockPrefabs.Length);
                                blockComponent = blockPrefabs[randomIndex].GetComponent<Block>();

                            } while ((blockComponent != null && blockComponent.blockType == BlockType.Duck && currentDuckCount >= maxDucksAllowed) ||
                                    (blockComponent != null && blockComponent.blockType == BlockType.Balloon && currentBalloonCount >= maxBalloonAllowed));
                        }

                        if (blockComponent.blockType == BlockType.Duck)
                        {
                            currentDuckCount++;
                        }
                        else if (blockComponent.blockType == BlockType.Balloon)
                        {
                            currentBalloonCount++;
                        }
                    }

                    GameObject blockObj = Instantiate(blockPrefabs[randomIndex]);
                    blockObj.transform.SetParent(this.transform);

                    float zPosition = (blockComponent.blockType == BlockType.Balloon || blockComponent.blockType == BlockType.Duck)
                        ? -2f  // Set them forward in Z-space
                        : -(float)y / height;  // Default for other block
                    float spawnY = height + 1;
                    float finalY = y - spacingY;

                    blockObj.transform.position = new Vector3(x - spacingX, spawnY, zPosition);

                    Block block = blockObj.GetComponent<Block>();
                    block.SetIndicies(x, y);
                    block.isFalling = true;

                    blockBoard[x, y] = new Node(blockObj);

                    StartCoroutine(MoveBlockDown(block, finalY, spawnY - finalY));
                }
            }
        }

        if (hasNewBlocks)
        {
            yield return new WaitForSeconds(0.3f);
        }
    }
    #endregion

    #region Animation for downing blocks
    private IEnumerator MoveBlockDown(Block block, float targetY, float fallDistance)
    {
        block.isFalling = true;

        float fallSpeed = 4f;
        Vector3 startPos = block.transform.position;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);

        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * fallSpeed;
            block.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime);
            yield return null;
        }

        block.transform.position = targetPos;
        float zPosition = (block.blockType == BlockType.Balloon || block.blockType == BlockType.Duck)
            ? -2f  // Set them forward in Z-space
            : -(float)block.yIndex / height;  // Default for other block
        block.transform.position = new Vector3(targetPos.x, targetPos.y, zPosition);

        yield return StartCoroutine(JumpAnimation(block, fallDistance));

        block.isFalling = false;

        // Duck removal at bottom
        if (block.blockType == BlockType.Duck && block.yIndex == 0 && GameManager.Instance.GetGameCondition() == GameCondition.OnGoing)
        {
            StartCoroutine(DelayedDuckRemoval(block, 0.2f));
        }
    }

    // Slightly delayed removal for duck block
    private IEnumerator DelayedDuckRemoval(Block duckBlock, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (duckBlock != null)
        {
            RemoveDuckBlock(duckBlock);
        }
    }
    #endregion

    #region Jump animation
    private IEnumerator JumpAnimation(Block block, float fallDistance)
    {
        if (fallDistance < 1) yield break;

        float jumpHeight = Mathf.Clamp(fallDistance * 0.07f, 0.05f, 0.3f);
        float jumpSpeed = Mathf.Clamp(fallDistance * 0.25f, 1f, 1.8f);

        Vector3 originalPos = block.transform.position;
        Vector3 peakPos = originalPos + new Vector3(0, jumpHeight, 0);

        float upTime = Mathf.Clamp(0.06f * fallDistance, 0.06f, 0.15f);
        float downTime = upTime * 0.6f;

        float elapsedTime = 0f;

        while (elapsedTime < upTime)
        {
            elapsedTime += Time.deltaTime * jumpSpeed;
            block.transform.position = Vector3.Lerp(originalPos, peakPos, elapsedTime / upTime);
            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < downTime)
        {
            elapsedTime += Time.deltaTime * jumpSpeed;
            block.transform.position = Vector3.Lerp(peakPos, originalPos, elapsedTime / downTime);
            yield return null;
        }

        block.transform.position = originalPos;
    }
    #endregion
}