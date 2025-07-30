# Configuration du Serveur de Signatures NFT

## 1. Génération de la clé privée serveur

### Option A: Avec Node.js (Recommandé)
```javascript
const { ethers } = require('ethers');

// Générer une nouvelle clé privée aléatoire
const wallet = ethers.Wallet.createRandom();
console.log('Adresse publique:', wallet.address);
console.log('Clé privée:', wallet.privateKey);
```

### Option B: Avec Metamask
1. Créer un nouveau compte Metamask dédié au serveur
2. Exporter la clé privée (Settings > Security & Privacy > Reveal Private Key)
3. Ne JAMAIS utiliser cette clé pour stocker des fonds

## 2. Configuration du serveur

1. Créer un fichier `.env` :
```bash
GAME_SERVER_PRIVATE_KEY=0x1234567890abcdef...
PORT=3001
```

2. Modifier `signature-server.js` pour utiliser les variables d'environnement :
```javascript
require('dotenv').config();
const gameServerSigner = new ethers.Wallet(process.env.GAME_SERVER_PRIVATE_KEY);
```

## 3. Déploiement du contrat

Dans Remix, lors du déploiement, utiliser l'adresse publique générée comme `_gameServerSigner`.

## 4. Sécurité

- La clé privée ne doit JAMAIS être exposée côté client
- Utiliser HTTPS en production
- Stocker la clé dans des variables d'environnement sécurisées
- Cette clé ne sert QUE à signer les autorisations, pas à payer les transactions

## 5. Test rapide

Pour tester immédiatement, vous pouvez utiliser cette clé de test :
```
Clé privée: 0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d
Adresse: 0x70997970C51812dc3A010C7d01b50e0d17dc79C8
```

⚠️ **ATTENTION**: Cette clé est publique, utilisez-la UNIQUEMENT pour les tests !
