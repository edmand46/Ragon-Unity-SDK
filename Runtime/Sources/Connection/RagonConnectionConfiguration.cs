using JetBrains.Annotations;
using UnityEngine;

namespace Ragon.Client
{
    [CreateAssetMenu()]
    public class RagonConnectionConfiguration: ScriptableObject
    {
        public RagonSocketType Type;
        public string Address;
        public string Protocol;
        public ushort Port;
        
        
        [CanBeNull] public RagonConnectionConfiguration Fallback;
    }
}