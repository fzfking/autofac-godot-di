using System.Collections.Generic;
using Autofac;
using Godot;

namespace AutofacGodotDi
{
    public abstract partial class SceneContext : DependencyBinding
    {
        public override void InstallScope(ILifetimeScope scope)
        {
            base.InstallScope(scope);
            InjectDependencies();
        }

        private void InjectDependencies()
        {
            LifetimeScope.InjectDependencies(this);
        }
    }
}