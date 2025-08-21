mergeInto(LibraryManager.library, {
  RegisterMessageCallbackWebGL: function(gameObjectName, methodName) {
    var gameObjectNameStr = UTF8ToString(gameObjectName);
    var methodNameStr = UTF8ToString(methodName);
    
    console.log('[Unity Plugin] Registering callback:', gameObjectNameStr, methodNameStr);
    
    // Stocker les références globalement
    window.unityGameObjectName = gameObjectNameStr;
    window.unityMethodName = methodNameStr;
    
    // Marquer Unity comme prêt
    window.unityReady = true;
    
    // Vérifier s'il y a déjà un résultat en attente dans localStorage
    var pendingResult = localStorage.getItem('MONAD_WALLET_RESULT');
    if (pendingResult) {
      console.log('[Unity Plugin] Found pending result, sending to Unity:', pendingResult);
      try {
        // Envoyer le résultat immédiatement
        if (window.unityInstance && window.unityInstance.SendMessage) {
          window.unityInstance.SendMessage(gameObjectNameStr, methodNameStr, pendingResult);
        }
        // Nettoyer le localStorage
        localStorage.removeItem('MONAD_WALLET_RESULT');
      } catch (e) {
        console.error('[Unity Plugin] Error sending pending result:', e);
      }
    }
    
    // Écouter les messages postMessage
    window.addEventListener('message', function(event) {
      if (event.data && event.data.type === 'MONAD_GAMES_ID_RESULT') {
        console.log('[Unity Plugin] Received postMessage:', event.data);
        try {
          var jsonData = JSON.stringify(event.data.data);
          if (window.unityInstance && window.unityInstance.SendMessage) {
            window.unityInstance.SendMessage(gameObjectNameStr, methodNameStr, jsonData);
            console.log('[Unity Plugin] ✅ Sent to Unity via SendMessage');
          }
        } catch (e) {
          console.error('[Unity Plugin] Error processing postMessage:', e);
        }
      }
    });
    
    console.log('[Unity Plugin] ✅ Callback registered successfully');
  },
  
  // Fonction utilitaire pour vérifier si Unity est prêt
  IsUnityReady: function() {
    return window.unityReady ? 1 : 0;
  },
  
  // Fonction pour lire le résultat depuis localStorage (fallback)
  ReadMonadWalletResult: function() {
    var result = localStorage.getItem('MONAD_WALLET_RESULT') || "";
    var bufferSize = lengthBytesUTF8(result) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(result, buffer, bufferSize);
    
    // Nettoyer après lecture
    if (result) {
      localStorage.removeItem('MONAD_WALLET_RESULT');
    }
    
    return buffer;
  }
});
