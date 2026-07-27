namespace EnglishQuest.UI.Loading
{
    /// <summary>
    /// Interface for scene loading implementations.
    /// Dependency Inversion: High-level modules depend on abstractions.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// Loads a scene by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        void LoadScene(string sceneName);

        /// <summary>
        /// Loads a scene by build index.
        /// </summary>
        /// <param name="sceneIndex">The build index of the scene to load.</param>
        void LoadScene(int sceneIndex);

        /// <summary>
        /// Whether a scene is currently being loaded.
        /// </summary>
        bool IsLoading { get; }
    }
}

