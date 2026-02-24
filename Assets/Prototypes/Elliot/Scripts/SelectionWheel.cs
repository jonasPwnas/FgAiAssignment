using System.Collections.Generic;
using Input;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UxPrototype;
using AbilityUI;

public class SelectionWheel : MonoBehaviour
{
    //delegates
    public delegate void ChangedElement(ElementTypes.ElementType newElement);
    public static event ChangedElement OnElementChanged;
    
    //Editor expose
    [Header("Charge Glow")]
    public Sprite chargeFire;
    public Sprite chargeWater;
    public Sprite chargeAir;
    public Sprite chargeNone;

    [Header("Slow Motion Settings")]
    public float slowedTimeScale = 0.5f;
    public  float normalTimeScale = 1f;
    [HideInInspector] public ElementTypes.ElementType currentElement;

    PauseManager pauseManager;
    PlayerInput playerInput;
    [SerializeField] private GameObject chargeContainer;
    ChargeManager chargeManager;
    PlayerStatus playerStatus;
    ElementSelector elementSelector;
    AbilityUIController abilityUIController;

    private void Awake()
    {
        pauseManager = FindAnyObjectByType<PauseManager>();
        playerInput = this.GetComponent<PlayerInput>();
        chargeManager = FindAnyObjectByType<ChargeManager>();
        playerStatus = FindAnyObjectByType<PlayerStatus>();
        elementSelector = FindAnyObjectByType<ElementSelector>();
        abilityUIController = FindAnyObjectByType<AbilityUIController>();
    }

    private void Start()
    {
        FireElement();
        playerInput.SwitchCurrentActionMap("Player");
    }

    /*private void OnEnable()
    {
        PlayerStatus.OnElementStackChanged += UpdateElements;
    }

    private void OnDisable()
    {
        PlayerStatus.OnElementStackChanged -= UpdateElements;
    }*/
    
    public void UpdateElements(ElementTypes.ElementType elementToUpdate)
    {
        switch(elementToUpdate)
        {
            case ElementTypes.ElementType.Fire:
                FireElement(); 
                break;
            case ElementTypes.ElementType.Water:
                WaterElement();
                break;
            case ElementTypes.ElementType.Air:
                AirElement();
                break;
        }
    }

    public void FireElement()
    {
        currentElement = ElementTypes.ElementType.Fire;
        chargeContainer.SetActive(true);
        OnElementChanged?.Invoke(currentElement);
    }

    public void WaterElement()
    {
        currentElement = ElementTypes.ElementType.Water;
        chargeContainer.SetActive(true);
        OnElementChanged?.Invoke(currentElement);
    }

    public void AirElement()
    {
        currentElement = ElementTypes.ElementType.Air;
        chargeContainer.SetActive(true);
        OnElementChanged?.Invoke(currentElement);
    }

    public void NoElement()
    {
        currentElement = ElementTypes.ElementType.NoElement;
        chargeContainer.SetActive(true);
        OnElementChanged?.Invoke(currentElement);
    }
    
    public void OnNextElement()
    {
        if (pauseManager.isPaused)
        {
            return;
        }
        abilityUIController.CycleAbilityRight();
        elementSelector.ChangeSelection(1);

        switch (currentElement)
        {
            case ElementTypes.ElementType.Fire:
                WaterElement();
                break;
            case ElementTypes.ElementType.Water:
                AirElement();
                break;
            case ElementTypes.ElementType.Air:
                FireElement();
                break;
        }
    }

    public void OnPreviousElement()
    {
        if (pauseManager.isPaused)
        {
            return;
        }
        abilityUIController.CycleAbilityLeft();
        elementSelector.ChangeSelection(-1);

        switch (currentElement)
        {
            case ElementTypes.ElementType.Fire:
                AirElement();
                break;
            case ElementTypes.ElementType.Water:
                FireElement();
                break;
            case ElementTypes.ElementType.Air:
                WaterElement();
                break;
        }
    }

    public void SwitchToChargingElement(ElementTypes.ElementType chargingElement)
    {
        elementSelector.SwitchToChargingElement(chargingElement);
    }
}
