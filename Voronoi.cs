using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Voronoi
{
    public bool finished;
    public int amountSites = 0, sitesLeft;

    private readonly List<VEvent> events = new();
    private readonly List<Vector2Int> sites = new();
    private readonly List<VEdge> completeEdges = new();
    private VTreeNode root = null;

    // mapSize is area to run Voronoi alogrithm in, map is cut by cutBorder from each border afterwards
    private readonly int mapSize;
    private readonly int cutBorder;

    public Voronoi (List<Vector2> sites, int mapSize, int cutBorder) 
    {
        // Store mapsize to extend last edges beyond borders
        this.mapSize = mapSize;
        finished = false;
        this.cutBorder = cutBorder;
        // Fill sites into the event queue
        foreach (var site in sites)
        {
            InsertNewEvent(true, site);
            amountSites++;
        }
        sitesLeft = amountSites;
    }

    public void RunAlgorithm()
    {
        // Iterate through all events until the queue is empty
        if (events.Count > 0)
        {
            if (events[0].isNewSite)
            {
                HandleSiteEvent(events[0]);
                sitesLeft--;
            }
            else
            {
                HandleIntersecEvent(events[0]);
            }
            events.RemoveAt(0);
        }
        else
        {
            // Handle all remaining nodes
            ClearTree(root);
            ShortenEdges();

            finished = true;
        }
    }

    public List<VEdge> GetCompleteEdges()
    {
        return completeEdges;
    }


    private VEvent InsertNewEvent(bool isNewSite, Vector2 pos, VParabola parabola = null, float yOfEvent = 0, int startIndex = 0)
    {
        if (isNewSite)
        {
            yOfEvent = pos.y;
            sites.Add(Vector2Int.RoundToInt(pos));
        }
        // Insert events with declining y and growing x
        int insertIdx = startIndex;
        bool keepSearching = true;
        while (insertIdx < events.Count && keepSearching)
        {
            if (events[insertIdx].yOfEvent > yOfEvent || events[insertIdx].yOfEvent == yOfEvent && events[insertIdx].newEvent.x < pos.x) { insertIdx++; }
            else { keepSearching = false; }
        }
        VEvent newEvent = new(pos, isNewSite, parabola, yOfEvent);
        events.Insert(insertIdx, newEvent);

        return newEvent;
    }

    private void HandleSiteEvent(VEvent eve)
    {
        VParabola nextNode;
        VEdge newLeftEdge;
        Vector2 newStart;

        if (root == null)
        {
            // Add new root node
            root = new VParabola(Vector2Int.RoundToInt(eve.newEvent), null);
            nextNode = (VParabola)root;
            // If there are several nodes with the same y coordinate at the start, add all of them and save correct edges
            while (events[0].yOfEvent == events[1].yOfEvent)
            {
                newStart = new Vector2(events[0].newEvent.x + (events[1].newEvent.x - events[0].newEvent.x) / 2, 10.0f * mapSize);
                newLeftEdge = nextNode.ReplaceWithNewNodes(events[1].yOfEvent, Vector2Int.RoundToInt(events[1].newEvent));
                if (newLeftEdge.leftChild is VParabola leftParabola && newLeftEdge.rightChild is VEdge rightEdge 
                    && rightEdge.leftChild is VParabola newParabola && rightEdge.rightChild is VParabola rightParabola)
                {
                    newLeftEdge.UpdateMembers(leftParabola.focus, newParabola.focus, newStart);
                    rightEdge.UpdateMembers(newParabola.focus, rightParabola.focus, newStart);
                    nextNode = newParabola;
                }
                else
                {
                    Debug.LogError("Unexpected behaviour");
                }
                events.RemoveAt(0);
            }
        }
        else
        {
            // Search for parabole above new site
            VTreeNode parabolaAbove = root;
            while (parabolaAbove is VEdge edge)
            {
                float xOfEdge = edge.GetCurrentEnd(eve.yOfEvent).x;
                if (xOfEdge <= eve.newEvent.x)
                {
                    parabolaAbove = edge.rightChild;
                }
                else
                {
                    parabolaAbove = edge.leftChild;
                }
            }

            if (parabolaAbove is VParabola parabola)
            {
                if (parabola.intersecEvent != null)
                {
                    // If this parabola already had an intersection event, delete it, it will be invalid
                    events.Remove(parabola.intersecEvent);
                }

                float yOfIntersection = parabola.GetYOfIntersection(Vector2Int.RoundToInt(eve.newEvent));
                // Replace the parabola above new site with two edges, the splitted parts and a new parabola
                newLeftEdge = parabola.ReplaceWithNewNodes(yOfIntersection, Vector2Int.RoundToInt(eve.newEvent));
                if (root is VParabola)
                {
                    root = newLeftEdge;
                }

                if (newLeftEdge.leftChild is VParabola leftParabola && newLeftEdge.rightChild is VEdge edge && edge.rightChild is VParabola rightParabola)
                {
                    CheckForIntersectionEvent(leftParabola, Mathf.RoundToInt(eve.yOfEvent));
                    CheckForIntersectionEvent(rightParabola, Mathf.RoundToInt(eve.yOfEvent));
                }
                else
                {
                    Debug.LogError("Unexpected behaviour");
                }
            }
            else
            {
                Debug.LogError("Unexpected behaviour");
            }
        }
    }

    private void CheckForIntersectionEvent(VParabola parabola, float sweepline)
    {
        if (parabola.intersecEvent != null)
        {
            // Delete old intersection event if there was one
            events.Remove(parabola.intersecEvent);
        }

        // Check if and where any of the splitted parts will be closed with existing edges
        bool newIntersection = false;
        float yOfIntersection = 0f;
        Vector2 intersection = parabola.CheckForIntersection(ref newIntersection, ref yOfIntersection);
        if (newIntersection && Mathf.RoundToInt(yOfIntersection) <= Mathf.RoundToInt(sweepline))
        {
            parabola.intersecEvent = InsertNewEvent(false, intersection, parabola, yOfIntersection, 1);
        }
        else
        {
            parabola.intersecEvent = null;
        }
    }

    private void HandleIntersecEvent(VEvent eve)
    {
        eve.parabolaNode.UpdateNeighbors();
        completeEdges.Add(eve.parabolaNode.leftEdge);
        completeEdges.Add(eve.parabolaNode.rightEdge);
        eve.parabolaNode.RemoveParabola(eve.newEvent, ref root);
        CheckForIntersectionEvent(eve.parabolaNode.leftArc, eve.yOfEvent);
        CheckForIntersectionEvent(eve.parabolaNode.rightArc, eve.yOfEvent);
    }

    private void ClearTree(VTreeNode node)
    {
        // recursively clears all remaining tree nodes
        if (node is VEdge edge)
        {
            if (edge.rightChild != null)
            {
                ClearTree(edge.rightChild);
                edge.rightChild = null;
            }
            if (edge.leftChild != null)
            {
                ClearTree(edge.leftChild);
                edge.leftChild = null;
            }
            if (edge.start.x <= mapSize && edge.start.x >= 0 && edge.start.y <= mapSize && edge.start.y >= 0)
            {
                if (edge.firstSite.x == 276 || edge.firstSite.x == 309)
                {
                    Debug.Log("look here");
                }
                edge.end = EdgeUntilBorder(edge.start, edge.direction);
                completeEdges.Add(edge);
            }
            else if (edge.complement != null)
            {
                // Discard the complement edge if this one is already out of scope
                edge.complement.complement = null;
                edge.complement = null;
            }
        }
    }

    private void ShortenEdges()
    {
        List<VEdge> toForget = new();
        foreach (VEdge edge in completeEdges)
        {
            // Shorten all edges that are beyond scope, forget the ones that are completely outside
            bool startAbove, endAbove, startBelow, endBelow, startLeft, endLeft, startRight, endRight;
            startAbove = edge.start.y >= mapSize - cutBorder;
            startBelow = edge.start.y <= cutBorder;
            startLeft = edge.start.x <= cutBorder;
            startRight = edge.start.x >= mapSize - cutBorder;
            endAbove = edge.end.y >= mapSize - cutBorder;
            endBelow = edge.end.y <= cutBorder;
            endLeft = edge.end.x <= cutBorder;
            endRight = edge.end.x >= mapSize - cutBorder;
            if (edge.firstSite.x == 276 || edge.firstSite.x == 309)
            {
                Debug.Log("look here");
            }
            if (startAbove && endAbove || startBelow && endBelow || startLeft && endLeft || startRight && endRight)
            {
                toForget.Add(edge);
            }
            else
            {
                if (startAbove || startBelow || startLeft || startRight)
                {
                    edge.start = EdgeUntilBorder(edge.end, (-1) * edge.direction);
                }
                if (endAbove || endBelow || endLeft || endRight)
                {
                    edge.end = EdgeUntilBorder(edge.start, edge.direction);
                }
                // Update start and end positions, forget also when after cutting both are out of scope
                startAbove = edge.start.y >= mapSize - cutBorder;
                startBelow = edge.start.y <= cutBorder;
                startLeft = edge.start.x <= cutBorder;
                startRight = edge.start.x >= mapSize - cutBorder;
                endAbove = edge.end.y >= mapSize - cutBorder;
                endBelow = edge.end.y <= cutBorder;
                endLeft = edge.end.x <= cutBorder;
                endRight = edge.end.x >= mapSize - cutBorder;
                if ((startAbove || startBelow || startLeft || startRight) && (endAbove || endBelow || endLeft || endRight))
                {
                    toForget.Add(edge);
                }
            }
        }

        foreach (VEdge edge in toForget)
        {
            if (edge.complement != null)
            {
                edge.complement.complement = null;
            }
            completeEdges.Remove(edge);
        }
    }

    private Vector2 EdgeUntilBorder(Vector2 start, Vector2 direction)
    {
        float mX, mY, m;

        // Check where edge will hit x=0 and y=0, choose smallest positive one
        mX = (mapSize - cutBorder - start.x) / direction.x;
        mX = Mathf.Max((cutBorder - start.x) / direction.x, mX);

        mY = (mapSize - cutBorder - start.y) / direction.y;
        mY = Mathf.Max((cutBorder - start.y) / direction.y, mY);

        m = Mathf.Min(mX, mY);

        return start + m * direction;
    }
}
