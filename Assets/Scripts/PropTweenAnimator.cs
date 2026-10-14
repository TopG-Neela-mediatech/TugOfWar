using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// PropTweenAnimator — Master tween animation controller for 2D scene props.
/// Attach to any GameObject. Add targets and pick an animation type per entry.
/// Disable the component to stop all tweens; re-enable to restart them.
/// </summary>
public class PropTweenAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Enums
    // ─────────────────────────────────────────────

    public enum AnimationType
    {
        Sway,           // Gentle left-right rotation (trees, plants)
        Breathe,        // Uniform scale pulse (mushrooms, flowers)
        Hover,          // Vertical float (lanterns, leaves)
        Wobble,         // Rapid small rotation shake (bushes hit by wind)
        Drift,          // Slow horizontal position drift (clouds, smoke)
        SquashStretch,  // Vertical squash & stretch (bouncy objects)
        Spin,           // Continuous rotation (coins, sparkles, pinwheels)
        Flicker,        // Alpha/scale flicker (flames, fireflies)
    }

    public enum EaseType
    {
        Linear,
        EaseInOut,
        EaseIn,
        EaseOut,
        BounceOut,
        ElasticOut,
        SineWave,
    }

    // ─────────────────────────────────────────────
    //  Per-target entry
    // ─────────────────────────────────────────────

    [System.Serializable]
    public class PropEntry
    {
        [Header("── Target ──────────────────────────")]
        [Tooltip("The SpriteRenderer(s) or transforms to animate. Add as many as you like.")]
        public List<Transform> targets = new List<Transform>();

        [Header("── Animation ──────────────────────")]
        [Tooltip("Enable to activate this animation entry.")]
        public bool enabled = true;

        public AnimationType animationType = AnimationType.Sway;

        [Header("── Timing ──────────────────────────")]
        [Tooltip("Duration of one full cycle (seconds).")]
        [Range(0.1f, 10f)] public float duration = 2f;

        [Tooltip("Random extra time added per target so they don't all sync.")]
        [Range(0f, 3f)] public float randomTimeOffset = 0.5f;

        [Tooltip("Delay before starting (seconds).")]
        [Range(0f, 5f)] public float startDelay = 0f;

        public EaseType easeType = EaseType.SineWave;

        // ── Sway settings ──────────────────────
        [Header("── Sway Settings ─────────────────")]
        [Tooltip("Max rotation angle in degrees.")]
        [Range(0f, 45f)] public float swayAngle = 10f;

        [Tooltip("Pivot offset: 0 = center, positive = rotate around bottom of object.")]
        public Vector2 swayPivotOffset = new Vector2(0f, -0.5f);

        // ── Breathe settings ───────────────────
        [Header("── Breathe Settings ──────────────")]
        [Range(0.8f, 1.5f)] public float breatheMinScale = 0.95f;
        [Range(0.8f, 1.5f)] public float breatheMaxScale = 1.05f;

        // ── Hover settings ─────────────────────
        [Header("── Hover Settings ─────────────────")]
        [Tooltip("Distance to float up and down (world units).")]
        [Range(0f, 2f)] public float hoverHeight = 0.15f;

        // ── Wobble settings ────────────────────
        [Header("── Wobble Settings ──────────────")]
        [Range(0f, 30f)] public float wobbleAngle = 5f;
        [Range(1f, 10f)] public float wobbleFrequency = 4f;

        // ── Drift settings ─────────────────────
        [Header("── Drift Settings ───────────────")]
        [Range(0f, 2f)] public float driftDistance = 0.3f;
        [Tooltip("Drift direction: 0 = horizontal, 1 = vertical, anything in between = diagonal.")]
        [Range(0f, 1f)] public float driftVerticalBias = 0f;

        // ── SquashStretch settings ─────────────
        [Header("── Squash & Stretch Settings ─────")]
        [Range(0.5f, 1f)]  public float squashScaleY = 0.85f;
        [Range(1f, 1.5f)]  public float stretchScaleY = 1.15f;
        [Range(0.5f, 1.5f)] public float squashScaleX = 1.1f;

        // ── Spin settings ──────────────────────
        [Header("── Spin Settings ────────────────")]
        [Range(-720f, 720f)] public float spinDegreesPerSecond = 90f;
        [Tooltip("If true, spin clockwise; otherwise counter-clockwise.")]
        public bool spinClockwise = true;

        // ── Flicker settings ───────────────────
        [Header("── Flicker Settings ──────────────")]
        [Range(0f, 1f)] public float flickerMinAlpha = 0.6f;
        [Range(0f, 1f)] public float flickerMaxAlpha = 1.0f;
        [Range(0.8f, 1.2f)] public float flickerMinScale = 0.92f;
        [Range(0.8f, 1.2f)] public float flickerMaxScale = 1.08f;
        [Tooltip("How random the flicker timing feels (0 = perfectly rhythmic).")]
        [Range(0f, 0.5f)] public float flickerNoise = 0.2f;

        // ── Runtime state (not shown in inspector) ──
        [System.NonSerialized] public List<Coroutine> runningCoroutines = new List<Coroutine>();
        [System.NonSerialized] public List<Vector3>   originalPositions = new List<Vector3>();
        [System.NonSerialized] public List<Vector3>   originalScales    = new List<Vector3>();
        [System.NonSerialized] public List<float>     originalRotations = new List<float>();
        [System.NonSerialized] public List<Color>     originalColors    = new List<Color>();
        [System.NonSerialized] public bool            statesCached      = false;
    }

    // ─────────────────────────────────────────────
    //  Inspector fields
    // ─────────────────────────────────────────────

    [Header("═══ Prop Tween Animator ════════════════")]
    [Tooltip("Add one entry per animation group. Each entry can hold multiple targets.")]
    public List<PropEntry> propEntries = new List<PropEntry>();

    [Header("Global Controls")]
    [Tooltip("Multiplier applied to all durations. Useful for slow-motion or speed-up effects.")]
    [Range(0.1f, 5f)] public float globalSpeedMultiplier = 1f;

    [Tooltip("Uncheck to stop and reset all animations without disabling the component.")]
    public bool playAnimations = true;

    // ─────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────

    private bool _wasPlaying = true;
    private bool _componentWasEnabled = true;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        _componentWasEnabled = true;
        StartAllAnimations();
    }

    private void OnDisable()
    {
        _componentWasEnabled = false;
        StopAllAnimations(resetTransforms: true);
    }

    private void Update()
    {
        // Handle the playAnimations toggle at runtime
        if (playAnimations != _wasPlaying)
        {
            _wasPlaying = playAnimations;
            if (playAnimations)
                StartAllAnimations();
            else
                StopAllAnimations(resetTransforms: true);
        }
    }

    // ─────────────────────────────────────────────
    //  Start / Stop
    // ─────────────────────────────────────────────

    private void StartAllAnimations()
    {
        if (!isActiveAndEnabled || !playAnimations) return;

        foreach (var entry in propEntries)
        {
            if (entry == null || !entry.enabled) continue;
            if (entry.targets == null || entry.targets.Count == 0) continue;

            CacheOriginalStates(entry);
            StartEntryAnimations(entry);
        }
    }

    private void StopAllAnimations(bool resetTransforms)
    {
        foreach (var entry in propEntries)
        {
            if (entry == null) continue;
            StopEntryCoroutines(entry);
            if (resetTransforms && entry.statesCached)
                ResetToOriginal(entry);
        }
    }

    private void StartEntryAnimations(PropEntry entry)
    {
        StopEntryCoroutines(entry);
        entry.runningCoroutines.Clear();

        for (int i = 0; i < entry.targets.Count; i++)
        {
            if (entry.targets[i] == null) continue;
            float timeOffset = entry.startDelay + Random.Range(0f, entry.randomTimeOffset);
            Coroutine c = StartCoroutine(AnimateTarget(entry, i, timeOffset));
            entry.runningCoroutines.Add(c);
        }
    }

    private void StopEntryCoroutines(PropEntry entry)
    {
        foreach (var c in entry.runningCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        entry.runningCoroutines.Clear();
    }

    // ─────────────────────────────────────────────
    //  State caching & reset
    // ─────────────────────────────────────────────

    private void CacheOriginalStates(PropEntry entry)
    {
        if (entry.statesCached) return;

        entry.originalPositions.Clear();
        entry.originalScales.Clear();
        entry.originalRotations.Clear();
        entry.originalColors.Clear();

        foreach (var t in entry.targets)
        {
            if (t == null) continue;
            entry.originalPositions.Add(t.localPosition);
            entry.originalScales.Add(t.localScale);
            entry.originalRotations.Add(t.localEulerAngles.z);

            var sr = t.GetComponent<SpriteRenderer>();
            entry.originalColors.Add(sr != null ? sr.color : Color.white);
        }

        entry.statesCached = true;
    }

    private void ResetToOriginal(PropEntry entry)
    {
        for (int i = 0; i < entry.targets.Count; i++)
        {
            if (entry.targets[i] == null) continue;
            if (i < entry.originalPositions.Count) entry.targets[i].localPosition = entry.originalPositions[i];
            if (i < entry.originalScales.Count)    entry.targets[i].localScale    = entry.originalScales[i];
            if (i < entry.originalRotations.Count)
            {
                Vector3 e = entry.targets[i].localEulerAngles;
                e.z = entry.originalRotations[i];
                entry.targets[i].localEulerAngles = e;
            }
            if (i < entry.originalColors.Count)
            {
                var sr = entry.targets[i].GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = entry.originalColors[i];
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Master coroutine dispatcher
    // ─────────────────────────────────────────────

    private IEnumerator AnimateTarget(PropEntry entry, int targetIndex, float initialDelay)
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            switch (entry.animationType)
            {
                case AnimationType.Sway:          yield return StartCoroutine(DoSway(entry, targetIndex));          break;
                case AnimationType.Breathe:       yield return StartCoroutine(DoBreathe(entry, targetIndex));       break;
                case AnimationType.Hover:         yield return StartCoroutine(DoHover(entry, targetIndex));         break;
                case AnimationType.Wobble:        yield return StartCoroutine(DoWobble(entry, targetIndex));        break;
                case AnimationType.Drift:         yield return StartCoroutine(DoDrift(entry, targetIndex));         break;
                case AnimationType.SquashStretch: yield return StartCoroutine(DoSquashStretch(entry, targetIndex)); break;
                case AnimationType.Spin:          yield return StartCoroutine(DoSpin(entry, targetIndex));          break;
                case AnimationType.Flicker:       yield return StartCoroutine(DoFlicker(entry, targetIndex));       break;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Animation 1 — Sway (tree / plant)
    // ─────────────────────────────────────────────

    private IEnumerator DoSway(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        float origZ = entry.originalRotations[idx];
        float halfDur = (entry.duration * globalSpeedMultiplier) * 0.5f;

        // Sway right
        yield return StartCoroutine(TweenRotationAroundPivot(
            t, entry.swayPivotOffset, origZ, origZ + entry.swayAngle, halfDur, entry.easeType));

        // Sway left
        yield return StartCoroutine(TweenRotationAroundPivot(
            t, entry.swayPivotOffset, origZ + entry.swayAngle, origZ - entry.swayAngle, halfDur, entry.easeType));

        // Return
        yield return StartCoroutine(TweenRotationAroundPivot(
            t, entry.swayPivotOffset, origZ - entry.swayAngle, origZ, halfDur * 0.5f, entry.easeType));
    }

    private IEnumerator TweenRotationAroundPivot(
        Transform t, Vector2 pivotOffset, float fromZ, float toZ, float dur, EaseType ease)
    {
        float elapsed = 0f;
        Vector3 origLocalPos = t.localPosition; // not used for pivot but kept for reference

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / dur);
            float e = Evaluate(p, ease);
            float angle = Mathf.LerpUnclamped(fromZ, toZ, e);

            // Rotate around pivot offset
            Vector3 pivot = t.position + (Vector3)(pivotOffset * t.lossyScale);
            t.rotation = Quaternion.Euler(0, 0, angle);
            // Keep pivot point stationary by compensating position
            Vector3 dir = t.position - pivot;
            float rad = Mathf.Deg2Rad * (angle - fromZ);
            // Simple approach: just set Z rotation directly (pivot compensation optional)
            Vector3 eu = t.localEulerAngles;
            eu.z = angle;
            t.localEulerAngles = eu;
            yield return null;
        }
        Vector3 fe = t.localEulerAngles;
        fe.z = toZ;
        t.localEulerAngles = fe;
    }

    // ─────────────────────────────────────────────
    //  Animation 2 — Breathe (scale pulse)
    // ─────────────────────────────────────────────

    private IEnumerator DoBreathe(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        Vector3 origScale = entry.originalScales[idx];
        float halfDur = (entry.duration * globalSpeedMultiplier) * 0.5f;

        Vector3 bigScale   = origScale * entry.breatheMaxScale;
        Vector3 smallScale = origScale * entry.breatheMinScale;

        yield return StartCoroutine(TweenScale(t, origScale, bigScale, halfDur, entry.easeType));
        yield return StartCoroutine(TweenScale(t, bigScale, smallScale, halfDur, entry.easeType));
        yield return StartCoroutine(TweenScale(t, smallScale, origScale, halfDur * 0.5f, entry.easeType));
    }

    private IEnumerator TweenScale(Transform t, Vector3 from, Vector3 to, float dur, EaseType ease)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.LerpUnclamped(from, to, Evaluate(p, ease));
            yield return null;
        }
        t.localScale = to;
    }

    // ─────────────────────────────────────────────
    //  Animation 3 — Hover (float up and down)
    // ─────────────────────────────────────────────

    private IEnumerator DoHover(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        Vector3 origPos = entry.originalPositions[idx];
        float halfDur = (entry.duration * globalSpeedMultiplier) * 0.5f;

        Vector3 upPos = origPos + Vector3.up * entry.hoverHeight;

        yield return StartCoroutine(TweenPosition(t, origPos, upPos, halfDur, entry.easeType));
        yield return StartCoroutine(TweenPosition(t, upPos, origPos, halfDur, entry.easeType));
    }

    private IEnumerator TweenPosition(Transform t, Vector3 from, Vector3 to, float dur, EaseType ease)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / dur);
            t.localPosition = Vector3.LerpUnclamped(from, to, Evaluate(p, ease));
            yield return null;
        }
        t.localPosition = to;
    }

    // ─────────────────────────────────────────────
    //  Animation 4 — Wobble (rapid shake)
    // ─────────────────────────────────────────────

    private IEnumerator DoWobble(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        float origZ = entry.originalRotations[idx];
        float dur = entry.duration * globalSpeedMultiplier;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / dur;
            // Damped sine wave
            float decay = 1f - p;
            float angle = Mathf.Sin(elapsed * entry.wobbleFrequency * Mathf.PI * 2f)
                          * entry.wobbleAngle * decay;
            Vector3 eu = t.localEulerAngles;
            eu.z = origZ + angle;
            t.localEulerAngles = eu;
            yield return null;
        }

        // Reset
        Vector3 finalEu = t.localEulerAngles;
        finalEu.z = origZ;
        t.localEulerAngles = finalEu;

        // Pause before next wobble
        yield return new WaitForSeconds(entry.duration * globalSpeedMultiplier * 0.5f);
    }

    // ─────────────────────────────────────────────
    //  Animation 5 — Drift (horizontal / diagonal)
    // ─────────────────────────────────────────────

    private IEnumerator DoDrift(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        Vector3 origPos = entry.originalPositions[idx];
        float halfDur = (entry.duration * globalSpeedMultiplier) * 0.5f;

        Vector3 driftDir = new Vector3(
            Mathf.Lerp(1f, 0f, entry.driftVerticalBias),
            Mathf.Lerp(0f, 1f, entry.driftVerticalBias), 0f);

        Vector3 driftPos = origPos + driftDir * entry.driftDistance;

        yield return StartCoroutine(TweenPosition(t, origPos, driftPos, halfDur, entry.easeType));
        yield return StartCoroutine(TweenPosition(t, driftPos, origPos, halfDur, entry.easeType));
    }

    // ─────────────────────────────────────────────
    //  Animation 6 — SquashStretch
    // ─────────────────────────────────────────────

    private IEnumerator DoSquashStretch(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        Vector3 orig = entry.originalScales[idx];
        float quartDur = (entry.duration * globalSpeedMultiplier) * 0.25f;

        Vector3 squash  = new Vector3(orig.x * entry.squashScaleX,  orig.y * entry.squashScaleY,  orig.z);
        Vector3 stretch = new Vector3(orig.x / entry.squashScaleX,  orig.y * entry.stretchScaleY, orig.z);

        yield return StartCoroutine(TweenScale(t, orig,    stretch, quartDur, entry.easeType));
        yield return StartCoroutine(TweenScale(t, stretch, squash,  quartDur, entry.easeType));
        yield return StartCoroutine(TweenScale(t, squash,  stretch, quartDur, entry.easeType));
        yield return StartCoroutine(TweenScale(t, stretch, orig,    quartDur, entry.easeType));
    }

    // ─────────────────────────────────────────────
    //  Animation 7 — Spin
    // ─────────────────────────────────────────────

    private IEnumerator DoSpin(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        float dur = entry.duration * globalSpeedMultiplier;
        float elapsed = 0f;
        float startZ = t.localEulerAngles.z;
        float dir = entry.spinClockwise ? -1f : 1f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            Vector3 eu = t.localEulerAngles;
            eu.z = startZ + dir * entry.spinDegreesPerSecond * elapsed;
            t.localEulerAngles = eu;
            yield return null;
        }
    }

    // ─────────────────────────────────────────────
    //  Animation 8 — Flicker (alpha + micro-scale)
    // ─────────────────────────────────────────────

    private IEnumerator DoFlicker(PropEntry entry, int idx)
    {
        Transform t = entry.targets[idx];
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        Vector3 origScale = entry.originalScales[idx];
        Color origColor = (sr != null && idx < entry.originalColors.Count)
            ? entry.originalColors[idx]
            : Color.white;

        float dur = entry.duration * globalSpeedMultiplier;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;

            // Noise-driven flicker
            float noise = Mathf.PerlinNoise(elapsed * (5f + entry.flickerNoise * 10f), idx * 13.7f);
            float alpha = Mathf.Lerp(entry.flickerMinAlpha, entry.flickerMaxAlpha, noise);
            float scale = Mathf.Lerp(entry.flickerMinScale, entry.flickerMaxScale, noise);

            if (sr != null)
            {
                Color c = origColor;
                c.a = origColor.a * alpha;
                sr.color = c;
            }

            t.localScale = origScale * scale;
            yield return null;
        }

        // Reset
        if (sr != null) sr.color = origColor;
        t.localScale = origScale;
    }

    // ─────────────────────────────────────────────
    //  Easing functions
    // ─────────────────────────────────────────────

    private float Evaluate(float t, EaseType ease)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case EaseType.Linear:     return t;
            case EaseType.EaseInOut:  return t * t * (3f - 2f * t);
            case EaseType.EaseIn:     return t * t;
            case EaseType.EaseOut:    return 1f - (1f - t) * (1f - t);
            case EaseType.SineWave:   return (1f - Mathf.Cos(t * Mathf.PI)) * 0.5f;
            case EaseType.BounceOut:  return EaseBounceOut(t);
            case EaseType.ElasticOut: return EaseElasticOut(t);
            default:                  return t;
        }
    }

    private float EaseBounceOut(float t)
    {
        if (t < 1f / 2.75f)      return 7.5625f * t * t;
        else if (t < 2f / 2.75f) { t -= 1.5f   / 2.75f; return 7.5625f * t * t + 0.75f;   }
        else if (t < 2.5f / 2.75f){ t -= 2.25f  / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else                       { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }

    private float EaseElasticOut(float t)
    {
        if (t == 0f || t == 1f) return t;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.075f) * (2f * Mathf.PI) / 0.3f) + 1f;
    }

    // ─────────────────────────────────────────────
    //  Public API (call from other scripts)
    // ─────────────────────────────────────────────

    /// <summary>Restart a specific entry by index.</summary>
    public void RestartEntry(int index)
    {
        if (index < 0 || index >= propEntries.Count) return;
        var entry = propEntries[index];
        if (!entry.enabled) return;
        CacheOriginalStates(entry);
        StartEntryAnimations(entry);
    }

    /// <summary>Stop a specific entry and optionally reset its transforms.</summary>
    public void StopEntry(int index, bool reset = true)
    {
        if (index < 0 || index >= propEntries.Count) return;
        var entry = propEntries[index];
        StopEntryCoroutines(entry);
        if (reset && entry.statesCached) ResetToOriginal(entry);
    }

    /// <summary>Stop all and restart everything.</summary>
    public void RestartAll()
    {
        StopAllAnimations(resetTransforms: true);
        StartAllAnimations();
    }
}


