using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [Header("Spawn variables and prefab")]
    [SerializeField] GameObject _pickUp;
    [SerializeField] int _currentPickUps;
    [SerializeField] int _maxPickUps;
    [SerializeField] float _spawnTime;
    [SerializeField] float _spawnCooldown = 2f;
    [SerializeField] float _spawnRange;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        PickUpObject.OnPickUpCollected += HandlePickUpCollected;
    }

    public override void OnNetworkDespawn()
    {
        PickUpObject.OnPickUpCollected -= HandlePickUpCollected;
    }

    private void HandlePickUpCollected()
    {
        _currentPickUps--;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer || !IsSpawned) return;

        SpawnPickUpObject();
    }

    void SpawnPickUpObject()
    {
        if (_currentPickUps >= _maxPickUps) return;     //ignore if there are enough spawned objects

        //Cooldown to spawn pickups
        _spawnTime -= Time.deltaTime;
        if (_spawnTime > 0) return;
        _spawnTime = _spawnCooldown;


        //instantiate pick up obj on network
        GameObject clone = Instantiate(_pickUp, GenerateSpawnPosition(), _pickUp.transform.rotation);
        NetworkObject netObj = clone.GetComponent<NetworkObject>();
        netObj.Spawn();

        _currentPickUps++;
    }

    //Generates a random position within the spawn range
    private Vector3 GenerateSpawnPosition()
    {
        //Random X and Z coordinates within the spawn range
        float spawnPosX = Random.Range(-_spawnRange, _spawnRange);
        float spawnPosZ = Random.Range(-_spawnRange, _spawnRange);

        //Return a new position vector at ground level (Y = 0)
        Vector3 randomPos = new Vector3(spawnPosX, 0.5f, spawnPosZ);

        return randomPos;
    }
}
