using UnityEngine;
using System.Collections;

public class PathNode : MonoBehaviour {

  public float distanceFromGoal;
  public float distanceFromStart;
  public PathNode parent;

  public float GetGValue() {
    return distanceFromGoal + distanceFromStart;
  }
}
