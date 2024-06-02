using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VIVE_Trackers;

public class TrackerDeviceInfoView : MonoBehaviour
{
    int currentIndex = -1;
    [SerializeField] Image bgImg;
    [SerializeField] Image bgLight;
    [SerializeField] Image batteryImg;
    [SerializeField] Image buttonHightLightImg;

    TrackerDeviceInfo deviceInfo;
    IAckable dongleAPI;
    public IAckable DongleAPI
    {
        get => dongleAPI; 
        set
        {
            dongleAPI = value;
            dongleAPI.OnButtonClicked += DongleAPI_OnButtonClicked;
            dongleAPI.OnButtonDown += DongleAPI_OnButtonDown;
            dongleAPI.OnConnected += DongleAPI_OnConnected;
            dongleAPI.OnDisconnected += DongleAPI_OnDisconnected;
            dongleAPI.OnTrackerStatus += DongleAPI_OnTrackerStatus;
        }
    }

    Color bgFromColor;
    Color lightFromColor;
    bool isConnected = false;

    private void Start()
    {
        currentIndex = transform.GetSiblingIndex();
        bgFromColor = bgImg.color;
        lightFromColor = bgLight.color;
    }

    void Update()
    {
        if (!isConnected)
        {
            bgImg.color = Color.Lerp(bgImg.color, Color.clear, Time.deltaTime);
            bgLight.color = Color.Lerp(bgLight.color, Color.clear, Time.deltaTime);
        }
        else
        {
            bgImg.color = Color.Lerp(bgImg.color, bgFromColor, Time.deltaTime);
            bgLight.color = Color.Lerp(bgLight.color, lightFromColor, Time.deltaTime);
        }
    }

    private void DongleAPI_OnTrackerStatus(TrackerDeviceInfo device)
    {
        if (device.CurrentIndex == currentIndex)
        {
            var battr = (device.Battery / 50f) * 6;
            batteryImg.fillAmount = battr; 
        }
    }

    private void DongleAPI_OnDisconnected(int trackerIndx)
    {
        currentIndex = transform.GetSiblingIndex();
        if (currentIndex == trackerIndx)
        {
            batteryImg.fillAmount = 0;
            isConnected = false;
        }
    }

    private void DongleAPI_OnConnected(int trackerIndx)
    {
        currentIndex = transform.GetSiblingIndex();
        if (currentIndex == trackerIndx)
        {
            isConnected = true;
        }
    }

    private void DongleAPI_OnButtonClicked(int trackerIndx)
    {
        currentIndex = transform.GetSiblingIndex();
        if (currentIndex == trackerIndx)
            buttonHightLightImg.gameObject.SetActive(false);
    }

    private void DongleAPI_OnButtonDown(int trackerIndx)
    {
        currentIndex = transform.GetSiblingIndex();
        if (currentIndex == trackerIndx)
            buttonHightLightImg.gameObject.SetActive(true);
    }
}
