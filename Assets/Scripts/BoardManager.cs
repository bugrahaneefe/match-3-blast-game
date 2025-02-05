using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UIElements;

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
    public Transform goalUIContainer;
    public GameObject goalRedPrefab;
    public GameObject goalBluePrefab;
    public GameObject goalGreenPrefab;
    public GameObject goalYellowPrefab;
    public GameObject goalPurplePrefab;
    public Node[,] blockBoard;
    public GameObject particlePrefabA;
    public GameObject particlePrefabB;
    public TMP_Text moveText;
    public int width = 9;
    public int height = 9;
    public float spacingX;
    public float spacingY;
    public UIDocument UIDoc;
    private VisualElement m_GamePanel;
    private Label m_MessageLabel;  
    private SpriteRenderer boardRenderer;
    private int remainingMoves;
    private GameCondition m_GameCondition;
    
    // ✅ Dictionary to store remaining goals
    private Dictionary<BlockType, int> remainingGoals = new Dictionary<BlockType, int>();
    private Dictionary<BlockType, GameObject> goalUIElements = new Dictionary<BlockType, GameObject>();
    private Dictionary<BlockType, GameObject> goalPrefabs;


    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    private void Start()
    {
        m_GameCondition = GameCondition.OnGoing;
        m_GamePanel = UIDoc.rootVisualElement.Q<VisualElement>("ui_background");
        m_MessageLabel = UIDoc.rootVisualElement.Q<Label>("ui_label");
        m_GamePanel.style.visibility = Visibility.Hidden;


        goalPrefabs = new Dictionary<BlockType, GameObject>
        {
            { BlockType.Red, goalRedPrefab },
            { BlockType.Blue, goalBluePrefab },
            { BlockType.Green, goalGreenPrefab },
            { BlockType.Yellow, goalYellowPrefab },
            { BlockType.Purple, goalPurplePrefab }
        };

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

    private void UpdateMoveText()
    {
        if (moveText != null)
            moveText.text = remainingMoves.ToString();
    }

    #region If mouse clicked on to block
    public void HandleBlockClick(Block clickedBlock)
    {
        if (clickedBlock == null) return;

        List<Block> connectedBlocks = GetConnectedBlocks(clickedBlock);

        // If match found remove the blocks
        if (connectedBlocks.Count >= 2)
        {
            UpdateGoals(connectedBlocks); // ✅ Track goal completion
            CheckRemainingMoves();
            RemoveBlocks(connectedBlocks);
        }
        else if (connectedBlocks.Count >= 5)
        {
            Debug.Log("Rocket implementation");
            UpdateGoals(connectedBlocks); // ✅ Track goal completion
            CheckRemainingMoves();
            RemoveBlocks(connectedBlocks);
        }
    }
    #endregion

    private void CheckRemainingMoves()
    {
        if (remainingMoves > 0)
        {
            remainingMoves--;
            UpdateMoveText();

            if (remainingMoves == 0)
            {
                RestartPanelShowing();
                return;
            }
        }
    }

    private void RestartPanelShowing()
    {
        m_MessageLabel.text = "You are out of moves!\nTap to restart.";
        m_GamePanel.style.visibility = Visibility.Visible;

        ClearBoard();
        m_GameCondition = GameCondition.GameOver;
    }

    private void LevelCompletePanelShowing()
    {
        m_MessageLabel.text = "You completed the level!\nTap for next level.";
        m_GamePanel.style.visibility = Visibility.Visible;

        ClearBoard();
        m_GameCondition = GameCondition.LevelPassed;
    }

    private void RestartLevel()
    {
        m_GameCondition = GameCondition.OnGoing;
        m_GamePanel.style.visibility = Visibility.Hidden;
        
        int currentLevel = LevelManager.Instance.GetCurrentLevelNumber();
        LevelManager.Instance.LoadLevel(currentLevel);
    }

    private void SetNextLevel()
    {
        m_GameCondition = GameCondition.OnGoing;
        m_GamePanel.style.visibility = Visibility.Hidden;
        
        LevelManager.Instance.SetCurrentLevelNumber(LevelManager.Instance.GetCurrentLevelNumber() + 1);
        int currentLevel = LevelManager.Instance.GetCurrentLevelNumber();
        LevelManager.Instance.LoadLevel(currentLevel);
    }

    public void LoadLevelData(int levelNumber)
    {
        ClearBoard();
        LevelData currentLevel = LevelManager.Instance.GetLevel(levelNumber);
        Debug.Log($"Level {currentLevel.level_number} has {currentLevel.move_count} moves.");

        width = currentLevel.grid_width;
        height = currentLevel.grid_height;
        remainingMoves = currentLevel.move_count;

        // ✅ Load goals from level data
        remainingGoals.Clear();
        remainingGoals[BlockType.Red] = currentLevel.red;
        remainingGoals[BlockType.Green] = currentLevel.green;
        remainingGoals[BlockType.Yellow] = currentLevel.yellow;
        remainingGoals[BlockType.Purple] = currentLevel.purple;
        remainingGoals[BlockType.Blue] = currentLevel.blue;

        UpdateMoveText();
        boardRenderer = GetComponent<SpriteRenderer>();
        ValidateBoard();
        GenerateBlocks();
        CenterCamera();

        m_GameCondition = GameCondition.OnGoing;

        SetupGoalUI();
    }
    
    #region ✅ Setup Goal UI Dynamically
    private void SetupGoalUI()
    {
        if (goalUIContainer == null)
        {
            Debug.LogError("❌ goalUIContainer is not assigned in the Unity Inspector!");
            return;
        }

        // ✅ Clear Previous Goal UI Elements
        foreach (Transform child in goalUIContainer)
        {
            Destroy(child.gameObject);
        }

        goalUIElements.Clear();

        int goalCount = 0;
        foreach (var goal in remainingGoals)
        {
            if (goal.Value > 0 && goalPrefabs.ContainsKey(goal.Key))
            {
                // ✅ Instantiate the Correct Goal Prefab
                GameObject goalBlock = Instantiate(goalPrefabs[goal.Key], goalUIContainer);
                goalUIElements[goal.Key] = goalBlock;

                // ✅ Find goal_text Inside Prefab and Update It
                TMP_Text goalText = goalBlock.transform.Find($"goal_{goal.Key.ToString().ToLower()}_text")?.GetComponent<TMP_Text>();
                if (goalText != null)
                {
                    goalText.text = goal.Value.ToString();
                }
                else
                {
                    Debug.LogError($"❌ {goal.Key} prefab is missing goal_{goal.Key.ToString().ToLower()}_text!");
                }

                goalCount++;
            }
        }

        AdjustGoalUISize(goalCount);
    }
    #endregion

#region ✅ Adjust Goal UI Size Based on Number of Goals
private void AdjustGoalUISize(int goalCount)
{
    foreach (var goalEntry in goalUIElements)
    {
        BlockType blockType = goalEntry.Key;
        GameObject goal = goalEntry.Value;

        if (goal == null)
        {
            Debug.LogError($"❌ Goal UI element for {blockType} is missing!");
            continue;
        }

        RectTransform rect = goal.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError($"❌ Missing RectTransform in goal UI for {blockType}!");
            continue;
        }

        // ✅ Corrected text reference
        string goalTextName = $"goal_{blockType.ToString().ToLower()}_text";
        TMP_Text goalText = goal.transform.Find(goalTextName)?.GetComponent<TMP_Text>();

        if (goalText == null)
        {
            Debug.LogError($"❌ {goalTextName} is missing in {blockType} goal prefab!");
            continue;
        }

        // ✅ Resize UI based on goal count
        if (goalCount > 3)
        {
            rect.sizeDelta = new Vector2(50, 50); // Reduce block size
            goalText.fontSize = 18;              // Reduce font size
        }
        else
        {
            rect.sizeDelta = new Vector2(100, 100); // Default size
            goalText.fontSize = 32;              // Default font size
        }
    }
}
#endregion


    #region ✅ Update Goals When Blocks are Removed
 private void UpdateGoals(List<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            if (remainingGoals.ContainsKey(block.blockType) && remainingGoals[block.blockType] > 0)
            {
                remainingGoals[block.blockType]--;

                // ✅ Update UI Text
                if (goalUIElements.ContainsKey(block.blockType))
                {
                    TMP_Text goalText = goalUIElements[block.blockType].transform.Find($"goal_{block.blockType.ToString().ToLower()}_text").GetComponent<TMP_Text>();
                    goalText.text = remainingGoals[block.blockType].ToString();
                }
            }
        }

        // ✅ Check if All Goals Are Met
        if (CheckAllGoalsCompleted())
        {
            LevelCompletePanelShowing();
        }
    }

    private bool CheckAllGoalsCompleted()
    {
        foreach (var goal in remainingGoals.Values)
        {
            if (goal > 0)
                return false;
        }
        return true;
    }
    #endregion

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




    #region Set limit to block generation & handle board background 'border sprite'
    private void ValidateBoard()
    {
        width = Mathf.Clamp(width, 4, 12);
        height = Mathf.Clamp(height, 4, 12);

        if (boardRenderer != null)
        {
            // **Set size dynamically based on grid size**
            boardRenderer.size = new Vector2(width, height);

            // **Adjust background position to center it with the board**
            float bgX = (float)(width - 1) / 2;
            float bgY = (float)((height - 1) / 2) + 1;
            boardRenderer.transform.position = new Vector3(0, -0.5f, 1); // Keep Z at 1 to stay behind the blocks
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

    #region Removing blocks & Apply fall implementations
    private void RemoveBlocks(List<Block> blocksToRemove)
    {
        if (blocksToRemove.Count > 0)
        {
            SoundManager.Instance.PlayCubeExplode();  // ✅ Play sound via SoundManager
        }

        foreach (Block b in blocksToRemove)
        {
            if (b == null) continue;
            ExplosionAffecting(b);
            blockBoard[b.xIndex, b.yIndex] = null;
            Destroy(b.gameObject);
        }

        StartCoroutine(FallExistingBlocks());
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
                    case BlockType.Balloon:    main.startColor = new Color(1.0f, 0.71f, 0.81f); break;
                    case BlockType.Duck:    main.startColor = Color.gray; break;
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

    #region Make block fall down
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

        // Jump animation after landing
        //yield return StartCoroutine(BlockJump(block, fallDistance));

        block.isFalling = false;
    }
    #endregion

    #region Jump animation
    private IEnumerator BlockJump(Block block, float fallDistance)
    {
        if (fallDistance < 1) yield break;

        float jumpHeight = Mathf.Clamp(fallDistance * 0.1f, 0.05f, 0.3f);
        float jumpSpeed = 1f;

        Vector3 originalPos = block.transform.position;
        Vector3 peakPos = originalPos + new Vector3(0, jumpHeight, 0);

        float elapsedTime = 0f;
        while (elapsedTime < 0.15f)
        {
            elapsedTime += Time.deltaTime * jumpSpeed;
            block.transform.position = Vector3.Lerp(originalPos, peakPos, elapsedTime);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < 0.15f)
        {
            elapsedTime += Time.deltaTime * jumpSpeed;
            block.transform.position = Vector3.Lerp(peakPos, originalPos, elapsedTime);
            yield return null;
        }

        block.transform.position = originalPos;
    }
    #endregion
}