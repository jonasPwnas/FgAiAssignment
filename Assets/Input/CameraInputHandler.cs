using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public class CameraInputHandler : MonoBehaviour
    {
        [HideInInspector] public InputAction lookAction;

        private void Start()
        {
            lookAction = InputSystem.actions.FindAction("Look");
        }
    }
}
