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
            { BlockType.Purple, goalPurplePrefab }
        };
    }

    #region Set panel message visible or not with a text
    public void SetPanelMessage(bool isHidden, string text)
    {
        m_MessageLabel.text = text;
        if (isHidden)
        {
            m_GamePanel.style.visibility = Visibility.Visible;
        } else
        {
            m_GamePanel.style.visibility = Visibility.Hidden;
        }
    }
    #endregion

    #region Move text updates
    public void UpdateMoveText(int remainingMoves)
    {
        if (moveText != null)
            moveText.text = remainingMoves.ToString();
    }
    #endregion
    
    #region Load goals from level data
    public void LoadGoals(LevelData currentLevel)
     {
        levelText.text = currentLevel.level_number.ToString();
        remainingGoals.Clear();
        remainingGoals[BlockType.Red] = currentLevel.red;
        remainingGoals[BlockType.Green] = currentLevel.green;
        remainingGoals[BlockType.Yellow] = currentLevel.yellow;
        remainingGoals[BlockType.Purple] = currentLevel.purple;
        remainingGoals[BlockType.Blue] = currentLevel.blue;
    }
    #endregion
        
    #region Setup goal_ui dynamically
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

                TMP_Text goalText = goalBlock.transform.Find($"goal_{goal.Key.ToString().ToLower()}_text")?.GetComponent<TMP_Text>();
                if (goalText != null)
                {
                    goalText.text = goal.Value.ToString();
                }
                goalCount++;
            }
        }

        AdjustGoalUISize(goalCount);
    }
    #endregion

    #region Adjust Goal UI Cell Size and Font Size Based on Number of Goals
    private void AdjustGoalUISize(int goalCount)
    {
        GridLayoutGroup layoutGroup = goalUIContainer.GetComponent<GridLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.cellSize = goalCount > 3 ? new Vector2(70f, 70f) : new Vector2(100f, 100f);
        }

        foreach (var goalEntry in goalUIElements)
        {
            BlockType blockType = goalEntry.Key;
            GameObject goalBlock = goalEntry.Value;

            if (goalBlock != null)
            {
                TMP_Text goalText = goalBlock.transform.Find($"goal_{blockType.ToString().ToLower()}_text")?.GetComponent<TMP_Text>();
                if (goalText != null)
                {
                    goalText.fontSize = goalCount > 3 ? 50f : 60f;
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

                if (goalUIElements.ContainsKey(block.blockType))
                {
                    TMP_Text goalText = goalUIElements[block.blockType].transform.Find($"goal_{block.blockType.ToString().ToLower()}_text").GetComponent<TMP_Text>();
                    goalText.text = remainingGoals[block.blockType].ToString();
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
}
