
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;



public class Instance : MonoBehaviour
{
    [Header("Instance Definition")]
    public Transform entry;
    public int requiredLevel = 1;
    [HideInInspector] public Bounds bounds;

    [Tooltip("Only allow so many instances of this type to protect server resources.")]
    public int instanceLimit = 5;

    
    
    [HideInInspector] public Dictionary<int, Instance> instances = new Dictionary<int, Instance>();

    [Header("Party Check")]
    public LayerMask playerLayers = ~0; 
    public float partyCheckInterval = 30;
    double nextPartyCheckTime;
    bool localPlayerEnteredYet;

    
    
    
    
    
    
    
    
    
    [Header("Spawn Points Cache")]
    public InstanceSpawnPoint[] spawnPoints;

    
    
    Instance template;

    
    int partyId = 0;

    
    
    
    
    static Collider[] hitsBuffer = new Collider[10000];

    void Awake()
    {
        
        

        
        bounds = Utils.CalculateBoundsForAllRenderers(gameObject);
    }

    void OnValidate()
    {
        

        
        spawnPoints = GetComponentsInChildren<InstanceSpawnPoint>();

        
        
        
        
        foreach (Transform tf in GetComponentsInChildren<Transform>())
            if (tf.gameObject.isStatic)
                Debug.LogWarning("Instance child " + tf.name + " shouldn't be static. It needs to be duplicated and moved to other positions when duplicating instances.");
    }

    HashSet<Player> FindAllPlayersInInstanceBounds()
    {
        
        
        HashSet<Player> result = new HashSet<Player>();
        int hits = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, hitsBuffer, transform.rotation, playerLayers);
        for (int i = 0; i < hits; ++i)
        {
            Collider co = hitsBuffer[i];
            Player player = co.GetComponentInParent<Player>();
            if (player != null)
                result.Add(player);
        }
        return result;
    }

    void DestroyAllNetworkIdentitiesInInstanceBounds()
    {
        int hits = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, hitsBuffer, transform.rotation);
        for (int i = 0; i < hits; ++i)
        {
            Collider co = hitsBuffer[i];
            NetworkIdentity identity = co.GetComponentInParent<NetworkIdentity>();
            if (identity != null)
                NetworkServer.Destroy(identity.gameObject);
        }
    }

    void Update()
    {
        
        
        if (partyId > 0)
        {
            
            if (NetworkServer.active)
            {
                
                if (NetworkTime.time >= nextPartyCheckTime)
                {
                    
                    
                    
                    
                    
                    
                    
                    HashSet<Player> playersInInstanceBounds = FindAllPlayersInInstanceBounds();

                    int playersRemaining = 0;
                    foreach (Player player in playersInInstanceBounds)
                    {
                        
                        if (player.party.party.partyId == partyId)
                        {
                            ++playersRemaining;
                        }
                        
                        else
                        {
                            Transform spawn = ((NetworkManagerMMO)NetworkManager.singleton).GetStartPositionFor(player.className);
                            player.movement.Warp(spawn.position);
                            Debug.Log("Removed player " + player.name + " with partyId=" + player.party.party.partyId + " from instance " + name + " with partyId=" + partyId);
                        }
                    }

                    
                    if (playersRemaining == 0)
                    {
                        
                        
                        
                        
                        Destroy(gameObject);
                        Debug.Log("Instance " + name + " destroyed because no members of party " + partyId + " are in it anymore.");
                    }

                    
                    nextPartyCheckTime = NetworkTime.time + partyCheckInterval;
                }
            }
            
            
            
            
            
            
            else if (Player.localPlayer != null)
            {
                
                if (bounds.Contains(Player.localPlayer.transform.position))
                {
                    localPlayerEnteredYet = true;
                }
                
                else if (localPlayerEnteredYet)
                {
                    Destroy(gameObject);
                    Debug.Log("Instance " + name + " destroyed for local player because he left the instance.");
                }
            }
        }
    }

    void OnDestroy()
    {
        
        

        
        
        if (template != null)
            template.instances.Remove(partyId);

        
        
        
        
        if (NetworkServer.active)
            DestroyAllNetworkIdentitiesInInstanceBounds();
    }

    
    
    
    
    
    public static Instance CreateInstance(Instance template, int partyId)
    {
        
        if (!template.instances.ContainsKey(partyId))
        {
            
            if (template.instances.Count < template.instanceLimit)
            {
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                float zOffset = (template.bounds.size.z + ((SpatialHashingInterestManagement)NetworkServer.aoi).visRange) * partyId;
                Debug.Log("Creating " + template.name + " Instance with zOffset=" + zOffset);

                
                Vector3 position = template.transform.position + new Vector3(0, 0, zOffset);
                GameObject go = Instantiate(template.gameObject, position, template.transform.rotation);
                Instance instance = go.GetComponent<Instance>();
                instance.template = template;

                
                
                
                
                
                instance.partyId = partyId;

                
                template.instances[partyId] = instance;

                
                if (NetworkServer.active && instance.spawnPoints != null)
                {
                    
                    foreach (InstanceSpawnPoint spawnPoint in instance.spawnPoints)
                    {
                        GameObject spawned = Instantiate(spawnPoint.prefab.gameObject, spawnPoint.transform.position, spawnPoint.transform.rotation);
                        spawned.name = spawnPoint.prefab.name; 
                        NetworkServer.Spawn(spawned);
                    }
                }

                
                return instance;
            }
        }
        else Debug.LogWarning("Instance " + template.name + " was already created for partyId=" + partyId + ". This should never happen.");

        return null;
    }
}
