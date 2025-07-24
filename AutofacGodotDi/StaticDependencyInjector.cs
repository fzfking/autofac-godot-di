using System;
using System.Collections.Generic;
using Autofac;
using Godot;

namespace AutofacGodotDi
{
    public static class StaticDependencyInjector
    {
        public static readonly Dictionary<Type, Action<Node, ILifetimeScope>> RegisteredBindings = new();
        public static IContainer GlobalContainer { get; private set; }
        private static Action _bindingAction = null;

        public static void SetupGlobalContainer(ContainerBuilder builder)
        {
            GlobalContainer = builder.Build();
            _bindingAction?.Invoke();
        }

        public static void Inject(this Node node, ILifetimeScope container)
        {
            if (RegisteredBindings.ContainsKey(node.GetType())) 
                RegisteredBindings[node.GetType()].Invoke(node, container);
        }

        public static void SetRegisterBindingsAction(Action bindingAction)
        {
            _bindingAction = bindingAction;
        }
    }
}