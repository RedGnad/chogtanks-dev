using Photon.Pun;
using UnityEngine;

public class MonadBadgeBroadcaster : MonoBehaviour
{
    private void OnEnable()
    {
        PhotonTankSpawner.OnTankSpawned += OnTankSpawned;
        Sample.MonadGamesIDManager.OnUsernameChanged += OnUsernameChanged;
    }

    private void OnDisable()
    {
        PhotonTankSpawner.OnTankSpawned -= OnTankSpawned;
        Sample.MonadGamesIDManager.OnUsernameChanged -= OnUsernameChanged;
    }

    private void OnTankSpawned(GameObject tank, PhotonView view)
    {
        if (view == null || !view.IsMine) return;

    bool isVerified = PlayerPrefs.GetInt("MonadVerified", 0) == 1;
    MonadBadgeState.Set(view.OwnerActorNr, isVerified); 
    view.RPC("RPC_SetMonadVerified", RpcTarget.AllBuffered, isVerified);
    }

    private void OnUsernameChanged(string _)
    {
        TryBroadcastFromLocalTank();
    }

    private void TryBroadcastFromLocalTank()
    {
        var tanks = GameObject.FindGameObjectsWithTag("Player");
        foreach (var t in tanks)
        {
            var view = t.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                bool isVerified = PlayerPrefs.GetInt("MonadVerified", 0) == 1;
                MonadBadgeState.Set(view.OwnerActorNr, isVerified);
                view.RPC("RPC_SetMonadVerified", RpcTarget.AllBuffered, isVerified);
                break;
            }
        }
    }
}
