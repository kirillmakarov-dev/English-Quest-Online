using TMPro;
using UnityEngine;


    public class UI_NameTagView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;

        public void SetName(string name)
        {
            if (_nameText != null)
            {
                _nameText.text = name;
                _nameText.isRightToLeftText = IsHebrew(name);
            }
        }

        private bool IsHebrew(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (var c in text)
            {
                // check if character is in Hebrew Unicode block
                if (c >= 0x0590 && c <= 0x05FF)
                    return true;
            }
            return false;
        }
    }
