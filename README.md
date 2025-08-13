# Unity Top-Down Vision

**Groza Games Top-Down Vision** is a powerful raycast-based solution for implementing line of sight visualization in top-down games. Create realistic vision cones and fog of war effects with this Unity package.

![Unity Version](https://img.shields.io/badge/Unity-6000.0+-blue.svg)
![URP Compatible](https://img.shields.io/badge/URP-Compatible-green.svg)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

![Image Sequence_001_0000](https://github.com/user-attachments/assets/5e027748-ecf0-465b-b1d3-4edf9f9c4882)


## ✨ Features

- **Real-time Line of Sight**: Dynamic vision calculation using raycast technology
- **URP Integration**: Seamless integration with Unity's Universal Render Pipeline
- **Customizable Vision Effects**: Multiple render features for different visual styles
- **Performance Optimized**: Efficient rendering pipeline designed for top-down games
- **Easy Setup**: Simple component-based architecture for quick implementation

## 🚀 Getting Started

### Prerequisites

- Unity 6000.0 or later
- Universal Render Pipeline (URP)

### Installation

1. Clone or download this repository
2. Copy the package contents to your Unity project's `Packages` folder
3. Or add via Unity Package Manager using the Git URL:
   ```
   https://github.com/aleverdes/unity-top-down-vision.git
   ```

## 🔧 Setup Instructions

### 1. Camera Setup

1. **Create Vision Camera**:
   - Create a new camera as a child of your main camera (perspective or orthographic)
   - Name it `VisionCamera`
   - Attach the `VisionCamera` script component

2. **Setup Target Camera**:
   - Add the `VisionTargetCamera` script to your main game camera

### 2. Character Setup

1. **Add Vision Origin**:
   - Attach the `VisionOrigin` script to your main character/player
   - This component acts as the source of the line of sight vision

### 3. URP Renderer Settings

Configure your URP Renderer asset by adding the following render features in order:

1. **Vision Black World Render Feature**
2. **Vision Effect Render Feature** 
3. **Vision Blur Render Feature**

> **Note**: The order of render features is important for proper visual output.

## 📁 Package Structure

```
├── Materials/           # Material assets for vision effects
├── Scripts/
│   ├── MonoBehaviours/  # Core vision components
│   └── URP/
│       ├── MonoBehaviours/      # URP-specific camera components
│       └── RenderFeatures/     # Custom URP render features
└── Shaders/            # Vision effect shaders and shader graphs
```

## 🎮 Usage

Once setup is complete, the vision system will automatically:
- Calculate line of sight from the character's position
- Render vision effects based on the configured render features
- Update vision dynamically as the character moves

## 🧪 Compatibility

- **Tested on**: Unity 6000.0+
- **Render Pipeline**: Universal Render Pipeline (URP) only
- **Platforms**: All platforms supported by Unity and URP

## 🐛 Support & Issues

If you encounter any issues or need support:

1. Check the [Issues](https://github.com/aleverdes/unity-top-down-vision/issues) page for existing reports
2. Create a new issue with detailed information about your problem
3. Include Unity version, URP version, and error logs if applicable

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for:
- Bug fixes
- Feature improvements
- Documentation updates
- Performance optimizations

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏆 Credits

Developed by **Groza Games**

---

**Good luck with your top-down vision implementation!** 🎯
