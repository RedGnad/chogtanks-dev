using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class CloakPowerup : MonoBehaviourPun
{
    [Header("SFX")]
    [SerializeField] private AudioClip pickupSFX;
    [SerializeField] private float pickupVolume = 1.3f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // We only want the master client to handle the logic
        if (!PhotonNetwork.IsMasterClient) return;

        // Check if the object that entered the trigger has a PhotonView and belongs to a player
        TankShoot2D tank = other.GetComponentInParent<TankShoot2D>();
        if (tank != null && tank.photonView != null && tank.photonView.Owner != null)
        {
            // Use an RPC to grant the power-up to the specific client who owns the tank
            tank.photonView.RPC("RPC_ActivateCloakPowerup", tank.photonView.Owner);

            // Play pickup sound for everyone via an RPC on this powerup's PhotonView
            photonView.RPC("RPC_PlayPickupFX", RpcTarget.All);

            // Master client destroys the power-up object
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    private void RPC_PlayPickupFX()
    {
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, pickupVolume);
        }
    }
}
