using UnityEngine;

namespace NPC
{
    public class NPCController : MonoBehaviour
    {
        [Header("NPC Data")]
        public string npcName;
        public NPCType npcType;

        [Header("View Reference")]
        [SerializeField] private UI_NameTagView _tagNameView;

        private void Start()
        {
            InitializeHeadTag();
        }

        private void InitializeHeadTag()
        {
            if (_tagNameView != null)
            {
                _tagNameView.SetName(npcName);
            }
        }
    }
}
