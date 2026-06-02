using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    public static TriggerZone Instance { get; private set; }

    [SerializeField] Light _dropLight;

    private void Awake()
    {
        Instance = this;
    }

    public void TurnLightsOn()
    {
        _dropLight.gameObject.SetActive(true);
    }

    public void TurnLightsOff()
    {
        _dropLight.gameObject.SetActive(false);
    }
}
