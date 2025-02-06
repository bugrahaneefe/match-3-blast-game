using UnityEngine;

public enum BlockType
{
    Red,
    Blue,
    Purple,
    Green,
    Yellow,
    Duck,
    Balloon,
    Rocket
}

public class Block : MonoBehaviour
{
    public BlockType blockType;
    public int xIndex;
    public int yIndex;
    public bool isFalling = false;
    private bool isVerticalRocket = false;

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

    private void Awake()
    {
        if (blockType == BlockType.Rocket)
        {
            isVerticalRocket = (Random.value < 0.5f);
        }
    }

    #region Add 2D Box Collider to block if is not available, to determine mouse clicks
    private void Start()
    {
        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        if (blockType == BlockType.Rocket && isVerticalRocket)
        {
            transform.rotation = Quaternion.Euler(0, 0, -90);  // Up
        }
    }
    #endregion

    #region If block is clickable call HandleBlockClick from BoardManager
    private void OnMouseDown()
    {
        // Prevent reaching block as it is falling
        if (isFalling) return;

        Node node = BoardManager.Instance.blockBoard[xIndex, yIndex];

        if (node == null || !node.isClickable)
        {
            return;
        }

        if (blockType == BlockType.Rocket)
        {
            ActivateRocket();
            StartCoroutine(BoardManager.Instance.FallExistingBlocks());
        }
        else
        {
            BoardManager.Instance.HandleBlockClick(this);
        }
    }

    private void ActivateRocket()
    {
        if (isVerticalRocket)
        {
            // Spawn rockets for vertical movement
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.up);    // Moves up
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.down);  // Moves down
        }
        else
        {
            // Spawn rockets for horizontal movement
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.left);  // Moves left
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.right); // Moves right
        }

        // 🚀 Remove the Rocket Block from the board
        BoardManager.Instance.blockBoard[xIndex, yIndex] = null;
        Destroy(gameObject);
    }
    #endregion
}