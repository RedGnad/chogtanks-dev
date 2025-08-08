// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "@openzeppelin/contracts/token/ERC721/extensions/ERC721Enumerable.sol";
import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/utils/Strings.sol";
import "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import "@openzeppelin/contracts/utils/cryptography/MessageHashUtils.sol";

contract ChogTanks is ERC721, ERC721Enumerable, Ownable {
    using Strings for uint256;
    using ECDSA for bytes32;
    using MessageHashUtils for bytes32;
    
    uint256 public constant MINT_PRICE = 0.001 ether;
    uint256 public constant MAX_LEVEL = 10;
    uint256 public constant MAX_SUPPLY = 600; 
    uint256 private _tokenIdCounter = 1;
    
    mapping(uint256 => uint256) public evolutionCosts;
    mapping(uint256 => uint256) public nftLevels;
    mapping(address => uint256[]) public walletNFTs;
    mapping(uint256 => uint256) private nftIndexInWallet;
    
    // 🎯 AMÉLIORATION 1: Restriction permanente de mint
    mapping(address => bool) public hasMintedBefore;
    
    address public gameServerSigner;
    mapping(bytes32 => bool) public usedSignatures;
    
    event NFTMinted(address indexed owner, uint256 tokenId);
    event NFTEvolved(address indexed owner, uint256 tokenId, uint256 newLevel, uint256 pointsConsumed);
    event GameServerSignerUpdated(address indexed newSigner);
    event MaxSupplyReached(uint256 totalSupply);
    
    constructor(address _gameServerSigner) ERC721("ChogTanks", "TANK") Ownable(msg.sender) {
        gameServerSigner = _gameServerSigner;
        
        // 🎯 AMÉLIORATION 2: Coûts d'évolution plus abordables
        evolutionCosts[2] = 2;
        evolutionCosts[3] = 100;    // Au lieu de 200
        evolutionCosts[4] = 200;    // Au lieu de 300
        evolutionCosts[5] = 300;    // Au lieu de 400
        evolutionCosts[6] = 400;    // Au lieu de 500
        evolutionCosts[7] = 500;    // Au lieu de 600
        evolutionCosts[8] = 600;    // Au lieu de 700
        evolutionCosts[9] = 700;    // Au lieu de 800
        evolutionCosts[10] = 800;   // Au lieu de 900
    }
    
    function totalSupply() public view override returns (uint256) {
        return _tokenIdCounter - 1;
    }
    
    function isMaxSupplyReached() public view returns (bool) {
        return totalSupply() >= MAX_SUPPLY;
    }
    
    function remainingSupply() public view returns (uint256) {
        uint256 current = totalSupply();
        if (current >= MAX_SUPPLY) {
            return 0;
        }
        return MAX_SUPPLY - current;
    }
    
    // 🎯 FONCTION ESSENTIELLE: canMintNFT (gardée de v2)
    function canMintNFT(address wallet) external view returns (bool, string memory) {
        if (isMaxSupplyReached()) {
            return (false, "Max supply reached");
        }
        if (hasMintedBefore[wallet]) {
            return (false, "Already minted NFT"); // 🎯 Restriction permanente
        }
        return (true, "");
    }
    
    function mintNFT(
        uint256 playerPoints,
        uint256 nonce,
        bytes memory signature
    ) external payable {
        require(msg.value >= MINT_PRICE, "Insufficient payment");
        require(!isMaxSupplyReached(), "Max supply reached");
        require(!hasMintedBefore[msg.sender], "Already minted NFT"); // 🎯 Restriction permanente
        
        bytes32 messageHash = keccak256(abi.encodePacked(
            msg.sender,
            playerPoints,
            nonce,
            "MINT"
        )).toEthSignedMessageHash();
        
        require(!usedSignatures[messageHash], "Signature already used");
        require(messageHash.recover(signature) == gameServerSigner, "Invalid signature");
        
        usedSignatures[messageHash] = true;
        hasMintedBefore[msg.sender] = true; // 🎯 Marquage permanent
        
        uint256 tokenId = _tokenIdCounter++;
        _safeMint(msg.sender, tokenId);
        
        nftLevels[tokenId] = 1;
        
        walletNFTs[msg.sender].push(tokenId);
        nftIndexInWallet[tokenId] = walletNFTs[msg.sender].length - 1;
        
        emit NFTMinted(msg.sender, tokenId);
        
        if (isMaxSupplyReached()) {
            emit MaxSupplyReached(totalSupply());
        }
    }
    
    function evolveNFT(
        uint256 tokenId,
        uint256 playerPoints,
        uint256 nonce,
        bytes memory signature
    ) external {
        require(ownerOf(tokenId) == msg.sender, "Not your NFT");
        
        uint256 currentLevel = nftLevels[tokenId];
        require(currentLevel < MAX_LEVEL, "NFT already at max level");
        
        uint256 targetLevel = currentLevel + 1;
        uint256 evolutionCost = evolutionCosts[targetLevel];
        
        require(playerPoints >= evolutionCost, "Insufficient points");
        
        // 🎯 AMÉLIORATION 3: Signature sécurisée avec targetLevel
        bytes32 messageHash = keccak256(abi.encodePacked(
            msg.sender,
            tokenId,
            targetLevel,
            playerPoints,
            nonce,
            "EVOLVE"
        )).toEthSignedMessageHash();
        
        require(!usedSignatures[messageHash], "Signature already used");
        require(messageHash.recover(signature) == gameServerSigner, "Invalid signature");
        
        usedSignatures[messageHash] = true;
        
        nftLevels[tokenId] = targetLevel;
        
        emit NFTEvolved(msg.sender, tokenId, targetLevel, evolutionCost);
    }
    
    function transferFrom(address from, address to, uint256 tokenId) public override(ERC721, IERC721) {
        super.transferFrom(from, to, tokenId);
        _updateWalletNFTsOnTransfer(from, to, tokenId);
    }
    
    function safeTransferFrom(address from, address to, uint256 tokenId, bytes memory data) public override(ERC721, IERC721) {
        super.safeTransferFrom(from, to, tokenId, data);
        _updateWalletNFTsOnTransfer(from, to, tokenId);
    }
    
    function _updateWalletNFTsOnTransfer(address from, address to, uint256 tokenId) private {
        if (from != address(0) && to != address(0)) {
            _removeNFTFromWallet(from, tokenId);
            walletNFTs[to].push(tokenId);
            nftIndexInWallet[tokenId] = walletNFTs[to].length - 1;
        }
    }
    
    // 🎯 AMÉLIORATION 4: Fix array bounds avec vérifications robustes
    function _removeNFTFromWallet(address wallet, uint256 tokenId) private {
        uint256[] storage nfts = walletNFTs[wallet];
        
        // Protection 1: Array vide
        if (nfts.length == 0) {
            return;
        }
        
        uint256 index = nftIndexInWallet[tokenId];
        
        // Protection 2: Index invalide
        if (index >= nfts.length) {
            return;
        }
        
        // Protection 3: Vérification que le tokenId correspond bien à l'index
        if (nfts[index] != tokenId) {
            // Fallback: recherche linéaire pour trouver le bon index
            bool found = false;
            for (uint256 i = 0; i < nfts.length; i++) {
                if (nfts[i] == tokenId) {
                    index = i;
                    found = true;
                    break;
                }
            }
            if (!found) {
                return; // TokenId pas trouvé dans l'array
            }
        }
        
        uint256 lastIndex = nfts.length - 1;
        
        if (index != lastIndex) {
            uint256 lastTokenId = nfts[lastIndex];
            nfts[index] = lastTokenId;
            nftIndexInWallet[lastTokenId] = index;
        }
        
        nfts.pop();
        delete nftIndexInWallet[tokenId];
    }
    
    function getLevel(uint256 tokenId) external view returns (uint256) {
        require(_ownerOf(tokenId) != address(0), "NFT does not exist");
        return nftLevels[tokenId];
    }
    
    function getWalletNFTs(address wallet) external view returns (uint256[] memory) {
        return walletNFTs[wallet];
    }
    
    // 🎯 FONCTION COMPLÈTE: getWalletNFTsDetails avec canEvolve + evolutionCosts
    function getWalletNFTsDetails(address wallet) external view returns (
        uint256[] memory tokenIds,
        uint256[] memory levels,
        bool[] memory canEvolveArray,
        uint256[] memory evolutionCostsArray
    ) {
        uint256[] memory nfts = walletNFTs[wallet];
        tokenIds = new uint256[](nfts.length);
        levels = new uint256[](nfts.length);
        canEvolveArray = new bool[](nfts.length);
        evolutionCostsArray = new uint256[](nfts.length);
        
        for (uint256 i = 0; i < nfts.length; i++) {
            tokenIds[i] = nfts[i];
            levels[i] = nftLevels[nfts[i]];
            canEvolveArray[i] = nftLevels[nfts[i]] < MAX_LEVEL;
            
            if (canEvolveArray[i]) {
                evolutionCostsArray[i] = evolutionCosts[nftLevels[nfts[i]] + 1];
            } else {
                evolutionCostsArray[i] = 0;
            }
        }
    }
    
    function getNextLevelCost(uint256 tokenId) external view returns (uint256) {
        require(_ownerOf(tokenId) != address(0), "NFT does not exist");
        
        uint256 currentLevel = nftLevels[tokenId];
        if (currentLevel >= MAX_LEVEL) {
            return 0;
        }
        
        return evolutionCosts[currentLevel + 1];
    }
    
    function canEvolve(uint256 tokenId) external view returns (bool) {
        if (_ownerOf(tokenId) == address(0)) return false;
        return nftLevels[tokenId] < MAX_LEVEL;
    }
    
    // 🎯 FONCTIONS UTILITAIRES (gardées de v2)
    function getMaxSupply() external pure returns (uint256) {
        return MAX_SUPPLY;
    }
    
    function getMintPrice() external pure returns (uint256) {
        return MINT_PRICE;
    }
    
    function getAllEvolutionCosts() external view returns (uint256[11] memory costs) {
        for (uint256 i = 2; i <= MAX_LEVEL; i++) {
            costs[i] = evolutionCosts[i];
        }
    }
    
    function setGameServerSigner(address newSigner) external onlyOwner {
        gameServerSigner = newSigner;
        emit GameServerSignerUpdated(newSigner);
    }
    
    function setEvolutionCost(uint256 level, uint256 cost) external onlyOwner {
        require(level >= 2 && level <= MAX_LEVEL, "Invalid level");
        evolutionCosts[level] = cost;
    }
    
    function tokenURI(uint256 tokenId) public view override returns (string memory) {
        require(_ownerOf(tokenId) != address(0), "NFT does not exist");
        
        uint256 level = nftLevels[tokenId];
        
        return string(
            abi.encodePacked(
                "https://df1284899722.ngrok-free.app/metadata/level",
                level.toString(),
                "/",
                tokenId.toString(),
                ".json"
            )
        );
    }
    
    function withdraw() external onlyOwner {
        uint256 balance = address(this).balance;
        require(balance > 0, "No funds to withdraw");
        
        (bool success, ) = owner().call{value: balance}("");
        require(success, "Withdrawal failed");
    }
    
    function _update(address to, uint256 tokenId, address auth)
        internal
        override(ERC721, ERC721Enumerable)
        returns (address)
    {
        return super._update(to, tokenId, auth);
    }

    function _increaseBalance(address account, uint128 value)
        internal
        override(ERC721, ERC721Enumerable)
    {
        super._increaseBalance(account, value);
    }

    function supportsInterface(bytes4 interfaceId)
        public
        view
        override(ERC721, ERC721Enumerable)
        returns (bool)
    {
        return super.supportsInterface(interfaceId);
    }
}