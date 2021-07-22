using GamecubeControllerSupport;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private GamecubeController controller;
    [SerializeField] private int _port;
    
    private void Awake()
    {
        GamecubeControllerAdapter.Start();
        controller = GamecubeControllerAdapter.GetController(_port);
    }

    private void Update()
    {
        if (controller.ButtonA)
        {
            Debug.Log("A pressed");
        }
        Debug.Log($"Left X: {controller.LeftStickX} | Y: {controller.LeftStickY}");
    }

    private void OnApplicationQuit() => GamecubeControllerAdapter.Stop();

}
