using UnityEngine;

public class PlayerPickUp : MonoBehaviour
{
    [SerializeField] int _objCollected;

    void AddPickedUpObj()
    {
        _objCollected++;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            AddPickedUpObj();
        }
    }
}
