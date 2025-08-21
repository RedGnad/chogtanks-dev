using System;
using System.Collections.Generic;
using AOT;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Privy
{
    /// <summary>
    ///     A wrapper of the ASWebAuthenticationSession class from the Authentication Services framework.
    ///     See:
    ///     <a href="https://developer.apple.com/documentation/authenticationservices/aswebauthenticationsession">Apple Docs</a>
    /// </summary>
    internal class ASWebAuthenticationSession : IDisposable
    {
        internal delegate void ASWebAuthenticationSessionCompletionHandler(Uri uri,
            ASWebAuthenticationSessionError error);

        private IntPtr _nativeInstance;

        internal ASWebAuthenticationSession(string url, string callbackURLScheme,
            ASWebAuthenticationSessionCompletionHandler completionHandler)
        {
            _nativeInstance =
                Wrapped_AS_ASWebAuthenticationSession_Init(url, callbackURLScheme, OnAuthenticationSessionCompleted);
            _completionHandlers.Add(_nativeInstance, completionHandler);
        }

        internal bool Start()
        {
            return Wrapped_AS_ASWebAuthenticationSession_Start(_nativeInstance);
        }

        public void Dispose()
        {
            _completionHandlers.Remove(_nativeInstance);
            _nativeInstance = IntPtr.Zero;
        }

        internal class ASWebAuthenticationSessionError
        {
            public int Code;
            public string Message;

            public ASWebAuthenticationSessionError(int code, string message)
            {
                Code = code;
                Message = message;
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private const string DllName = "__Internal";

        [DllImport(DllName)]
        private static extern IntPtr Wrapped_AS_ASWebAuthenticationSession_Init(string url, string callbackUrlScheme, ASWebAuthenticationSessionCompletedCallback completionHandler);

        [DllImport(DllName)]
        private static extern bool Wrapped_AS_ASWebAuthenticationSession_Start(IntPtr instance);
#else
        private static IntPtr Wrapped_AS_ASWebAuthenticationSession_Init(string url, string callbackUrlScheme,
            ASWebAuthenticationSessionCompletedCallback completionHandler)
        {
            throw new NotImplementedException("ASWebAuthenticationSession is only supported on iOS.");
        }

        private static bool Wrapped_AS_ASWebAuthenticationSession_Start(IntPtr instance)
        {
            throw new NotImplementedException("ASWebAuthenticationSession is only supported on iOS.");
        }
#endif

        // Unity has a restriction that delegates passed into native code need to be static.
        // OnAuthenticationSessionCompleted plays this role, and internally maps to the right instance using the static Dictionary.
        private delegate void ASWebAuthenticationSessionCompletedCallback(IntPtr instance, string callbackUrl,
            int errorCode, string errorMessage);

        private static readonly Dictionary<IntPtr, ASWebAuthenticationSessionCompletionHandler> _completionHandlers =
            new();

        [MonoPInvokeCallback(typeof(ASWebAuthenticationSessionCompletedCallback))]
        private static void OnAuthenticationSessionCompleted(IntPtr instance, string callbackUrl, int errorCode,
            string errorMessage)
        {
            if (_completionHandlers.TryGetValue(instance, out var callback))
            {
                if (!string.IsNullOrEmpty(callbackUrl))
                {
                    var uri = new Uri(callbackUrl);
                    callback.Invoke(uri, null);
                }
                else
                {
                    var error = new ASWebAuthenticationSessionError(errorCode, errorMessage);
                    callback.Invoke(null, error);
                }
            }
        }
    }
}