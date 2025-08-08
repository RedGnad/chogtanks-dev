using Fusion;
using UnityEngine;

public class CannonPivotSync : NetworkBehaviour
{
    [SerializeField] private Transform cannonPivot; 
    private float networkedZ = 0f;

    void Update()
    {
        if (!Object)
        {
            Vector3 rot = cannonPivot.localEulerAngles;
            rot.z = Mathf.LerpAngle(rot.z, networkedZ, Time.deltaTime * 10f);
            cannonPivot.localEulerAngles = rot;
        }
    }

    // OnPhotonSerializeView removed for Fusion - method commented out
    public void OnPhotonSerializeViewFusion()
    {
        // Fusion networking handled differently
        // if (stream.IsWriting)
        // {
        //     stream.SendNext(cannonPivot.localEulerAngles.z);
        // }
        // else
        // {
        //     networkedZ = (float)stream.ReceiveNext();
        // }
    }
}
