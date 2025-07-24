using System.Collections.Generic;
using Autofac;
using Autofac.Core.Lifetime;
using Godot;

namespace AutofacGodotDi
{
    public static class InjectionExtensions
    {
        public static void InjectDependencies(this ILifetimeScope builder, Node root)
        {
            var stack = new Stack<Node>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                current.Inject(builder);

                foreach (var child in current.GetChildren())
                {
                    if (child is SceneContext childContext)
                    {
                        var childScope = builder.BeginLifetimeScope(b => childContext.InstallBindings(b));
                        childContext.InstallScope(childScope);
                    }
                    else
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }
}