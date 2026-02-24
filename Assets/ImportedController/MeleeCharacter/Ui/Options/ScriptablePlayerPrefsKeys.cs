using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPrefsKeys", menuName = "Scriptable Objects/PlayerPrefsKeys")]
public class ScriptablePlayerPrefsKeys : ScriptableObject
{
    public string cameraRotationSpeedKey = "CameraRotationSpeed";
    public string isUsingControllerKey = "IsUsingController";
}
