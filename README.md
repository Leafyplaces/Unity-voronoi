# Unity-voronoi
Voronoi algorithm in Unity

This is a voronoi algorithm implemented in unity. It works for square spaces with adjustable size.

Methods:
public Voronoi (List<Vector2> sites, int mapSize, int cutBorder)
  Constructor for the class
  sites: The coordinates of the voronoi sites, as a List of Vector2
  mapSize: Size of the map as integer, applies to both horizontal and vertical size
  cutBorder: All edges are cut this far from the border in the end to create a nicer edge

public void RunAlgorithm()
  Call this method until the public member finished is true

public List<VEdge> GetCompleteEdges()
  Call this method when the public member finished is true
  Returns a List of the class VEdge for the edges created with this algorithm

Public members:
bool finished: Is true when the algorithm is finished and the complete edges can be recalled
int amountSites: Total amount of sites given to this algorithm
int sitesLeft: Amount of sites left to compute
