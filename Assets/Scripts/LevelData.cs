using UnityEngine;

[System.Serializable]
public class BoardBlock
{
    public int x;
    public int y;
    public string blockType;
}

[System.Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public int red;
    public int green;
    public int yellow;
    public int purple;
    public int blue;
    public int duck;
    public int balloon;

    // New fields:
    public bool allowManualBoardGeneration;
    public BoardBlock[] boardBlocks;
}
