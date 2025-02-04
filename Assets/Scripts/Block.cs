using UnityEngine;

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

public class Block : MonoBehaviour
{
    public BlockType blockType;
    public int xIndex;
    public int yIndex;

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

    #region Add 2D Box Collider to block if is not available, to determine mouse clicks
    private void Start()
    {
        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
    }
    #endregion

    #region If block is clickable call HandleBlockClick from BoardManager
    private void OnMouseDown()
    {
        Node node = BoardManager.Instance.blockBoard[xIndex, yIndex];

        if (node == null || !node.isClickable)
        {
            return;
        }

        BoardManager.Instance.HandleBlockClick(this);
    }
    #endregion
}