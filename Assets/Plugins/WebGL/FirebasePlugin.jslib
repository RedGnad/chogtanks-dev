mergeInto(LibraryManager.library, {
  // Initialisation de Firebase
  InitializeFirebaseJS: function (
    appIdPtr,
    apiKeyPtr,
    authDomainPtr,
    projectIdPtr,
    storageBucketPtr,
    messagingSenderIdPtr,
    measurementIdPtr
  ) {
    try {
      var appId = UTF8ToString(appIdPtr);
      var apiKey = UTF8ToString(apiKeyPtr);
      var authDomain = UTF8ToString(authDomainPtr);
      var projectId = UTF8ToString(projectIdPtr);
      var storageBucket = UTF8ToString(storageBucketPtr);
      var messagingSenderId = UTF8ToString(messagingSenderIdPtr);
      var measurementId = UTF8ToString(measurementIdPtr);

      var firebaseConfig = {
        apiKey: apiKey,
        authDomain: authDomain,
        projectId: projectId,
        storageBucket: storageBucket,
        messagingSenderId: messagingSenderId,
        appId: appId,
        measurementId: measurementId,
      };

      // Initialiser Firebase s'il n'est pas déjà initialisé
      if (firebase.apps.length === 0) {
        firebase.initializeApp(firebaseConfig);
      }

      console.log("Firebase initialized successfully");
      return true;
    } catch (error) {
      console.error("Failed to initialize Firebase:", error);
      return false;
    }
  },

  // Authentification anonyme (utilisée si le wallet n'est pas connecté)
  SignInAnonymouslyJS: function () {
    try {
      firebase
        .auth()
        .signInAnonymously()
        .then(() => {
          console.log("Signed in anonymously to Firebase");
          var callback = "OnFirebaseAuthSuccess";
          var uid = firebase.auth().currentUser.uid;
          unityInstance.SendMessage("FirebaseManager", callback, uid);
        })
        .catch((error) => {
          console.error("Anonymous auth error:", error);
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnFirebaseAuthError",
            error.message
          );
        });
      return true;
    } catch (error) {
      console.error("SignInAnonymously error:", error);
      return false;
    }
  },

  // Authentification avec CustomToken (créé à partir de l'adresse wallet)
  SignInWithCustomTokenJS: function (tokenPtr) {
    try {
      var token = UTF8ToString(tokenPtr);
      firebase
        .auth()
        .signInWithCustomToken(token)
        .then(() => {
          console.log("Signed in with custom token");
          var callback = "OnFirebaseAuthSuccess";
          var uid = firebase.auth().currentUser.uid;
          unityInstance.SendMessage("FirebaseManager", callback, uid);
        })
        .catch((error) => {
          console.error("Custom token auth error:", error);
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnFirebaseAuthError",
            error.message
          );
        });
      return true;
    } catch (error) {
      console.error("SignInWithCustomToken error:", error);
      return false;
    }
  },

  // Soumettre un score à Firebase Firestore
  SubmitScoreJS: function (scorePtr, bonusPtr, walletAddressPtr) {
    try {
      var score = parseInt(UTF8ToString(scorePtr));
      var bonus = parseInt(UTF8ToString(bonusPtr));
      var walletAddress = UTF8ToString(walletAddressPtr);

      if (!walletAddress || walletAddress.length < 5) {
        console.error("Invalid wallet address");
        return false;
      }

      var totalScore = score + bonus;
      var db = firebase.firestore();

      // D'abord vérifier si un document existe déjà pour ce wallet
      db.collection("WalletScores")
        .doc(walletAddress)
        .get()
        .then((doc) => {
          if (doc.exists) {
            var currentScore = doc.data().score || 0;
            var newScore = currentScore + totalScore;

            // Mettre à jour le score existant
            return db.collection("WalletScores").doc(walletAddress).update({
              score: newScore,
              lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
            });
          } else {
            // Créer un nouveau document pour ce wallet
            return db.collection("WalletScores").doc(walletAddress).set({
              wallet: walletAddress,
              score: totalScore,
              created: firebase.firestore.FieldValue.serverTimestamp(),
              lastUpdated: firebase.firestore.FieldValue.serverTimestamp(),
            });
          }
        })
        .then(() => {
          console.log("Score submitted successfully");
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnScoreSubmitted",
            "Success"
          );
        })
        .catch((error) => {
          console.error("Error submitting score:", error);
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnScoreSubmitError",
            error.message
          );
        });

      return true;
    } catch (error) {
      console.error("SubmitScoreJS error:", error);
      return false;
    }
  },

  // Récupérer le classement des joueurs (top X)
  GetLeaderboardJS: function (limitPtr) {
    try {
      var limit = parseInt(UTF8ToString(limitPtr));
      var db = firebase.firestore();

      db.collection("WalletScores")
        .orderBy("score", "desc")
        .limit(limit)
        .get()
        .then((querySnapshot) => {
          var leaderboardData = [];
          querySnapshot.forEach((doc) => {
            var data = doc.data();
            leaderboardData.push({
              wallet: data.wallet,
              score: data.score,
            });
          });

          var jsonData = JSON.stringify(leaderboardData);
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnLeaderboardReceived",
            jsonData
          );
        })
        .catch((error) => {
          console.error("Error getting leaderboard:", error);
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnLeaderboardError",
            error.message
          );
        });

      return true;
    } catch (error) {
      console.error("GetLeaderboardJS error:", error);
      return false;
    }
  },

  // Vérifier si l'utilisateur est connecté à Firebase
  IsUserSignedInJS: function () {
    try {
      var isSignedIn = firebase.auth().currentUser != null;
      return isSignedIn;
    } catch (error) {
      console.error("IsUserSignedInJS error:", error);
      return false;
    }
  },

  // Récupérer l'ID de l'utilisateur actuel
  GetCurrentUserUidJS: function () {
    try {
      var user = firebase.auth().currentUser;
      if (user) {
        var uid = user.uid;
        var bufferSize = lengthBytesUTF8(uid) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(uid, buffer, bufferSize);
        return buffer;
      } else {
        return null;
      }
    } catch (error) {
      console.error("GetCurrentUserUidJS error:", error);
      return null;
    }
  },

  // Pour la déconnexion
  SignOutJS: function () {
    try {
      firebase
        .auth()
        .signOut()
        .then(() => {
          console.log("Signed out from Firebase");
          unityInstance.SendMessage("FirebaseManager", "OnSignOutSuccess", "");
        })
        .catch((error) => {
          console.error("Sign out error:", error);
          unityInstance.SendMessage(
            "FirebaseManager",
            "OnSignOutError",
            error.message
          );
        });
      return true;
    } catch (error) {
      console.error("SignOutJS error:", error);
      return false;
    }
  },
});
