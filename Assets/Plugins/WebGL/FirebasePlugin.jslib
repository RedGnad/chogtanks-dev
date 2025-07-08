mergeInto(LibraryManager.library, {
  // Initialisation de Firebase
  InitializeFirebaseJS: function () {
    try {
      console.log("Firebase déjà initialisé depuis index.html");
      return true;
    } catch (error) {
      console.error("Erreur d'initialisation Firebase:", error);
      return false;
    }
  },

  // Soumettre un score
  SubmitScoreJS: function (score, bonus, walletAddress) {
  // Vérification stricte d'adresse Ethereum (0x + 40 hex)
  function isValidEthAddress(addr) {
    return /^0x[a-fA-F0-9]{40}$/.test(addr);
  }
    try {
    const scoreValue = parseInt(UTF8ToString(score), 10);
    const bonusValue = parseInt(UTF8ToString(bonus), 10) || 0;
    const address = UTF8ToString(walletAddress);
    
    // Validation plus permissive
    if (!address) {
      console.error("[SCORE] Adresse invalide ou vide");
      return false;
    }

    // Normalisation systématique
    const normalizedAddress = address.toLowerCase().trim();
    if (!isValidEthAddress(normalizedAddress)) {
      console.error(`[SCORE][SECURITE] Adresse Ethereum invalide pour soumission score: '${normalizedAddress}'`);
      return false;
    }
    
    const totalScore = scoreValue + bonusValue;
    
    // Générer un ID unique pour cette session de jeu
    // Si on n'a pas encore d'ID de session pour ce score, on en crée un
    if (!window.lastScoreSessionId) {
      window.lastScoreSessionId = Date.now().toString();
      window.lastScoreValue = totalScore;
      console.log(`[SCORE] Nouvelle session de score #${window.lastScoreSessionId}, valeur: ${totalScore}`);
    } else if (window.lastScoreValue === totalScore) {
      // Même score détecté = probable doublon
      console.warn(`[SCORE] ⚠️ Doublon probable détecté! Score ${totalScore} déjà soumis récemment. Ignorant.`);
      return true; // Ignorer silencieusement le doublon
    } else {
      // Nouveau score différent = nouvelle session
      window.lastScoreSessionId = Date.now().toString();
      window.lastScoreValue = totalScore;
      console.log(`[SCORE] Nouvelle session de score #${window.lastScoreSessionId}, valeur: ${totalScore}`);
    }
    
    console.log(`[SCORE] Score soumis à Firebase pour ${normalizedAddress}: ${scoreValue} (+${bonusValue})`);

      firebase.auth().onAuthStateChanged((user) => {
        if (user) {
          const db = firebase.firestore();
          const docRef = db.collection("WalletScores").doc(normalizedAddress);
          
          // Vérifier d'abord si le document existe
          docRef.get().then((doc) => {
            if (!doc.exists) {
              // Création d'un nouveau document sans condition
              docRef.set({
                score: totalScore,
                nftLevel: 0, // Initialisation à 0
                walletAddress: normalizedAddress,
                lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
                createdAt: firebase.firestore.FieldValue.serverTimestamp(),
              })
              .then(() => {
                console.log(`[SCORE] ✅ Nouveau document créé pour ${normalizedAddress} avec score: ${totalScore}`);
              })
              .catch((error) => {
                console.error("[SCORE] ❌ Erreur création document:", error);
              });
            } else {
              // Mise à jour du document existant sans vérification de timestamp
              const currentScore = Number(doc.data().score || 0);
              
              // Addition normale des scores (la détection des doublons en amont évite les doubles envois)
              const newScore = currentScore + totalScore;
              console.log(`[SCORE] Addition des scores: ${currentScore} + ${totalScore} = ${newScore}`);
              
              docRef.update({
                score: newScore,
                walletAddress: normalizedAddress, 
                lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
              }, { merge: true }) // Utiliser merge pour être plus permissif
              .then(() => {
                console.log(`[SCORE] 🚀 Score soumis à Firebase pour ${normalizedAddress}: ${newScore} (${currentScore} + ${totalScore})`);
              })
              .catch((error) => {
                // En cas d'erreur, essayer une approche encore plus permissive
                console.warn("[SCORE] ⚠️ Première tentative échouée, essai alternatif:", error);
                
                // Tentative alternative avec set et merge
                docRef.set({
                  score: newScore,
                  walletAddress: normalizedAddress,
                  lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
                }, { merge: true })
                .then(() => {
                  console.log(`[SCORE] ✅ Score mis à jour (méthode alternative) pour ${normalizedAddress}: ${newScore}`);
                })
                .catch((error2) => {
                  console.error("[SCORE] ❌ Erreur critique mise à jour score:", error2);
                });
              });
            }
          }).catch((error) => {
            console.error("[SCORE] ❌ Erreur récupération document:", error);
          });

        } else {
          console.log("[SCORE] Auth anonyme en cours...");
          firebase.auth().signInAnonymously().catch((error) => {
            console.error("[SCORE] Erreur auth:", error);
          });
        }
      });

      return true;
    } catch (error) {
      console.error("[SCORE] Erreur SubmitScoreJS:", error);
      return false;
    }
  },

  // Vérifier si le wallet peut minter un NFT (nftLevel == 0)
  CanMintNFTJS: function (walletAddress, callbackMethod) {
    try {
      const address = UTF8ToString(walletAddress);
      const callback = UTF8ToString(callbackMethod);
      const normalizedAddress = address.toLowerCase().trim();
      
      console.log(`[NFT] CanMintNFTJS called with address: ${address}, callback: ${callback}`);
      
      // Vérifier si unityInstance est défini
      if (typeof unityInstance === "undefined") {
        console.error('[NFT][ERREUR CRITIQUE] unityInstance n\'est pas défini dans CanMintNFTJS');
        return false;
      }
      
      if (!callback || callback.trim() === '') {
        console.error('[NFT] Callback method name is empty!');
        return false;
      }
      if (!/^0x[a-fA-F0-9]{40}$/.test(normalizedAddress)) {
        console.error(`[NFT][SECURITE] Adresse Ethereum invalide pour CanMintNFTJS: '${normalizedAddress}'`);
        unityInstance.SendMessage("ChogTanksNFTManager", callback, JSON.stringify({ canMint: false, error: "Adresse Ethereum invalide" }));
        return false;
      }
      
      // Vérifier si firebase est défini
      if (typeof firebase === "undefined") {
        console.error('[NFT][ERREUR] Firebase n\'est pas initialisé dans CanMintNFTJS');
        unityInstance.SendMessage("ChogTanksNFTManager", callback, JSON.stringify({ canMint: false, error: "Firebase non initialisé" }));
        return false;
      }
      
      firebase.auth().onAuthStateChanged(function(user) {
        if (user) {
          const db = firebase.firestore();
          db.collection("WalletScores").doc(normalizedAddress).get().then(function(doc) {
            let canMint = true;
            if (doc.exists && Number(doc.data().nftLevel || 0) > 0) {
              canMint = false;
            }
            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              callback,
              JSON.stringify({ canMint: canMint })
            );
          }).catch(function(error) {
            console.error("[NFT] Erreur CanMintNFTJS:", error);
            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              callback,
              JSON.stringify({ canMint: false, error: "Erreur Firestore" })
            );
          });
        } else {
          firebase.auth().signInAnonymously().catch(console.error);
          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            callback,
            JSON.stringify({ canMint: false, error: "Non authentifié" })
          );
        }
      });
      return true;
    } catch (error) {
      console.error("[NFT] Erreur CanMintNFTJS:", error);
      unityInstance.SendMessage(
        "ChogTanksNFTManager",
        callback,
        JSON.stringify({ canMint: false, error: "Exception JS" })
      );
      return false;
    }
  },

  // Mettre à jour le niveau NFT
  UpdateNFTLevelJS: function (walletAddress, newLevel) {
    // Log explicite pour debug
    console.log("[NFT][DEBUG] UpdateNFTLevelJS called with:", walletAddress, newLevel);
    try {
      const address = UTF8ToString(walletAddress);
      let levelStr = UTF8ToString(newLevel);
      
      // Protection contre les valeurs vides ou nulles (cas de mint initial)
      if (!levelStr || levelStr.trim() === '') {
        console.warn(`[NFT] Niveau NFT vide reçu. Utilisation valeur par défaut '1' (mint initial)`); 
        levelStr = '1'; // Valeur par défaut pour mint initial
      }
      
      console.log(`[NFT] Traitement de la mise à jour niveau: adresse=${address}, niveau=${levelStr}`);
      
      let nftLevel;
      try {
        // Assurer que la valeur est un nombre entier valide
        nftLevel = parseInt(levelStr, 10);
        if (isNaN(nftLevel) || !isFinite(nftLevel)) {
          console.error(`[NFT] Erreur de parsing du niveau NFT: '${levelStr}', utilisation valeur par défaut 1`);
          nftLevel = 1; // Plus logique d'utiliser 1 comme minimum (pour mint initial)
        }
      } catch (e) {
        console.error(`[NFT] Exception lors du parsing du niveau NFT: ${e.message}`);
        nftLevel = 1; // Valeur par défaut pour mint initial
      }  
      
      const normalizedAddress = address.toLowerCase().trim();
      console.log(`[NFT] Mise à jour niveau NFT: ${nftLevel} pour ${normalizedAddress}`);

      firebase.auth().onAuthStateChanged((user) => {
        if (user) {
          const db = firebase.firestore();

          // Forcer le type number pour nftLevel
          const nftLevelNumber = Number(nftLevel);
          
          db.collection("WalletScores")
            .doc(normalizedAddress)
            .set(
              {
                nftLevel: nftLevelNumber, // Utiliser la variable convertie explicitement
                walletAddress: normalizedAddress,
                lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
              },
              { merge: true }
            )
            .then(() => {
              console.log(`[NFT] Niveau NFT mis à jour: ${nftLevelNumber}`);
              if (typeof unityInstance !== "undefined") {
                unityInstance.SendMessage(
                  "ChogTanksNFTManager",
                  "OnNFTLevelUpdated", 
                  String(nftLevelNumber)
                );
              }
            })
            .catch((error) => {
              console.error("[NFT] Erreur mise à jour niveau:", error);
            });
        } else {
          console.log("[NFT] Auth anonyme en cours...");
          firebase.auth().signInAnonymously().catch(console.error);
        }
      });

      return true;
    } catch (error) {
      console.error("[NFT] Erreur UpdateNFTLevelJS:", error);
      return false;
    }
  },

  // Vérifier l'éligibilité à l'évolution
  CheckEvolutionEligibilityJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim();
      console.log(`[EVOL] Vérification éligibilité pour ${normalizedAddress}`);

      firebase.auth().onAuthStateChanged((user) => {
        if (user) {
          const db = firebase.firestore();

          db.collection("WalletScores")
            .doc(normalizedAddress)
            .get()
            .then((doc) => {
              if (doc.exists) {
                const data = doc.data();
                // S'assurer d'avoir des nombres valides avec conversion explicite
                const currentScore = Number(data.score || 0);
                const currentLevel = Number(data.nftLevel || 0);

                // Logique d'éligibilité selon les seuils spécifiques
              let requiredScore;
              if (currentLevel === 1) { // Déjà au niveau 1, pour niveau 2
                requiredScore = 2; // Niveau 1->2 nécessite seulement 2 points
              } else if (currentLevel >= 2) { // Niveau 2 et plus
                requiredScore = 100 * (currentLevel - 1); // Niveau 2->3 = 100 points, 3->4 = 200 points, etc.
              } else {
                requiredScore = 0; // Cas impossible (currentLevel = 0 = pas de NFT)
              }
              const isEligible = currentScore >= requiredScore;

                console.log(`[EVOL] Score ${currentScore}, niveau ${currentLevel}, requis ${requiredScore}, éligible: ${isEligible}`);
                if (typeof unityInstance !== "undefined") {
                  unityInstance.SendMessage(
                    "ChogTanksNFTManager",
                    "OnEvolutionCheckComplete",
                    JSON.stringify({
                      authorized: Boolean(isEligible),
                      score: Number(currentScore) || 0,
                      requiredScore: Number(requiredScore) || 0,
                      currentLevel: Number(currentLevel) || 0
                    })
                  );
                }
              } else {
                // Document n'existe pas
                console.log(`[EVOL] Aucun document pour ${normalizedAddress}`);
                if (typeof unityInstance !== "undefined") {
                  unityInstance.SendMessage(
                    "ChogTanksNFTManager",
                    "OnEvolutionCheckComplete",
                    JSON.stringify({
                      authorized: false,
                      score: 0,
                      currentLevel: 0,
                      requiredScore: 100,
                      error: "Aucun document trouvé"
                    })
                  );
                }
              }
            })
            .catch((error) => {
              console.error("[EVOL] Erreur vérification:", error);
            });
        } else {
          console.log("[EVOL] Auth anonyme en cours...");
          firebase.auth().signInAnonymously().catch(console.error);
        }
      });

      return true;
    } catch (error) {
      console.error("[EVOL] Erreur CheckEvolutionEligibilityJS:", error);
      return false;
    }
  },

  // Obtenir l'état actuel du NFT
  GetNFTStateJS: function (walletAddress) {
  try {
    const address = UTF8ToString(walletAddress);
    const normalizedAddress = address.toLowerCase().trim();
    console.log(`[NFT][DEBUG] GetNFTStateJS - Récupération de l'état NFT pour: ${normalizedAddress}`);
    
    // Créer une réponse par défaut
    let response = {
      hasNFT: false,
      level: 0,
      score: 0,
      walletAddress: normalizedAddress
    };
    
    // Vérifier si Firebase est initialisé
    if (typeof firebase === "undefined" || !firebase.apps.length) {
      console.error("[NFT][ERREUR] Firebase n'est pas initialisé dans GetNFTStateJS");
      // Essai de recuperation de unityInstance en cas d'erreur
      if (typeof unityInstance !== "undefined") {
        console.log("[NFT][DEBUG] unityInstance est défini, envoi du message de fallback");
        unityInstance.SendMessage("ChogTanksNFTManager", "OnNFTStateLoaded", JSON.stringify(response));
      } else {
        console.error("[NFT][ERREUR CRITIQUE] unityInstance n'est pas défini dans GetNFTStateJS");
      }
      return;
    }

    console.log("[NFT][DEBUG] Avant firebase.auth().onAuthStateChanged");
    firebase.auth().onAuthStateChanged(function(user) {
      console.log("[NFT][DEBUG] Dans onAuthStateChanged, user:", user ? "connecté" : "non connecté");
      if (user) {
        console.log("[NFT][DEBUG] Utilisateur authentifié, accès à Firestore");
        const db = firebase.firestore();

        db.collection("WalletScores")
          .doc(normalizedAddress)
          .get()
          .then(function(doc) {
            console.log("[NFT][DEBUG] Document Firestore récupéré, existe:", doc.exists);
            if (doc.exists) {
              const data = doc.data();
              // Forcer la conversion numérique pour éviter les NaN
              const nftLevel = Number(data.nftLevel || 0);
              const score = Number(data.score || 0);
              
              response = {
                hasNFT: nftLevel > 0,
                level: nftLevel,
                score: score,
                walletAddress: normalizedAddress, // Toujours utiliser l'adresse normalisée
              };
            }

            console.log(`[NFT][DEBUG] État récupéré: ${JSON.stringify(response)}`);
            // Vérifier explicitement si unityInstance existe
            if (typeof unityInstance === "undefined") {
              console.error("[NFT][ERREUR CRITIQUE] unityInstance n'est pas défini lors de l'envoi du résultat");
              return;
            }
            
            try {
              // Assurez-vous que tous les champs sont bien formatés
              const safeResponse = {
                hasNFT: Boolean(response.hasNFT),
                level: Number(response.level) || 0,
                score: Number(response.score) || 0,
                walletAddress: String(response.walletAddress || '')
              };
              console.log("[NFT][DEBUG] Envoi du résultat à Unity via SendMessage");
              unityInstance.SendMessage(
                "ChogTanksNFTManager",
                "OnNFTStateLoaded", 
                JSON.stringify(safeResponse)
              );
              console.log("[NFT][DEBUG] SendMessage exécuté avec succès");
            } catch (e) {
              console.error("[NFT][ERREUR CRITIQUE] Erreur lors de l'appel à SendMessage:", e);
            }
          })
          .catch(function(error) {
            console.error("[NFT][ERREUR] Erreur récupération état:", error);
            // En cas d'erreur, essayer quand même d'envoyer une réponse par défaut
            if (typeof unityInstance !== "undefined") {
              unityInstance.SendMessage(
                "ChogTanksNFTManager",
                "OnNFTStateLoaded", 
                JSON.stringify(response)
              );
            }
          });
      } else {
        console.log("[NFT][DEBUG] Auth anonyme en cours...");
        firebase.auth().signInAnonymously()
          .then(function() {
            console.log("[NFT][DEBUG] Authentification anonyme réussie");
            // On ne fait rien ici, onAuthStateChanged sera rappelé avec user non null
          })
          .catch(function(error) {
            console.error("[NFT][ERREUR] Échec authentification anonyme:", error);
            // En cas d'erreur, essayer quand même d'envoyer une réponse par défaut
            if (typeof unityInstance !== "undefined") {
              unityInstance.SendMessage(
                "ChogTanksNFTManager",
                "OnNFTStateLoaded", 
                JSON.stringify(response)
              );
            }
          });
      }
    });

    console.log("[NFT][DEBUG] GetNFTStateJS terminé avec succès");
    return true;
  } catch (error) {
    console.error("[NFT][ERREUR CRITIQUE] Exception dans GetNFTStateJS:", error);
    try {
      // Essayer d'envoyer une réponse par défaut même en cas d'erreur
      if (typeof unityInstance !== "undefined") {
        const fallbackResponse = {
          hasNFT: false,
          level: 0,
          score: 0,
          walletAddress: UTF8ToString(walletAddress).toLowerCase().trim()
        };
        unityInstance.SendMessage(
          "ChogTanksNFTManager",
          "OnNFTStateLoaded", 
          JSON.stringify(fallbackResponse)
        );
      }
    } catch (e) {
      console.error("[NFT][ERREUR FATALE] Impossible d'envoyer la réponse de fallback:", e);
    }
    return false;
  }
  },
});
