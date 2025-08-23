mergeInto(LibraryManager.library, {
    RegisterMessageCallbackWebGL: function(gameObjectNamePtr, methodNamePtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        
        console.log('[MONAD PLUGIN] Registering callback:', gameObjectName, methodName);
        
        // Écouter les messages postMessage de la page React
        window.addEventListener('message', function(event) {
            if (event.data && event.data.type === 'MONAD_GAMES_ID_RESULT') {
                console.log('[MONAD PLUGIN] Received message from React:', event.data);
                
                try {
                    // Envoyer à Unity
                    unityInstance.SendMessage(gameObjectName, methodName, JSON.stringify(event.data.data));
                } catch (error) {
                    console.error('[MONAD PLUGIN] Error sending to Unity:', error);
                }
            }
        });
        
        // Écouter les changements d'URL pour callback scheme
        var originalPushState = history.pushState;
        var originalReplaceState = history.replaceState;
        
        function handleUrlChange() {
            var url = window.location.href;
            if (url.includes('chogtanks://monad-result')) {
                console.log('[MONAD PLUGIN] URL callback detected:', url);
                
                try {
                    var params = new URLSearchParams(url.split('?')[1]);
                    var result = {};
                    
                    for (var [key, value] of params) {
                        result[key] = value;
                    }
                    
                    unityInstance.SendMessage(gameObjectName, methodName, JSON.stringify(result));
                } catch (error) {
                    console.error('[MONAD PLUGIN] Error parsing URL callback:', error);
                }
            }
        }
        
        history.pushState = function() {
            originalPushState.apply(history, arguments);
            handleUrlChange();
        };
        
        history.replaceState = function() {
            originalReplaceState.apply(history, arguments);
            handleUrlChange();
        };
        
        window.addEventListener('popstate', handleUrlChange);
    },
    
    ReadMonadWalletResult: function() {
        var result = localStorage.getItem('MONAD_WALLET_RESULT') || '';
        console.log('[MONAD PLUGIN] Reading from localStorage:', result);
        
        // Convertir en pointeur de chaîne pour Unity
        var bufferSize = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(result, buffer, bufferSize);
        return buffer;
    },
    
    IsUnityReady: function() {
        return (typeof unityInstance !== 'undefined' && unityInstance) ? 1 : 0;
    }
});
