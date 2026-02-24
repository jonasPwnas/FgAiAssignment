using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerObjectInteraction : MonoBehaviour
{
    public delegate void PickUp(bool canPickup, PlayerObjectInteraction playerObjectInteraction);
    public static event PickUp OnPickUp;

    [Tooltip("The Given Element Type")]
    [SerializeField] private ElementTypes.ElementType m_elementType = ElementTypes.ElementType.NoElement;
    [Tooltip("The Amount of Sources Dropped")]
    [SerializeField] private int elementAmountToGivePlayer = 1;
    [Tooltip("The Source Object")]
    [SerializeField] private GameObject elementDropPrefab;
    [Tooltip("OnBoardingWindow")]
    [SerializeField] private GameObject tutorialWindow;

    [SerializeField] private GameObject glowingSphere;
    [SerializeField] private GameObject dropLogicObject;

    PlayerInput playerInput;

    private bool isPickedUp = false;

    public virtual void Awake()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
    }

    public virtual void Start() //blork 
    {
        StartCoroutine(UglyInteractFix());
    }

    public void InvokeTheEvent(bool canInteract)
    {
        OnPickUp?.Invoke(canInteract, this);
    }
    
    public virtual void OnTriggerEnter(Collider other) //tell UI to show pickup prompt here
    {
        print("pick: player enter pickup on  " +  other.gameObject.GetComponentInParent<Transform>().gameObject.name);
        OnPickUp?.Invoke(true, this);
    }

    public virtual void OnTriggerExit(Collider other) //tell UI to show pickup prompt here
    { 
        print("pick: player exit");
        OnPickUp?.Invoke(false,this);
        
        if(isPickedUp)
            Destroy(dropLogicObject);
    }

    public void OpenTutorialWindow()
    {
        tutorialWindow.SetActive(true);
        playerInput.SwitchCurrentActionMap("UI");
    }

    public virtual void DoInteract()
    {
        if (isPickedUp)
            return;
        
        Destroy(glowingSphere);
        
        for (int i = 0; i < elementAmountToGivePlayer; i++)
        {
            DropdElementalCharge charge = Instantiate(elementDropPrefab, transform.position, Quaternion.identity).GetComponent<DropdElementalCharge>();
            charge.SetElementType(m_elementType);
        }

        isPickedUp = true;
    }

    IEnumerator UglyInteractFix()
    {
        yield return new WaitForSeconds(1.5f);
        OnPickUp?.Invoke(false,this);
    }
    
}
