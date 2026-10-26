using UnityEngine;

/// <summary>Score trigger child sitting inside a pipe gap; fires once when the bird passes.</summary>
public class PipeMiddleScript : MonoBehaviour
{
    private LogicScript logic;
    private bool counted;

    void Start()
    {
        logic = LogicScript.Instance;
        if (logic == null)
        {
            var logicGo = GameObject.FindGameObjectWithTag("Logic");
            if (logicGo != null) logic = logicGo.GetComponent<LogicScript>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (counted || logic == null) return;
        if (collision.GetComponent<BirdScript>() != null)
        {
            counted = true;
            logic.PipePassed(transform.position);
        }
    }

    /// <summary>Reset so a pooled pipe can score again.</summary>
    public void ResetTrigger() => counted = false;
}
