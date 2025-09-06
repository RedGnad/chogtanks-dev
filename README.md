# CHOGTANKS

**A Web3-powered multiplayer tank battle game built with Unity WebGL, featuring NFT evolution, blockchain scoring, and real-time PvP combat.**


CHOGTANKS is a competitive multiplayer tank battle game that combines classic arcade gameplay with cutting-edge Web3 features. Players engage in real-time PvP combat while earning blockchain-verified scores and evolving their NFT tanks through gameplay achievements.

### 🌟 Key Features

- **Real-time Multiplayer PvP** - Battle up to 8 players simultaneously
- **NFT Tank Evolution** - Upgrade your tank's onchain level through gameplay
- **Blockchain Scoring** - Verified leaderboards on Monad Games ID
- **Cross-Platform** - Play on any device with WebGL support
- **Anti-Farming Protection** - Fair play enforcement with wallet binding
- **Dynamic Audio** - Immersive sound effects and music
- **Mobile Optimized** - Touch controls and responsive UI

---

### Game Modes
- **Solo Practice**: Fight AI enemies with progressive difficulty
- **Multiplayer PvP**: Real-time battles with up to 20 players
- **Ranked Matches**: Climb the Monad Games ID leaderboard 

---

## NFT System

### Tank Evolution
Transform your tank's appearance by achieving gameplay milestones
NFT avolve directly in contract : 0x04223adab3a0c1a2e8aade678bebd3fddd580a38
IPSF evolutive metadata
Store your level onchain, higher level = higher reward
If you sell it, the buyer will earn the next rewards

### Requirements
- **Mint Cost**: FREE
- **Evolution**: Spend earned XP to upgrade your tank
- **Verification**: Blockchain-verified ownership and progression

---

##  Blockchain Integration

### Monad Games ID
- **Leaderboard**: Global ranking system
- **Score Verification**: Tamper-proof score submission
- **Cross-Game Identity**: Unified gaming profile
- **Achievement Tracking**: Permanent record of accomplishments

### Wallet Support
- ** All monad featured wallets
- ** Accounct abstraction Discord / Farecaster soon

---

## 🏗️ Technical Architecture

### Frontend
- **Unity 2022.3 LTS**: Game engine and WebGL build
- **Photon PUN2**: Real-time multiplayer networking
- **React WebView**: Blockchain authentication interface
- **Responsive Design**: Mobile and desktop optimization

### Backend Services
- **Node.js Servers**: Game logic and NFT management
- **Express.js APIs**: RESTful endpoints for game data
- **Blockchain RPC**: Direct smart contract interaction
- **Anti-Farming System**: Persistent wallet binding protection

### Infrastructure
- **GitHub Pages**: Game hosting and deployment
- **Render.com**: Backend server hosting
- **Monad Testnet**: Monad testnet network
- **Smart Contracts**: NFT minting and score verification

---

## 🚀 Getting Started

### Play Online
1. Visit [CHOGTANKS](https://chogtanks.vercel.app/)
2. Connect your wallet or create account
3. Just go BRAWL with your friends

### Development Setup

#### Prerequisites
- Unity 2022.3 LTS or later
- Node.js 18+ and npm
- Git

#### Clone Repository
```bash
git clone https://github.com/RedGnad/chogtanks-dev.git
cd chogtanks-dev
```

#### Unity Setup
1. Open project in Unity Hub
2. Install WebGL Build Support
3. Configure build settings for WebGL
4. Build and run locally

#### Server Setup
```bash
# Install dependencies
cd chogtanks-servers-clean
npm install

# Configure environment
cp .env.example .env
# Add your private keys and RPC endpoints

# Start development server
npm start
```

#### React WebView Setup
```bash
cd monad-react-webview
npm install
npm run dev
```

---

## 📊 Project Statistics

- **Languages**: C#, JavaScript, TypeScript
- **Frameworks**: Unity, React, Node.js, Express
- **Blockchain**: Monad Testnet integration
- **Multiplayer**: Photon Fusion networking
- **Deployment**: Automated CI/CD pipeline

---

## 🤝 Contributing

We welcome contributions! Please see our contributing guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request
