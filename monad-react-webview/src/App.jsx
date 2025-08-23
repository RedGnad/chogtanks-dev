import React, { useState, useEffect } from 'react';
import { PrivyProvider, usePrivy } from '@privy-io/react-auth';
import { useCrossAppAccounts } from '@privy-io/react-auth';
import { sendToUnity, closeWindowAfterDelay, isOpenedFromUnity } from './unityBridge';

// Configuration Privy
const PRIVY_APP_ID = "cmek64iqd02lql70b9fl64lm9";
const MONAD_GAMES_ID = "cmd8euall0037le0my79qpz42";
const PRIVY_CLIENT_ID = "client-WY6Ppw4LLAHEMShmi9brMwkW43C9mQfy9r7Z2RyJJojW8";

function MonadLoginComponent() {
  const { ready, authenticated, user, login } = usePrivy();
  const { linkCrossAppAccount } = useCrossAppAccounts();
  const [status, setStatus] = useState('ready');
  const [error, setError] = useState('');
  const [walletInfo, setWalletInfo] = useState(null);
  const [sentToUnity, setSentToUnity] = useState(false);

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
      handleSendToUnity({
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

  const handleSendToUnity = (data) => {
    console.log('[MONAD WEBVIEW] 📤 Sending to Unity:', data);
    setSentToUnity(true);
    
    // Utiliser notre bridge optimisé pour envoyer les données à Unity
    const success = sendToUnity(data);
    
    if (success && data.success) {
      // Planifier la fermeture de la fenêtre après un délai
      closeWindowAfterDelay(3000);
    }
  };
  
  // Vérifier si la fenêtre est ouverte depuis Unity
  useEffect(() => {
    const fromUnity = isOpenedFromUnity();
    console.log('[MONAD WEBVIEW] 🔍 Opened from Unity:', fromUnity);
    
    // Ajouter un écouteur d'événements pour les messages de Unity
    const handleMessage = (event) => {
      if (event.data && event.data.type === 'UNITY_READY') {
        console.log('[MONAD WEBVIEW] 📨 Received UNITY_READY message');
        // Si nous avons déjà des données à envoyer, les renvoyer
        if (walletInfo && status === 'success') {
          handleSendToUnity({
            success: true,
            walletAddress: walletInfo.address,
            username: walletInfo.username,
            userId: user?.id || ''
          });
        }
      }
    };
    
    window.addEventListener('message', handleMessage);
    return () => window.removeEventListener('message', handleMessage);
  }, [walletInfo, status]);

  // Auto-login si déjà authentifié avec cross-app
  useEffect(() => {
    if (ready && authenticated && crossAppAccount && status === 'ready') {
      handleLogin();
    }
    
    // Mise à jour de l'UI quand l'utilisateur est authentifié et a un compte cross-app
    if (ready && authenticated && crossAppAccount && status !== 'success') {
      const embeddedWallet = crossAppAccount.embeddedWallets?.[0];
      if (embeddedWallet?.address) {
        console.log('[MONAD WEBVIEW] 🔄 Updating UI with wallet info');
        setWalletInfo({
          address: embeddedWallet.address,
          username: crossAppAccount.username || user.id
        });
        setStatus('success');
        
        // Envoyer automatiquement à Unity si pas déjà fait
        if (!sentToUnity) {
          handleSendToUnity({
            success: true,
            walletAddress: embeddedWallet.address,
            username: crossAppAccount.username || user.id,
            userId: user.id
          });
        }
      }
    }
  }, [ready, authenticated, crossAppAccount, status, user, sentToUnity]);

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
            disabled={!ready || isLoading || status === 'success'}
            style={{
              background: isLoading ? '#ccc' : status === 'success' ? '#4CAF50' : 'linear-gradient(45deg, #667eea, #764ba2)',
              color: 'white',
              border: 'none',
              padding: '15px 30px',
              borderRadius: '25px',
              fontSize: '16px',
              fontWeight: 'bold',
              cursor: isLoading || status === 'success' ? 'not-allowed' : 'pointer',
              width: '100%',
              marginBottom: '20px'
            }}
          >
            {status === 'success' ? '✅ Connected' : isLoading ? getStatusMessage() : 'Sign in with Monad Games ID'}
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
