using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class VTreeNode
{
    public VEdge parent;

    protected VTreeNode(VEdge parent)
    {
        this.parent = parent;
    }

}
