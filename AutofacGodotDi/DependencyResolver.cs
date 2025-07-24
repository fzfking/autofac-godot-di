using System.Collections.Generic;
using Autofac;
using Godot;

namespace AutofacGodotDi
{
    public abstract partial class DependencyResolver : Node
    {
        private SceneController _sceneController;
        private readonly ContainerBuilder _containerBuilder = new();

        private void InstallBindings()
        {
            BindSystemServices();
            var stack = new Stack<Node>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node is DependencyBinding dependencyBinding)
                {
                    dependencyBinding.InstallBindings(_containerBuilder);
                }

                foreach (var child in node.GetChildren())
                    stack.Push(child);
            }

            StaticDependencyInjector.SetupGlobalContainer(_containerBuilder);
        }

        public override void _EnterTree()
        {
            _sceneController = new SceneController(GetTree());
            InstallBindings();
        }

        public override void _Ready()
        {
            BindScene(GetTree().CurrentScene);
        }

        private void BindScene(Node node)
        {
            if (node is SceneContext sceneContext)
            {
                var lifetimeScope = StaticDependencyInjector.GlobalContainer.BeginLifetimeScope(b => sceneContext.InstallBindings(b));
                sceneContext.InstallScope(lifetimeScope);
            }
            else
            {
                StaticDependencyInjector.GlobalContainer.InjectDependencies(node);
            }
        }

        private void BindSystemServices()
        {
            _sceneController.OnChangeScene += BindScene;
            _containerBuilder
                .RegisterInstance(_sceneController)
                .SingleInstance()
                .AsImplementedInterfaces()
                .AsSelf();
        }

        protected override void Dispose(bool disposing)
        {
            _sceneController.OnChangeScene -= BindScene;
            StaticDependencyInjector.GlobalContainer.Dispose();
            base.Dispose(disposing);
        }
    }
}