using System;
using System.Linq;
using UnityEngine;

namespace AdsExtensions
{
    [Serializable]
    public class Placement
    {
        public string placement;
        public AdType type;

        [SerializeField] bool isOpen;
        public bool IsOpen => isOpen;

        public DateTime lastShow;
    }
}
