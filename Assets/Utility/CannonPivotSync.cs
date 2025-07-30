using Photon.Pun;
using UnityEngine;

public class CannonPivotSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private Transform cannonPivot; 
    private float networkedZ = 0f;

    void Update()
    {
        if (!photonView.IsMine)
        {
            Vector3 rot = cannonPivot.localEulerAngles;
            rot.z = Mathf.LerpAngle(rot.z, networkedZ, Time.deltaTime * 10f);
            cannonPivot.localEulerAngles = rot;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(cannonPivot.localEulerAngles.z);
        }
        else
        {
            networkedZ = (float)stream.ReceiveNext();
        }
    }
}
