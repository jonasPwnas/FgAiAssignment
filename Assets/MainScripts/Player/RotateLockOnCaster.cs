using System;
using UnityEngine;

public class RotateLockOnCaster : MonoBehaviour
{
    private Camera m_camera;
    
    private void Awake()
    {
        m_camera = Camera.main;
    }

    void Update()
    {
        Vector3 newRotation = m_camera.transform.rotation.eulerAngles;
        newRotation.z = 0;
        newRotation.x = 0;
        transform.rotation = Quaternion.Euler(newRotation);
    }
}
