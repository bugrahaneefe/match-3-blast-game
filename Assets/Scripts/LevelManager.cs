using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    private static LevelManager _instance;
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("LevelManager");
                _instance = obj.AddComponent<LevelManager>();
                DontDestroyOnLoad(obj);
                _instance.Initialize();
            }
            return _instance;
        }
    }

    public string levelsDirectory = "Levels";
    private Dictionary<int, LevelData> levels = new Dictionary<int, LevelData>();
    private int currentLevelNumber = 1;

    public void Initialize()
    {
        LoadLevels();
    }

    private void LoadLevels()
    {
        string path = Path.Combine(Application.streamingAssetsPath, levelsDirectory);
        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            LevelData level = JsonUtility.FromJson<LevelData>(json);

            if (level != null)
            {
                levels[level.level_number] = level;
            }
            else
            {
                Debug.LogError($"Failed to parse JSON: {file}");
            }
        }
    }

    public LevelData GetLevel(int levelNumber)
    {
        if (levels.TryGetValue(levelNumber, out LevelData level))
        {
            return level;
        }
        else
        {
            Debug.LogError($"Level {levelNumber} not found!");
            return null;
        }
    }

    public int GetCurrentLevelNumber()
    {
        return currentLevelNumber;
    }

    public void SetCurrentLevelNumber(int levelNumber)
    {
        currentLevelNumber = levelNumber;
    }

    public void LoadLevel(int levelNumber)
    {
        if (!levels.ContainsKey(levelNumber))
        {
            UIManager.Instance.SetPanelMessage(true, "Game Completed!");
            Debug.LogError($"Level {levelNumber} does not exist!");
            return;
        }

        currentLevelNumber = levelNumber;
        
        BoardManager.Instance.LoadLevelData(levelNumber);
    }
}
