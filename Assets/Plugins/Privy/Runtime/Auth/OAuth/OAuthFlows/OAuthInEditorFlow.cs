using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Privy
{
    internal class OAuthInEditorFlow : OAuthFlow
    {
        private TaskCompletionSource<OAuthResultData> oauthFlowCompletion;

        private const string InEditorHttpListenerUrl = "http://localhost:8181/oauth/callback/";

        public string TransformRedirectUrl(string redirectUrl) => InEditorHttpListenerUrl;

        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            HttpListener httpListener = new HttpListener();

            httpListener.Prefixes.Add(InEditorHttpListenerUrl);
            httpListener.Start();
            httpListener.BeginGetContext(IncomingHttpRequest, httpListener);

            oauthFlowCompletion = new TaskCompletionSource<OAuthResultData>();

            try
            {
                PrivyLogger.Debug($"Attempting to open url: {oAuthUrl}");
                Application.OpenURL(oAuthUrl);

                // Add 5-minute timeout to the OAuth flow
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(oauthFlowCompletion.Task, timeoutTask);
                if (completedTask == timeoutTask) throw new TimeoutException("OAuth flow timed out after 5 minutes");

                return await oauthFlowCompletion.Task;
            }
            finally
            {
                httpListener.Stop();
            }
        }

        private void IncomingHttpRequest(IAsyncResult result)
        {
            var httpListener = (HttpListener)result.AsyncState;
            var context = httpListener.EndGetContext(result);
            var oAuthResultData = OAuthResultData.parseFromUri(context.Request.Url);

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("You can now close this window.");
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();

            oauthFlowCompletion.SetResult(oAuthResultData);
        }
    }
}