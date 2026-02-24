using UnityEngine;

public interface IDamageableElementalSource
{
    //public HitInfo GetHitInfo(HitInfo hitInfo);
    public enum ElementInteractType{Enemy, Source, User}
    public float ChargeElementTime();
    public void TakeDamage(float damageAmount, ElementTypes.ElementType elementType, bool UsedSword);
    public void Heal(float healAmount, ElementTypes.ElementType elementType);
    public ElementInteractType InteractType();
    public int EquippedElementAmount();
    public int ElementAmountToGive();
    public ElementTypes.ElementType GetSourceElement();
    public void AddElement(ElementTypes.ElementType elementType, int amount, bool fromCharge);
    public void RemoveElement(ElementTypes.ElementType elementType, int amount);
}
