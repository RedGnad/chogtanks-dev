const express = require('express');
const { ethers } = require('ethers');
const cors = require('cors');
require('dotenv').config();

const app = express();
app.use(express.json());
app.use(cors());

const port = process.env.PORT || 3001;

if (!process.env.GAME_SERVER_PRIVATE_KEY) {
    console.error('ERREUR: GAME_SERVER_PRIVATE_KEY non définie dans le fichier .env');
    process.exit(1);
}

const gameWallet = new ethers.Wallet(process.env.GAME_SERVER_PRIVATE_KEY);

console.log("Game Server Signer Address:", gameWallet.address);

// Health check endpoint pour monitoring
app.get('/health', (req, res) => {
    res.status(200).json({ 
        status: 'OK', 
        timestamp: new Date().toISOString(),
        uptime: process.uptime(),
        version: '1.0.0'
    });
});

app.post('/api/mint-authorization', async (req, res) => {
    try {
        const { playerAddress, mintCost } = req.body;
        
        if (!playerAddress || !mintCost) {
            return res.status(400).json({ error: "Adresse du joueur et coût de mint requis" });
        }
        
        const message = ethers.utils.solidityKeccak256(
            ['address', 'uint256'],
            [playerAddress, mintCost]
        );
        
        const signature = await gameWallet.signMessage(ethers.utils.arrayify(message));
        
        console.log(`Autorisation de mint générée pour ${playerAddress} avec un coût de ${mintCost}`);
        
        res.json({
            signature: signature,
            mintCost: mintCost,
            gameServerAddress: gameWallet.address
        });
        
    } catch (error) {
        console.error('Erreur d\'autorisation de mint:', error);
        res.status(500).json({ error: "Erreur interne du serveur" });
    }
});

app.post('/api/evolve-authorization', async (req, res) => {
    try {
        const { playerAddress, tokenId, targetLevel } = req.body;
        
        if (!playerAddress || !tokenId || !targetLevel) {
            return res.status(400).json({ error: "Adresse du joueur, ID du token et niveau cible requis" });
        }
        
        const evolutionCosts = {
            1: 50,   // Level 0 -> 1
            2: 100,  // Level 1 -> 2
            3: 150,  // Level 2 -> 3
            4: 200   // Level 3 -> 4
        };
        
        const requiredPoints = evolutionCosts[targetLevel];
        
        if (!requiredPoints) {
            return res.status(400).json({ error: "Niveau cible invalide" });
        }
        
        const message = ethers.utils.solidityKeccak256(
            ['address', 'uint256', 'uint256', 'uint256'],
            [playerAddress, tokenId, targetLevel, requiredPoints]
        );
        
        const signature = await gameWallet.signMessage(ethers.utils.arrayify(message));
        
        console.log(`Autorisation d'évolution générée pour ${playerAddress}, token ${tokenId} vers niveau ${targetLevel}`);
        
        res.json({
            signature: signature,
            evolutionCost: requiredPoints,
            targetLevel: targetLevel
        });
        
    } catch (error) {
        console.error('Evolve authorization error:', error);
        res.status(500).json({ error: "Internal server error" });
    }
});

// Anti-farming: Map pour lier les wallets Privy aux wallets AppKit
const walletBindings = new Map();

async function getNextNonce(wallet) {
    try {
        // Toujours récupérer le nonce le plus récent depuis la blockchain
        const nonce = await wallet.getTransactionCount('latest');
        console.log(`[NONCE] Nonce récupéré depuis blockchain: ${nonce}`);
        return nonce;
    } catch (error) {
        console.error('[NONCE] Erreur récupération nonce:', error);
        throw error;
    }
}

app.post('/api/monad-games-id/update-player', async (req, res) => {
    try {
        const { playerAddress, appKitWallet, scoreAmount, transactionAmount, actionType } = req.body;
        
        if (!playerAddress || !appKitWallet || scoreAmount === undefined || transactionAmount === undefined) {
            return res.status(400).json({ error: 'Missing required parameters' });
        }
        
        console.log(`[Monad Games ID] Received request: ${actionType} for ${playerAddress}`);
        console.log(`[Monad Games ID] Score: ${scoreAmount}, Transactions: ${transactionAmount}`);
        console.log(`[Monad Games ID] AppKit wallet: ${appKitWallet}`);
        
        // ANTI-FARMING: Vérifier la liaison des wallets
        const boundWallet = walletBindings.get(playerAddress);
        
        if (!boundWallet) {
            // Premier mint/evolution: lier les wallets
            walletBindings.set(playerAddress, appKitWallet);
            console.log(`[ANTI-FARMING] 🔗 Liaison créée: Privy ${playerAddress} → AppKit ${appKitWallet}`);
        } else if (boundWallet !== appKitWallet) {
            // Tentative de farming détectée
            console.error(`[ANTI-FARMING] 🚫 FARMING DÉTECTÉ!`);
            console.error(`[ANTI-FARMING] Privy: ${playerAddress}`);
            console.error(`[ANTI-FARMING] Wallet lié: ${boundWallet}`);
            console.error(`[ANTI-FARMING] Wallet actuel: ${appKitWallet}`);
            
            return res.status(403).json({ 
                error: "Wallet farming detected", 
                details: "This Monad Games ID account is bound to a different AppKit wallet"
            });
        } else {
            console.log(`[ANTI-FARMING] ✅ Wallet vérifié: ${appKitWallet}`);
        }
        
        const provider = new ethers.providers.JsonRpcProvider('https://testnet-rpc.monad.xyz/');
        const wallet = new ethers.Wallet(process.env.GAME_SERVER_PRIVATE_KEY, provider);
        
        const MONAD_GAMES_ID_CONTRACT = "0xceCBFF203C8B6044F52CE23D914A1bfD997541A4";
        const contractABI = [
            "function updatePlayerData(address player, uint256 scoreAmount, uint256 transactionAmount)"
        ];
        
        const contract = new ethers.Contract(MONAD_GAMES_ID_CONTRACT, contractABI, wallet);
        
        console.log(`[Monad Games ID] Calling updatePlayerData(${playerAddress}, ${scoreAmount}, ${transactionAmount})`);
        
        const nonce = await getNextNonce(wallet);
        
        const tx = await contract.updatePlayerData(playerAddress, scoreAmount, transactionAmount, {
            gasLimit: 100000, // Set explicit gas limit for consistent costs
            maxPriorityFeePerGas: ethers.utils.parseUnits('1', 'gwei'), // 1 gwei priority fee
            maxFeePerGas: ethers.utils.parseUnits('60', 'gwei'), // 50 base + 10 buffer
            nonce: nonce
        });
        
        console.log(`[Monad Games ID] Transaction sent: ${tx.hash}`);
        
        const receipt = await tx.wait();
        console.log(`[Monad Games ID] Transaction confirmed in block ${receipt.blockNumber}`);
        console.log(`[Monad Games ID] Gas used: ${receipt.gasUsed.toString()}`);
        
        res.json({ 
            success: true, 
            transactionHash: tx.hash, 
            blockNumber: receipt.blockNumber, 
            gasUsed: receipt.gasUsed.toString(),
            playerAddress, 
            scoreAmount, 
            transactionAmount, 
            actionType,
            message: "Score submitted to Monad Games ID contract"
        });
        
    } catch (error) {
        console.error('[Monad Games ID] Error:', error);
        res.status(500).json({ 
            error: "Failed to submit to Monad Games ID", 
            details: error.message 
        });
    }
});

app.listen(port, () => {
    console.log(`Signature server running on port ${port}`);
    console.log(`Game Server Address: ${gameWallet.address}`);
});