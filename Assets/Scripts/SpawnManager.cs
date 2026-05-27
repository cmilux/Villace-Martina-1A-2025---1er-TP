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
    [SerializeField] float _spawnRange;             //area to spawn

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //suscribe to events
            PickUpObject.OnPickUpCollected += HandlePickUpCollected;
            GameManager.OnGameOver += HandleGameOver;
        }
    }

    public override void OnNetworkDespawn()
    {
        //desuscribe to events
        PickUpObject.OnPickUpCollected -= HandlePickUpCollected;
        GameManager.OnGameOver -= HandleGameOver;
    }

    private void HandlePickUpCollected()
    {
        _currentPickUps--;
    }

    //stops spawning after games ends
    void HandleGameOver()
    {
        if (!IsServer) return;

        //despawns remaining pickups
        var pickUps = FindObjectsByType<PickUpObject>(FindObjectsSortMode.None);
        foreach (var p in pickUps)
        {
            var net = p.GetComponent<NetworkObject>();
            if (net != null && net.IsSpawned)
            {
                net.Despawn();
            }
        }

        _currentPickUps = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer || !IsSpawned) return;

        //only spawns when game is running
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying()) return;

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
