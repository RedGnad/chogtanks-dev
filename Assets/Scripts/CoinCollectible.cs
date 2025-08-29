using UnityEngine;
using Photon.Pun;

public class CoinCollectible : MonoBehaviourPun
{
    [Header("Coin Settings")]
    public int coinValue = 1; 
    public float rotationSpeed = 90f; 
    
    [Header("Audio")]
    public AudioClip collectSound; 
    
    private bool isCollected = false; 
    
    void Start()
    {
        // Debug.Log("[COIN] 🪙 Coin spawné avec succès !");
    }
    
    void Update()
    {
        if (!isCollected)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        ShellCollisionHandler shellHandler = other.GetComponent<ShellCollisionHandler>();
        if (shellHandler != null) return; 
        
        TankMovement2D tankMovement = other.GetComponent<TankMovement2D>();
        if (tankMovement == null) return; 
        
        PhotonView tankPhotonView = other.GetComponent<PhotonView>();
        if (tankPhotonView == null) return;
        
        if (!tankPhotonView.IsMine) return;
        
        isCollected = true;
        
        photonView.RPC("CollectCoinRPC", RpcTarget.All, tankPhotonView.Owner.ActorNumber);
    }
    
    [PunRPC]
    void CollectCoinRPC(int collectorActorNumber)
    {
        if (collectSound != null && SFXManager.Instance != null && SFXManager.Instance.audioSource != null)
        {
            SFXManager.Instance.audioSource.PlayOneShot(collectSound, 0.7f);
        }
        
        if (PhotonNetwork.IsMasterClient && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(collectorActorNumber, coinValue);
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
