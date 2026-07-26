using System;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    [Serializable]
    public class LetterData
    {
        [SerializeField] private string id;
        [SerializeField] private string value;

        public string Id => id;
        public string Value => value;

        public string Character => value;
    }
}
