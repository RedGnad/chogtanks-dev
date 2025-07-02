using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Interpolation réseau avancée avec buffer (historical interpolation).
/// À attacher sur tanks et shells pour un mouvement ultra fluide même en cas de lag.
/// Le PhotonView doit observer ce script.
/// </summary>
public class NetworkBufferedInterpolator : MonoBehaviourPun, IPunObservable
{
    [Header("Interpolation avancée")]
    [Tooltip("Temps de retard d'interpolation en secondes (0.1 = 100ms)")]
    public float interpolationBackTime = 0.1f;
    [Tooltip("Supprime les points trop vieux après ce délai (en s)")]
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

            // Supprime les états trop vieux
            stateBuffer.RemoveAll(s => s.timestamp < PhotonNetwork.Time - bufferTimeLimit);

            // Cherche les deux states autour du temps d'interpolation
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
            // Si trop vieux ou trop récent, extrapole ou fixe sur le plus proche
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
            // Garde le buffer trié
            stateBuffer.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
        }
    }
}
