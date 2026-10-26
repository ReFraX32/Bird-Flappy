using System;
using System.Collections;
using UnityEngine;

/// <summary>Minimal coroutine-based tween helpers for UI + world animation.</summary>
public static class Tween
{
    public static Coroutine Scale(Transform target, Vector3 from, Vector3 to, float dur,
                                  Func<float, float> ease = null, Action onDone = null)
        => Run(target.gameObject, dur, t => target.localScale = Vector3.LerpUnclamped(from, to, t), ease, onDone);

    public static Coroutine ScaleTo(Transform target, Vector3 to, float dur,
                                    Func<float, float> ease = null, Action onDone = null)
        => Scale(target, target.localScale, to, dur, ease, onDone);

    public static Coroutine Position(Transform target, Vector3 from, Vector3 to, float dur,
                                     Func<float, float> ease = null, Action onDone = null)
        => Run(target.gameObject, dur, t => target.localPosition = Vector3.LerpUnclamped(from, to, t), ease, onDone);

    public static Coroutine Fade(CanvasGroup group, float from, float to, float dur,
                                 Func<float, float> ease = null, Action onDone = null)
        => Run(group.gameObject, dur, t => group.alpha = Mathf.LerpUnclamped(from, to, t), ease, onDone);

    public static void Kill(UnityEngine.Object owner)
    {
        if (owner is GameObject go && go != null) StopOn(go);
        else if (owner is MonoBehaviour mb && mb != null) StopOn(mb.gameObject);
    }

    private static readonly System.Collections.Generic.Dictionary<GameObject, Coroutine> Active
        = new System.Collections.Generic.Dictionary<GameObject, Coroutine>();

    public static Coroutine Run(GameObject owner, float dur, Action<float> apply,
                                Func<float, float> ease = null, Action onDone = null)
    {
        if (Active.TryGetValue(owner, out var old) && old != null) StopOn(owner);
        var runner = Runner.Instance;
        var co = runner.StartCoroutine(RunCo(owner, dur, apply, ease ?? EaseOutQuad, onDone));
        Active[owner] = co;
        return co;
    }

    private static IEnumerator RunCo(GameObject owner, float dur, Action<float> apply,
                                     Func<float, float> ease, Action onDone)
    {
        float t = 0f;
        while (t < 1f)
        {
            if (owner == null) yield break;
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            apply(ease(Mathf.Clamp01(t)));
            yield return null;
        }
        Active.Remove(owner);
        onDone?.Invoke();
    }

    private static void StopOn(GameObject owner)
    {
        if (Active.TryGetValue(owner, out var co) && co != null)
            Runner.Instance.StopCoroutine(co);
        Active.Remove(owner);
    }

    // eases
    public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    public static float EaseInQuad(float t) => t * t;
    public static float EaseOutBack(float t)
    {
        const float c = 1.70158f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }
    public static float Linear(float t) => t;

    private class Runner : MonoBehaviour
    {
        private static Runner _instance;
        public static Runner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TweenRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Runner>();
                }
                return _instance;
            }
        }
    }
}
