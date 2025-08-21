using System;
using System.Collections.Generic;
using AOT;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Privy
{
    internal class ASAuthorizationAppleIDProvider : IDisposable
    {
        internal delegate void ASAuthorizationAppleIDRequestCompletionHandler(
            ASAuthorizationAppleIDCredentials credentials, ASAuthorizationAppleIDRequestError error);

        /// <summary>
        /// A pointer to the underlying instance, in managed memory
        /// </summary>
        private IntPtr _nativeInstance;

        internal ASAuthorizationAppleIDProvider(ASAuthorizationAppleIDRequestCompletionHandler completionHandler)
        {
            _nativeInstance =
                Wrapped_ASAuthorizationAppleIDProvider_Init(OnAppleIDAuthorizationRequestCompleted);
            _completionHandlers.Add(_nativeInstance, completionHandler);
        }

        public void Dispose()
        {
            _completionHandlers.Remove(_nativeInstance);
            Wrapped_ASAuthorizationAppleIDProvider_Dispose(_nativeInstance);
            _nativeInstance = IntPtr.Zero;
        }

        internal void RequestAppleID(string state)
        {
            Wrapped_ASAuthorizationAppleIDProvider_RequestAppleID(_nativeInstance, state);
        }

        internal class ASAuthorizationAppleIDRequestError
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }

        internal class ASAuthorizationAppleIDCredentials
        {
            public string AuthorizationCode { get; set; }
            public string State { get; set; }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private const string DllName = "__Internal";

        [DllImport(DllName)]
        private static extern IntPtr Wrapped_ASAuthorizationAppleIDProvider_Init(ASAuthorizationAppleIDRequestCompletionCallback completionCallback);

        [DllImport(DllName)]
        private static extern void Wrapped_ASAuthorizationAppleIDProvider_RequestAppleID(IntPtr instance, string state);

        [DllImport(DllName)]
        private static extern void Wrapped_ASAuthorizationAppleIDProvider_Dispose(IntPtr instance);
#else
        private static IntPtr Wrapped_ASAuthorizationAppleIDProvider_Init(
            ASAuthorizationAppleIDRequestCompletionCallback completionCallback)
        {
            throw new NotImplementedException("ASAuthorizationAppleIDProvider is only supported on iOS.");
        }

        private static void Wrapped_ASAuthorizationAppleIDProvider_RequestAppleID(IntPtr instance, string state)
        {
            throw new NotImplementedException("ASAuthorizationAppleIDProvider is only supported on iOS.");
        }

        private static void Wrapped_ASAuthorizationAppleIDProvider_Dispose(IntPtr instance)
        {
            throw new NotImplementedException("ASAuthorizationAppleIDProvider is only supported on iOS.");
        }
#endif

        // Unity has a restriction that delegates passed into native code need to be static.
        // OnAppleIDAuthorizationRequestCompleted plays this role, and internally maps to the right instance using the static Dictionary.
        private delegate void ASAuthorizationAppleIDRequestCompletionCallback(IntPtr instance, string state,
            string authorizationCode, int errorCode, string errorMessage);

        private static readonly Dictionary<IntPtr, ASAuthorizationAppleIDRequestCompletionHandler> _completionHandlers =
            new();

        [MonoPInvokeCallback(typeof(ASAuthorizationAppleIDRequestCompletionCallback))]
        private static void OnAppleIDAuthorizationRequestCompleted(IntPtr instance, string state,
            string authorizationCode, int errorCode, string errorMessage)
        {
            if (_completionHandlers.TryGetValue(instance, out var callback))
            {
                var credentials = new ASAuthorizationAppleIDCredentials
                    { State = state, AuthorizationCode = authorizationCode };
                var error = new ASAuthorizationAppleIDRequestError
                    { Code = errorCode, Message = errorMessage };
                callback.Invoke(credentials, error);
            }
        }
    }
}