using UnityEngine;
using System.Collections;

public enum GameCondition
{
    GameOver,
    LevelPassed,
    OnGoing
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private GameCondition m_GameCondition;
    private int m_RemainingMoves;

    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
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

    public void SetGameCondition(GameCondition gameCondition)
    {
        m_GameCondition = gameCondition;
    }

    public GameCondition GetGameCondition()
    {
        return m_GameCondition;
    }

    public void SetRemainingMoves(int remainingMoves)
    {
        m_RemainingMoves = remainingMoves;
    }

    public int GetRemainingMoves()
    {
        return m_RemainingMoves;
    }

    #region Top UI remaining moves
    public void CheckRemainingMoves()
    {
        if (m_RemainingMoves > 0)
        {
            m_RemainingMoves--;
            UIManager.Instance.UpdateMoveText(m_RemainingMoves);

            if (m_RemainingMoves == 0)
            {
                // Start 1-sec delayed restart panel
                StartCoroutine(ShowRestartPanelWithDelay());
                return;
            }
        }
    }

    private IEnumerator ShowRestartPanelWithDelay()
    {
        // Wait 1 second
        yield return new WaitForSeconds(1f);
        RestartPanelShowing();
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

    #region Level panels
    public void RestartPanelShowing()
    {
        UIManager.Instance.SetPanelMessage(true, "You are out of moves!\nTap to restart.");
        BoardManager.Instance.ClearBoard();
        m_GameCondition = GameCondition.GameOver;
    }

    public void LevelCompletePanelShowing()
    {
        UIManager.Instance.SetPanelMessage(true, "You completed the level!\nTap for next level.");
        BoardManager.Instance.ClearBoard();
        m_GameCondition = GameCondition.LevelPassed;
    }
    #endregion

    // Parsing blocktype from jsonfile
    public BlockType ParseBlockType(string blockTypeString)
    {
        if (System.Enum.TryParse(blockTypeString, true, out BlockType result))
        {
            return result;
        }
        else
        {
            // default red blocktype
            return BlockType.Red;
        }
    }
}
