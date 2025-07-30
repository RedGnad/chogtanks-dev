using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class NetworkBufferedInterpolator : MonoBehaviourPun, IPunObservable
{
    [Header("Interpolation avancée")]
    public float interpolationBackTime = 0.1f;
    public float bufferTimeLimit = 1.0f;

    private struct State
    {
        public double timestamp;
        public Vector3 position;
        public Quaternion rotation;
    }
    private List<State> stateBuffer = new List<State>();

    void Update()
    {
        if (!photonView.IsMine && stateBuffer.Count >= 2)
        {
            double interpTime = PhotonNetwork.Time - interpolationBackTime;

            stateBuffer.RemoveAll(s => s.timestamp < PhotonNetwork.Time - bufferTimeLimit);

            for (int i = 0; i < stateBuffer.Count - 1; i++)
            {
                if (stateBuffer[i].timestamp <= interpTime && interpTime <= stateBuffer[i + 1].timestamp)
                {
                    State s0 = stateBuffer[i];
                    State s1 = stateBuffer[i + 1];
                    float t = (float)((interpTime - s0.timestamp) / (s1.timestamp - s0.timestamp));
                    transform.position = Vector3.Lerp(s0.position, s1.position, t);
                    transform.rotation = Quaternion.Slerp(s0.rotation, s1.rotation, t);
                    return;
                }
            }
            State latest = stateBuffer[stateBuffer.Count - 1];
            transform.position = latest.position;
            transform.rotation = latest.rotation;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            State state = new State
            {
                timestamp = info.SentServerTime,
                position = (Vector3)stream.ReceiveNext(),
                rotation = (Quaternion)stream.ReceiveNext()
            };
            stateBuffer.Add(state);
            stateBuffer.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
        }
    }
}
