using System;
using ArcadeVP;
using UnityEngine;

public class VehicleActionsManager : MonoBehaviour
{
    [SerializeField] private ArcadeVehicleController vehicle;
    private Animator _animator;

    [SerializeField] private ParticleSystem ventAir;
    
    [Header("Door Controllers")] 
    private bool isFrontRightOpen;
    private bool isFrontLeftOpen;
    private bool isRearRightOpen;
    private bool isRearLeftOpen;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void OpenDoorFR()
    {
        if (!isFrontRightOpen)
        {
            _animator.SetTrigger("opendoor_fr");
            isFrontRightOpen = true;
        }
    }
    public void OpenDoorRR()
    {
        if (!isRearRightOpen)
        {
            _animator.SetTrigger("opendoor_rr");
            isRearRightOpen = true;
        }
    }
    public void OpenDoorFL()
    {
        if (!isFrontLeftOpen)
        {
            _animator.SetTrigger("opendoor_fl");
            isFrontLeftOpen = true;
        }
    }
    public void OpenDoorRL()
    {
        if (!isRearLeftOpen)
        {
            _animator.SetTrigger("opendoor_rl");
            isRearLeftOpen = true;
        }
    }

    public void CloseDoorRR()
    {
        if (isRearRightOpen)
        {
            _animator.SetTrigger("closedoor_rr");
            isRearRightOpen = false;
        }
    }
    public void CloseDoorRL()
    {
        if (isRearLeftOpen)
        {
            _animator.SetTrigger("closedoor_rl");
            isRearLeftOpen = false;
        }
    }
    public void CloseDoorFR()
    {
        if (isFrontRightOpen)
        {
            _animator.SetTrigger("closedoor_fr");
            isFrontRightOpen = false;
        }
    }
    public void CloseDoorFL()
    {
        if (isFrontLeftOpen)
        {
            _animator.SetTrigger("closedoor_fl");
            isFrontLeftOpen = false;
        }
    }

    public void OpenAllDoors()
    {
        OpenDoorFL();
        OpenDoorRR();
        OpenDoorRL();
        OpenDoorFR();
    }

    public void CloseAllDoors()
    {
        CloseDoorFL();
        CloseDoorRR();
        CloseDoorRL();
        CloseDoorFR();
    }
    

    public void DriftMode()
    {
        vehicle.accelaration = 7;
        vehicle.kartLike = true;
        vehicle.turn = 12;
        vehicle.downforce = 5;
        vehicle.MaxSpeed = 110;
    }

    public void DefaultMode()
    {
        vehicle.accelaration = 3;
        vehicle.kartLike = false;
        vehicle.turn = 4;
        vehicle.downforce = 5;
        vehicle.MaxSpeed = 100;
    }
    
    public void RaceMode()
    {
        vehicle.accelaration = 5;
        vehicle.kartLike = false;
        vehicle.turn = 5;
        vehicle.downforce = 15;
        vehicle.MaxSpeed = 250;
    }

    public void OpenVents()
    {
        ventAir.Play();
    }

    public void CloseVents()
    {
        ventAir.Stop();
    }

    public void OpenEngine()
    {
        vehicle.engineSound.Play();
        vehicle.isEngineOn = true;
    }

    public void CloseEngine()
    {
        vehicle.isEngineOn = false;
    }
}
