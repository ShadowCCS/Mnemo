<div align="center">
  
![Mnemo Logo](./gitAssets/Mnemo.svg)

**A free and open-source flashcard and spaced repetition application to enhance your learning journey**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-0.3.0-green.svg)](https://github.com/ShadowCCS/mnemo/releases)
![Framework](https://img.shields.io/badge/framework-.NET%208.0-purple.svg)
![UI](https://img.shields.io/badge/UI-Avalonia%2011-orange.svg)

![Linux](https://img.shields.io/badge/-Linux-333?logo=linux&logoColor=white)
![macOS](https://img.shields.io/badge/-macOS-333?logo=apple&logoColor=white)
![Windows](https://img.shields.io/badge/-Windows-333?logo=windows&logoColor=white)


</div>

## 📚 Overview

Mnemo is a powerful, intuitive study application designed to help students, researchers, and lifelong learners optimize their educational journey. Built with Avalonia UI for cross-platform compatibility, it provides customizable learning paths, interactive flashcards, comprehensive note-taking capabilities, and detailed progress tracking to help you master any subject.

![Mnemo Dashboard](./gitAssets/dashboard.png)

| Unit Overview | Theory Section | Question Preview |
|:-------------:|:--------------:|:----------------:|
| <img src="./gitAssets/learningpath_unitoverview.png" width="300"/> | <img src="./gitAssets/learningpath_theory.png" width="300"/> | <img src="./gitAssets/question.png" width="300"/> |


## 👀 Features

###  Structured Learning Paths
- Create customized learning journeys for any subject
- Organize content into logical units with theory and practice sections
- Track completion and progress through each learning module

###  Comprehensive Study Tools
- **Interactive Flashcards**: Create and practice with multiple study modes (Spaced Repetition, Batch, Endless)
- **Detailed Notes**: Take and organize rich-text notes with markdown support
- **Custom Quizzes**: Test your knowledge with various question types

###  Advanced Progress Tracking
- Monitor study time and session history
- Track retention rates across subjects
- Visualize learning progress with detailed statistics

###  Customization & Settings
- Personalize themes and layout
- Configure data storage and backup options
- Tailor the application to your unique learning style

## 📷 More Screenshots
<div align="center">
  <img src="./gitAssets/learningpath.png" alt="Learning Path Overview" width="400"/>
  <img src="./gitAssets/learningpath_generateUnit.png" alt="Creating a Unit" width="400"/>
  <img src="./gitAssets/settings.png" alt="Settings" width="400"/>
</div>

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer
- Dependencies (automatically handled by the package manager):
  - Avalonia UI 11.2+
  - Avalonia.Svg.Skia
  - Entity Framework Core (SQLite provider)
  - CommunityToolkit.Mvvm
  - Markdown.Avalonia

### Installation

```bash
# Clone the repository
git clone https://github.com/ShadowCCS/mnemo.git

# Navigate to the project directory
cd mnemo

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

### Building from Source

```bash
# Build for your current platform
dotnet publish -c Release

# Build for specific platforms
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
```

## 📋 Usage Guide

### Creating a Learning Path
1. Navigate to the Learning Path section from the sidebar
2. Click "New Learning Path"
3. Define your units, topics, and learning objectives
4. Add theory content and practice questions
5. Follow your progress with the built-in tracking system

### Working with Flashcards
1. Go to the Flashcards section
2. Create a new deck or select an existing one
3. Add cards with questions, answers, and optional hints
4. Choose your study mode:
   - Spaced Repetition for long-term retention
   - Batch mode for focused study sessions
   - Endless mode for continuous practice

### Quiz and Assessment
1. Create custom quizzes from your learning materials
2. Include various question types (multiple choice, short answer, etc.)
3. Review results and identify areas for improvement

## 🛠️ Technical Architecture

Mnemo is built using:
- **Language**: C# with .NET 8.0
- **UI Framework**: Avalonia UI 11 (cross-platform UI framework)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Data Storage**: Entity Framework Core with SQLite
- **Graphics**: Avalonia.Svg.Skia for vector graphics support
- **Packages**: CommunityToolkit.Mvvm for MVVM implementation, Markdown.Avalonia for rich text

## 🤝 Contributing

Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

Distributed under the MIT License. See [LICENSE](LICENSE) for more information.

## 👨‍💻 Developers

- [@ShadowCCS](https://github.com/ShadowCCS) - Creator and Main Developer

## 🙏 Acknowledgments

- Thanks to [Avalonia UI](https://avaloniaui.net/) for making cross-platform UI development possible
- Inspired by modern learning science and spaced repetition research
- Special thanks to all contributors and early testers

---

<div align="center">
  
**Made with ❤️ for learners everywhere**

</div>