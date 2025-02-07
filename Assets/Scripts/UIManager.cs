using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Transform goalUIContainer;
    public GameObject goalRedPrefab;
    public GameObject goalBluePrefab;
    public GameObject goalGreenPrefab;
    public GameObject goalYellowPrefab;
    public GameObject goalPurplePrefab;
    public GameObject goalDuckPrefab;
    public GameObject goalBalloonPrefab;
    // Clones for goal animation
    public GameObject cloneRedPrefab;
    public GameObject cloneBluePrefab;
    public GameObject cloneGreenPrefab;
    public GameObject cloneYellowPrefab;
    public GameObject clonePurplePrefab;
    public GameObject cloneDuckPrefab;
    public GameObject cloneBalloonPrefab;
    // *
    public GameObject particlePrefabA;
    public GameObject particlePrefabB;
    public TMP_Text moveText;
    public TMP_Text levelText;
    public UIDocument UIDoc;
    
    private VisualElement m_GamePanel;
    private Label m_MessageLabel;

    private Dictionary<BlockType, int> remainingGoals = new Dictionary<BlockType, int>();
    private Dictionary<BlockType, GameObject> goalUIElements = new Dictionary<BlockType, GameObject>();
    private Dictionary<BlockType, GameObject> goalPrefabs;

    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    void Start()
    {
        m_GamePanel = UIDoc.rootVisualElement.Q<VisualElement>("ui_background");
        m_MessageLabel = UIDoc.rootVisualElement.Q<Label>("ui_label");
        m_GamePanel.style.visibility = Visibility.Hidden;

        goalPrefabs = new Dictionary<BlockType, GameObject>
        {
            { BlockType.Red, goalRedPrefab },
            { BlockType.Blue, goalBluePrefab },
            { BlockType.Green, goalGreenPrefab },
            { BlockType.Yellow, goalYellowPrefab },
            { BlockType.Purple, goalPurplePrefab },
            { BlockType.Duck, goalDuckPrefab },
            { BlockType.Balloon, goalBalloonPrefab },
        };
    }

    #region Panel message handling
    public void SetPanelMessage(bool isHidden, string text)
    {
        m_MessageLabel.text = text;
        if (isHidden)
        {
            m_GamePanel.style.visibility = Visibility.Visible;
        }
        else
        {
            m_GamePanel.style.visibility = Visibility.Hidden;
        }
    }
    #endregion

    #region Move text
    public void UpdateMoveText(int remainingMoves)
    {
        if (moveText != null)
            moveText.text = remainingMoves.ToString();
    }
    #endregion
    
    #region Load goals
    public void LoadGoals(LevelData currentLevel)
    {
        levelText.text = currentLevel.level_number.ToString();
        remainingGoals.Clear();
        remainingGoals[BlockType.Red] = currentLevel.red;
        remainingGoals[BlockType.Green] = currentLevel.green;
        remainingGoals[BlockType.Yellow] = currentLevel.yellow;
        remainingGoals[BlockType.Purple] = currentLevel.purple;
        remainingGoals[BlockType.Blue] = currentLevel.blue;
        remainingGoals[BlockType.Duck] = currentLevel.duck;
        remainingGoals[BlockType.Balloon] = currentLevel.balloon;
    }
    #endregion
        
    #region Setup goal UI
    public void SetupGoalUI()
    {
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
                GameObject goalBlock = Instantiate(goalPrefabs[goal.Key], goalUIContainer);
                goalUIElements[goal.Key] = goalBlock;

                Transform checkmarkTransform = goalBlock.transform.Find("checkmark");
                if (checkmarkTransform != null)
                {
                    checkmarkTransform.gameObject.SetActive(false);
                }

                TMP_Text goalText = goalBlock.transform.Find($"goal_{goal.Key.ToString().ToLower()}_text")
                                              ?.GetComponent<TMP_Text>();
                if (goalText != null)
                {
                    goalText.text = goal.Value.ToString();
                }

                goalCount++;
            }
        }

        AdjustGoalUISize(goalCount);
    }

    private void AdjustGoalUISize(int goalCount)
    {
        GridLayoutGroup layoutGroup = goalUIContainer.GetComponent<GridLayoutGroup>();
        if (layoutGroup != null)
        {
            if (goalCount > 4)
            {
                layoutGroup.cellSize = new Vector2(50f, 50f);
            }
            else if (goalCount > 3)
            {
                layoutGroup.cellSize = new Vector2(70f, 70f);
            }
            else
            {
                layoutGroup.cellSize = new Vector2(100f, 100f);
            }
        }

        foreach (var goalEntry in goalUIElements)
        {
            BlockType blockType = goalEntry.Key;
            GameObject goalBlock = goalEntry.Value;

            if (goalBlock != null)
            {
                TMP_Text goalText = goalBlock.transform.Find($"goal_{blockType.ToString().ToLower()}_text")
                                          ?.GetComponent<TMP_Text>();
                if (goalText != null)
                {
                    if (goalCount > 4)
                    {
                        goalText.fontSize = 40f;
                    }
                    else if (goalCount > 3)
                    {
                        goalText.fontSize = 50f;
                    }
                    else
                    {
                        goalText.fontSize = 60f;
                    }
                }
            }
        }
    }
    #endregion

    #region Update goals when blocks are removed
    public void UpdateGoals(List<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            if (remainingGoals.ContainsKey(block.blockType) && remainingGoals[block.blockType] > 0)
            {
                remainingGoals[block.blockType]--;

                GenerateBlockClone(block);

                if (goalUIElements.ContainsKey(block.blockType))
                {
                    GameObject uiGoalObject = goalUIElements[block.blockType];
                    TMP_Text goalText = uiGoalObject.transform
                        .Find($"goal_{block.blockType.ToString().ToLower()}_text")
                        ?.GetComponent<TMP_Text>();

                    Transform checkmarkTransform = uiGoalObject.transform.Find("checkmark");

                    if (remainingGoals[block.blockType] == 0)
                    {
                        if (goalText != null) goalText.gameObject.SetActive(false);
                        if (checkmarkTransform != null) checkmarkTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        if (goalText != null)
                        {
                            goalText.text = remainingGoals[block.blockType].ToString();
                        }
                    }
                }
            }
        }
    }

    public bool CheckAllGoalsCompleted()
    {
        foreach (var goal in remainingGoals.Values)
        {
            if (goal > 0)
                return false;
        }
        return true;
    }
    #endregion

    #region Clone animation
    public void GenerateBlockClone(Block block)
    { 
        GameObject clonePrefab = null;

        switch (block.blockType)
        {
            case BlockType.Red: clonePrefab = cloneRedPrefab; break;
            case BlockType.Blue: clonePrefab = cloneBluePrefab; break;
            case BlockType.Green: clonePrefab = cloneGreenPrefab; break;
            case BlockType.Yellow: clonePrefab = cloneYellowPrefab; break;
            case BlockType.Purple: clonePrefab = clonePurplePrefab; break;
            case BlockType.Duck: clonePrefab = cloneDuckPrefab; break;
            case BlockType.Balloon: clonePrefab = cloneBalloonPrefab; break;
            default:
                Debug.LogError($"{block.blockType}");
                return;
        }

        GameObject clone;
        
        if (block.blockType == BlockType.Duck)
        {
            clone = Instantiate(clonePrefab, block.transform.position, Quaternion.identity);
        }
        else
        {
            clone = Instantiate(clonePrefab, block.transform.position, Quaternion.identity);
        }

        Block cloneBlock = clone.GetComponent<Block>();

        SpriteRenderer cloneRenderer = clone.GetComponent<SpriteRenderer>();
        SpriteRenderer originalRenderer = block.GetComponent<SpriteRenderer>();

        if (cloneRenderer && originalRenderer)
        {
            cloneRenderer.sortingOrder = originalRenderer.sortingOrder + 1;
        }
        else if (cloneRenderer)
        {
            cloneRenderer.sortingOrder = 10;
        }

        if (!goalUIElements.ContainsKey(block.blockType))
        {
            Destroy(clone);
            return;
        }

        GameObject goalUI = goalUIElements[block.blockType];

        StartCoroutine(MoveCloneToGoal(clone, goalUI.transform.position));
    }

    private IEnumerator MoveCloneToGoal(GameObject clone, Vector3 goalPosition)
    {
        float duration = 0.4f;
        float elapsedTime = 0f;
        Vector3 startPosition = clone.transform.position;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            clone.transform.position = Vector3.Lerp(startPosition, goalPosition, t);

            yield return null;
        }

        SoundManager.Instance.PlayCollectSound();

        Destroy(clone);

        if (CheckAllGoalsCompleted())
        {
            yield return new WaitForSeconds(1f);
            GameManager.Instance.LevelCompletePanelShowing();
        }
    }
    #endregion

    #region Explosion effect for removed blocks
    public void ExplosionAffecting(Block block)
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
}
