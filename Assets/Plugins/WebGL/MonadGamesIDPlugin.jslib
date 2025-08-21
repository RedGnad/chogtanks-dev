mergeInto(LibraryManager.library, {
    RegisterMessageCallbackWebGL: function(gameObjectNamePtr, methodNamePtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        
        console.log('[MONAD PLUGIN] Registering callback:', gameObjectName, methodName);
        
        // Écouter les messages postMessage de la page React
        window.addEventListener('message', function(event) {
            if (event.data && event.data.type === 'CHOGTANKS_MONAD_RESULT') {
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
    
    OpenMonadGamesIDWindow: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        console.log('[MONAD PLUGIN] Opening Monad Games ID window:', url);
        
        // Ouvrir dans une nouvelle fenêtre avec dimensions optimales
        window.monadGamesWindow = window.open(
            url, 
            'MonadGamesID', 
            'width=500,height=700,scrollbars=yes,resizable=yes,location=no,menubar=no,toolbar=no'
        );
        
        // Vérifier si la fenêtre s'est fermée
        var checkClosed = setInterval(function() {
            if (window.monadGamesWindow.closed) {
                console.log('[MONAD PLUGIN] Monad Games ID window closed');
                clearInterval(checkClosed);
                
                // Notifier Unity que la fenêtre s'est fermée
                try {
                    unityInstance.SendMessage('MonadGamesIDWebView', 'OnWebViewClosed', '');
                } catch (error) {
                    console.error('[MONAD PLUGIN] Error notifying Unity of window close:', error);
                }
            }
        }, 1000);
    },
    
    CloseMonadGamesIDWindow: function() {
        console.log('[MONAD PLUGIN] Closing Monad Games ID window');
        
        if (window.monadGamesWindow && !window.monadGamesWindow.closed) {
            window.monadGamesWindow.close();
        }
    }
});
