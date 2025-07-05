using Autofac;
using Godot;

namespace AutofacGodotDi;

public static class StaticDependencyInjector
{
    public static readonly Dictionary<Type, Action<Node, ILifetimeScope>> RegisteredBindings = new();
    public static IContainer GlobalContainer { get; private set; }

    public static void SetupGlobalContainer(ContainerBuilder builder)
    {
        GlobalContainer = builder.Build();
        BindingRegistrar.RegisterBindings();
    }

    public static void Inject(this Node node, ILifetimeScope container)
    {
        if (RegisteredBindings.ContainsKey(node.GetType())) 
            RegisteredBindings[node.GetType()].Invoke(node, container);
    }
}