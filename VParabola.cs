using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class VParabola : VTreeNode
{
    public Vector2Int focus;
    public VEvent intersecEvent;
    public VEdge leftEdge;
    public VEdge rightEdge;
    public VParabola leftArc;
    public VParabola rightArc;

    public VParabola(Vector2Int focus, VEdge parent) : base(parent)
    {
        this.focus = focus;
        intersecEvent = null;
        leftEdge = null;
        rightEdge = null;
        leftArc = null;
        rightArc = null;
        intersecEvent = null;
    }

    public float GetYOfIntersection(Vector2Int newSite)
    {
        // distance between focus f and intersection i is the same as between intersection and nearest point on sweep line l
        // intersection and nearest point on sweep line have the same x coordinate -> ix-lx = 0
        // sqrt((fx-ix)^2+(fy-iy)^2) = sqrt((iy-ly)^2)
        // solve for iy

        double iy = Mathf.Pow(focus.x - newSite.x, 2);
        iy += Mathf.Pow(focus.y, 2) - Mathf.Pow(newSite.y, 2);
        iy /= 2 * (focus.y - newSite.y);

        return (float)iy;
    }

    public VEdge ReplaceWithNewNodes(float yOfIntersection, Vector2Int newSite)
    {
        // Order sites by x, this edge is left edge and gets site with smaller x as first site
        Vector2Int firstSite = newSite, secondSite = focus;
        Vector2 start = new(newSite.x, yOfIntersection);
        VParabola newParabola, leftPart, rightPart;
        VEdge newLeftEdge, newRightEdge;

        if (focus.y > newSite.y)
        {
            firstSite = focus;
            secondSite = newSite;
        }

        newLeftEdge = new VEdge(firstSite, secondSite, start, parent);
        if (parent != null)
        {
            if (parent.leftChild == this)
            {
                parent.leftChild = newLeftEdge;
            }
            else
            {
                parent.rightChild = newLeftEdge;
            }
        }

        // Create left parabola, has same focus as before
        leftPart = new VParabola(focus, newLeftEdge);
        newLeftEdge.leftChild = leftPart;

        // Create complement / right edge with swapped sites, same start
        newRightEdge = new VEdge(secondSite, firstSite, start, newLeftEdge);
        newLeftEdge.rightChild = newRightEdge;

        // Create as children of right edge a parabola around the new site and the right part of the old parabola
        newParabola = new VParabola(newSite, newRightEdge);
        newRightEdge.leftChild = newParabola;

        rightPart = new VParabola(focus, newRightEdge);
        newRightEdge.rightChild = rightPart;

        // Save complement edges
        newLeftEdge.complement = newRightEdge;
        newRightEdge.complement = newLeftEdge;

        return newLeftEdge;
    }

    public Vector2 CheckForIntersection(ref bool hasIntersection, ref float lineAtIntersection)
    {
        // Checks for an intersection event, sets argument true if there is one and returns the intersection
        // Stores the intersection edges
        Vector2 intersection = new();

        Debug.Log("Checking for intersection event for parabola with focus " + focus);

        UpdateNeighbors();

        // Calculate intersection coordinates
        if (leftEdge == null || rightEdge == null || leftEdge.direction.x == 0 && rightEdge.direction.x == 0 
            || Mathf.Abs(leftEdge.start.x - rightEdge.start.x) < 0.01 && Mathf.Abs(leftEdge.start.y - rightEdge.start.y) < 0.01)
        {
            hasIntersection = false;
            Debug.Log("No neighboring edge or both x directions 0");
        }
        else
        {
            // Set equal the x and y coordinates of both edges
            double mLeftEdge, mRightEdge;
            if (rightEdge.direction.x == 0)
            {
                // Check that divisor is not 0 (both direction x coordinates cannot be 0 or edges would start at same point and never meet)
                mLeftEdge = (rightEdge.start.x - leftEdge.start.x) / (double)leftEdge.direction.x;
                mRightEdge = (leftEdge.start.y + mLeftEdge * leftEdge.direction.y - rightEdge.start.y) / rightEdge.direction.y;
            }
            else if (((double)leftEdge.direction.y / leftEdge.direction.x) == ((double)rightEdge.direction.y / rightEdge.direction.x))
            {
                // Edges have exact same direction and will not intersect
                mLeftEdge = -1;
                mRightEdge = -1;
                Debug.Log("Directions are same. " + (leftEdge.direction.y / leftEdge.direction.x) + ", " + rightEdge.direction.y / rightEdge.direction.x);
            }
            else
            {
                mLeftEdge = (leftEdge.start.x - rightEdge.start.x) * rightEdge.direction.y / (double)rightEdge.direction.x;
                mLeftEdge += rightEdge.start.y - leftEdge.start.y;
                mLeftEdge /= leftEdge.direction.y - rightEdge.direction.y / (double)rightEdge.direction.x * leftEdge.direction.x;
                mRightEdge = (leftEdge.start.x + mLeftEdge * leftEdge.direction.x - rightEdge.start.x) / rightEdge.direction.x;
            }

            if (mLeftEdge < 0 || mRightEdge < 0)
            {
                hasIntersection = false;
                Debug.Log("Edges never intersect. m1 = " + mLeftEdge + "; m2 = " + mRightEdge);
            }
            else
            {
                hasIntersection = true;
                intersection.x = (float)(leftEdge.start.x + mLeftEdge * leftEdge.direction.x);
                intersection.y = (float)(leftEdge.start.y + mLeftEdge * leftEdge.direction.y);

                float dist = Mathf.Sqrt(Mathf.Pow(intersection.x - focus.x, 2) + Mathf.Pow(intersection.y - focus.y, 2));
                lineAtIntersection = intersection.y - dist;

            }
        }

        return new Vector2(intersection.x, intersection.y);
    }
    public void UpdateNeighbors()
    {
        VTreeNode searchingLeaf;
        VEdge searchingEdge, lastEdge;

        leftEdge = null;
        rightEdge = null;
        leftArc = null;
        rightArc = null;

        // Search for next left arc
        searchingEdge = parent;
        lastEdge = searchingEdge;
        while (searchingEdge.parent != null && (searchingEdge.leftChild == lastEdge || searchingEdge.leftChild == this))
        {
            lastEdge = searchingEdge;
            searchingEdge = searchingEdge.parent;
        }
        if (searchingEdge.leftChild != lastEdge && searchingEdge.leftChild != this)
        {
            leftEdge = searchingEdge;
            searchingLeaf = searchingEdge.leftChild;
            while (searchingLeaf is not VParabola)
            {
                searchingEdge = (VEdge)searchingLeaf;
                searchingLeaf = searchingEdge.rightChild;
            }
            leftArc = (VParabola)searchingLeaf;
        }

        // Search for next right arc
        searchingEdge = parent;
        lastEdge = searchingEdge;
        while (searchingEdge.parent != null && (searchingEdge.rightChild == lastEdge || searchingEdge.rightChild == this))
        {
            lastEdge = searchingEdge;
            searchingEdge = searchingEdge.parent;
        }
        if (searchingEdge.rightChild != lastEdge && searchingEdge.rightChild != this)
        {
            rightEdge = searchingEdge;
            searchingLeaf = searchingEdge.rightChild;
            while (searchingLeaf is not VParabola)
            {
                searchingEdge = (VEdge)searchingLeaf;
                searchingLeaf = searchingEdge.leftChild;
            }
            rightArc = (VParabola)searchingLeaf;
        }
    }

    public void RemoveParabola(Vector2 intersection, ref VTreeNode root)
    {
        VEdge grandparent = parent.parent, otherEdge, newEdge;
        VTreeNode sibling;
        Vector2Int oneSite, otherSite;

        // Append sibling to grandparent
        if (parent.leftChild == this) { sibling = parent.rightChild; }
        else { sibling = parent.leftChild; }

        sibling.parent = grandparent;
        if (grandparent.leftChild == parent) { grandparent.leftChild = sibling; }
        else { grandparent.rightChild = sibling; }

        // Replace other edge with new edge
        if (parent == leftEdge) { otherEdge = rightEdge; }
        else { otherEdge = leftEdge; }

        if (rightEdge.firstSite == focus) { oneSite = rightEdge.secondSite; }
        else { oneSite = rightEdge.firstSite; }
        if (leftEdge.firstSite == focus) { otherSite = leftEdge.secondSite; }
        else { otherSite = leftEdge.firstSite; }

        newEdge = new VEdge(oneSite, otherSite, intersection, parent.direction + otherEdge.direction, otherEdge.parent);
        otherEdge.leftChild.parent = newEdge;
        otherEdge.rightChild.parent = newEdge;
        newEdge.leftChild = otherEdge.leftChild;
        newEdge.rightChild = otherEdge.rightChild;
        newEdge.parent = otherEdge.parent;
        if (otherEdge.parent != null && otherEdge.parent.leftChild == otherEdge)
        {
            otherEdge.parent.leftChild = newEdge;
        }
        else if (otherEdge.parent != null)
        {
            otherEdge.parent.rightChild = newEdge;
        }
        else
        {
            root = newEdge;
        }

        // Save end point of edges
        leftEdge.end = intersection;
        rightEdge.end = intersection;
    }
}
