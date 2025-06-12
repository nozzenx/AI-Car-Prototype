using System;
using System.Globalization;
using ArcadeVP;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI accelerationText;
    [SerializeField] private TextMeshProUGUI topSpeedText;
    [SerializeField] private TextMeshProUGUI driftScaleText;
    [SerializeField] private TextMeshProUGUI gravityScaleText;
    [SerializeField] private TextMeshProUGUI downforceText;

    [SerializeField] private GameObject functionsInfoMenu;
    [SerializeField] private GameObject menuOpenInfo;
    [SerializeField] private GameObject engineOffInfo;
    private bool _isMenuOpen;
    
    [SerializeField] private TextMeshProUGUI assistant;
    [SerializeField] private AICarController carAI;
    
    
    [SerializeField] private ArcadeVehicleController vehicleController;

    [SerializeField] private GameObject micSprite;

    private void Start()
    {
        carAI.micOn += OpenMicSprite;
        carAI.micOff += CloseMicSprite;
    }

    private void Update()
    {
        accelerationText.text = "ACCELERATION: " + vehicleController.accelaration.ToString(CultureInfo.InvariantCulture);
        topSpeedText.text = "TOP SPEED: " + vehicleController.MaxSpeed.ToString(CultureInfo.InvariantCulture);
        driftScaleText.text = "DRIFT SCALE: " + vehicleController.turn.ToString(CultureInfo.InvariantCulture);
        gravityScaleText.text = "GRAVITY SCALE: " + vehicleController.gravity.ToString(CultureInfo.InvariantCulture);
        downforceText.text = "DOWNFORCE: " + vehicleController.downforce.ToString(CultureInfo.InvariantCulture);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isMenuOpen = !_isMenuOpen;
            functionsInfoMenu.SetActive(_isMenuOpen);
            menuOpenInfo.SetActive(!_isMenuOpen);
        }

        if (vehicleController.isEngineOn)
        {
            engineOffInfo.SetActive(false);
        }

        assistant.text = carAI.aiMessage;
    }

    private void OpenMicSprite()
    {
        micSprite.SetActive(true);
    }

    private void CloseMicSprite()
    {
        micSprite.SetActive(false);
    }
    
    
}
