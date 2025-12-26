# Nano_Mango
SAMPLE APK LINK- https://drive.google.com/file/d/1mdRF9tgjzKtEKcMilbOYAfw2vWdT3ED1/view?usp=sharing

A professional, well-architected card matching game built with Unity 2021 LTS, following SOLID principles and clean architecture patterns.

## 🏗️ Architecture

The project follows a clean architecture pattern with clear separation of concerns:

```
Assets/Scripts/
├── Core/           # Interfaces and contracts
├── Models/         # Data models
├── Views/          # UI and presentation layer
├── Managers/       # Business logic and coordination
├── Services/       # External services (Audio, Save/Load)
└── Utils/          # Utilities and constants
```

### Key Components

#### **Core** (Interfaces)
- `IGameState` - Game state management interface
- `ISaveService` - Save/load service interface
- `IAudioService` - Audio service interface

#### **Models** (Data)
- `CardData` - Card state and properties
- `GameData` - Serializable game state for persistence
- `ScoreData` - Score calculation data

#### **Views** (Presentation)
- `CardView` - Card display and animations
- `GameUIView` - Game UI management

#### **Managers** (Business Logic)
- `GameManager` - Main game coordinator
- `CardManager` - Card creation and layout
- `ScoreManager` - Scoring and combo system

#### **Services** (External)
- `AudioService` - Audio playback service
- `SaveService` - Game data persistence

#### **Utils** (Utilities)
- `GameConstants` - Game configuration constants
- `GameEnums` - Game-related enumerations

## ✨ Features

### Core Gameplay
- ✅ Smooth card flip animations
- ✅ Continuous card flipping (no waiting for comparisons)
- ✅ Multiple grid layouts (2x2, 2x3, up to 6x6)
- ✅ Cards scale to fit display area
- ✅ Responsive and optimized performance

### Scoring System
- ✅ Base score per match
- ✅ Combo system with multipliers
- ✅ Time-based bonuses
- ✅ Move tracking
- ✅ Real-time score display

### Save/Load System
- ✅ Persistent game state
- ✅ Resume game functionality
- ✅ Auto-save on moves
- ✅ Auto-save on pause/focus loss
- ✅ JSON-based data storage

### Audio System
- ✅ Card flip sound effects
- ✅ Match sound effects
- ✅ Mismatch sound effects
- ✅ Game over sound effects
- ✅ Volume control

### Code Quality
- ✅ SOLID principles
- ✅ Clean architecture
- ✅ Proper separation of concerns
- ✅ Interface-based design
- ✅ Comprehensive error handling
- ✅ No warnings or errors
- ✅ Professional naming conventions

## 🎮 How to Use

### Setup
1. Open the project in Unity 2021 LTS
2. Ensure all scripts are compiled without errors
3. Set up the scene with:
   - GameManager component
   - Card prefab with CardView component
   - GameUIView component
   - AudioService in scene

### Configuration
- Adjust grid size using the slider (2x2 to 6x6)
- Configure audio clips in AudioService
- Set card sprites in GameManager
- Customize constants in `GameConstants.cs`

### Game Flow
1. **Start New Game**: Creates a fresh game with selected grid size
2. **Resume Game**: Loads previous game state (if available)
3. **Gameplay**: Click cards to flip and match pairs
4. **Scoring**: Earn points for matches, combos, and speed
5. **Save**: Game auto-saves after each move

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/              # Interfaces
│   ├── Models/            # Data models
│   ├── Views/             # UI components
│   ├── Managers/          # Game logic
│   ├── Services/          # External services
│   └── Utils/             # Utilities
├── Images/                # Card images
├── Sounds/                # Audio clips
├── Prefab/                # Card prefab
└── Sprites/               # Card sprites
```

## 🔧 Technical Details

### Design Patterns
- **Singleton**: AudioService
- **Observer**: Event-driven card interactions
- **Service Locator**: Service injection
- **MVC-like**: Separation of Models, Views, and Controllers

### Performance Optimizations
- Object pooling ready (can be added)
- Efficient sprite preloading
- Optimized animation coroutines
- Minimal allocations in hot paths

### Save System
- JSON serialization using Unity's JsonUtility
- Persistent data path storage
- Version-safe data structure
- Automatic save on critical events

## 🚀 Future Enhancements

Potential improvements:
- Object pooling for cards
- Particle effects on matches
- Achievement system
- Leaderboard
- Multiple difficulty levels
- Theme customization

## 📝 Notes

- All code follows C# naming conventions
- Comprehensive XML documentation
- Error handling throughout
- No magic numbers (all constants defined)
- Clean, maintainable codebase

## 🎯 Requirements Met

✅ Unity 2021 LTS  
✅ Smooth animations  
✅ Continuous card flipping  
✅ Multiple layouts  
✅ Scaling cards  
✅ Save/Load system  
✅ Scoring mechanism  
✅ Combo system  
✅ Sound effects  
✅ No crashes/errors/warnings  
✅ Professional code quality  

---

**Built with professional software engineering practices**
