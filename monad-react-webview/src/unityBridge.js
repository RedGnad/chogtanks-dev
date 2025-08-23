/**
 * Unity Bridge - Communication entre React et Unity WebGL
 * Permet d'envoyer des données de l'application React vers Unity
 */

// Fonction pour envoyer des données à Unity via postMessage et localStorage
export const sendToUnity = (data) => {
  try {
    // Convertir en string si c'est un objet
    const jsonData = typeof data === 'object' ? JSON.stringify(data) : data;
    
    // Stocker dans localStorage pour le polling
    localStorage.setItem('MONAD_WALLET_RESULT', jsonData);
    
    console.log('[REACT → UNITY] 📤 Données envoyées via localStorage:', data);
    
    // Essayer d'envoyer via postMessage à la fenêtre parente (Unity WebGL)
    if (window.opener) {
      window.opener.postMessage({
        type: 'MONAD_GAMES_ID_RESULT',
        data: data
      }, '*');
      console.log('[REACT → UNITY] 📤 Données envoyées via postMessage à window.opener');
    } else {
      // Essayer d'envoyer à la fenêtre parente
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({
          type: 'MONAD_GAMES_ID_RESULT',
          data: data
        }, '*');
        console.log('[REACT → UNITY] 📤 Données envoyées via postMessage à window.parent');
      }
    }
    
    // Essayer d'appeler directement la fonction Unity si disponible
    if (window.OnMonadGamesIDResult) {
      window.OnMonadGamesIDResult(jsonData);
      console.log('[REACT → UNITY] 📤 Données envoyées via OnMonadGamesIDResult');
    }
    
    return true;
  } catch (err) {
    console.error('[REACT → UNITY] ❌ Erreur lors de l\'envoi des données:', err);
    return false;
  }
};

// Fonction pour fermer la fenêtre après envoi réussi
export const closeWindowAfterDelay = (delay = 1000) => {
  setTimeout(() => {
    try {
      console.log('[REACT → UNITY] 🚪 Fermeture de la fenêtre après délai');
      window.close();
    } catch (err) {
      console.error('[REACT → UNITY] ❌ Impossible de fermer la fenêtre:', err);
    }
  }, delay);
};

// Fonction pour vérifier si la fenêtre est ouverte depuis Unity
export const isOpenedFromUnity = () => {
  return window.opener !== null || window.parent !== window;
};

// Exporter un objet global pour utilisation directe dans les balises script
if (typeof window !== 'undefined') {
  window.MonadUnityBridge = {
    sendToUnity,
    closeWindowAfterDelay,
    isOpenedFromUnity
  };
}

export default {
  sendToUnity,
  closeWindowAfterDelay,
  isOpenedFromUnity
};
