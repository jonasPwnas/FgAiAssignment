using UnityEngine;

[CreateAssetMenu(fileName = "FloatCurve", menuName = "Scriptable Objects/Float Curve")]
public class ScriptableFloatCurve : ScriptableObject
{
    [SerializeField] public AnimationCurve floatCurve;
}
