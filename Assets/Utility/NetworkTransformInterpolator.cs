using Fusion;
using UnityEngine;

public class NetworkTransformInterpolator : NetworkBehaviour
{
    [Header("Interpolation")]
    public float positionLerpSpeed = 15f;
    public float rotationLerpSpeed = 15f;

    private Vector3 networkedPosition;
    private Quaternion networkedRotation;

    private void Awake()
    {
        networkedPosition = transform.position;
        networkedRotation = transform.rotation;
    }

    void Update()
    {
        if (!Object)
        {
            transform.position = Vector3.Lerp(transform.position, networkedPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkedRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }

    // OnPhotonSerializeView removed for Fusion - method commented out
    public void OnPhotonSerializeViewFusion()
    {
        // Fusion networking handled differently
        // if (stream.IsWriting)
        // {
        //     stream.SendNext(transform.position);
        //     stream.SendNext(transform.rotation);
        // }
        // else
        // {
        //     networkedPosition = (Vector3)stream.ReceiveNext();
        //     networkedRotation = (Quaternion)stream.ReceiveNext();
        // }
    }
}
