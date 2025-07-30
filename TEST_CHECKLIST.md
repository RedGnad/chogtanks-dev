# Checklist Exhaustive des Tests Locaux - Système NFT TANKS

## ✅ Prérequis avant tests

### Serveur de signatures
- [ ] Serveur tourne sur `http://localhost:3001`
- [ ] Clé privée configurée dans `.env`
- [ ] Adresse serveur affichée : `0x8107edd492E8201a286b163f38d896a779AFA6b9`

### Smart Contract
- [ ] Contrat déployé avec l'adresse serveur comme `_gameServerSigner`
- [ ] Supply max = 600 NFTs
- [ ] Symbole = "TANKS"

### Unity WebGL Build
- [ ] Build WebGL créé et fonctionnel
- [ ] Firebase configuré et connecté
- [ ] Wallet connection (Reown AppKit) opérationnelle

---

## 🧪 Tests Phase 1 : Connexion et État Initial

### Test 1.1 : Connexion Wallet
- [ ] Ouvrir le jeu WebGL dans le navigateur
- [ ] Cliquer "Connect Wallet"
- [ ] Vérifier que l'adresse wallet s'affiche correctement
- [ ] Vérifier que le statut passe à "connecté"

### Test 1.2 : État Initial Sans NFT
- [ ] Wallet connecté sans NFT existant
- [ ] Vérifier que le bouton affiche "MINT NFT"
- [ ] Vérifier que `canMintNFT` retourne `true`
- [ ] Vérifier que le panel NFT est vide

### Test 1.3 : Firebase Initial
- [ ] Vérifier que le score/points du wallet est visible
- [ ] Vérifier la connexion Firebase (pas d'erreurs console)

---

## 🎯 Tests Phase 2 : Mint NFT

### Test 2.1 : Premier Mint
- [ ] Cliquer "MINT NFT" avec wallet vide
- [ ] Vérifier demande d'autorisation au serveur
- [ ] Vérifier signature générée (logs serveur)
- [ ] Confirmer transaction dans wallet
- [ ] Vérifier que le NFT est minté (tokenId = 1)
- [ ] Vérifier que `canMintNFT` retourne maintenant `false`

### Test 2.2 : Vérification Post-Mint
- [ ] Vérifier niveau NFT = 1 (lecture blockchain)
- [ ] Vérifier que le NFT apparaît dans le panel
- [ ] Vérifier image NFT niveau 1 affichée
- [ ] Vérifier bouton "EVOLVE (Level 2)" disponible

### Test 2.3 : Tentative Double Mint
- [ ] Essayer de minter un 2ème NFT avec le même wallet
- [ ] Vérifier que c'est bloqué (message d'erreur approprié)

---

## 🚀 Tests Phase 3 : Évolution NFT

### Test 3.1 : Évolution Niveau 1→2
- [ ] Avoir suffisamment de points (≥100)
- [ ] Cliquer "EVOLVE" sur le NFT niveau 1
- [ ] Vérifier demande d'autorisation serveur
- [ ] Vérifier signature avec nonce unique
- [ ] Confirmer transaction évolution
- [ ] Vérifier niveau NFT = 2 (blockchain)
- [ ] Vérifier consommation 100 points (Firebase)

### Test 3.2 : Synchronisation Post-Évolution
- [ ] Vérifier image NFT niveau 2 affichée
- [ ] Vérifier bouton "EVOLVE (Level 3)" disponible
- [ ] Vérifier nouveau solde points affiché
- [ ] Actualiser page → vérifier persistance niveau

### Test 3.3 : Évolution Sans Points Suffisants
- [ ] Réduire points à <200 dans Firebase
- [ ] Essayer évoluer niveau 2→3
- [ ] Vérifier refus d'autorisation serveur
- [ ] Vérifier message d'erreur approprié

### Test 3.4 : Évolutions Multiples
- [ ] Évoluer progressivement jusqu'au niveau 5
- [ ] Vérifier chaque évolution consomme les bons points
- [ ] Vérifier images changent à chaque niveau
- [ ] Vérifier coûts d'évolution corrects (100,200,300,400,500)

---

## 🔄 Tests Phase 4 : Transferts et Multi-Wallet

