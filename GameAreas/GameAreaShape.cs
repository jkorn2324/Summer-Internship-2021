using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Game Area Shape Interface.
/// </summary>
public interface IGameAreaShape
{

    bool IsWithinShape(Vector3 point);

    bool IsWithinShape(GameAreaPoint point);

    Vector3? RandomPointInShape(float yPosition = 0.0f);

    void ApplyOffset(Vector3 offset);
}
