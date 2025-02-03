using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType blockType;

    public int xIndex;
    public int yIndex;

    public bool isMatched;

    public Block(int _x, int _y)
    {
        xIndex = _x;
        yIndex = _y;
    }

    public void SetIndicies(int _x, int _y)
    {
        xIndex = _x;
        yIndex = _y;
    }


}

public enum BlockType
{
    Red,
    Blue,
    Purple,
    Green,
    Yellow,
    Duck,
    Balloon
}
