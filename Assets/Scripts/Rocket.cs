using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    public float startSpeed = 5f;
    public float maxSpeed = 25f;
    public float accelerationTime = 0.2f;
    public Vector2 direction;

    private float currentSpeed;
    private bool hasDestroyedBlocks = false;

    private void Start()
    {
        currentSpeed = startSpeed;
        StartCoroutine(AccelerateRocket());
        StartCoroutine(MoveRocket());
        StartCoroutine(DestroyAfterTime(1.4f));
    }

    private IEnumerator AccelerateRocket()
    {
        float elapsedTime = 0f;
        while (elapsedTime < accelerationTime)
        {
            elapsedTime += Time.deltaTime;
            currentSpeed = Mathf.Lerp(startSpeed, maxSpeed, elapsedTime / accelerationTime);
            yield return null;
        }

        currentSpeed = maxSpeed;
    }

    private IEnumerator MoveRocket()
    {
        while (true)
        {
            transform.position += (Vector3)direction * currentSpeed * Time.deltaTime;
            CheckForBlocksOnPath();
            yield return null;
        }
    }

    private void CheckForBlocksOnPath()
    {
        Vector2Int gridPosition = GetGridPosition(transform.position);

        if (IsValidGridPosition(gridPosition))
        {
            // Access the block at this position (if any)
            Block block = BoardManager.Instance.blockBoard[gridPosition.x, gridPosition.y]
                          ?.block?.GetComponent<Block>();

            if (block != null)
            {
                if (block.blockType == BlockType.Rocket)
                {
                    // trigger another rocket
                    block.ActivateRocket(true);
                    hasDestroyedBlocks = true;
                }
                else
                {
                    // skip ducks
                    if (block.blockType == BlockType.Duck)
                    {
                        return;
                    }
                    // Remove normal block
                    BoardManager.Instance.RemoveBlocks(new List<Block> { block }, true);
                    UIManager.Instance.UpdateGoals(new List<Block> { block });
                    hasDestroyedBlocks = true;
                }
            }
        }
    }

    #region Grid Position Helpers
    private Vector2Int GetGridPosition(Vector3 worldPos)
    {
        float spacingX = BoardManager.Instance.spacingX;
        float spacingY = BoardManager.Instance.spacingY;

        int x = Mathf.RoundToInt(worldPos.x + spacingX);
        int y = Mathf.RoundToInt(worldPos.y + spacingY);

        return new Vector2Int(x, y);
    }

    private bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < BoardManager.Instance.width &&
               gridPos.y >= 0 && gridPos.y < BoardManager.Instance.height;
    }
    #endregion

    private IEnumerator DestroyAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hasDestroyedBlocks)
        {
            if (UIManager.Instance.CheckAllGoalsCompleted())
            {
                GameManager.Instance.LevelCompletePanelShowing();
            }
        }
        Destroy(gameObject);
    }
}