### Test 4.1 : Transfert NFT
- [ ] Transférer NFT vers un autre wallet
- [ ] Vérifier que l'ancien wallet ne voit plus le NFT
- [ ] Vérifier que le nouveau wallet voit le NFT
- [ ] Vérifier que le niveau est préservé après transfert

### Test 4.2 : Mint Après Transfert
- [ ] Avec l'ancien wallet (qui a transféré son NFT)
- [ ] Vérifier que `canMintNFT` retourne `true`
- [ ] Minter un nouveau NFT
- [ ] Vérifier que c'est un nouveau tokenId

### Test 4.3 : Multi-NFT par Wallet
- [ ] Wallet ayant reçu un NFT par transfert
- [ ] Minter un nouveau NFT avec ce wallet
- [ ] Vérifier que le panel affiche les 2 NFTs
- [ ] Évoluer chaque NFT indépendamment

---

## 🛡️ Tests Phase 5 : Sécurité et Edge Cases

### Test 5.1 : Tentatives de Bypass
- [ ] Essayer appeler `mintNFT` directement sur Remix
- [ ] Vérifier que ça échoue (signature invalide)
- [ ] Essayer `evolveNFT` direct sur Remix
- [ ] Vérifier que ça échoue (signature invalide)

### Test 5.2 : Nonces Anti-Replay
- [ ] Capturer une signature d'évolution
- [ ] Essayer la réutiliser (même nonce)
- [ ] Vérifier que ça échoue

### Test 5.3 : Supply Limit
- [ ] Vérifier `totalSupply()` et `remainingSupply()`
- [ ] Si proche de 600, vérifier blocage mint
- [ ] Vérifier `isMaxSupplyReached()` = true à 600

### Test 5.4 : Évolution Max Level
- [ ] Évoluer un NFT jusqu'au niveau 10
- [ ] Vérifier bouton devient "MAX LEVEL"
- [ ] Vérifier qu'on ne peut plus évoluer

---

## 🔧 Tests Phase 6 : Robustesse

### Test 6.1 : Reconnexion Wallet
- [ ] Déconnecter wallet
- [ ] Reconnecter avec même adresse
- [ ] Vérifier que les NFTs réapparaissent
- [ ] Vérifier synchronisation Firebase

### Test 6.2 : Refresh Page
- [ ] Actualiser la page pendant utilisation
- [ ] Reconnecter wallet
- [ ] Vérifier que tout fonctionne encore

### Test 6.3 : Erreurs Réseau
- [ ] Couper serveur signatures temporairement
- [ ] Essayer mint/evolve
- [ ] Vérifier messages d'erreur appropriés
- [ ] Redémarrer serveur et vérifier récupération

---

## 📊 Tests Phase 7 : Monitoring et Logs

### Test 7.1 : Logs Serveur
- [ ] Vérifier logs serveur pour chaque action
- [ ] Vérifier génération nonces uniques
- [ ] Vérifier signatures correctes

### Test 7.2 : Logs Unity
- [ ] Vérifier console Unity (F12) pour erreurs
- [ ] Vérifier appels Firebase réussis
- [ ] Vérifier transactions blockchain confirmées

### Test 7.3 : Firebase Console
- [ ] Vérifier mise à jour points en temps réel
- [ ] Vérifier stockage données NFT
- [ ] Vérifier pas de données corrompues

---

## ✅ Validation Finale

### Scénario Complet
- [ ] Nouveau wallet → Connect → Mint → Évoluer 3 fois → Transférer → Nouveau mint
- [ ] Tout fonctionne sans erreur
- [ ] Synchronisation parfaite blockchain ↔ Firebase ↔ Unity
- [ ] Sécurité respectée (pas de bypass possible)

### Performance
- [ ] Temps de réponse serveur <2s
- [ ] Chargement images NFT fluide
- [ ] Pas de lag interface Unity

---

## 🚨 Points Critiques à Vérifier

1. **Sécurité** : Impossible de mint/evolve sans passer par le jeu
2. **Synchronisation** : Niveau NFT identique blockchain/Firebase/Unity
3. **Points** : Consommation effective lors évolution
4. **Multi-NFT** : Gestion correcte plusieurs NFTs par wallet
5. **Transferts** : Niveau préservé, mint possible après transfert
6. **Supply** : Limite 600 NFTs respectée
7. **UI** : Affichage correct images et boutons selon état NFT
