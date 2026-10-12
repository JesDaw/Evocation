using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RecticalSnap : MonoBehaviour
{
    [SerializeField] LayerMask groundTargetMask;
    [SerializeField] LayerMask enemyTargetMask;
    [SerializeField] List<Vector3> enemyPositions;
    [SerializeField] float rayLength;
    [SerializeField] float sphereLength;
    [SerializeField] bool DebugDrawing = false;

    RaycastHit2D floorTargetLocation;

    public Vector3 SnapToGround(Vector3 _inputPos)
    {
        //ground
        floorTargetLocation = Physics2D.Raycast(_inputPos, Vector2.down, rayLength, groundTargetMask);

        enemyPositions = Physics2D.CircleCastAll(_inputPos, sphereLength, Vector2.down, sphereLength, enemyTargetMask)
                             .Select(hit => (Vector3)hit.point)
                             .ToList();

        if(DebugDrawing)
            Debug.DrawLine(_inputPos, _inputPos + new Vector3(0, -rayLength, 0), Color.green);

        if(floorTargetLocation.collider == null) return _inputPos;
        if(enemyPositions.Count == 0) return floorTargetLocation.point;

        //enemy

        Vector3 minDistancePosition = enemyPositions[0];
        float minDistance = Vector3.Distance(minDistancePosition, enemyPositions[0]);

        for(int I = 1; I < enemyPositions.Count; I++)
        {
            if(minDistance < Vector3.Distance(minDistancePosition, enemyPositions[I]));
                minDistancePosition = enemyPositions[I];
        }
        
        return minDistancePosition;
    }
}
