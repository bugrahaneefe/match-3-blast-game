using UnityEngine;
using System.Collections;

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

    //true if vertical rocket, false if horizontal rocket
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

    #region rocket activation
    public void ActivateRocket(bool isTriggeredByRocket = false)
    {
        // If it was triggered by a user click, check adjacency & possible big combo
        if (!isTriggeredByRocket)
        {
            // Check if there's an adjacent rocket
            if (IsAdjacentToRocket())
            {
                //big rocket combination
                StartCoroutine(RocketComboRoutine());
                return;
            }

            // Check remaining moves only when clicked by player
            GameManager.Instance.CheckRemainingMoves();
        }

        // normal execution of rocket
        if (isVerticalRocket)
        {
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.up);
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.down);
        }
        else
        {
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.left);
            BoardManager.Instance.SpawnRocket(transform.position, Vector2.right);
        }

        //remove rocket block from the board and destroy
        BoardManager.Instance.blockBoard[xIndex, yIndex] = null;
        Destroy(gameObject);
    }

    private IEnumerator RocketComboRoutine()
    {
        Vector3 initialPosition = transform.position;

        //hide clicked rocket visually
        SpriteRenderer rocketRenderer = GetComponent<SpriteRenderer>();
        if (rocketRenderer != null)
        {
            rocketRenderer.enabled = false;
        }

        //create a new big rocket at the clicked position
        GameObject bigRocket = Instantiate(gameObject, initialPosition, Quaternion.identity);
        bigRocket.transform.localScale = transform.localScale * 2;
        bigRocket.transform.SetParent(BoardManager.Instance.transform);

        if (isVerticalRocket)
        {
            bigRocket.transform.rotation = Quaternion.Euler(0, 0, -90);
        }
        else
        {
            bigRocket.transform.rotation = Quaternion.identity;
        }

        SpriteRenderer bigRocketRenderer = bigRocket.GetComponent<SpriteRenderer>();
        if (bigRocketRenderer != null)
        {
            bigRocketRenderer.sortingOrder = 100;
        }
        else
        {
            bigRocket.transform.position += new Vector3(0, 0, -1);
        }

        //get connected rockets, excluding this one
        var neighbors = BoardManager.Instance.GetConnectedBlocks(this)
                        .FindAll(block => block != this);

        //hide neighbor rockets visually instead of removing them
        foreach (Block neighbor in neighbors)
        {
            if (neighbor != null && neighbor.blockType == BlockType.Rocket)
            {
                SpriteRenderer neighborRenderer = neighbor.GetComponent<SpriteRenderer>();
                if (neighborRenderer != null)
                {
                    neighborRenderer.enabled = false; // hide neighbor rocket
                }
            }
        }

        //big rocket tanimation
        for (int i = 0; i < 3; i++)
        {
            if (bigRocketRenderer != null)
            {
                bigRocketRenderer.enabled = false;
            }
            yield return new WaitForSeconds(0.1f);

            if (bigRocketRenderer != null)
            {
                bigRocketRenderer.enabled = true; //
            }
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.2f);
        Destroy(bigRocket);

        // both vertical and horizontal rockets
        BoardManager.Instance.SpawnRocket(initialPosition, Vector2.up);
        BoardManager.Instance.SpawnRocket(initialPosition, Vector2.down);
        BoardManager.Instance.SpawnRocket(initialPosition, Vector2.left);
        BoardManager.Instance.SpawnRocket(initialPosition, Vector2.right);

        // After spawning the rockets, now remove the neighbor rockets from the board
        foreach (Block neighbor in neighbors)
        {
            if (neighbor != null && neighbor.blockType == BlockType.Rocket)
            {
                BoardManager.Instance.blockBoard[neighbor.xIndex, neighbor.yIndex] = null;
                Destroy(neighbor.gameObject);
            }
        }

        // Remove this rocket from the board and destroy it
        BoardManager.Instance.blockBoard[xIndex, yIndex] = null;
        Destroy(gameObject);
    }

    private bool IsAdjacentToRocket()
    {
        var neighbors = BoardManager.Instance.GetNeighbors(this);
        foreach (Block neighbor in neighbors)
        {
            if (neighbor != null && neighbor.blockType == BlockType.Rocket)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Click handling
    private void OnMouseDown()
    {
        // don't click a falling block
        if (isFalling) return;

        Node node = BoardManager.Instance.blockBoard[xIndex, yIndex];
        if (node == null || !node.isClickable) return;

        if (blockType == BlockType.Rocket)
        {
            // If user clicks a rocket, isTriggeredByRocket=false
            ActivateRocket(false);
            StartCoroutine(BoardManager.Instance.FallExistingBlocks());
        }
        else
        {
            // Normal block click
            BoardManager.Instance.HandleBlockClick(this);
        }
    }
    #endregion
}
