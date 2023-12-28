using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VEvent
{
    public Vector2 newEvent;
    public bool isNewSite;
    public VParabola parabolaNode;
    public float yOfEvent;

    public VEvent(Vector2 newEvent, bool isNewSite = true, VParabola parabolaNode = null, float yOfEvent = 0)
    {
        this.newEvent = newEvent;
        this.isNewSite = isNewSite;
        this.parabolaNode = parabolaNode;
        this.yOfEvent = yOfEvent;
    }
}
