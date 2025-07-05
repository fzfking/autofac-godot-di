using Autofac;
using Godot;

namespace AutofacGodotDi;

public abstract partial class SceneContext : DependencyBinding
{
    public override void InstallScope(ILifetimeScope scope)
    {
        base.InstallScope(scope);
        InjectDependencies();
    }

    private void InjectDependencies()
    {
        var stack = new Stack<Node>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            current.Inject(LifetimeScope);

            foreach (var child in current.GetChildren())
            {
                if (child is SceneContext childContext)
                {
                    var childScope = LifetimeScope.BeginLifetimeScope(b => childContext.InstallBindings(b));
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