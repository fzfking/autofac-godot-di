using System;
using Godot;

namespace AutofacGodotDi
{
    public interface ISceneController
    {
        void ChangeScene(string filePath, Action onFinish = null);
        void ChangeScene(PackedScene scene, Action onFinish = null);
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

        public void ChangeScene(string filePath, Action onFinish = null)
        {
            Callable.From(() => ChangeSceneByPath(filePath, onFinish)).CallDeferred();
        }

        public void ChangeScene(PackedScene scene, Action onFinish = null)
        {
            Callable.From(() => ChangeSceneToPackedScene(scene, onFinish)).CallDeferred();
        }

        public Node GetCurrentScene()
        {
            return _rootTree.CurrentScene;
        }

        private void ChangeSceneByPath(string filePath, Action onFinish = null)
        {
            var sceneChangeResult = _rootTree.ChangeSceneToFile(filePath);
            if (sceneChangeResult == Error.Ok)
            {
                _rootTree
                    .ToSignal(_rootTree, SceneTree.SignalName.NodeAdded)
                    .OnCompleted(() =>
                    {
                        OnChangeScene?.Invoke(_rootTree.CurrentScene);
                        onFinish?.Invoke();
                    });
            }
        }

        private void ChangeSceneToPackedScene(PackedScene scene, Action onFinish = null)
        {
            var sceneChangeResult = _rootTree.ChangeSceneToPacked(scene);
            if (sceneChangeResult == Error.Ok)
            {
                _rootTree
                    .ToSignal(_rootTree, SceneTree.SignalName.TreeChanged)
                    .OnCompleted(() =>
                    {
                        OnChangeScene?.Invoke(_rootTree.CurrentScene);
                        onFinish?.Invoke();
                    });
            }
        }
    }
}