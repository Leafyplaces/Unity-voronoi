using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class VEdge : VTreeNode
{
    public Vector2Int firstSite;
    public Vector2Int secondSite;
    public Vector2 start;
    public Vector2 end;
    public Vector2 direction;
    public VEdge complement;
    public VTreeNode leftChild, rightChild;

    public VEdge(Vector2Int firstSite, Vector2Int secondSite, Vector2 start, VEdge parent) : base(parent)
    {
        UpdateMembers(firstSite, secondSite, start);
        leftChild = null;
        rightChild = null;
    }

    public VEdge(Vector2Int firstSite, Vector2Int secondSite, Vector2 start, Vector2 direction, VEdge parent) : base(parent)
    {
        UpdateMembers(firstSite, secondSite, start);
        this.direction = direction;
        leftChild = null;
        rightChild = null;
    }

    public void UpdateMembers(Vector2Int firstSite, Vector2Int secondSite, Vector2 start)
    {
        this.firstSite = firstSite;
        this.secondSite = secondSite;
        this.start = start;

        // calculate vector from first to second site, negate one coordinate
        // complement edge must receive same sites but swapped
        // give site with larger y to left edge as first site
        direction = new Vector2(secondSite.y - firstSite.y, -(secondSite.x - firstSite.x));
        if (Mathf.RoundToInt(direction.x) == 0)
        {
            direction.x = 0.01f * direction.y / Mathf.Abs(direction.y);
        }
        complement = null;
    }

    public Vector2 GetCurrentEnd(float yOfLine)
    {
        float endX, endY;
        float e, e1 = 0f, e2 = 0f, a = 0f, b = 0f, c = 0f;

        // current end of the edge lies on line of start + e * direction, and has same distance to line as to each site
        // end.x = start.x + e * direction.x
        // end.y = start.y + e * direction.y
        // sqrt((site.x - end.x)^2 + (site.y - end.y)^2) = sqrt((end.y - yOfLine)^2)
        // inserting equations resolves in quadratic equation, one result is negative, return only result with positive e

        if (direction.x == 0)
        {
            if (firstSite.y == yOfLine) e = 1f;
            else e = (Mathf.Pow(firstSite.x - start.x, 2) + Mathf.Pow(firstSite.y - start.y, 2) - Mathf.Pow(yOfLine - start.y, 2)) / (2 * direction.y * (firstSite.y - yOfLine));
        }
        else
        {
            a = Mathf.Pow(direction.x, 2);
            b = 2 * (direction.x * (start.x - firstSite.x) + direction.y * (yOfLine - firstSite.y));
            c = Mathf.Pow(firstSite.x - start.x, 2) + Mathf.Pow(firstSite.y - start.y, 2) - Mathf.Pow(yOfLine - start.y, 2);

            if (4 * a * c > b * b)
            {
                b = 2 * (direction.x * (start.x - secondSite.x) + direction.y * (yOfLine - secondSite.y));
                c = Mathf.Pow(secondSite.x - start.x, 2) + Mathf.Pow(secondSite.y - start.y, 2) - Mathf.Pow(yOfLine - start.y, 2);
            }

            e1 = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
            e2 = (-b - Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
            e = Mathf.Max(e1, e2);

            if (e1 >= 0 && e2 <= 0) e = e1;
            else if (e2 >= 0 && e1 <= 0) e = e2;
            else
            {
                Debug.LogError("Unexpected result in x of current end calculation. Factors e are " + e1 + ", " + e2);
            }
        }

        endX = start.x + e * direction.x;
        endY = start.y + e * direction.y;

        return new Vector2(endX, endY);
    }
}
