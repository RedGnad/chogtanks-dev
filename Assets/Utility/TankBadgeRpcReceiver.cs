using Photon.Pun;
using UnityEngine;

// Attach to the Tank root (same GameObject as PhotonView) to receive badge RPCs reliably
public class TankBadgeRpcReceiver : MonoBehaviourPun
{
    [PunRPC]
    public void RPC_SetMonadVerified(bool isVerified)
    {
        if (photonView != null && photonView.Owner != null)
        {
            MonadBadgeState.Set(photonView.Owner.ActorNumber, isVerified);
        }
        var display = GetComponent<PlayerNameDisplay>();
        if (display != null)
        {
            display.SetBadgeState(isVerified);
        }
        else
        {
            Debug.LogWarning("[BADGE-RPC] PlayerNameDisplay not found on tank root");
        }
    }
}
