using UnityEngine;

public class CurveParamDefaultsSMB : StateMachineBehaviour
{

    [Header("When to apply")]
    public bool resetOnStateMachineEnter = false;
    public bool resetOnStateMachineExit = true;   // NOTE: should be work best to clean up on leaving,  but nice to have options with one script


    [System.Serializable]
    public struct FloatDefault
    {
        public bool enabled;
        public string paramName;
        public float value;
    }

    [Header("Defaults to apply")]
    public FloatDefault[] defaults =
    {
        // NOTE: More control over what parameters we wan't to be effected, easier to add to script in future and exlude
        //       resetting only some parameters in special ocations wwithout more code change.
        //
        //       Below is the default set of reset rules, can be toggled in inspector to change per SM:  
        new FloatDefault { enabled = true,  paramName = "AttackMomentum", value = 0f },
        new FloatDefault { enabled = true,  paramName = "AttackSteerDeg", value = 0f },
        new FloatDefault { enabled = true,  paramName = "ComboWindow",    value = 0f },
        new FloatDefault { enabled = true,  paramName = "ExitAllowed",    value = 0f },

        // NOTE: unsure if PlayRate should be included, or if resetting every state enter can fight with intended blending/authoring 
        new FloatDefault { enabled = false, paramName = "PlayRate",       value = 1f },
    };


    // Cache hashes outside the serialized defaults[] to avoid editing AnimatorController asset at runtime
    [System.NonSerialized] private int[] _hashes;
    [System.NonSerialized] private int _cachedLen;


    void EnsureCache()
    {
        if (defaults == null) return;

        if (_hashes == null || _cachedLen != defaults.Length)
        {
            _cachedLen = defaults.Length;
            _hashes = new int[_cachedLen];

            for (int idx = 0; idx < _cachedLen; idx++)
            {
                string name = defaults[idx].paramName;
                _hashes[idx] = string.IsNullOrEmpty(name) ? 0 : Animator.StringToHash(name);
            }
        }
    }


    void Apply(Animator animator)
    {
        if (defaults == null) return;
        EnsureCache();

        for (int idx = 0; idx < defaults.Length; idx++)
        {
            // skips entries where enabled == false  /or empty names
            if (!defaults[idx].enabled) continue;
            if (string.IsNullOrEmpty(defaults[idx].paramName)) continue;

            int hash = _hashes[idx];
            animator.SetFloat(hash, defaults[idx].value);
        }
    }


#if UNITY_EDITOR
    // Makes editing during play / inspector changes less confusing
    private void OnValidate()
    {
        _hashes = null;
        _cachedLen = 0;
    }
#endif

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (resetOnStateMachineEnter) Apply(animator);
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (resetOnStateMachineExit) Apply(animator);
    }


}
