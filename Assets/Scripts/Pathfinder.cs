using UnityEngine;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour {

  protected PathNode _goalNode;
  protected PathNode _startNode;
  protected PathNode _currentNode;

  // The node grid.
  protected List<PathNode> _pathNodes;
  public Vector2 gridSize;

  // Currently considered nodes.
  protected Dictionary<PathNode, int> _openSet = new Dictionary<PathNode, int>();

  // All previously considered nodes.
  protected List<PathNode> _closedSet = new List<PathNode>();

  public delegate float DistanceCalculationMethod(int xDistance, int yDistance);
  DistanceCalculationMethod _currentCalculationMethod;

  // Heuristic values
  public float orthogonalH = 10;
  public float diagonalH = 14;

  public float distanceValue = 10;

  // Use this for initialization
  void Start () {
    _pathNodes = new List<PathNode>(GetComponentsInChildren<PathNode>());

    _currentCalculationMethod = CalculateManhattanDistance;

    ValidateGrid();
    PrecalculateDistances();
    CalculatePath();
	}

  void ValidateGrid() {
    if(gridSize.x * gridSize.y != _pathNodes.Count) {
      throw new System.Exception("Grid size does not match number of path nodes!");
    }

    List<PathNode> goalNodes = _pathNodes.FindAll(pathNode => {
      return pathNode.tag == "Goal";
    });

    // If we have more than one goal, then stop execution.
    if(goalNodes.Count > 1) {
      throw new System.Exception("More than one goal is specified; only one goal is allowed!");
    }
    else if(goalNodes.Count == 0) {
      throw new System.Exception("No goal node specified.");
    }
    else {
      _goalNode = goalNodes[0];
    }

    List<PathNode> startNodes = _pathNodes.FindAll(pathNode => {
      return pathNode.tag == "Start";
    });
    // If we have more than one goal, then stop execution.
    if(startNodes.Count > 1) {
      throw new System.Exception("More than one start node is specified; only one start node is allowed!");
    }
    else if(startNodes.Count == 0) {
      throw new System.Exception("No start node specified.");
    }
    else {
      _startNode = startNodes[0];
    }
  }

  void PrecalculateDistances() {
    Vector2 goalLocation = GetNodeLocation(_goalNode);

    for(int y = 0; y < gridSize.y; y++) {
      for(int x = 0; x < gridSize.x; x++) {
        PathNode currentPathNode = _pathNodes[x + (y * (int)gridSize.x)];
        if(currentPathNode.CompareTag("Obstacle")) {
          currentPathNode.distanceFromGoal = -1;
        }
        else {
          int xDifference = Mathf.Abs((int)goalLocation.x - x);
          int yDifference = Mathf.Abs((int)goalLocation.y - y);
          currentPathNode.distanceFromGoal = _currentCalculationMethod(xDifference, yDifference);
        }
      }
    }
  }

  public void CalculatePath() {
    PathNode currentNode = _startNode;

    while(true) {
      _closedSet.Add(currentNode);

      // find all local nodes
      Vector2 currentNodeLocation = GetNodeLocation(currentNode);

      // check orthogonal directions
      QueueNodeIfNotNull((int)currentNodeLocation.x, (int)currentNodeLocation.y - 1); // north
      QueueNodeIfNotNull((int)currentNodeLocation.x, (int)currentNodeLocation.y + 1); // south
      QueueNodeIfNotNull((int)currentNodeLocation.x + 1, (int)currentNodeLocation.y); // east
      QueueNodeIfNotNull((int)currentNodeLocation.x - 1, (int)currentNodeLocation.y); // west

      // check diagonal directions
      QueueNodeIfNotNull((int)currentNodeLocation.x - 1, (int)currentNodeLocation.y - 1); // north-west
      QueueNodeIfNotNull((int)currentNodeLocation.x + 1, (int)currentNodeLocation.y - 1); // north-east
      QueueNodeIfNotNull((int)currentNodeLocation.x + 1, (int)currentNodeLocation.y + 1); // south-east
      QueueNodeIfNotNull((int)currentNodeLocation.x - 1, (int)currentNodeLocation.y + 1); // south-west

      // find the closed-set nodes with the smallest heuristic value.
    }
  }

  public void QueueNodeIfNotNull(int xLocation, int yLocation) {
    PathNode node = GetNodeAtLocation(xLocation, yLocation);
    if(node != null && !node.CompareTag("Obstacle") && !_closedSet.Contains(node)) {
      // If we're still considering it.
      if(!_openSet.ContainsKey(node)) {
        _openSet.Add(node, -1);
      }

      Vector2 distanceFromStart = CalculateDistanceFromNode(node, _startNode);
      node.distanceFromStart = CalculateHeuristicDistance((int)distanceFromStart.x, (int)distanceFromStart.y);

      Vector2 distanceFromCurrentNode = CalculateDistanceFromNode(node, _currentNode);
      float hValue = CalculateHeuristicDistance((int)distanceFromStart.x, (int)distanceFromStart.y);

      float fValue = (int)(node.GetGValue() + hValue);

      // If this new F-value is less than our current F, then we're going to reparent.
      if(fValue < _openSet[node] || _openSet[node] != -1) {
        _openSet[node] = (int)fValue;
        node.parent = _currentNode;
      }
    }
  }

  public Vector2 GetNodeLocation(PathNode pathNode) {
    int nodeIndex = _pathNodes.IndexOf(pathNode);
    int nodeY = nodeIndex % (int)gridSize.x;
    int nodeX = nodeIndex - (nodeY * (int)gridSize.x);
    return new Vector2(nodeX, nodeY);
  }

  public PathNode GetNodeAtLocation(Vector2 pathNodeLocation) {
    return GetNodeAtLocation((int)pathNodeLocation.x, (int)pathNodeLocation.y);
  }

  public PathNode GetNodeAtLocation(int xLocation, int yLocation) {
    PathNode toReturn = null;

    int nodeIndex = (int)(xLocation + (yLocation * gridSize.x));
    if(xLocation > 0 && xLocation < gridSize.x && yLocation > 0 && yLocation < gridSize.y) {
      toReturn = _pathNodes[nodeIndex];
    }
    return toReturn;
  }

  Vector2 CalculateDistanceFromNode(PathNode firstNode, PathNode targetNode) {
    Vector2 firstNodeLocation = GetNodeLocation(firstNode);
    Vector2 targetNodeLocation = GetNodeLocation(targetNode);

    float xDifference = Mathf.Abs(firstNodeLocation.x - targetNodeLocation.x);
    float yDifference = Mathf.Abs(firstNodeLocation.y - targetNodeLocation.y);
    return new Vector2(xDifference, yDifference); 
  }

  float CalculateManhattanDistance(int xDistance, int yDistance) {
    return (xDistance + yDistance) * distanceValue;
  }

  float CalculateHeuristicDistance(int xDistance, int yDistance) {
    int totalDistance = (xDistance + yDistance);
    int diagonalOverlapCount = Mathf.Abs(xDistance - yDistance);

    int orthogonalCount = totalDistance - diagonalOverlapCount;
    return (orthogonalCount * orthogonalH) + (diagonalOverlapCount * diagonalH);
  }
}
