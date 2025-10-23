using UnityEngine;

namespace CompanionAI
{
    /// <summary>
    /// Handles all companion visual effects - glow effects, combat indicators, status displays
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CompanionVisualEffects : MonoBehaviour
    {
        [Header("Combat Visual Effects")]
        public GameObject followingGlowEffectPrefab;
        public GameObject combatGlowEffectPrefab;
        public GameObject deathEffectPrefab;
        
        [Header("Effect Settings")]
        public bool enableVisualEffects = true;
        public float effectTransitionSpeed = 2f;
        
        private CompanionCore _core;
        private Transform _transform;
        
        // Active effects
        private GameObject _followingGlowEffect;
        private GameObject _combatGlowEffect;
        private GameObject _currentActiveEffect;
        
        public void Initialize(CompanionCore core)
        {
            _core = core;
            _transform = transform;
            
            if (enableVisualEffects)
            {
                InitializeEffects();
            }
        }
        
        private void InitializeEffects()
        {
            // Create following glow effect
            if (followingGlowEffectPrefab != null)
            {
                _followingGlowEffect = Instantiate(followingGlowEffectPrefab, _transform);
                _followingGlowEffect.SetActive(false);
            }
            
            // Create combat glow effect
            if (combatGlowEffectPrefab != null)
            {
                _combatGlowEffect = Instantiate(combatGlowEffectPrefab, _transform);
                _combatGlowEffect.SetActive(false);
            }
            
            // Start with following effect
            ShowFollowingEffects();
        }
        
        public void ShowFollowingEffects()
        {
            if (!enableVisualEffects) return;
            
            SwitchToEffect(_followingGlowEffect);
        }
        
        public void ShowCombatEffects()
        {
            if (!enableVisualEffects) return;
            
            SwitchToEffect(_combatGlowEffect);
        }
        
        public void ShowDeathEffects()
        {
            if (!enableVisualEffects) return;
            
            // Disable all glow effects
            SwitchToEffect(null);
            
            // Show death effect if available
            if (deathEffectPrefab != null)
            {
                GameObject deathEffect = Instantiate(deathEffectPrefab, _transform.position, _transform.rotation);
                
                // Auto-destroy death effect after a delay
                Destroy(deathEffect, 5f);
            }
        }
        
        private void SwitchToEffect(GameObject newEffect)
        {
            // Disable current effect
            if (_currentActiveEffect != null)
            {
                _currentActiveEffect.SetActive(false);
            }
            
            // Enable new effect
            _currentActiveEffect = newEffect;
            if (_currentActiveEffect != null)
            {
                _currentActiveEffect.SetActive(true);
            }
        }
        
        public void SetEffectsEnabled(bool enabled)
        {
            enableVisualEffects = enabled;
            
            if (!enabled)
            {
                SwitchToEffect(null);
            }
            else
            {
                // Restore appropriate effect based on current state
                switch (_core.CurrentState)
                {
                    case CompanionCore.CompanionState.Following:
                        ShowFollowingEffects();
                        break;
                    case CompanionCore.CompanionState.Engaging:
                    case CompanionCore.CompanionState.Attacking:
                        ShowCombatEffects();
                        break;
                    case CompanionCore.CompanionState.Dead:
                        ShowDeathEffects();
                        break;
                }
            }
        }
        
        public void UpdateEffectIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            
            // Update intensity of active effects
            if (_currentActiveEffect != null)
            {
                // Try to find particle systems and adjust their emission
                ParticleSystem[] particles = _currentActiveEffect.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particles)
                {
                    var emission = ps.emission;
                    emission.rateOverTime = emission.rateOverTime.constant * intensity;
                }
                
                // Try to find lights and adjust their intensity
                Light[] lights = _currentActiveEffect.GetComponentsInChildren<Light>();
                foreach (Light light in lights)
                {
                    light.intensity *= intensity;
                }
            }
        }
        
        public void Cleanup()
        {
            if (_followingGlowEffect != null)
            {
                Destroy(_followingGlowEffect);
            }
            
            if (_combatGlowEffect != null)
            {
                Destroy(_combatGlowEffect);
            }
            
            _currentActiveEffect = null;
        }
    }
}