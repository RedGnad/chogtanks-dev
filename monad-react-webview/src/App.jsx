import React, { useState, useEffect } from 'react';
import { PrivyProvider, usePrivy } from '@privy-io/react-auth';
import { useCrossAppAccounts } from '@privy-io/react-auth';

// Configuration Privy
const PRIVY_APP_ID = "cmek64iqd02lql70b9fl64lm9";
const PRIVY_CLIENT_ID = "client-WY6Ppw4LLAHEMShmi9brMwkW43C9mQfy9r7Z2RyJJojW8";
const MONAD_GAMES_ID = "cmd8euall0037le0my79qpz42";

function MonadLoginComponent() {
  const { ready, authenticated, user, login } = usePrivy();
  const { linkCrossAppAccount } = useCrossAppAccounts();
  const [status, setStatus] = useState('ready');
  const [error, setError] = useState('');
  const [walletInfo, setWalletInfo] = useState(null);

  // Vérifier si l'utilisateur a déjà un compte cross-app lié
  const crossAppAccount = user?.linkedAccounts?.find(
    account => account.type === 'cross_app' && 
               account.providerApp?.id === MONAD_GAMES_ID
  );

  useEffect(() => {
    console.log('[MONAD WEBVIEW] Component mounted');
    console.log('[MONAD WEBVIEW] Ready:', ready, 'Authenticated:', authenticated);
    console.log('[MONAD WEBVIEW] User:', user);
    console.log('[MONAD WEBVIEW] Cross App Account:', crossAppAccount);
  }, [ready, authenticated, user, crossAppAccount]);

  const handleLogin = async () => {
    try {
      setStatus('logging_in');
      setError('');
      
      console.log('[MONAD WEBVIEW] Starting login process...');
      
      // Étape 1: Login Privy si pas encore authentifié
      if (!authenticated) {
        console.log('[MONAD WEBVIEW] User not authenticated, logging in...');
        await login();
        return; // Le useEffect se déclenchera après l'auth
      }

      // Étape 2: Lier compte Cross App si pas encore fait
      if (!crossAppAccount) {
        console.log('[MONAD WEBVIEW] Linking Cross App account...');
        setStatus('linking_account');
        
        await linkCrossAppAccount({ 
          appId: MONAD_GAMES_ID 
        });
        
        // Attendre que le user soit mis à jour
        setTimeout(() => {
          window.location.reload();
        }, 1000);
        return;
      }

      // Étape 3: Récupérer wallet address
      console.log('[MONAD WEBVIEW] Getting wallet address...');
      setStatus('getting_wallet');
      
      const embeddedWallet = crossAppAccount.embeddedWallets?.[0];
      if (!embeddedWallet?.address) {
        throw new Error('No embedded wallet found in cross-app account');
      }

      const walletAddress = embeddedWallet.address;
      const username = crossAppAccount.username || user.id;

      console.log('[MONAD WEBVIEW] ✅ Success! Wallet:', walletAddress);
      
      setWalletInfo({
        address: walletAddress,
        username: username
      });

      // Envoyer à Unity
      sendToUnity({
        success: true,
        walletAddress: walletAddress,
        username: username,
        userId: user.id
      });

      setStatus('success');

    } catch (err) {
      console.error('[MONAD WEBVIEW] ❌ Error:', err);
      setError(err.message || 'Unknown error occurred');
      setStatus('error');
    }
  };

  const sendToUnity = (data) => {
    console.log('[MONAD WEBVIEW] 📤 Sending to Unity:', data);
    
    try {
      // Méthode 1: Communication directe Unity WebGL
      if (window.unityInstance && window.unityInstance.SendMessage) {
        window.unityInstance.SendMessage('MonadGamesIDWebView', 'OnMonadGamesIDResult', JSON.stringify(data));
        console.log('[MONAD WEBVIEW] ✅ Sent via unityInstance.SendMessage');
      }
      
      // Méthode 2: PostMessage pour communication parent
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({
          type: 'MONAD_GAMES_ID_RESULT',
          data: data
        }, '*');
        console.log('[MONAD WEBVIEW] ✅ Sent via postMessage');
      }
      
      // Méthode 3: Fallback localStorage
      localStorage.setItem('MONAD_WALLET_RESULT', JSON.stringify(data));
      console.log('[MONAD WEBVIEW] ✅ Saved to localStorage');
      
      // Auto-fermeture après 3 secondes
      setTimeout(() => {
        if (window.close) {
          window.close();
        }
      }, 3000);
      
    } catch (err) {
      console.error('[MONAD WEBVIEW] ❌ Error sending to Unity:', err);
    }
  };

  // Auto-login si déjà authentifié avec cross-app
  useEffect(() => {
    if (ready && authenticated && crossAppAccount && status === 'ready') {
      handleLogin();
    }
  }, [ready, authenticated, crossAppAccount, status]);

  const getStatusMessage = () => {
    switch (status) {
      case 'logging_in': return 'Connecting to Privy...';
      case 'linking_account': return 'Linking Monad Games ID...';
      case 'getting_wallet': return 'Getting wallet address...';
      case 'success': return 'Success! Sending to Unity...';
      case 'error': return 'Error occurred';
      default: return 'Ready to connect';
    }
  };

  const isLoading = ['logging_in', 'linking_account', 'getting_wallet'].includes(status);

  return (
    <div className="container">
      <div className="logo">🎮 CHOGTANKS</div>
      
      {!ready && (
        <div className="loading">Loading Privy SDK...</div>
      )}
      
      {ready && (
        <>
          <button 
            onClick={handleLogin}
            disabled={!ready || isLoading}
            style={{
              background: isLoading ? '#ccc' : 'linear-gradient(45deg, #667eea, #764ba2)',
              color: 'white',
              border: 'none',
              padding: '15px 30px',
              borderRadius: '25px',
              fontSize: '16px',
              fontWeight: 'bold',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              width: '100%',
              marginBottom: '20px'
            }}
          >
            {isLoading ? getStatusMessage() : 'Sign in with Monad Games ID'}
          </button>
          
          {status === 'success' && walletInfo && (
            <div className="success">
              <strong>✅ Connected Successfully!</strong>
              <div className="wallet-info">
                <div><strong>Username:</strong> {walletInfo.username}</div>
                <div><strong>Wallet:</strong> {walletInfo.address}</div>
              </div>
              <div style={{fontSize: '14px', marginTop: '10px'}}>
                Sending to Unity... Window will close automatically.
              </div>
            </div>
          )}
          
          {error && (
            <div className="error">
              <strong>❌ Error:</strong> {error}
              <button 
                onClick={() => {setError(''); setStatus('ready');}}
                style={{
                  background: '#e74c3c',
                  color: 'white',
                  border: 'none',
                  padding: '8px 16px',
                  borderRadius: '15px',
                  fontSize: '12px',
                  marginTop: '10px',
                  cursor: 'pointer'
                }}
              >
                Retry
              </button>
            </div>
          )}
          
          <div className="loading">{getStatusMessage()}</div>
        </>
      )}
    </div>
  );
}

function App() {
  return (
    <PrivyProvider 
      appId={PRIVY_APP_ID}
      config={{
        loginMethodsAndOrder: {
          primary: [`privy:${MONAD_GAMES_ID}`],
        },
        embeddedWallets: {
          createOnLogin: 'users-without-wallets'
        }
      }}
    >
      <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>
        <h1>🎮 Monad Games ID</h1>
        <MonadLoginComponent />
      </div>
    </PrivyProvider>
  );
}

export default App;
