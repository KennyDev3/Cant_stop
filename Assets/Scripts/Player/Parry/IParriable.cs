using UnityEngine;

namespace StarterAssets
{
    public interface IParriable
    {
        void OnParried(Vector3 parrySourcePosition);
    }
}