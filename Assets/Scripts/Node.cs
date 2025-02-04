using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public GameObject block;
    public bool isClickable;

    public Node(GameObject _block)
    {
        block = _block;

        #region Determine whether blocktype is clickable or not
        Block blockComponent = _block.GetComponent<Block>();
        if (blockComponent != null)
        {
            isClickable = !(blockComponent.blockType == BlockType.Duck || blockComponent.blockType == BlockType.Balloon);
        }
        else
        {
            isClickable = false;
        }
        #endregion
    }
}
