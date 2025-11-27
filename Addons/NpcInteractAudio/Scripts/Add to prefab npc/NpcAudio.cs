using Mirror;
using UnityEngine;

public class NpcAudio : NetworkBehaviour
{
    public Npc npc;

    [Header("[-=-=-[ Npc Audio Interact ]-=-=-]")]
    public AudioClip[] interactAudio;
    [Range(0, 1)] public float adjustedVolumeInteract = 1f;

    private int selectedSound = 0;

    public override void OnStartServer()
    {
        npc.onInteract.AddListener(onInteract_NpcAudio);
    }

    private void onInteract_NpcAudio()
    {
        Player player = Player.localPlayer;
        
        if (player != null && player.target != null && player.target is Npc npc && Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        {
            if (npc.npcAudio.interactAudio == null) return;
            npc.npcAudio.PlayInteractSound();
        }

    }

    private void PlayInteractSound()
    {

        AudioSource tempSource = GetComponentInParent<AudioSource>();
        if(tempSource == null) return;
        if(interactAudio.Length >= 1)
        {
            selectedSound = Random.Range(0, interactAudio.Length);
            tempSource.PlayOneShot(interactAudio[selectedSound], adjustedVolumeInteract);
        }
        
    }
}