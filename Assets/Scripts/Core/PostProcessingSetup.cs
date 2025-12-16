using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pipe.Core
{
    public class PostProcessingSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // unique check
            var existing = FindObjectOfType<Volume>();
            if (existing != null && existing.isGlobal) return;

            GameObject obj = new GameObject("Global Volume");
            Volume vol = obj.AddComponent<Volume>();
            vol.isGlobal = true;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            
            // Add Bloom for Neon Glow
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.0f); 
            bloom.threshold.Override(3.0f); // Even Higher threshold to isolate highest highlights
            bloom.scatter.Override(0.15f); // Very tight glow
            bloom.tint.Override(Color.white);
            
            // Add Tonemapping for better color range
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            
            // Add Vignette for focus
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);

            vol.profile = profile;
            DontDestroyOnLoad(obj);
            
            Debug.Log("Global Volume (Bloom/ACES) initialized via Script.");
        }
    }
}
