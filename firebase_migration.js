// Script de migration Firebase pour normaliser les adresses wallet
// À exécuter dans la console Firebase ou via Node.js

const admin = require('firebase-admin');

// Initialiser Firebase Admin
const serviceAccount = require('./path/to/serviceAccountKey.json');
admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

const db = admin.firestore();

async function migrateWalletAddresses() {
  console.log('🔄 Début de la migration des adresses wallet...');
  
  const walletScoresRef = db.collection('WalletScores');
  const snapshot = await walletScoresRef.get();
  
  const migrations = [];
  
  snapshot.forEach(doc => {
    const originalId = doc.id;
    const normalizedId = originalId.toLowerCase();
    
    if (originalId !== normalizedId) {
      console.log(`📝 Migration nécessaire: ${originalId} → ${normalizedId}`);
      migrations.push({
        originalId,
        normalizedId,
        data: doc.data()
      });
    }
  });
  
  console.log(`📊 ${migrations.length} documents à migrer`);
  
  for (const migration of migrations) {
    try {
      // Créer le nouveau document avec l'ID normalisé
      await walletScoresRef.doc(migration.normalizedId).set({
        ...migration.data,
        walletAddress: migration.normalizedId,
        migratedFrom: migration.originalId,
        migrationDate: admin.firestore.FieldValue.serverTimestamp()
      });
      
      // Supprimer l'ancien document
      await walletScoresRef.doc(migration.originalId).delete();
      
      console.log(`✅ Migré: ${migration.originalId} → ${migration.normalizedId}`);
    } catch (error) {
      console.error(`❌ Erreur migration ${migration.originalId}:`, error);
    }
  }
  
  console.log('🎉 Migration terminée !');
}

// Exécuter la migration
migrateWalletAddresses().catch(console.error);
