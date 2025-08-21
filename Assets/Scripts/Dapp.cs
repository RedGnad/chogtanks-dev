using UnityEngine;

namespace Sample
{
    public class Dapp : MonoBehaviour
    {
        private void Awake()
        {
            // Script complètement désactivé - migration vers Privy
            Destroy(gameObject);
        }
    }
}
