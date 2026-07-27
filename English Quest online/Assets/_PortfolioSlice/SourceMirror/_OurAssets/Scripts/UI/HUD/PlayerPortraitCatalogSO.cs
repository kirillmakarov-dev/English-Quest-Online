using UnityEngine;

namespace EnglishQuest.UI.HUD
{
    /// <summary>
    /// Maps <see cref="CoreSaveData.SelectedAvatarIndex"/> to HUD portrait textures.
    /// Assign portraits in order; index 0 is the default.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerPortraitCatalog", menuName = ScriptableObjectMenuPaths.UI + "/Player Portrait Catalog")]
    public class PlayerPortraitCatalogSO : ScriptableObject
    {
        [SerializeField] private Texture[] _portraits;

        public Texture GetPortrait(int index)
        {
            if (_portraits == null || _portraits.Length == 0)
                return null;

            if (index < 0 || index >= _portraits.Length)
                return _portraits[0];

            return _portraits[index] != null ? _portraits[index] : _portraits[0];
        }
    }
}

