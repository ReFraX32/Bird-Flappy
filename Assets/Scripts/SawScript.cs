using UnityEngine;

/// <summary>Saw-blade hazard sweeping a pipe gap vertically; timing puzzle on top of flying.</summary>
public class SawScript : MonoBehaviour
{
    private float amp;
    private float speed;
    private float phase;
    private float baseY;

    public void Setup(float gap)
    {
        amp = Mathf.Max(1f, gap / 2f - 1.8f);
        speed = Random.Range(GameConfig.SawOscSpeedMin, GameConfig.SawOscSpeedMax);
        phase = Random.value * Mathf.PI * 2f;
        baseY = 0f;
        transform.localPosition = new Vector3(0f, 0f, 0f);
    }

    private void Update()
    {
        var logic = LogicScript.Instance;
        if (logic == null || !logic.IsPlaying) return;

        phase += speed * Time.deltaTime;
        transform.localPosition = new Vector3(0f, baseY + Mathf.Sin(phase) * amp, 0f);
        transform.Rotate(0, 0, -540f * Time.deltaTime);
    }
}
