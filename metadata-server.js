const express = require('express');
const cors = require('cors');
const app = express();
const port = process.env.PORT || 3002;

// Enable CORS for all origins
app.use(cors());
app.use(express.json());

// Metadata template for each level
const getMetadata = (level, tokenId) => {
    const levelData = {
        1: {
            name: `ChogTank #${tokenId} - Rookie`,
            description: "A basic tank ready for battle. This is just the beginning of your journey.",
            image: "https://via.placeholder.com/400x400/4CAF50/FFFFFF?text=Level+1",
            attributes: [
                { "trait_type": "Level", "value": 1 },
                { "trait_type": "Rarity", "value": "Common" },
                { "trait_type": "Power", "value": 10 },
                { "trait_type": "Defense", "value": 5 }
            ]
        },
        2: {
            name: `ChogTank #${tokenId} - Veteran`,
            description: "An upgraded tank with enhanced capabilities.",
            image: "https://via.placeholder.com/400x400/2196F3/FFFFFF?text=Level+2",
            attributes: [
                { "trait_type": "Level", "value": 2 },
                { "trait_type": "Rarity", "value": "Common" },
                { "trait_type": "Power", "value": 20 },
                { "trait_type": "Defense", "value": 15 }
            ]
        },
        3: {
            name: `ChogTank #${tokenId} - Elite`,
            description: "A powerful tank with advanced weaponry.",
            image: "https://via.placeholder.com/400x400/FF9800/FFFFFF?text=Level+3",
            attributes: [
                { "trait_type": "Level", "value": 3 },
                { "trait_type": "Rarity", "value": "Uncommon" },
                { "trait_type": "Power", "value": 35 },
                { "trait_type": "Defense", "value": 25 }
            ]
        },
        4: {
            name: `ChogTank #${tokenId} - Champion`,
            description: "A formidable tank feared on the battlefield.",
            image: "https://via.placeholder.com/400x400/9C27B0/FFFFFF?text=Level+4",
            attributes: [
                { "trait_type": "Level", "value": 4 },
                { "trait_type": "Rarity", "value": "Rare" },
                { "trait_type": "Power", "value": 55 },
                { "trait_type": "Defense", "value": 40 }
            ]
        },
        5: {
            name: `ChogTank #${tokenId} - Master`,
            description: "A master-class tank with superior technology.",
            image: "https://via.placeholder.com/400x400/E91E63/FFFFFF?text=Level+5",
            attributes: [
                { "trait_type": "Level", "value": 5 },
                { "trait_type": "Rarity", "value": "Epic" },
                { "trait_type": "Power", "value": 80 },
                { "trait_type": "Defense", "value": 60 }
            ]
        },
        6: {
            name: `ChogTank #${tokenId} - Legendary`,
            description: "A legendary tank spoken of in whispers.",
            image: "https://via.placeholder.com/400x400/F44336/FFFFFF?text=Level+6",
            attributes: [
                { "trait_type": "Level", "value": 6 },
                { "trait_type": "Rarity", "value": "Legendary" },
                { "trait_type": "Power", "value": 110 },
                { "trait_type": "Defense", "value": 85 }
            ]
        },
        7: {
            name: `ChogTank #${tokenId} - Mythic`,
            description: "A mythic tank of unparalleled power.",
            image: "https://via.placeholder.com/400x400/795548/FFFFFF?text=Level+7",
            attributes: [
                { "trait_type": "Level", "value": 7 },
                { "trait_type": "Rarity", "value": "Mythic" },
                { "trait_type": "Power", "value": 145 },
                { "trait_type": "Defense", "value": 115 }
            ]
        },
        8: {
            name: `ChogTank #${tokenId} - Divine`,
            description: "A divine tank blessed by the gods of war.",
            image: "https://via.placeholder.com/400x400/607D8B/FFFFFF?text=Level+8",
            attributes: [
                { "trait_type": "Level", "value": 8 },
                { "trait_type": "Rarity", "value": "Divine" },
                { "trait_type": "Power", "value": 185 },
                { "trait_type": "Defense", "value": 150 }
            ]
        },
        9: {
            name: `ChogTank #${tokenId} - Cosmic`,
            description: "A cosmic entity that transcends earthly warfare.",
            image: "https://via.placeholder.com/400x400/3F51B5/FFFFFF?text=Level+9",
            attributes: [
                { "trait_type": "Level", "value": 9 },
                { "trait_type": "Rarity", "value": "Cosmic" },
                { "trait_type": "Power", "value": 230 },
                { "trait_type": "Defense", "value": 190 }
            ]
        },
        10: {
            name: `ChogTank #${tokenId} - Transcendent`,
            description: "The ultimate evolution. A tank that has transcended all limitations.",
            image: "https://via.placeholder.com/400x400/000000/FFD700?text=MAX+LEVEL",
            attributes: [
                { "trait_type": "Level", "value": 10 },
                { "trait_type": "Rarity", "value": "Transcendent" },
                { "trait_type": "Power", "value": 300 },
                { "trait_type": "Defense", "value": 250 },
                { "trait_type": "Special", "value": "Max Level Achieved" }
            ]
        }
    };

    return levelData[level] || levelData[1]; // Default to level 1 if not found
};

// Handle metadata requests
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
    
    // Add OpenSea compatibility
    metadata.external_url = `https://chogtanks.com/nft/${tokenId}`;
    metadata.background_color = "1a1a1a";
    
    console.log(`✅ Returning metadata for ChogTank #${tokenId} Level ${level}`);
    res.json(metadata);
});

// Health check endpoint
app.get('/health', (req, res) => {
    res.json({ 
        status: 'ok', 
        message: 'ChogTanks Metadata Server is running',
        timestamp: new Date().toISOString()
    });
});

// Root endpoint
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