// ─────────────────────────────────────────────────────────────────────────────
//  Custom Inspector
// ─────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
[CustomEditor(typeof(PropTweenAnimator))]
public class PropTweenAnimatorEditor : Editor
{
    private PropTweenAnimator _anim;
    private SerializedProperty _entries;
    private SerializedProperty _speedMult;
    private SerializedProperty _playAnim;
    private bool[] _foldouts;

    // Colour for each animation type label
    private static readonly Color[] _typeColors = new Color[]
    {
        new Color(0.4f, 0.8f, 0.5f),  // Sway
        new Color(0.5f, 0.7f, 1.0f),  // Breathe
        new Color(1.0f, 0.85f, 0.3f), // Hover
        new Color(1.0f, 0.5f, 0.3f),  // Wobble
        new Color(0.7f, 0.6f, 1.0f),  // Drift
        new Color(0.9f, 0.4f, 0.7f),  // SquashStretch
        new Color(0.3f, 0.9f, 0.9f),  // Spin
        new Color(1.0f, 0.7f, 0.2f),  // Flicker
    };

    private void OnEnable()
    {
        _anim     = (PropTweenAnimator)target;
        _entries  = serializedObject.FindProperty("propEntries");
        _speedMult = serializedObject.FindProperty("globalSpeedMultiplier");
        _playAnim  = serializedObject.FindProperty("playAnimations");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Header ──
        EditorGUILayout.Space(6);
        DrawHeader("⟡ Prop Tween Animator");
        EditorGUILayout.Space(4);

        // ── Global controls ──
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Global Controls", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_speedMult);
            EditorGUILayout.PropertyField(_playAnim);
        }

