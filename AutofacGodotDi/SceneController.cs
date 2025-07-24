using System;
using Godot;

namespace AutofacGodotDi
{
    public interface ISceneController
    {
        void ChangeScene(string filePath);
        void ChangeScene(PackedScene scene);
        Node GetCurrentScene();
    }

    public class SceneController : ISceneController
    {
        public event Action<Node> OnChangeScene;

        private readonly SceneTree _rootTree;

        public SceneController(SceneTree rootTree)
        {
            _rootTree = rootTree;
        }

        public void ChangeScene(string filePath)
        {
            Callable.From(() => ChangeSceneByPath(filePath)).CallDeferred();
        }

        public void ChangeScene(PackedScene scene)
        {
            Callable.From(() => ChangeSceneToPackedScene(scene)).CallDeferred();
        }

        public Node GetCurrentScene()
        {
            return _rootTree.CurrentScene;
        }

        private void ChangeSceneByPath(string filePath)
        {
            var sceneChangeResult = _rootTree.ChangeSceneToFile(filePath);
            if (sceneChangeResult == Error.Ok)
            {
                _rootTree
                    .ToSignal(_rootTree, SceneTree.SignalName.TreeChanged)
                    .OnCompleted(() => OnChangeScene?.Invoke(_rootTree.CurrentScene));
            }
        }

        private void ChangeSceneToPackedScene(PackedScene scene)
        {
            var sceneChangeResult = _rootTree.ChangeSceneToPacked(scene);
            if (sceneChangeResult == Error.Ok)
            {
                _rootTree
                    .ToSignal(_rootTree, SceneTree.SignalName.TreeChanged)
                    .OnCompleted(() => OnChangeScene?.Invoke(_rootTree.CurrentScene));
            }
        }
    }
}