INSTRUCTIONS D'INSTALLATION DU SYSTÈME DE RESPAWN SIMPLIFIÉ

1. Ouvrez votre scène de jeu principale (où les combats de tanks se déroulent)

2. Créez un GameObject vide (GameObject > Create Empty)
   - Renommez-le "RespawnManager"
   - Assurez-vous qu'il est positionné à l'origine (0,0,0)
   - Cochez la case "GameObject Static" si disponible

3. Ajoutez le composant TankComponentAdder à ce GameObject
   - Sélectionnez le GameObject "RespawnManager"
   - Add Component > Scripts > TankComponentAdder

4. Assignez le gameOverUIPrefab
   - Glissez-déposez le même prefab UI que vous utilisiez avant dans le TankHealth2D
   - Ce prefab doit avoir le tag "GameOverUI" et contenir un GameOverUIController

5. Si le système ne fonctionne pas immédiatement, assurez-vous que:
   - Le prefab du tank contient bien le composant TankHealth2D
   - Aucune erreur n'apparaît dans la console
   - Les PhotonView sur vos tanks ont bien la propriété Observe Type = "Observe Position"

EXPLICATION DU NOUVEAU SYSTÈME:
1. Lorsqu'un tank est tué, au lieu d'être détruit, il est simplement rendu invisible
2. Une UI de game over est affichée pour le joueur concerné
3. Après 5 secondes, le tank est réactivé à sa position de spawn
4. Cette approche évite les problèmes de synchronisation réseau de destruction/création

En cas de problème ou pour personnaliser:
- Temps de respawn: modifier la variable respawnTime dans SimpleTankRespawn
- Position de respawn: le système utilise les mêmes spawnPoints que votre PhotonTankSpawner