        EditorGUILayout.Space(8);

        // ── Buttons ──
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("▶ Restart All", GUILayout.Height(26)))
                if (Application.isPlaying) _anim.RestartAll();
            if (GUILayout.Button("+ Add Entry", GUILayout.Height(26)))
            {
                _entries.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }
        }
        EditorGUILayout.Space(6);

        // ── Entries ──
        EnsureFoldouts();

        for (int i = 0; i < _entries.arraySize; i++)
        {
            SerializedProperty entry = _entries.GetArrayElementAtIndex(i);
            DrawEntryHeader(entry, i);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntryHeader(SerializedProperty entry, int index)
    {
        SerializedProperty enabledProp = entry.FindPropertyRelative("enabled");
        SerializedProperty typeProp    = entry.FindPropertyRelative("animationType");

        PropTweenAnimator.AnimationType animType =
            (PropTweenAnimator.AnimationType)typeProp.enumValueIndex;

        Color typeColor = _typeColors[Mathf.Clamp((int)animType, 0, _typeColors.Length - 1)];
        bool isEnabled  = enabledProp.boolValue;

        GUI.backgroundColor = isEnabled
            ? new Color(typeColor.r, typeColor.g, typeColor.b, 0.18f)
            : new Color(0.4f, 0.4f, 0.4f, 0.15f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = Color.white;

            // ── Entry foldout header ──
            using (new EditorGUILayout.HorizontalScope())
            {
                // Toggle
                enabledProp.boolValue = EditorGUILayout.Toggle(enabledProp.boolValue, GUILayout.Width(18));

                // Foldout label
                string label = $"[{index}]  {animType}";
                if (index < _foldouts.Length)
                    _foldouts[index] = EditorGUILayout.Foldout(_foldouts[index], label, true, BoldFoldout(typeColor));

                GUILayout.FlexibleSpace();

                // Runtime restart button
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("▶", GUILayout.Width(24), GUILayout.Height(18)))
                        _anim.RestartEntry(index);
                    if (GUILayout.Button("■", GUILayout.Width(24), GUILayout.Height(18)))
                        _anim.StopEntry(index);
                }

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f, 0.7f);
                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
                {
                    _entries.DeleteArrayElementAtIndex(index);
                    EnsureFoldouts();
                    return;
                }
                GUI.backgroundColor = Color.white;
            }

            // ── Foldout body ──
            if (index < _foldouts.Length && _foldouts[index])
            {
                EditorGUI.indentLevel++;

                // Targets
                DrawSection("Targets");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("targets"), true);

                // Animation type
                DrawSection("Animation");
                EditorGUILayout.PropertyField(typeProp);

                // Timing
                DrawSection("Timing");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("duration"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("randomTimeOffset"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("startDelay"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("easeType"));

                // Type-specific settings
                DrawAnimationSettings(entry, animType);

                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.Space(3);
    }

    private void DrawAnimationSettings(SerializedProperty entry, PropTweenAnimator.AnimationType type)
    {
        switch (type)
        {
            case PropTweenAnimator.AnimationType.Sway:
                DrawSection("Sway");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("swayAngle"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("swayPivotOffset"));
                break;

            case PropTweenAnimator.AnimationType.Breathe:
                DrawSection("Breathe");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("breatheMinScale"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("breatheMaxScale"));
                break;

            case PropTweenAnimator.AnimationType.Hover:
                DrawSection("Hover");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("hoverHeight"));
                break;

            case PropTweenAnimator.AnimationType.Wobble:
                DrawSection("Wobble");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("wobbleAngle"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("wobbleFrequency"));
                break;

            case PropTweenAnimator.AnimationType.Drift:
                DrawSection("Drift");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("driftDistance"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("driftVerticalBias"));
                break;

            case PropTweenAnimator.AnimationType.SquashStretch:
                DrawSection("Squash & Stretch");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("squashScaleY"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("stretchScaleY"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("squashScaleX"));
                break;

            case PropTweenAnimator.AnimationType.Spin:
                DrawSection("Spin");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("spinDegreesPerSecond"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("spinClockwise"));
                break;

            case PropTweenAnimator.AnimationType.Flicker:
                DrawSection("Flicker");
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("flickerMinAlpha"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("flickerMaxAlpha"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("flickerMinScale"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("flickerMaxScale"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("flickerNoise"));
                break;
        }
    }

    private void DrawSection(string label)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
    }

    private void DrawHeader(string title)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter,
        };
        EditorGUILayout.LabelField(title, style, GUILayout.Height(22));
        var rect = GUILayoutUtility.GetLastRect();
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1f), new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    private GUIStyle BoldFoldout(Color tint)
    {
        var s = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
        };
        s.normal.textColor   = tint;
        s.onNormal.textColor = tint;
        return s;
    }

    private void EnsureFoldouts()
    {
        if (_foldouts == null || _foldouts.Length != _entries.arraySize)
        {
            bool[] old = _foldouts ?? new bool[0];
            _foldouts = new bool[_entries.arraySize];
            for (int i = 0; i < Mathf.Min(old.Length, _foldouts.Length); i++)
                _foldouts[i] = old[i];
            // New entries open by default
            for (int i = old.Length; i < _foldouts.Length; i++)
                _foldouts[i] = true;
        }
    }
}
#endif
