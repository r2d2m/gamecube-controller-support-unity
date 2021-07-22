# Usage:
```c#
using GameCubeControllerSupport;

public class MyScript : MonoBehaviour
{
    // Start polling data from the adapter
    private void Awake() => GameCubeControllerAdapter.Start();

    // make sure to always stop polling when your application quits
    private void OnApplicationQuit() => GameCubeControllerAdapter.Stop();

    private void Update()
    {
        // Get input data from Port 1 of the adapter
        GamecubeController controller = GameCubeControllerAdapter.GetController(0);

        if(controller.ButtonA)
        {
          // do action
        }
    }
}
```
---