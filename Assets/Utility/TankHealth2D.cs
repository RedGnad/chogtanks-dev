using Photon.Pun;
using UnityEngine;
using System.Linq;

public class TankHealth2D : MonoBehaviourPun
{
    [Header("Paramètres de santé")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Prefab UI contenant le Canvas Game Over & You Win")]
    [SerializeField] private GameObject gameOverUIPrefab;

    private float currentHealth = 0f;
    private bool _isDead = false;
    public float CurrentHealth => currentHealth;
    public bool IsDead => _isDead;

    private void Start()
    {
        if (photonView.IsMine)
        {
            currentHealth = maxHealth;
            _isDead = false;
            EnableInputs();
            // Détruit l’UI GameOver/Win s’il y en a une
            if (uiGOInstance != null)
            {
                Destroy(uiGOInstance);
                uiGOInstance = null;
            }
            // Supprime aussi toutes les UI GameOver/Win résiduelles (au cas où)
            foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
            {
                Destroy(ui);
            }
        }
    }

    private void EnableInputs()
    {
        var move = GetComponent<TankMovement2D>();
        if (move != null) move.enabled = true;
        var shoot = GetComponent<TankShoot2D>();
        if (shoot != null) shoot.enabled = true;
    }

    [PunRPC]
    public void TakeDamageRPC(float amount)
    {
        if (_isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth <= 0f)
        {
            Debug.Log("[TANK DEBUG] Mort détectée, envoi HandleDeathRPC");
            _isDead = true;
            photonView.RPC("HandleDeathRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void HandleDeathRPC()
    {
        Debug.Log($"[TANK DEATH DEBUG] HandleDeathRPC appelé sur {PhotonNetwork.LocalPlayer.NickName} (Actor {PhotonNetwork.LocalPlayer.ActorNumber}), IsMine={photonView.IsMine}, IsMasterClient={PhotonNetwork.IsMasterClient}");
        // 1) Affiche l’UI GameOver uniquement sur le joueur local détruit
        Debug.Log("[TANK DEBUG] Mort (destruction réseau)");

        // 2) Vérifie côté MasterClient s’il ne reste qu’un tank vivant
        if (PhotonNetwork.IsMasterClient)
        {
            var tanks = FindObjectsOfType<TankHealth2D>();
            var alive = tanks.Where(t => !t._isDead).ToList();
            Debug.Log($"[TANK DEATH DEBUG] MasterClient check: tanks vivants={alive.Count}");
            if (alive.Count == 1)
            {
                Debug.Log($"[TANK DEATH DEBUG] Un seul tank vivant ({alive[0].photonView.Owner.NickName}), envoi WinRPC et lancement du reset soft dans 3s");
                // Envoie le Win uniquement au joueur gagnant
                alive[0].photonView.RPC("WinRPC", alive[0].photonView.Owner);
                // Le reset soft automatique est maintenant déclenché dans ShowGameOverUI/ShowWinUI sur chaque client
            }
        }

        // 3) Détruit le tank uniquement si propriétaire
        if (photonView.IsMine)
        {
            Debug.Log("[TANK DEBUG] Affichage explicite de l'UI GameOver avant destruction");
            ShowGameOverUI();
            Debug.Log("[TANK DEBUG] Destruction réseau du tank (PhotonNetwork.Destroy)");
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    private void WinRPC()
    {
        ShowWinUI();
    }

    private GameObject uiGOInstance;

    // Désactive les scripts de contrôle sur le tank local
    private void DisableInputs()
    {
        var move = GetComponent<TankMovement2D>();
        if (move != null) move.enabled = false;
        var shoot = GetComponent<TankShoot2D>();
        if (shoot != null) shoot.enabled = false;
    }

    // Coroutine pour relancer automatiquement la partie après 3 secondes
    private System.Collections.IEnumerator AutoReplayAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        PhotonLauncher.CallRestartMatchSoft();
    }



    private void ShowGameOverUI()
    {
        DisableInputs();
        if (gameOverUIPrefab == null)
        {
            Debug.LogError("[TankHealth2D] gameOverUIPrefab non assigné !");
            return;
        }
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[TankHealth2D] Impossible de trouver Camera.main !");
            return;
        }
        if (uiGOInstance != null) Destroy(uiGOInstance);
        uiGOInstance = Instantiate(gameOverUIPrefab, mainCam.transform);
        RectTransform rt = uiGOInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = new Vector3(0f, 0f, 1f);
            rt.localRotation = Quaternion.identity;
            float baseScale = 1f;
            float dist = Vector3.Distance(mainCam.transform.position, rt.position);
            float scaleFactor = baseScale * (dist / mainCam.orthographicSize) * 0.1f;
            rt.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }
        var controller = uiGOInstance.GetComponent<GameOverUIController>();
        if (controller != null)
        {
            controller.ShowGameOver();
        }
        else
        {
            Debug.LogError("[TankHealth2D] Le prefab UI n’a pas GameOverUIController !");
        }
        // Lancer automatiquement le reset soft après 3 secondes
        StartCoroutine(AutoReplayAfterDelay());
    }

    private void ShowWinUI()
    {
        DisableInputs();
        if (gameOverUIPrefab == null)
        {
            Debug.LogError("[TankHealth2D] gameOverUIPrefab non assigné !");
            return;
        }
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[TankHealth2D] Impossible de trouver Camera.main !");
            return;
        }
        if (uiGOInstance != null) Destroy(uiGOInstance);
        uiGOInstance = Instantiate(gameOverUIPrefab, mainCam.transform);
        RectTransform rt = uiGOInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = new Vector3(0f, 0f, 1f);
            rt.localRotation = Quaternion.identity;
            float baseScale = 1f;
            float dist = Vector3.Distance(mainCam.transform.position, rt.position);
            float scaleFactor = baseScale * (dist / mainCam.orthographicSize) * 0.1f;
            rt.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }
        var controller = uiGOInstance.GetComponent<GameOverUIController>();
        if (controller != null)
        {
            controller.ShowWin();
        }
        else
        {
            Debug.LogError("[TankHealth2D] Le prefab UI n’a pas GameOverUIController !");
        }
        // Lancer automatiquement le reset soft après 3 secondes
        StartCoroutine(AutoReplayAfterDelay());
    }
}