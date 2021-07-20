using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectSnapper
{

    public static Vector3? SnapPosition(Vector3 pos)
    {
        return SnapPosition(pos, Vector3.zero);
    }

    public static Vector3? SnapPosition(Vector3 pos, Vector3 offset)
    {
        // Checks if there is an object below.
        Vector3? positionHit = GetPositionFromRaycast(pos, Vector3.down, offset);
        if (positionHit.HasValue)
        {
            return positionHit.Value;
        }

        // Checks if the object is within an active terrain.
        Terrain closestTerrain = GetClosestTerrain(pos);
        if (closestTerrain)
        {
            Vector3 xyPosition = pos;
            xyPosition.y = closestTerrain.SampleHeight(xyPosition) +
                            closestTerrain.GetPosition().y;
            return xyPosition + offset;
        }
        // Checks if there is an object above.
        return GetPositionFromRaycast(pos, Vector3.up, offset);
    }

    public static void SnapPosition(Transform transform, Vector3 offset)
    {
        Vector3? snappedPosition = SnapPosition(transform.position, offset);
        if(snappedPosition.HasValue)
        {
            transform.position = snappedPosition.Value;
        }
    }

    private static Vector3? GetPositionFromRaycast(Transform transform, Vector3 direction)
    {
        return GetPositionFromRaycast(transform, direction, Vector3.zero);
    }

    private static Vector3? GetPositionFromRaycast(Transform transform, Vector3 direction, Vector3 offset)
    {
        return GetPositionFromRaycast(transform.position, direction, offset);
    }

    private static Vector3? GetPositionFromRaycast(Vector3 position, Vector3 direction)
    {
        return GetPositionFromRaycast(position, direction, Vector3.zero);
    }

    private static Vector3? GetPositionFromRaycast(Vector3 position, Vector3 direction, Vector3 offset)
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(
            position, Vector3.down, out raycastHit, Mathf.Infinity))
        {
            return raycastHit.point + offset;
        }
        return null;
    }

    private static Terrain GetClosestTerrain(Vector3 pos)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains.Length == 0)
        {
            return null;
        }

        if (terrains.Length == 1)
        {
            return terrains[0];
        }

        float lowestDistance = Vector3.Distance(pos, terrains[0].GetPosition());
        int lowestTerrainDistIndex = 0;

        for (int i = 1; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            float distance = Vector3.Distance(pos, terrain.GetPosition());
            if (distance < lowestDistance)
            {
                lowestDistance = distance;
                lowestTerrainDistIndex = i;
            }
        }
        Terrain closestTerrain = terrains[lowestTerrainDistIndex];
        Vector3 terrainSize = closestTerrain.terrainData.size;
        Vector3 position = closestTerrain.GetPosition();
        Vector3 maxPosition = position + terrainSize;

        if (pos.x >= position.x && pos.x <= maxPosition.x
            && pos.z >= position.z && pos.z <= maxPosition.z)
        {
            return closestTerrain;
        }
        return null;
    }
}
