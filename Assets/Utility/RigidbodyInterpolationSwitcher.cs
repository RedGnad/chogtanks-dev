using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RigidbodyInterpolationSwitcher : MonoBehaviourPun
{
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        ApplyInterpolationMode();
    }

    private void Start()
    {
        ApplyInterpolationMode();
    }

    public void ApplyInterpolationMode()
    {
        if (_rb == null) return;

        _rb.interpolation = photonView != null && photonView.IsMine
            ? RigidbodyInterpolation2D.Interpolate
            : RigidbodyInterpolation2D.None;
    }
}
