const express = require('express');
const cors = require('cors');
const app = express();
const port = process.env.PORT || 3002;

app.use(cors());
app.use(express.json());

// 🎯 Configuration IPFS pour vos images NFT
const IPFS_CID = "bafybeicbpotmazbf2u2hu6s6pdufdj5en7hdrqy4yldtqa4yoie2kq4ssy";

// Fonction pour générer l'URL des images basée sur vos noms de fichiers
const getImageUrl = (level) => {
    if (level >= 1 && level <= 9) {
        return `ipfs://${IPFS_CID}/molazi-metadata-${level}.png`;
    }
    // Fallback pour le niveau 10 (réutilise l'image du niveau 9)
    return `ipfs://${IPFS_CID}/molazi-metadata-9.png`;
};

const getMetadata = (level, tokenId) => {
    const levelData = {
        1: {
            name: `ChogTank #${tokenId} - Rookie`,
            description: "A basic tank ready for battle.",
            image: getImageUrl(1), // ✅ molazi-metadata-1.png
            attributes: [
                { "trait_type": "Level", "value": 1 },
                { "trait_type": "Rarity", "value": "Common" },
            ]
        },
        2: {
            name: `ChogTank #${tokenId} - Soldier`,
            description: "An upgraded tank ready for the brawl.",
            image: getImageUrl(2), // ✅ molazi-metadata-2.png
            attributes: [
                { "trait_type": "Level", "value": 2 },
                { "trait_type": "Rarity", "value": "Common" },
            ]
        },
        3: {
            name: `ChogTank #${tokenId} - Elite`,
            description: "A powerful tank with advanced weaponry.",
            image: getImageUrl(3), // ✅ molazi-metadata-3.png
            attributes: [
                { "trait_type": "Level", "value": 3 },
                { "trait_type": "Rarity", "value": "Uncommon" },
            ]
        },
        4: {
            name: `ChogTank #${tokenId} - General`,
            description: "A formidable tank feared on the battlefield.",
            image: getImageUrl(4), // ✅ molazi-metadata-4.png
            attributes: [
                { "trait_type": "Level", "value": 4 },
                { "trait_type": "Rarity", "value": "Rare" },
            ]
        },
        5: {
            name: `ChogTank #${tokenId} - Master`,
            description: "A master-class tank with superior technology.",
            image: getImageUrl(5), // ✅ molazi-metadata-5.png
            attributes: [
                { "trait_type": "Level", "value": 5 },
                { "trait_type": "Rarity", "value": "Ultrarare" },
            ]
        },
        6: {
            name: `ChogTank #${tokenId} - Champion`,
            description: "A champion tank spoken of in whispers.",
            image: getImageUrl(6), // ✅ molazi-metadata-6.png
            attributes: [
                { "trait_type": "Level", "value": 6 },
                { "trait_type": "Rarity", "value": "Epic" },
            ]
        },
        7: {
            name: `ChogTank #${tokenId} - Legendary`,
            description: "A legendary tank of unparalleled power.",
            image: getImageUrl(7), // ✅ molazi-metadata-7.png
            attributes: [
                { "trait_type": "Level", "value": 7 },
                { "trait_type": "Rarity", "value": "Legendary" },
            ]
        },
        8: {
            name: `ChogTank #${tokenId} - Mythic`,
            description: "A mythic tank blessed by the gods of war.",
            image: getImageUrl(8), // ✅ molazi-metadata-8.png
            attributes: [
                { "trait_type": "Level", "value": 8 },
                { "trait_type": "Rarity", "value": "Mythic" },
            ]
        },
        9: {
            name: `ChogTank #${tokenId} - Cosmic`,
            description: "A cosmic entity that transcends earthly warfare.",
            image: getImageUrl(9), // ✅ molazi-metadata-9.png
            attributes: [
                { "trait_type": "Level", "value": 9 },
                { "trait_type": "Rarity", "value": "Cosmic" },
            ]
        },
        10: {
            name: `ChogTank #${tokenId} - Transcendent`,
            description: "The ultimate evolution. A tank that has transcended all limitations.",
            image: getImageUrl(10), // ✅ molazi-metadata-9.png (réutilise niveau 9)
            attributes: [
                { "trait_type": "Level", "value": 10 },
                { "trait_type": "Rarity", "value": "Transcendent" },
                { "trait_type": "Special", "value": "Max Level Achieved" }
            ]
        }
    };

    return levelData[level] || levelData[1]; // Default to level 1 if not found
};

app.get('/metadata/level:level/:tokenId.json', (req, res) => {
    const level = parseInt(req.params.level);
    const tokenId = parseInt(req.params.tokenId);
    
    console.log(`📊 Metadata request: Level ${level}, Token #${tokenId}`);
    
    if (level < 1 || level > 10) {
        return res.status(400).json({ error: 'Invalid level. Must be between 1 and 10.' });
    }
    
    if (tokenId < 1) {
        return res.status(400).json({ error: 'Invalid token ID.' });
    }
    
    const metadata = getMetadata(level, tokenId);
    
    metadata.external_url = `https://chogtanks.com/nft/${tokenId}`;
    metadata.background_color = "1a1a1a";
    
    console.log(`✅ Returning metadata for ChogTank #${tokenId} Level ${level}`);
    res.json(metadata);
});

app.get('/health', (req, res) => {
    res.json({ 
        status: 'ok', 
        message: 'ChogTanks Metadata Server is running',
        timestamp: new Date().toISOString()
    });
});

app.get('/', (req, res) => {
    res.json({
        message: 'ChogTanks Metadata Server',
        usage: 'GET /metadata/level{1-10}/{tokenId}.json',
        example: '/metadata/level1/1.json',
        health: '/health'
    });
});

app.listen(port, () => {
    console.log(`🚀 ChogTanks Metadata Server running on port ${port}`);
    console.log(`📊 Metadata endpoint: http://localhost:${port}/metadata/level1/1.json`);
    console.log(`💚 Health check: http://localhost:${port}/health`);
});
