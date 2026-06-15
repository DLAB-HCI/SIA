using UnityEngine;
using UnityEngine.InputSystem;

public class PreventSleep : MonoBehaviour
{
    private float timer = 0f;

    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 30f)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                _ = gamepad.leftTrigger.ReadValue();
            }
            timer = 0f;
        }
    }
}

