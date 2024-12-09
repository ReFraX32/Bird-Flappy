using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public bool IsFlapping { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        IsFlapping = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                IsFlapping = true;
            }
        }
    }
}