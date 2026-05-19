using UnityEngine;

public class TerrainLimits : MonoBehaviour
{
    float _floorLimit = 24f;

    // Update is called once per frame
    void FixedUpdate()
    {
        //Restricts the player from leaving the terrain
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -_floorLimit, _floorLimit),            //Mathf.Clamp returns a value between a min and max value
            transform.position.y,
            Mathf.Clamp(transform.position.z, -_floorLimit, _floorLimit)
            );
    }
}
