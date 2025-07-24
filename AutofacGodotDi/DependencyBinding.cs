using Autofac;
using Godot;

namespace AutofacGodotDi
{
    public abstract partial class DependencyBinding : Node
    {
        public ILifetimeScope LifetimeScope { get; protected set; }

        public abstract void InstallBindings(ContainerBuilder builder);

        public virtual void InstallScope(ILifetimeScope scope)
        {
            LifetimeScope = scope;
        }

        protected override void Dispose(bool disposing)
        {
            LifetimeScope?.Dispose();
            base.Dispose(disposing);
        }
    }
}