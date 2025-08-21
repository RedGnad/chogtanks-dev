using UnityEngine;

namespace Privy
{
    public class WebGLPopupMessageInterceptor : MonoBehaviour
    {
        public delegate void OnSignedInDelegate(string redirectUri);

        public delegate void OnSignInFailedDelegate();

        public OnSignedInDelegate OnSignedIn;
        public OnSignInFailedDelegate OnSignInFailed;

        public void SignedIn(string redirectUri)
        {
            OnSignedIn(redirectUri);
        }

        public void SignInFailed()
        {
            OnSignInFailed();
        }
    }
}