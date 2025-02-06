using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public enum GameCondition
{
    GameOver,
    LevelPassed,
    OnGoing
}

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public GameObject[] blockPrefabs;
    public Node[,] blockBoard;
    public GameObject particlePrefabA;
    public GameObject particlePrefabB;
    public int width = 9;
    public int height = 9;
    public float spacingX;
    public float spacingY;  
    private SpriteRenderer boardRenderer;
    private int remainingMoves;
    private GameCondition m_GameCondition;

    private int currentDuckCount = 0;
    private int maxDucksAllowed = 0;

    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    private void Start()
    {
        m_GameCondition = GameCondition.OnGoing;
        LevelManager.Instance.LoadLevel(1);
    }

    private void Update()
    {
        if (m_GameCondition == GameCondition.GameOver)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                //RestartLevel();
            }

            if (Input.GetMouseButtonDown(0))
            {
                //RestartLevel();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                RestartLevel();
            }
        }
        else if (m_GameCondition == GameCondition.LevelPassed)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SetNextLevel();
            }
        }
    }

    #region If mouse clicked on to block
    public void HandleBlockClick(Block clickedBlock)
    {
        if (clickedBlock == null) return;

        List<Block> connectedBlocks = GetConnectedBlocks(clickedBlock);

        // If match found remove the blocks
        if (connectedBlocks.Count >= 2)
        {
            UIManager.Instance.UpdateGoals(connectedBlocks);
            if (UIManager.Instance.CheckAllGoalsCompleted())
            {
                LevelCompletePanelShowing();
            }
            CheckRemainingMoves();
            RemoveBlocks(connectedBlocks);
        }
        else if (connectedBlocks.Count >= 5)
        {
            Debug.Log("Rocket implementation");
            UIManager.Instance.UpdateGoals(connectedBlocks);
            if (UIManager.Instance.CheckAllGoalsCompleted())
            {
                LevelCompletePanelShowing();
            }
            CheckRemainingMoves();
            RemoveBlocks(connectedBlocks);
        }
    }
    #endregion

    #region Top UI remaining moves
    private void CheckRemainingMoves()
    {
        if (remainingMoves > 0)
        {
            remainingMoves--;
            UIManager.Instance.UpdateMoveText(remainingMoves);

            if (remainingMoves == 0)
            {
                RestartPanelShowing();
                return;
            }
        }
    }
    #endregion

    #region Level panels
    private void RestartPanelShowing()
    {
        UIManager.Instance.SetPanelMessage(true, "You are out of moves!\nTap to restart.");
        ClearBoard();
        m_GameCondition = GameCondition.GameOver;
    }

    private void LevelCompletePanelShowing()
    {
        UIManager.Instance.SetPanelMessage(true, "You completed the level!\nTap for next level.");
        ClearBoard();
        m_GameCondition = GameCondition.LevelPassed;
    }
    #endregion

    #region Level creation
    private void RestartLevel()
    {
        m_GameCondition = GameCondition.OnGoing;
        UIManager.Instance.SetPanelMessage(false, "RestartLevel");
        
        int currentLevel = LevelManager.Instance.GetCurrentLevelNumber();
        LevelManager.Instance.LoadLevel(currentLevel);
    }

    private void SetNextLevel()
    {
        m_GameCondition = GameCondition.OnGoing;
        UIManager.Instance.SetPanelMessage(false, "SetNextLevel");
        
        LevelManager.Instance.SetCurrentLevelNumber(LevelManager.Instance.GetCurrentLevelNumber() + 1);
        int currentLevel = LevelManager.Instance.GetCurrentLevelNumber();
        LevelManager.Instance.LoadLevel(currentLevel);
    }
    #endregion

    #region Load level data from level manager and update UI accordingly
    public void LoadLevelData(int levelNumber)
    {
        ClearBoard();
        LevelData currentLevel = LevelManager.Instance.GetLevel(levelNumber);
        Debug.Log($"Level {currentLevel.level_number} has {currentLevel.move_count} moves.");

        width = currentLevel.grid_width;
        height = currentLevel.grid_height;
        remainingMoves = currentLevel.move_count;

        UIManager.Instance.LoadGoals(currentLevel);
        UIManager.Instance.UpdateMoveText(remainingMoves);

        boardRenderer = GetComponent<SpriteRenderer>();
        ValidateBoard();
        CenterCamera();
        GenerateBlocks();

        m_GameCondition = GameCondition.OnGoing;
        UIManager.Instance.SetupGoalUI();
    }
    #endregion

    #region Clear board
    private void ClearBoard()
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
        Camera.main.transform.position = new Vector3(0, 0, -10);
        float ortSize = Mathf.Max(height, width);
        Camera.main.orthographicSize = ortSize;
    }
    #endregion

    #region Board Generation
    private void GenerateBlocks()
    {
        blockBoard = new Node[width, height];

        spacingX = (float)(width - 1) / 2;
        spacingY = (float)((height - 1) / 2) + 1;

        LevelData currentLevel = LevelManager.Instance.GetLevel(LevelManager.Instance.GetCurrentLevelNumber());
        maxDucksAllowed = currentLevel.duck;
        // Reset current duck for next level
        currentDuckCount = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY);
                int randomIndex = Random.Range(0, blockPrefabs.Length);
                Block blockComponent = blockPrefabs[randomIndex].GetComponent<Block>();

                // If maxDucksAllowed is zero, dont generate duck at all.
                if (maxDucksAllowed == 0 && blockComponent != null && blockComponent.blockType == BlockType.Duck)
                {
                    do
                    {
                        randomIndex = Random.Range(0, blockPrefabs.Length);
                        blockComponent = blockPrefabs[randomIndex].GetComponent<Block>();
                    } while (blockComponent != null && blockComponent.blockType == BlockType.Duck);
                }

                // No duck generation at bottom
                if ((y == 0) || (blockComponent != null && blockComponent.blockType == BlockType.Duck && currentDuckCount >= maxDucksAllowed))
                {
                    bool allowExtraDuck = Random.value < 0.15f;

                    if (!allowExtraDuck || y == 0)
                    {
                        do
                        {
                            randomIndex = Random.Range(0, blockPrefabs.Length);
                            blockComponent = blockPrefabs[randomIndex].GetComponent<Block>();
                        } while (blockComponent != null && blockComponent.blockType == BlockType.Duck);
                    }
                }

                if (blockComponent != null && blockComponent.blockType == BlockType.Duck)
                {
                    currentDuckCount++;
                }

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
    
    #region Each matched pairs should be unique & stored in list of blocks
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

    #region Remove blocks & Apply fall implementations
    private void RemoveBlocks(List<Block> blocksToRemove)
    {
        if (blocksToRemove.Count > 0)
        {
            SoundManager.Instance.PlayCubeExplode();
        }

        foreach (Block b in blocksToRemove)
        {
            if (b == null) continue;

            ExplosionAffecting(b);
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
        UIManager.Instance.UpdateGoals(singleDuck);

        // Check if that completes all goals
        if (UIManager.Instance.CheckAllGoalsCompleted())
        {
            LevelCompletePanelShowing();
        }

        // Remove from board
        RemoveBlocks(singleDuck);
    }
    #endregion

    #region Explosion effect for removed blocks
    private void ExplosionAffecting(Block block)
    {
        if (block == null) return;

        GameObject chosenPrefab = Random.value > 0.5f ? particlePrefabA : particlePrefabB;
        GameObject explosion = Instantiate(chosenPrefab, block.transform.position, Quaternion.identity);
        ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            var main = particleSystem.main;  
            SpriteRenderer blockRenderer = block.GetComponent<SpriteRenderer>();

            if (blockRenderer != null)
            {
                // Color based on block type
                switch (block.blockType)
                {
                    case BlockType.Red:     main.startColor = Color.red; break;
                    case BlockType.Blue:    main.startColor = Color.blue; break;
                    case BlockType.Purple:  main.startColor = new Color(0.74f, 0.56f, 0.94f); break;
                    case BlockType.Green:   main.startColor = Color.green; break;
                    case BlockType.Yellow:  main.startColor = Color.yellow; break;
                    case BlockType.Balloon: main.startColor = new Color(1.0f, 0.71f, 0.81f); break;
                    case BlockType.Duck:    main.startColor = Color.yellow; break;
                    default: main.startColor = Color.white; break;
                }
            }

            main.startSpeed = Random.Range(3f, 6f); 
            main.gravityModifier = 1.5f;
            var emission = particleSystem.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0, Random.Range(5, 10)));  

            StartCoroutine(RandomizeParticleSizes(particleSystem));
        }

        ParticleSystemRenderer renderer = explosion.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Default";  
            renderer.sortingOrder = 2;  
        }

        Destroy(explosion, 1.7f);
    }

    private IEnumerator RandomizeParticleSizes(ParticleSystem particleSystem)
    {
        yield return new WaitForSeconds(0.05f);

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        int count = particleSystem.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            particles[i].startSize = Random.Range(0.16f, 0.215f);
        }

        particleSystem.SetParticles(particles, count);
    }
    #endregion

    #region Fall Implementation for existing blocks
    private IEnumerator FallExistingBlocks()
    {
        bool hasFallingBlocks = false;

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

        if (m_GameCondition == GameCondition.OnGoing) 
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

                    // Avoid extra ducks if we are over the limit
                    if (blockComponent != null && blockComponent.blockType == BlockType.Duck)
                    {
                        currentDuckCount++;
                        if (currentDuckCount > maxDucksAllowed)
                        {
                            int indexWithoutDuck = Random.Range(0, blockPrefabs.Length - 1);
                            randomIndex = indexWithoutDuck;
                        }
                    }

                    GameObject blockObj = Instantiate(blockPrefabs[randomIndex]);
                    blockObj.transform.SetParent(this.transform);

                    float spawnY = height + 1;
                    float finalY = y - spacingY;

                    blockObj.transform.position = new Vector3(x - spacingX, spawnY, -(float)y / height);

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
        float zPosition = -(float)block.yIndex / height;
        block.transform.position = new Vector3(targetPos.x, targetPos.y, zPosition);

        yield return StartCoroutine(JumpAnimation(block, fallDistance));

        block.isFalling = false;

        // Duck removal at bottom
        if (block.blockType == BlockType.Duck && block.yIndex == 0 && m_GameCondition == GameCondition.OnGoing)
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