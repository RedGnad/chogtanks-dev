# Guide de Déploiement du Serveur de Signatures

## Déploiement sur Railway (Recommandé)

1. **Créer un compte sur Railway.app**
2. **Connecter votre repository GitHub**
3. **Configurer les variables d'environnement :**
   - `GAME_SERVER_PRIVATE_KEY` = votre clé privée
   - `PORT` = 3001

4. **Railway détectera automatiquement votre `package.json`**

## Déploiement sur Heroku

1. **Installer Heroku CLI**
2. **Créer une app Heroku :**
```bash
heroku create votre-app-name
```

3. **Configurer les variables d'environnement :**
```bash
heroku config:set GAME_SERVER_PRIVATE_KEY=votre_cle_privee
```

4. **Déployer :**
```bash
git push heroku main
```

## Configuration Unity

Une fois déployé, mettez à jour l'URL dans `FirebasePluginV2.jslib` :

```javascript
// Remplacer localhost par votre URL de production
const SERVER_URL = 'https://votre-app.railway.app';
// ou
const SERVER_URL = 'https://votre-app.herokuapp.com';
```

## Sécurité en Production

- Utiliser HTTPS uniquement
- Configurer CORS pour votre domaine Unity
- Monitorer les logs du serveur
- Sauvegarder la clé privée de façon sécurisée

## Test de l'API

Testez vos endpoints :
```bash
curl https://votre-app.railway.app/mint-authorization \
  -H "Content-Type: application/json" \
  -d '{"walletAddress":"0x..."}'
```
