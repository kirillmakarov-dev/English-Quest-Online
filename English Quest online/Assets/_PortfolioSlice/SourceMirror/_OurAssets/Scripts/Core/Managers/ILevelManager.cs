using Cysharp.Threading.Tasks;

public interface ILevelManager
{
    UniTask TransitionToSceneAsync(string sceneName);
    void ReturnToMenu();
}
