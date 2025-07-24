# Autofac Godot Di

DI (Dependency Injection) container for Godot 4.4 and more using Autofac with source generation for automatic method injection.

This library simplifies dependency injection in Godot projects by allowing you to mark methods with the `[Inject]` attribute. A NuGet source generator automatically creates the necessary binding code.

The core integration logic is consumed as a Git submodule, ensuring compatibility with the Godot editor.

## Features

*   **Autofac Integration:** Leverages the powerful Autofac DI container.
*   **Source Generation:** Automatically generates binding code for `[Inject]` methods at compile time, reducing boilerplate.
*   **Godot Node Integration:** Provides base classes (`DependencyBinding`, `DependencyResolver`, `SceneContext`) to easily integrate DI into the Godot node hierarchy.
*   **Scoped Lifetimes:** Supports Autofac's lifetime scopes, particularly useful for scenes via `SceneContext`.
*   **Scene Management Helper:** Includes `SceneController` for changing scenes while maintaining DI context.

## Installation

This library is split into two parts for optimal Godot compatibility:

1.  **Core Integration Logic:** Added as a Git submodule.
2.  **Source Generator:** Added as a NuGet package.

### Prerequisites

*   Godot 4.4 and more with C# support.

### Steps

1.  **Add the Core Integration as a Submodule:**
    *   Open your terminal in your Godot project's root directory.
    *   Run the following command to add the submodule:
        ```bash
        git submodule add https://github.com/fzfking/autofac-godot-di.git addons/AutofacGodotDi
        ```
    *   This will clone the repository into the `addons/AutofacGodotDi` folder within your project.

2.  **Add the Source Generator NuGet Package:**
    *   In your Godot's C# project add package `AutofacGodotDi`

3.  **(Optional) Build:**
    *   You might need to build your project once to ensure the submodule's `.dll` is correctly referenced and the source generator runs.

## Usage

### 1. Set up the Global Context

*   Create a class that inherits from `AutofacGodotDi.DependencyResolver`. This class will initialize the global Autofac container.
    ```csharp
    // GlobalDependencyResolver.cs
    using AutofacGodotDi;
    using Godot;

    public partial class GlobalDependencyResolver : DependencyResolver
    {
        // Override methods if needed for custom global setup,
        // but often no code is needed here for basic functionality.
    }
    ```
*   Create a new scene (e.g., `GlobalResolver.tscn`).
*   Set the root node of this scene to be of type `GlobalDependencyResolver` (or your derived class name).
*   Add this scene to your project's **AutoLoad** list:
    *   Go to `Project -> Project Settings -> Global -> Autoload` in the Godot editor.
    *   Select created scene at `Path`
    *   Click `Add`
    *   Ensure it's enabled.

### 2. Define Global Services if needed

*   Create a class that inherits from `AutofacGodotDi.DependencyBinding`. This class will define services available globally.
    ```csharp
    // GameServicesBinding.cs
    using Autofac;
    using AutofacGodotDi;
    using Godot;

    public partial class GameServicesBinding : DependencyBinding
    {
        public override void InstallBindings(ContainerBuilder builder)
        {
            // Register services as Singletons (one instance shared everywhere)
            builder.RegisterType<ScoreService>().As<IScoreService>().SingleInstance();
            builder.RegisterType<AudioManager>().AsSelf().SingleInstance();

            // Register services with default lifetime (new instance each time)
            builder.RegisterType<EnemyFactory>().As<IEnemyFactory>();
        }
    }
    ```
*   Add an instance of `GameServices` (or your derived class name) as a child node of your `GlobalDependencyResolver` node in the `GlobalResolver.tscn` scene.

### 3. Inject Dependencies into Nodes

*   In any Godot `Node` (including `Node2D`, `Node3D`, `Control`, etc.), add a method and mark it with the `[Inject]` attribute (from `AutofacGodotDi.Attributes`).
*   Define the method parameters to be the types you want to inject (services registered in step 2).
    ```csharp
    // Player.cs
    using AutofacGodotDi.Attributes;
    using Godot;

    public partial class Player : CharacterBody2D
    {
        private IScoreService _scoreService;
        private AudioManager _audioManager;

        // This method will be automatically called with resolved dependencies
        // after the node enters the scene tree and the container is set up.
        [Inject] // <-- Mark the method for injection
        public void Construct(IScoreService scoreService, AudioManager audioManager)
        {
            _scoreService = scoreService;
            _audioManager = audioManager;
        }

        public override void _Ready()
        {
            // Dependencies are injected before _Ready is called by the library.
            GD.Print($"Initial Score: {_scoreService.Score}");
        }

        public void OnEnemyKilled()
        {
            _scoreService.AddPoints(100);
            _audioManager.PlaySound("enemy_death");
        }
    }
    ```
    *   **Important:** The injection happens automatically after the node enters the scene tree *and* the scene is processed by the DI system (see Scene Context below).

### 4. Use Scene Scoping and Scene Changes

*   **Scene Context:** For each scene you intend to load as the `CurrentScene` (e.g., levels, menus), the **root node must inherit from `AutofacGodotDi.SceneContext`**.
    ```csharp
    // LevelOneContext.cs (root node of LevelOne.tscn)
    using Autofac;
    using AutofacGodotDi;
    using Godot;

    public partial class LevelOneContext : SceneContext
    {
        public override void InstallBindings(ContainerBuilder builder)
        {
            // Services registered here are specific to this scene's lifetime scope and its child nodes.
            // They are resolved from this scope first, falling back to the global scope if not found.
            builder.RegisterType<LevelOneSpawner>().As<ISpawner>().InstancePerLifetimeScope();
            builder.RegisterInstance(new LevelData { LevelName = "Level One" }).SingleInstance();
        }
    }
    ```
*   **Scene Changes:** To ensure `SceneContext` initialization and dependency injection work correctly, **always change scenes using the provided `ISceneController`**.
    *   Inject `ISceneController` into your nodes where scene changes are triggered.
    ```csharp
    // Menu.cs
    using AutofacGodotDi.Attributes;
    using Godot;

    public partial class Menu : Control
    {
        private ISceneController _sceneController;

        [Inject]
        public void Construct(ISceneController sceneController)
        {
            _sceneController = sceneController;
        }

        public void OnStartButtonPressed()
        {
            // Use the injected controller to change scenes.
            // This triggers the DI setup for the new scene.
            _sceneController.ChangeScene("res://Scenes/Levels/LevelOne.tscn");
            // Or if you have a PackedScene resource loaded:
            // _sceneController.ChangeScene(preloadedLevelOnePackedScene);
        }
    }
    ```

## Important Notes

*   **Injection Timing:** Dependency injection for a scene's nodes occurs when that scene becomes the `CurrentScene` *and* its root node is a `SceneContext`. This is automatically handled if you use `ISceneController.ChangeScene`.
*   **Runtime Object Creation:** If you need to create objects and resolve their dependencies at runtime (outside of scene loading), you will need to implement a factory pattern.
    *   You can inject `ILifetimeScope` (either the global one or a scene-specific one from `SceneContext.LifetimeScope`) into your factory.
    *   Use `StaticDependencyInjector.Inject` within the factory to create and get dependencies injected into the new object instance.
*   **Example Project:** For a complete example of how to structure and use this library, see the example project repository: [Tic-Tac-Toe](https://github.com/fzfking/TicTacToe) (Work in progress).
