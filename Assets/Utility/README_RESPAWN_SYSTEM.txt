# SYSTÈME DE RESPAWN POUR TANKS MULTIJOUEUR

## RÉSUMÉ DES MODIFICATIONS
Le système de mort et de respawn a été entièrement réécrit pour être plus robuste et éviter les problèmes liés à la destruction/réinstanciation des GameObjects réseau. Voici ses principales caractéristiques :

1. Les tanks ne sont plus détruits à leur mort, mais simplement rendus invisibles et inactifs
2. Une UI Game Over locale est affichée pour le joueur dont le tank est mort
3. Après un délai configurable, le tank réapparaît à un point de spawn aléatoire
4. Tous les événements sont synchronisés sur le réseau via PUN RPC

## COMPOSANTS DU SYSTÈME

### SimpleTankRespawn.cs
- Composant attaché automatiquement à tous les tanks
- Gère la mort (désactivation des composants) et le respawn (réactivation et repositionnement)
- Affiche l'UI Game Over locale
- Attribue les scores (si le client est MasterClient)

### TankComponentAdder.cs
- À placer sur un GameObject dans la scène (ex: "RespawnManager")
- Surveille les nouveaux tanks et leur ajoute automatiquement le SimpleTankRespawn
- Attribue le prefab UI Game Over au composant

### TankHealth2D.cs (modifié)
- Simplifié pour déléguer la mort au SimpleTankRespawn via RPC Die
- Ne stocke plus de référence à SimpleTankRespawn au démarrage

## INSTALLATION

1. Créez un nouveau GameObject vide dans votre scène et nommez-le "RespawnManager"

2. Ajoutez le composant TankComponentAdder au GameObject "RespawnManager"

3. Assignez votre prefab UI Game Over dans le champ "Game Over UI Prefab" du TankComponentAdder
   - Ce prefab doit avoir un script qui gère l'affichage du message Game Over
   - Le script doit avoir une méthode publique ShowGameOver() qui sera appelée

4. Assurez-vous que les points de spawn sont correctement configurés dans votre scène
   - Le système cherche des GameObjects avec le tag "SpawnPoint"
   - Si aucun n'est trouvé, il réutilisera la position d'origine du tank

5. Les délais de respawn sont configurables dans le script SimpleTankRespawn
   - respawnDelay : délai avant réapparition du tank (par défaut 3 secondes)
   - invincibilityTime : période d'invincibilité après respawn (par défaut 2 secondes)

## DÉPANNAGE

Si les tanks ne meurent pas correctement :
- Vérifiez que le RespawnManager est présent dans la scène
- Assurez-vous que le prefab UI Game Over est correctement assigné
- Consultez les logs pour voir si SimpleTankRespawn est bien ajouté aux tanks

Si les tanks ne respawnent pas :
- Vérifiez que vous avez des points de spawn avec le tag "SpawnPoint"
- Assurez-vous que le RPC Die est bien appelé (voir logs)

Si l'UI Game Over n'apparaît pas :
- Vérifiez que le prefab UI a bien un script avec une méthode ShowGameOver()
- Assurez-vous qu'il est correctement assigné dans le TankComponentAdder

## LOGS DE DÉBOGAGE

Le système affiche des logs détaillés pour faciliter le débogage :
- [TankComponentAdder] Ajout de SimpleTankRespawn au tank {nom}
- [TANK DEATH] Appel de SimpleTankRespawn.Die pour {nom}
- [SimpleTankRespawn] Tank {nom} est mort, tué par {tueur}
- [SimpleTankRespawn] Respawn du tank {nom} à {position}

## PERSONNALISATION

Pour personnaliser davantage le système :
- Ajoutez des effets visuels de mort dans la méthode Die() de SimpleTankRespawn
- Modifiez la logique de respawn dans la méthode Respawn()
- Changez les délais respawnDelay et invincibilityTime selon vos besoins
