mergeInto(LibraryManager.library, {
  InitializeWasmErrorHandler: function () {
    window.addEventListener("error", function (e) {
      if (
        e &&
        e.message &&
        (e.message.indexOf("wasm") !== -1 ||
          e.message.indexOf("memory") !== -1 ||
          e.message.indexOf("out of memory") !== -1)
      ) {
        console.error("[WebGL/WASM] Erreur détectée: " + e.message);

        if (!window.wasmErrorShown) {
          window.wasmErrorShown = true;

          var container = document.createElement("div");
          container.style.position = "absolute";
          container.style.width = "80%";
          container.style.top = "20%";
          container.style.left = "10%";
          container.style.backgroundColor = "rgba(0,0,0,0.8)";
          container.style.color = "white";
          container.style.padding = "20px";
          container.style.borderRadius = "10px";
          container.style.zIndex = "999";
          container.style.textAlign = "center";
          container.style.fontFamily = "Arial, sans-serif";

          container.innerHTML =
            "<h3>Problème de compatibilité détecté</h3>" +
            "<p>Votre appareil ne dispose peut-être pas de suffisamment de mémoire pour exécuter ce jeu.</p>" +
            "<p>Essayez de fermer d'autres applications et de rafraîchir la page.</p>" +
            '<button id="retryButton" style="padding: 10px; background-color: #4CAF50; border: none; color: white; border-radius: 5px; margin-top: 10px;">Réessayer</button>';

          document.body.appendChild(container);

          document
            .getElementById("retryButton")
            .addEventListener("click", function () {
              location.reload();
            });
        }

        return true;
      }
    });

    if (
      /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
        navigator.userAgent
      )
    ) {
      if (unityInstance && unityInstance.Module) {
        console.log("[WebGL] Optimisation pour mobile activée");
      }
    }
  },

  InitializeFirebaseJS: function () {
    try {
      console.log("Firebase déjà initialisé depuis index.html");
      return true;
    } catch (error) {
      console.error("Erreur d'initialisation Firebase:", error);
      return false;
    }
  },

  SubmitScoreJS: function (score, bonus, walletAddress) {
    function isValidEthAddress(addr) {
      return /^0x[a-fA-F0-9]{40}$/.test(addr);
    }
    try {
      const scoreValue = parseInt(UTF8ToString(score), 10);
      const bonusValue = parseInt(UTF8ToString(bonus), 10) || 0;
      const address = UTF8ToString(walletAddress);

      if (!address) {
        console.error("[SCORE] Adresse invalide ou vide");
        return false;
      }

      const normalizedAddress = address.toLowerCase().trim();
      if (!isValidEthAddress(normalizedAddress)) {
        console.error(
          `[SCORE][SECURITE] Adresse Ethereum invalide pour soumission score: '${normalizedAddress}'`
        );
        return false;
      }

      const totalScore = scoreValue + bonusValue;

      if (!window.lastScoreSessionId) {
        window.lastScoreSessionId = Date.now().toString();
        window.lastScoreValue = totalScore;
        console.log(
          `[SCORE] Nouvelle session de score #${window.lastScoreSessionId}, valeur: ${totalScore}`
        );
      } else if (window.lastScoreValue === totalScore) {
        console.warn(
          `[SCORE] ⚠️ Doublon probable détecté! Score ${totalScore} déjà soumis récemment. Ignorant.`
        );
        return true;
      } else {
        window.lastScoreSessionId = Date.now().toString();
        window.lastScoreValue = totalScore;
        console.log(
          `[SCORE] Nouvelle session de score #${window.lastScoreSessionId}, valeur: ${totalScore}`
        );
      }

      console.log(
        `[SCORE] Score soumis à Firebase pour ${normalizedAddress}: ${scoreValue} (+${bonusValue})`
      );

      firebase.auth().onAuthStateChanged((user) => {
        if (user) {
          const db = firebase.firestore();
          const docRef = db.collection("WalletScores").doc(normalizedAddress);

          docRef
            .get()
            .then((doc) => {
              if (!doc.exists) {
                docRef
                  .set({
                    score: totalScore,
                    nftLevel: 0,
                    walletAddress: normalizedAddress,
                    lastUpdated:
                      firebase.firestore.FieldValue.serverTimestamp(),
                    createdAt: firebase.firestore.FieldValue.serverTimestamp(),
                  })
                  .then(() => {
                    console.log(
                      `[SCORE] ✅ Nouveau document créé pour ${normalizedAddress} avec score: ${totalScore}`
                    );
                  })
                  .catch((error) => {
                    console.error(
                      "[SCORE] ❌ Erreur création document:",
                      error
                    );
                  });
              } else {
                const currentScore = Number(doc.data().score || 0);

                const newScore = currentScore + totalScore;
                console.log(
                  `[SCORE] Addition des scores: ${currentScore} + ${totalScore} = ${newScore}`
                );

                docRef
                  .update(
                    {
                      score: newScore,
                      walletAddress: normalizedAddress,
                      lastUpdated:
                        firebase.firestore.FieldValue.serverTimestamp(),
                    },
                    { merge: true }
                  )
                  .then(() => {
                    console.log(
                      `[SCORE] 🚀 Score soumis à Firebase pour ${normalizedAddress}: ${newScore} (${currentScore} + ${totalScore})`
                    );
                  })
                  .catch((error) => {
                    console.warn(
                      "[SCORE] ⚠️ Première tentative échouée, essai alternatif:",
                      error
                    );

                    docRef
                      .set(
                        {
                          score: newScore,
                          walletAddress: normalizedAddress,
                          lastUpdated:
                            firebase.firestore.FieldValue.serverTimestamp(),
                        },
                        { merge: true }
                      )
                      .then(() => {
                        console.log(
                          `[SCORE] ✅ Score mis à jour (méthode alternative) pour ${normalizedAddress}: ${newScore}`
                        );
                      })
                      .catch((error2) => {
                        console.error(
                          "[SCORE] ❌ Erreur critique mise à jour score:",
                          error2
                        );
                      });
                  });
              }
            })
            .catch((error) => {
              console.error("[SCORE] ❌ Erreur récupération document:", error);
            });
        } else {
          console.log("[SCORE] Auth anonyme en cours...");
          firebase
            .auth()
            .signInAnonymously()
            .catch((error) => {
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

  CanMintNFTJS: function (walletAddress, callbackMethod) {
    try {
      const address = UTF8ToString(walletAddress);
      const callback = UTF8ToString(callbackMethod);
      const normalizedAddress = address.toLowerCase().trim();

      console.log(
        `[NFT] CanMintNFTJS called with address: ${address}, callback: ${callback}`
      );

      if (typeof unityInstance === "undefined") {
        console.error(
          "[NFT][ERREUR CRITIQUE] unityInstance n'est pas défini dans CanMintNFTJS"
        );
        return false;
      }

      if (!callback || callback.trim() === "") {
        console.error("[NFT] Callback method name is empty!");
        return false;
      }
      if (!/^0x[a-fA-F0-9]{40}$/.test(normalizedAddress)) {
        console.error(
          `[NFT][SECURITE] Adresse Ethereum invalide pour CanMintNFTJS: '${normalizedAddress}'`
        );
        unityInstance.SendMessage(
          "ChogTanksNFTManager",
          callback,
          JSON.stringify({ canMint: false, error: "Adresse Ethereum invalide" })
        );
        return false;
      }

      if (typeof firebase === "undefined") {
        console.error(
          "[NFT][ERREUR] Firebase n'est pas initialisé dans CanMintNFTJS"
        );
        unityInstance.SendMessage(
          "ChogTanksNFTManager",
          callback,
          JSON.stringify({ canMint: false, error: "Firebase non initialisé" })
        );
        return false;
      }

      firebase.auth().onAuthStateChanged(function (user) {
        if (user) {
          const db = firebase.firestore();
          db.collection("WalletScores")
            .doc(normalizedAddress)
            .get()
            .then(function (doc) {
              let canMint = true;
              if (doc.exists && Number(doc.data().nftLevel || 0) > 0) {
                canMint = false;
              }
              unityInstance.SendMessage(
                "ChogTanksNFTManager",
                callback,
                JSON.stringify({ canMint: canMint })
              );
            })
            .catch(function (error) {
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

  UpdateNFTLevelJS: function (walletAddress, newLevel) {
    console.log(
      "[NFT][DEBUG] UpdateNFTLevelJS called with:",
      walletAddress,
      newLevel
    );
    try {
      const address = UTF8ToString(walletAddress);
      let nftLevel = newLevel;

      console.log(
        `[NFT] Traitement de la mise à jour niveau: adresse=${address}, niveau=${nftLevel}`
      );

      if (nftLevel === 0 || isNaN(nftLevel) || !isFinite(nftLevel)) {
        console.warn(
          `[NFT] Niveau NFT invalide reçu: ${nftLevel}. Utilisation valeur par défaut '1' (mint initial)`
        );
        nftLevel = 1;
      }

      const normalizedAddress = address.toLowerCase().trim();
      console.log(
        `[NFT] Mise à jour niveau NFT: ${nftLevel} pour ${normalizedAddress}`
      );

      firebase.auth().onAuthStateChanged((user) => {
        if (user) {
          const db = firebase.firestore();

          const nftLevelNumber = Number(nftLevel);

          db.collection("WalletScores")
            .doc(normalizedAddress)
            .set(
              {
                nftLevel: nftLevelNumber,
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

  UpdateNFTDataJS: function (walletAddress, tokenId, newLevel) {
    try {
      const address = UTF8ToString(walletAddress);
      const tokenIdValue = tokenId;
      const levelValue = newLevel;

      if (!address) {
        console.error("[NFT] Adresse invalide ou vide");
        return false;
      }

      const normalizedAddress = address.toLowerCase().trim();

      console.log(
        `[NFT] Mise à jour complète NFT dans Firebase: Wallet=${normalizedAddress}, TokenID=${tokenIdValue}, Level=${levelValue}`
      );

      firebase.auth().onAuthStateChanged(function (user) {
        if (user) {
          const db = firebase.firestore();
          const docRef = db.collection("WalletScores").doc(normalizedAddress);

          docRef
            .update({
              tokenId: tokenIdValue,
              nftLevel: levelValue,
              lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
            })
            .then(function () {
              console.log(
                `[NFT] ✅ Mise à jour complète NFT réussie: TokenID=${tokenIdValue}, Level=${levelValue}`
              );

              setTimeout(function () {
                docRef.get().then(function (doc) {
                  if (doc.exists) {
                    const data = doc.data();
                    console.log(
                      `[NFT] Vérification après mise à jour: tokenId=${data.tokenId}, nftLevel=${data.nftLevel}`
                    );

                    if (
                      data.tokenId !== tokenIdValue ||
                      data.nftLevel !== levelValue
                    ) {
                      console.warn(
                        `[NFT] ⚠️ Incohérence détectée, nouvel essai de mise à jour...`
                      );

                      docRef.set(
                        {
                          tokenId: tokenIdValue,
                          nftLevel: levelValue,
                          lastUpdated:
                            firebase.firestore.FieldValue.serverTimestamp(),
                        },
                        { merge: true }
                      );
                    }
                  }
                });
              }, 1000);
            })
            .catch(function (error) {
              console.error(
                "[NFT] ❌ Erreur lors de la mise à jour NFT:",
                error
              );

              docRef
                .set(
                  {
                    tokenId: tokenIdValue,
                    nftLevel: levelValue,
                    lastUpdated:
                      firebase.firestore.FieldValue.serverTimestamp(),
                  },
                  { merge: true }
                )
                .then(function () {
                  console.log(`[NFT] ✅ Mise à jour NFT par fallback réussie`);
                })
                .catch(function (error) {
                  console.error(
                    "[NFT] ❌ Échec complet de mise à jour NFT:",
                    error
                  );
                });
            });
        } else {
          console.log("[NFT] Auth anonyme en cours...");
          firebase
            .auth()
            .signInAnonymously()
            .catch(function (error) {
              console.error("[NFT] ❌ Échec authentification anonyme:", error);
            });
        }
      });

      return true;
    } catch (error) {
      console.error("[NFT] ❌ Exception dans UpdateNFTDataJS:", error);
      return false;
    }
  },

  SetUnityNFTStateJS: function (nftStateJson) {
    try {
      const nftStateStr = UTF8ToString(nftStateJson);
      const nftState = JSON.parse(nftStateStr);
      
      // Stocker l'état NFT Unity dans window pour que CheckEvolutionEligibilityJS puisse l'utiliser
      window.unityNFTState = nftState;
      console.log(`[NFT-STATE] Unity NFT state reçu:`, nftState);
      
      return true;
    } catch (error) {
      console.error("[NFT-STATE] Erreur lors du stockage de l'état NFT Unity:", error);
      return false;
    }
  },

  CheckEvolutionEligibilityJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim();
      console.log(
        `[EVOL-ONCHAIN] Vérification on-chain pour ${normalizedAddress}`
      );

      const checkOnChainFirst = async () => {
        try {
          console.log(`[EVOL] Utilisation des données Unity vérifiées au lieu de window.ethereum`);
          
          // Récupérer les données NFT depuis Unity qui a déjà vérifié la blockchain avec AppKit
          const unityNFTState = window.unityNFTState || {
            hasNFT: false,
            level: 0,
            tokenId: 0
          };
          
          let onChainLevel = 0;
          let foundTokenId = null;
          let foundContract = "0x68c582651d709f6e2b6113c01d69443f8d27e30d";
          
          if (unityNFTState.hasNFT && unityNFTState.level > 0) {
            onChainLevel = unityNFTState.level;
            foundTokenId = unityNFTState.tokenId;
            console.log(`[EVOL] Données Unity: Level=${onChainLevel}, TokenId=${foundTokenId}`);
          } else {
            console.log(`[EVOL] Aucun NFT détecté par Unity`);
          }

          console.log(`[EVOL-ONCHAIN] Niveau final on-chain: ${onChainLevel}`);

          firebase.auth().onAuthStateChanged(async (user) => {
            if (user) {
              const db = firebase.firestore();
              const docRef = db
                .collection("WalletScores")
                .doc(normalizedAddress);

              try {
                const doc = await docRef.get();
                let currentScore = 0;
                let firebaseLevel = 0;

                if (doc.exists) {
                  const data = doc.data();
                  currentScore = Number(data.score || 0);
                  firebaseLevel = Number(data.nftLevel || 0);
                  console.log(
                    `[EVOL-SYNC] Firebase: score=${currentScore}, level=${firebaseLevel}`
                  );
                }

                if (onChainLevel !== firebaseLevel) {
                  console.log(
                    `[EVOL-SYNC] Désynchronisation détectée! On-chain=${onChainLevel}, Firebase=${firebaseLevel}`
                  );

                  await docRef.set(
                    {
                      score: currentScore,
                      nftLevel: onChainLevel,
                      tokenId: foundTokenId,
                      contractAddress: foundContract,
                      lastSyncTimestamp: Date.now(),
                    },
                    { merge: true }
                  );

                  console.log(
                    `[EVOL-SYNC] Firebase mis à jour avec niveau on-chain: ${onChainLevel}`
                  );
                }

                let requiredScore;
                if (onChainLevel === 1) {
                  requiredScore = 2;
                } else if (onChainLevel >= 2) {
                  requiredScore = 100 * onChainLevel;
                } else {
                  requiredScore = 0;
                }

                const isEligible =
                  currentScore >= requiredScore && onChainLevel > 0;

                console.log(
                  `[EVOL-FINAL] Score=${currentScore}, Niveau on-chain=${onChainLevel}, Requis=${requiredScore}, Éligible=${isEligible}`
                );

                if (typeof unityInstance !== "undefined") {
                  if (isEligible) {
                    // Appeler le serveur de signature réel comme pour le mint
                    console.log(`[EVOL] Calling signature server for evolution authorization...`);
                    
                    fetch("http://localhost:3001/api/evolve-authorization", {
                      method: "POST",
                      headers: {
                        "Content-Type": "application/json",
                      },
                      body: JSON.stringify({
                        walletAddress: normalizedAddress,
                        tokenId: foundTokenId,
                        playerPoints: Number(currentScore),
                        targetLevel: onChainLevel + 1
                      }),
                    })
                    .then((response) => response.json())
                    .then((data) => {
                      console.log(`[EVOL] Server response:`, data);
                      
                      if (data.authorized) {
                        const authData = {
                          authorized: true,
                          walletAddress: normalizedAddress,
                          tokenId: foundTokenId || 0,
                          currentPoints: Number(currentScore) || 0,
                          evolutionCost: Number(requiredScore) || 0,
                          targetLevel: onChainLevel + 1,
                          nonce: data.nonce,        // ✅ Vrai nonce du serveur
                          signature: data.signature // ✅ VRAIE signature du serveur
                        };
                        
                        console.log(`[EVOL] Calling OnEvolutionAuthorized with real signature:`, authData);
                        unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify(authData));
                      } else {
                        console.error(`[EVOL] Server denied authorization:`, data.error);
                        unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify({
                          authorized: false,
                          error: data.error || "Server denied evolution authorization"
                        }));
                      }
                    })
                    .catch((error) => {
                      console.error(`[EVOL] Server error:`, error);
                      
                      // Fallback to mock for development if server is down
                      console.log(`[EVOL] Falling back to mock signature for development`);
                      const mockAuth = {
                        authorized: true,
                        walletAddress: normalizedAddress,
                        tokenId: foundTokenId || 0,
                        currentPoints: Number(currentScore) || 0,
                        evolutionCost: Number(requiredScore) || 0,
                        targetLevel: onChainLevel + 1,
                        nonce: Date.now(),
                        signature: "0x1234567890abcdef" // Mock signature
                      };
                      
                      unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify(mockAuth));
                    });
                  } else {
                    // Pas éligible
                    unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify({
                      authorized: false,
                      error: `Insufficient points: ${currentScore}/${requiredScore}`
                    }));
                  }
                }
              } catch (firebaseError) {
                console.error("[EVOL-SYNC] Erreur Firebase:", firebaseError);
              }
            } else {
              console.log("[EVOL] Auth anonyme en cours...");
              firebase.auth().signInAnonymously().catch(console.error);
            }
          });
        } catch (onChainError) {
          console.error(
            "[EVOL-ONCHAIN] Erreur lecture blockchain:",
            onChainError
          );
        }
      };

      checkOnChainFirst();
      return true;
    } catch (error) {
      console.error("[EVOL] Erreur CheckEvolutionEligibilityJS:", error);
      return false;
    }
  },

  GetNFTStateJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim(); // 🔧 SIMPLE FIX: Normalize address
      console.log(
        `[NFT][DEBUG] GetNFTStateJS - Récupération de l'état NFT pour: ${normalizedAddress}`
      );

      let response = {
        hasNFT: false,
        level: 0,
        score: 0,
        walletAddress: normalizedAddress,
      };

      if (typeof firebase === "undefined" || !firebase.apps.length) {
        console.error(
          "[NFT][ERREUR] Firebase n'est pas initialisé dans GetNFTStateJS"
        );
        if (typeof unityInstance !== "undefined") {
          console.log(
            "[NFT][DEBUG] unityInstance est défini, envoi du message de fallback"
          );
          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            "OnNFTStateLoaded",
            JSON.stringify(response)
          );
        } else {
          console.error(
            "[NFT][ERREUR CRITIQUE] unityInstance n'est pas défini dans GetNFTStateJS"
          );
        }
        return;
      }

      console.log("[NFT][DEBUG] Avant firebase.auth().onAuthStateChanged");
      firebase.auth().onAuthStateChanged(function (user) {
        console.log(
          "[NFT][DEBUG] Dans onAuthStateChanged, user:",
          user ? "connecté" : "non connecté"
        );
        if (user) {
          console.log(
            "[NFT][DEBUG] Utilisateur authentifié, accès à Firestore"
          );
          const db = firebase.firestore();

          db.collection("WalletScores")
            .doc(normalizedAddress)
            .get()
            .then(function (doc) {
              console.log(
                "[NFT][DEBUG] Document Firestore récupéré, existe:",
                doc.exists
              );
              if (doc.exists) {
                const data = doc.data();
                console.log("[NFT][DEBUG] Document data:", data);
                const nftLevel = Number(data.nftLevel || 0);
                const score = Number(data.score || 0);
                console.log(
                  `[NFT][DEBUG] Parsed - nftLevel: ${nftLevel}, score: ${score}`
                );

                response = {
                  hasNFT: nftLevel > 0,
                  level: nftLevel,
                  score: score,
                  walletAddress: normalizedAddress,
                };
              } else {
                console.log(
                  `[NFT][DEBUG] Document does not exist for wallet: ${normalizedAddress}`
                );
                console.log(
                  "[NFT][DEBUG] Creating default response with score=0"
                );
              }

              console.log(
                `[NFT][DEBUG] État récupéré: ${JSON.stringify(response)}`
              );
              if (typeof unityInstance === "undefined") {
                console.error(
                  "[NFT][ERREUR CRITIQUE] unityInstance n'est pas défini lors de l'envoi du résultat"
                );
                return;
              }

              try {
                const safeResponse = {
                  hasNFT: Boolean(response.hasNFT),
                  level: Number(response.level) || 0,
                  score: Number(response.score) || 0,
                  walletAddress: String(response.walletAddress || ""),
                };
                console.log(
                  "[NFT][DEBUG] Envoi du résultat à Unity via SendMessage"
                );
                unityInstance.SendMessage(
                  "ChogTanksNFTManager",
                  "OnNFTStateLoaded",
                  JSON.stringify(safeResponse)
                );
                console.log("[NFT][DEBUG] SendMessage exécuté avec succès");
              } catch (e) {
                console.error(
                  "[NFT][ERREUR CRITIQUE] Erreur lors de l'appel à SendMessage:",
                  e
                );
              }
            })
            .catch(function (error) {
              console.error("[NFT][ERREUR] Erreur récupération état:", error);
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
          firebase
            .auth()
            .signInAnonymously()
            .then(function () {
              console.log("[NFT][DEBUG] Authentification anonyme réussie");
            })
            .catch(function (error) {
              console.error(
                "[NFT][ERREUR] Échec authentification anonyme:",
                error
              );
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
      console.error(
        "[NFT][ERREUR CRITIQUE] Exception dans GetNFTStateJS:",
        error
      );
      try {
        if (typeof unityInstance !== "undefined") {
          const fallbackResponse = {
            hasNFT: false,
            level: 0,
            score: 0,
            walletAddress: UTF8ToString(walletAddress).toLowerCase().trim(),
          };
          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            "OnNFTStateLoaded",
            JSON.stringify(fallbackResponse)
          );
        }
      } catch (e) {
        console.error(
          "[NFT][ERREUR FATALE] Impossible d'envoyer la réponse de fallback:",
          e
        );
      }
      return false;
    }
  },

  // 🎆 NOUVELLE FONCTION: Marquer le mint comme réussi dans Firebase
  MarkMintSuccessJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim();

      console.log(
        `[MINT-SUCCESS] 🎆 Marking mint as successful for wallet: ${normalizedAddress}`
      );

      if (typeof firebase === "undefined" || !firebase.apps.length) {
        console.error("[MINT-SUCCESS] Firebase not initialized");
        return false;
      }

      const db = firebase.firestore();

      db.collection("WalletScores")
        .doc(normalizedAddress)
        .set(
          {
            hasMintedNFT: true,
            walletAddress: normalizedAddress,
            lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
          },
          { merge: true }
        )
        .then(() => {
          console.log(
            `[MINT-SUCCESS] ✅ hasMintedNFT set to true for ${normalizedAddress}`
          );
          if (typeof unityInstance !== "undefined") {
            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              "OnMintMarkedInFirebase",
              "success"
            );
          }
        })
        .catch((error) => {
          console.error(`[MINT-SUCCESS] ❌ Error marking mint success:`, error);
        });

      return true;
    } catch (error) {
      console.error(`[MINT-SUCCESS] ❌ Exception in MarkMintSuccessJS:`, error);
      return false;
    }
  },

  // 🔍 NOUVELLE FONCTION: Lire le champ hasMintedNFT depuis Firebase pour l'auto-mint
  CheckHasMintedNFTJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim();

      console.log(
        `[AUTO-MINT-CHECK] 🔍 Checking hasMintedNFT for wallet: ${normalizedAddress}`
      );

      if (typeof firebase === "undefined" || !firebase.apps.length) {
        console.error("[AUTO-MINT-CHECK] Firebase not initialized");
        return false;
      }

      const db = firebase.firestore();

      db.collection("WalletScores")
        .doc(normalizedAddress)
        .get()
        .then((doc) => {
          let hasMinted = false;

          if (doc.exists) {
            const data = doc.data();
            hasMinted = data.hasMintedNFT === true;
            console.log(
              `[AUTO-MINT-CHECK] 📊 Document found: hasMintedNFT=${hasMinted}`
            );
          } else {
            console.log(
              `[AUTO-MINT-CHECK] 📊 No document found, hasMintedNFT=false (first time)`
            );
          }

          // Retourner le résultat à Unity
          const result = {
            walletAddress: normalizedAddress,
            hasMintedNFT: hasMinted,
            shouldAutoMint: !hasMinted, // Auto-mint si jamais minté
          };

          console.log(`[AUTO-MINT-CHECK] ✅ Sending result to Unity:`, result);

          if (typeof unityInstance !== "undefined") {
            unityInstance.SendMessage(
              "NFTDisplayPanel",
              "OnHasMintedNFTChecked",
              JSON.stringify(result)
            );
          }
        })
        .catch((error) => {
          console.error(
            `[AUTO-MINT-CHECK] ❌ Error checking hasMintedNFT:`,
            error
          );

          // En cas d'erreur, considérer comme "pas encore minté" pour sécurité
          const fallbackResult = {
            walletAddress: normalizedAddress,
            hasMintedNFT: false,
            shouldAutoMint: true,
            error: error.message,
          };

          if (typeof unityInstance !== "undefined") {
            unityInstance.SendMessage(
              "NFTDisplayPanel",
              "OnHasMintedNFTChecked",
              JSON.stringify(fallbackResult)
            );
          }
        });

      return true;
    } catch (error) {
      console.error(
        `[AUTO-MINT-CHECK] ❌ Exception in CheckHasMintedNFTJS:`,
        error
      );
      return false;
    }
  },

  ReadNFTFromBlockchainJS: function (walletAddress, callbackMethod) {
    try {
      const address = UTF8ToString(walletAddress);
      const callback = UTF8ToString(callbackMethod);
      const normalizedAddress = address.toLowerCase().trim();

      console.log(
        `[BLOCKCHAIN] Starting NFT verification for wallet: ${normalizedAddress}`
      );

      if (!/^0x[a-fA-F0-9]{40}$/.test(normalizedAddress)) {
        console.error(
          `[BLOCKCHAIN] Invalid wallet address: ${normalizedAddress}`
        );
        unityInstance.SendMessage(
          "ChogTanksNFTManager",
          callback,
          JSON.stringify({
            hasNFT: false,
            level: 0,
            tokenId: 0,
            walletAddress: normalizedAddress,
            score: 0,
          })
        );
        return false;
      }

      let provider = null;

      if (typeof window.appKit !== "undefined" && window.appKit.getProvider) {
        provider = window.appKit.getProvider();
        console.log("[BLOCKCHAIN] Using AppKit provider");
      } else if (typeof window.ethereum !== "undefined") {
        provider = window.ethereum;
        console.log("[BLOCKCHAIN] Using window.ethereum provider");
      } else {
        console.error("[BLOCKCHAIN] No Web3 provider found");
        unityInstance.SendMessage(
          "ChogTanksNFTManager",
          callback,
          JSON.stringify({
            hasNFT: false,
            level: 0,
            tokenId: 0,
            walletAddress: normalizedAddress,
            score: 0,
          })
        );
        return false;
      }

      const contractAddress = "0x68c582651d709f6e2b6113c01d69443f8d27e30d";

      function padHex(value, length = 64) {
        return value.toString(16).padStart(length, "0");
      }

      async function ethCall(to, data) {
        try {
          console.log(
            `[BLOCKCHAIN] Making eth_call to ${to} with data ${data}`
          );
          const result = await provider.request({
            method: "eth_call",
            params: [{ to: to, data: data }, "latest"],
          });
          console.log(`[BLOCKCHAIN] eth_call result: ${result}`);
          return result;
        } catch (error) {
          console.error("[BLOCKCHAIN] eth_call error:", error);
          return null;
        }
      }

      async function checkNFTOwnership() {
        try {
          console.log("[BLOCKCHAIN] Checking NFT ownership...");

          const chainId = await provider.request({ method: "eth_chainId" });
          console.log(`[BLOCKCHAIN] Current chain ID: ${chainId}`);
          console.log(
            `[BLOCKCHAIN] Proceeding with verification regardless of chain ID...`
          );

          const totalSupplyData = "0x18160ddd";
          const totalSupplyResult = await ethCall(
            contractAddress,
            totalSupplyData
          );

          if (!totalSupplyResult || totalSupplyResult === "0x") {
            console.error(
              "[BLOCKCHAIN] Failed to get total supply, result:",
              totalSupplyResult
            );
            console.log(
              "[BLOCKCHAIN] Assuming no NFTs exist, returning empty result"
            );
            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              callback,
              JSON.stringify({
                hasNFT: false,
                level: 0,
                tokenId: 0,
                walletAddress: normalizedAddress,
                score: 0,
              })
            );
            return;
          }

          const totalSupply = parseInt(totalSupplyResult, 16);
          console.log(`[BLOCKCHAIN] Total supply: ${totalSupply}`);

          if (isNaN(totalSupply) || totalSupply === 0) {
            console.log("[BLOCKCHAIN] No NFTs minted yet");
            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              callback,
              JSON.stringify({
                hasNFT: false,
                level: 0,
                tokenId: 0,
                walletAddress: normalizedAddress,
                score: 0,
              })
            );
            return;
          }

          for (
            let tokenId = 1;
            tokenId <= Math.min(totalSupply, 600);
            tokenId++
          ) {
            try {
              const ownerOfData = "0x6352211e" + padHex(tokenId);
              const ownerResult = await ethCall(contractAddress, ownerOfData);

              if (ownerResult && ownerResult !== "0x") {
                const owner = "0x" + ownerResult.slice(-40);

                if (owner.toLowerCase() === normalizedAddress) {
                  console.log(
                    `[BLOCKCHAIN] ✅ Found NFT! TokenID: ${tokenId}, Owner: ${owner}`
                  );

                  const getLevelData = "0x86481d40" + padHex(tokenId);
                  const levelResult = await ethCall(
                    contractAddress,
                    getLevelData
                  );

                  let level = 1;
                  if (levelResult && levelResult !== "0x") {
                    level = parseInt(levelResult, 16);
                  }

                  console.log(`[BLOCKCHAIN] ✅ NFT Level: ${level}`);

                  unityInstance.SendMessage(
                    "ChogTanksNFTManager",
                    callback,
                    JSON.stringify({
                      hasNFT: true,
                      level: level,
                      tokenId: tokenId,
                      walletAddress: normalizedAddress,
                      score: 0,
                    })
                  );
                  return;
                }
              }
            } catch (error) {
              console.log(
                `[BLOCKCHAIN] Token ${tokenId} check error: ${error.message}`
              );
            }

            if (tokenId % 50 === 0) {
              await new Promise((resolve) => setTimeout(resolve, 100));
            }
          }

          console.log("[BLOCKCHAIN] No NFT found for this wallet");
          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            callback,
            JSON.stringify({
              hasNFT: false,
              level: 0,
              tokenId: 0,
              walletAddress: normalizedAddress,
              score: 0,
            })
          );
        } catch (error) {
          console.error("[BLOCKCHAIN] Error during NFT verification:", error);
          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            callback,
            JSON.stringify({
              hasNFT: false,
              level: 0,
              tokenId: 0,
              walletAddress: normalizedAddress,
              score: 0,
              error: error.message,
            })
          );
        }
      }

      checkNFTOwnership();
      return true;
    } catch (error) {
      console.error(
        "[BLOCKCHAIN] Exception in ReadNFTFromBlockchainJS:",
        error
      );
      return false;
    }
  },

  GetAllNFTsJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim();
      console.log(
        `[NFT-LIST] Récupération de tous les NFTs pour ${normalizedAddress}`
      );

      const getAllNFTsFromBlockchain = async () => {
        try {
          const contractAddresses = [
            "0x68c582651d709f6e2b6113c01d69443f8d27e30d",
          ];

          let allNFTs = [];

          for (const contractAddr of contractAddresses) {
            try {
              console.log(`[NFT-LIST] Vérification contrat ${contractAddr}`);

              const balanceData = await window.ethereum.request({
                method: "eth_call",
                params: [
                  {
                    to: contractAddr,
                    data:
                      "0x70a08231" +
                      normalizedAddress.slice(2).padStart(64, "0"),
                  },
                  "latest",
                ],
              });

              const balance = parseInt(balanceData, 16);
              console.log(
                `[NFT-LIST] Balance pour ${contractAddr}: ${balance}`
              );

              if (balance > 0) {
                for (let i = 0; i < balance; i++) {
                  try {
                    const tokenData = await window.ethereum.request({
                      method: "eth_call",
                      params: [
                        {
                          to: contractAddr,
                          data:
                            "0x2f745c59" +
                            normalizedAddress.slice(2).padStart(64, "0") +
                            i.toString(16).padStart(64, "0"),
                        },
                        "latest",
                      ],
                    });

                    const tokenId = parseInt(tokenData, 16);
                    console.log(`[NFT-LIST] TokenId trouvé: ${tokenId}`);

                    if (tokenId > 0) {
                      const levelData = await window.ethereum.request({
                        method: "eth_call",
                        params: [
                          {
                            to: contractAddr,
                            data:
                              "0x86481d40" +
                              tokenId.toString(16).padStart(64, "0"),
                          },
                          "latest",
                        ],
                      });

                      const level = parseInt(levelData, 16);
                      console.log(
                        `[NFT-LIST] NFT #${tokenId} niveau ${level} trouvé`
                      );

                      allNFTs.push({
                        tokenId: tokenId,
                        level: level,
                        contractAddress: contractAddr,
                      });
                    }
                  } catch (tokenError) {
                    console.log(`[NFT-LIST] Erreur token ${i}:`, tokenError);
                  }
                }
              }
            } catch (contractError) {
              console.log(
                `[NFT-LIST] Erreur contrat ${contractAddr}:`,
                contractError
              );
            }
          }

          console.log(`[NFT-LIST] Total NFTs trouvés: ${allNFTs.length}`);

          const result = {
            tokenIds: allNFTs.map((nft) => nft.tokenId),
            levels: allNFTs.map((nft) => nft.level),
            count: allNFTs.length,
          };

          if (typeof unityInstance !== "undefined") {
            unityInstance.SendMessage(
              "NFTDisplayPanel",
              "OnNFTListReceived",
              JSON.stringify(result)
            );
          }
        } catch (error) {
          console.error("[NFT-LIST] Erreur récupération NFTs:", error);

          if (typeof unityInstance !== "undefined") {
            unityInstance.SendMessage(
              "NFTDisplayPanel",
              "OnNFTListReceived",
              JSON.stringify({ tokenIds: [], levels: [], count: 0 })
            );
          }
        }
      };

      getAllNFTsFromBlockchain();
      return true;
    } catch (error) {
      console.error("[NFT-LIST] Erreur GetAllNFTsJS:", error);
      return false;
    }
  },

  // Fonction simplifiée pour mint direct (test)
  DirectMintNFTJS: function (walletAddress) {
    try {
      const address = UTF8ToString(walletAddress);
      console.log(`[DIRECT-MINT] Starting direct mint for wallet: ${address}`);

      if (typeof unityInstance === "undefined") {
        console.error("[DIRECT-MINT] unityInstance not defined");
        return false;
      }

      // Call signature server for real authorization
      console.log(`[DIRECT-MINT] Calling signature server...`);

      fetch("http://localhost:3001/api/mint-authorization", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          walletAddress: address,
          playerPoints: 0, // Mint requires 0 points
        }),
      })
        .then((response) => response.json())
        .then((data) => {
          console.log(`[DIRECT-MINT] Server response:`, data);

          if (data.authorized) {
            const authData = {
              authorized: true,
              walletAddress: address,
              mintPrice: 1000000000000000, // 0.001 ETH in wei
              nonce: data.nonce,
              signature: data.signature,
            };

            console.log(
              `[DIRECT-MINT] Calling OnMintAuthorized with real signature:`,
              authData
            );

            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              "OnMintAuthorized",
              JSON.stringify(authData)
            );
          } else {
            console.error(
              `[DIRECT-MINT] Server denied authorization:`,
              data.error
            );

            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              "OnMintAuthorized",
              JSON.stringify({
                authorized: false,
                error: data.error || "Server denied mint authorization",
              })
            );
          }
        })
        .catch((error) => {
          console.error(`[DIRECT-MINT] Server error:`, error);

          // Fallback to mock for development if server is down
          console.log(
            `[DIRECT-MINT] Falling back to mock signature for development`
          );

          const mockAuth = {
            authorized: true,
            walletAddress: address,
            mintPrice: 1000000000000000,
            nonce: Date.now(),
            signature: "0x1234567890abcdef", // Mock signature
          };

          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            "OnMintAuthorized",
            JSON.stringify(mockAuth)
          );
        });

      return true;
    } catch (error) {
      console.error("[DIRECT-MINT] Error:", error);
      return false;
    }
  },

  // NOUVELLE FONCTION : Synchroniser le niveau NFT blockchain avec Firebase
  SyncNFTLevelWithFirebaseJS: function (
    walletAddress,
    blockchainLevel,
    tokenId
  ) {
    try {
      const address = UTF8ToString(walletAddress);
      const normalizedAddress = address.toLowerCase().trim(); // 🔧 SIMPLE FIX: Normalize address
      const level = blockchainLevel;
      const nftTokenId = tokenId;

      console.log(
        `[FIREBASE-SYNC] 🔄 Starting sync for wallet ${normalizedAddress}`
      );
      console.log(
        `[FIREBASE-SYNC] 🔗 Blockchain data: level=${level}, tokenId=${nftTokenId}`
      );

      const syncNFTLevel = async () => {
        try {
          // Étape 1 : Récupérer les données actuelles de Firebase (pour les points)
          console.log(
            `[FIREBASE-SYNC] 📊 Fetching current Firebase data for score...`
          );

          const docRef = db.collection("WalletScores").doc(normalizedAddress); // 🔧 SIMPLE FIX: Use WalletScores + normalized address
          const doc = await docRef.get();

          let currentScore = 0;
          if (doc.exists) {
            const data = doc.data();
            currentScore = data.score || 0;
            console.log(
              `[FIREBASE-SYNC] 📊 Found existing score in Firebase: ${currentScore}`
            );

            // Vérifier si le niveau a changé
            const firebaseLevel = data.level || 0;
            if (firebaseLevel !== level) {
              console.log(
                `[FIREBASE-SYNC] ⚠️ Level mismatch! Firebase: ${firebaseLevel}, Blockchain: ${level}`
              );
              console.log(
                `[FIREBASE-SYNC] 🔄 Updating Firebase level to match blockchain...`
              );
            } else {
              console.log(
                `[FIREBASE-SYNC] ✅ Levels already synchronized (${level})`
              );
            }
          } else {
            console.log(
              `[FIREBASE-SYNC] 🆕 No existing Firebase document, creating new one`
            );
          }

          // Étape 2 : Mettre à jour Firebase avec les données blockchain + score existant
          const syncedData = {
            walletAddress: normalizedAddress, // 🔧 SIMPLE FIX: Use normalized address
            hasNFT: true,
            level: level, // PRIORITÉ BLOCKCHAIN
            tokenId: nftTokenId, // PRIORITÉ BLOCKCHAIN
            score: currentScore, // CONSERVÉ DE FIREBASE
            lastSyncTimestamp: Date.now(),
            syncSource: "blockchain", // Indiquer que cette sync vient de la blockchain
          };

          console.log(
            `[FIREBASE-SYNC] 💾 Updating Firebase document with synced data:`,
            syncedData
          );

          await docRef.set(syncedData, { merge: true });

          console.log(
            `[FIREBASE-SYNC] ✅ Firebase successfully synchronized with blockchain data`
          );

          // Étape 3 : Retourner les données synchronisées à Unity
          const finalState = {
            hasNFT: true,
            level: level,
            tokenId: nftTokenId,
            walletAddress: normalizedAddress, // 🔧 SIMPLE FIX: Use normalized address
            score: currentScore,
          };

          console.log(
            `[FIREBASE-SYNC] 🎯 Sending final synchronized state to Unity:`,
            finalState
          );

          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            "OnFirebaseSyncCompleted",
            JSON.stringify(finalState)
          );
        } catch (error) {
          console.error(`[FIREBASE-SYNC] ❌ Sync failed:`, error);

          // En cas d'erreur, retourner les données blockchain avec score par défaut
          const fallbackState = {
            hasNFT: true,
            level: level,
            tokenId: nftTokenId,
            walletAddress: normalizedAddress, // 🔧 SIMPLE FIX: Use normalized address
            score: 100, // Score par défaut
          };

          console.log(
            `[FIREBASE-SYNC] 🔄 Using fallback state:`,
            fallbackState
          );

          unityInstance.SendMessage(
            "ChogTanksNFTManager",
            "OnFirebaseSyncCompleted",
            JSON.stringify(fallbackState)
          );
        }
      };

      syncNFTLevel();
      return true;
    } catch (error) {
      console.error(
        `[FIREBASE-SYNC] ❌ Exception in SyncNFTLevelWithFirebaseJS:`,
        error
      );
      return false;
    }
  },

  // ✅ MÉTHODE SUPPRIMÉE : ConsumePointsDirectlyJS (redondante)
  // Remplacée par CheckAndConsumePointsBeforeEvolutionJS pour un flux optimisé

  // Méthode pour demander directement la signature d'évolution après consommation des points
  RequestEvolutionSignatureJS: function (walletAddressPtr, tokenId, playerPoints, targetLevel) {
    const walletAddress = UTF8ToString(walletAddressPtr);
    const normalizedAddress = walletAddress.toLowerCase().trim();
    
    console.log(`[EVOL-DIRECT] Requesting signature for evolution:`);
    console.log(`[EVOL-DIRECT] Wallet: ${normalizedAddress}`);
    console.log(`[EVOL-DIRECT] TokenId: ${tokenId}`);
    console.log(`[EVOL-DIRECT] Player points: ${playerPoints}`);
    console.log(`[EVOL-DIRECT] Target level: ${targetLevel}`);
    
    // Appeler directement le serveur de signature
    fetch("http://localhost:3001/api/evolve-authorization", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        walletAddress: normalizedAddress,
        tokenId: tokenId,
        playerPoints: Number(playerPoints),
        targetLevel: targetLevel
      }),
    })
    .then((response) => response.json())
    .then((data) => {
      console.log(`[EVOL-DIRECT] Server response:`, data);
      
      if (data.authorized) {
        const authData = {
          authorized: true,
          walletAddress: normalizedAddress,
          tokenId: tokenId,
          currentPoints: Number(playerPoints),
          evolutionCost: data.evolutionCost || 0,
          targetLevel: targetLevel,
          nonce: data.nonce,
          signature: data.signature
        };
        
        console.log(`[EVOL-DIRECT] ✅ Evolution authorized, sending to Unity:`, authData);
        unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify(authData));
      } else {
        console.error(`[EVOL-DIRECT] ❌ Server denied authorization:`, data.error);
        unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify({
          authorized: false,
          error: data.error || "Server denied evolution authorization"
        }));
      }
    })
    .catch((error) => {
      console.error(`[EVOL-DIRECT] ❌ Server error:`, error);
      
      // Fallback to mock for development if server is down
      console.log(`[EVOL-DIRECT] Falling back to mock signature for development`);
      const mockAuth = {
        authorized: true,
        walletAddress: normalizedAddress,
        tokenId: tokenId,
        currentPoints: Number(playerPoints),
        evolutionCost: 200, // Coût pour niveau 2→3
        targetLevel: targetLevel,
        nonce: Date.now(),
        signature: "0x1234567890abcdef" // Mock signature
      };
      
      console.log(`[EVOL-DIRECT] 🔧 Using mock authorization:`, mockAuth);
      unityInstance.SendMessage("ChogTanksNFTManager", "OnEvolutionAuthorized", JSON.stringify(mockAuth));
    });
  },

  // 🔄 NOUVELLE MÉTHODE : Vérifier et consommer les points AVANT l'évolution blockchain
  CheckAndConsumePointsBeforeEvolutionJS: function (walletAddressPtr, pointsRequired, tokenId, targetLevel) {
    try {
      const walletAddress = UTF8ToString(walletAddressPtr);
      const normalizedAddress = walletAddress.toLowerCase().trim();
      
      console.log(`[PRE-EVOLUTION] 🔍 Checking points before evolution:`);
      console.log(`[PRE-EVOLUTION] Wallet: ${normalizedAddress}`);
      console.log(`[PRE-EVOLUTION] Points required: ${pointsRequired}`);
      console.log(`[PRE-EVOLUTION] Token ID: ${tokenId}`);
      console.log(`[PRE-EVOLUTION] Target level: ${targetLevel}`);
      
      if (typeof firebase === "undefined" || !firebase.apps.length) {
        console.error("[PRE-EVOLUTION] ❌ Firebase not initialized");
        return;
      }
      
      firebase.auth().onAuthStateChanged((user) => {
        if (user) {
          const db = firebase.firestore();
          const userDocRef = db.collection("WalletScores").doc(normalizedAddress);
          
          userDocRef.get()
          .then((docSnap) => {
            let currentScore = 0;
            if (docSnap.exists) {
              currentScore = docSnap.data().score || 0;
              console.log(`[PRE-EVOLUTION] 📊 Current score in Firebase: ${currentScore}`);
            }
            
            // Vérifier si l'utilisateur a assez de points
            if (currentScore >= pointsRequired) {
              console.log(`[PRE-EVOLUTION] ✅ Sufficient points (${currentScore} >= ${pointsRequired})`);
              
              // Consommer les points MAINTENANT
              const newScore = Math.max(0, currentScore - pointsRequired);
              console.log(`[PRE-EVOLUTION] 💰 Consuming points: ${currentScore} - ${pointsRequired} = ${newScore}`);
              
              return userDocRef.update({
                score: newScore,
                lastPreEvolution: {
                  tokenId: tokenId,
                  pointsConsumed: pointsRequired,
                  timestamp: Date.now(),
                  previousScore: currentScore,
                  newScore: newScore
                }
              }).then(() => {
                console.log(`[PRE-EVOLUTION] ✅ Points consumed successfully`);
                
                // Maintenant autoriser l'évolution blockchain
                const result = {
                  success: true,
                  authorized: true,
                  pointsConsumed: pointsRequired,
                  newScore: newScore,
                  tokenId: tokenId,
                  targetLevel: targetLevel
                };
                
                if (typeof unityInstance !== "undefined") {
                  unityInstance.SendMessage(
                    "ChogTanksNFTManager",
                    "OnPointsPreConsumed",
                    JSON.stringify(result)
                  );
                }
              });
            } else {
              console.log(`[PRE-EVOLUTION] ❌ Insufficient points (${currentScore} < ${pointsRequired})`);
              
              const result = {
                success: false,
                authorized: false,
                error: `Insufficient points: ${currentScore}/${pointsRequired}`,
                currentScore: currentScore,
                pointsRequired: pointsRequired
              };
              
              if (typeof unityInstance !== "undefined") {
                unityInstance.SendMessage(
                  "ChogTanksNFTManager",
                  "OnPointsPreConsumed",
                  JSON.stringify(result)
                );
              }
            }
          })
          .catch((error) => {
            console.error(`[PRE-EVOLUTION] Firebase error:`, error);
            
            if (typeof unityInstance !== "undefined") {
              unityInstance.SendMessage(
                "ChogTanksNFTManager",
                "OnPointsPreConsumed",
                JSON.stringify({ success: false, authorized: false, error: error.message })
              );
            }
          });
        } else {
          console.error("[PRE-EVOLUTION] User not authenticated");
          if (typeof unityInstance !== "undefined") {
            unityInstance.SendMessage(
              "ChogTanksNFTManager",
              "OnPointsPreConsumed",
              JSON.stringify({ success: false, authorized: false, error: "User not authenticated" })
            );
          }
        }
      });
      
    } catch (error) {
      console.error("[PRE-EVOLUTION] Error:", error);
      return false;
    }
  },
});
