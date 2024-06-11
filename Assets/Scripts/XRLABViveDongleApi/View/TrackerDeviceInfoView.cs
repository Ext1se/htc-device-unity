using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VIVE_Trackers;

public class TrackerDeviceInfoView : MonoBehaviour
{
    int currentIndex = -1;
    [SerializeField] Image bgImg;
    [SerializeField] Image bgLight;
    [SerializeField] Image borderImg;
    [SerializeField] Image batteryImg;
    [SerializeField] Image buttonHightLightImg;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] TMP_Dropdown roleIDDD;
    [SerializeField] Button centralBtn;

    TrackerDeviceInfo deviceInfo;
    IVIVEDongle dongleAPI;
    public IVIVEDongle DongleAPI
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
            dongleAPI.OnDongleStatus += Trackers_OnDongleStatus;
        }
    }

    Color bgFromColor;
    Color lightFromColor;
    bool isConnected = false;

    private void Start()
    {
        UnityDispatcher.Create();
        currentIndex = transform.GetSiblingIndex();
        bgFromColor = bgImg.color;
        lightFromColor = bgLight.color;

        roleIDDD.onValueChanged.AddListener(OnRoleChange);
        centralBtn.onClick.AddListener(OnCentralBtnClick);
    }

    private void OnCentralBtnClick()
    {
        currentIndex = transform.GetSiblingIndex();
        dongleAPI.ExperimentalFileDownload(currentIndex);
    }

    private void OnRoleChange(int roleID)
    {
        if (deviceInfo != null)
            dongleAPI.SetRoleID(deviceInfo.SerialNumber, roleID);
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
    private void Trackers_OnDongleStatus(PairState[] states)
    {
        UnityDispatcher.Invoke(() =>
        {
            var state = states[currentIndex];
            switch (state)
            {
                case PairState.ReadyForScan:
                    borderImg.color = Color.blue;
                    break;
                case PairState.PairedIdle:
                    borderImg.color = Color.green;
                    break;
                case PairState.PairedLocked:
                    borderImg.color = Color.green;
                    break;
                case PairState.UnpairedNoInfo:
                    borderImg.color = Color.black;
                    break;
                case PairState.Paired0:
                    borderImg.color = Color.green;
                    break;
                case PairState.Paired1:
                    borderImg.color = Color.green;
                    break;
                case PairState.Paired2:
                    borderImg.color = Color.green;
                    break;
                case PairState.RequiredSetup:
                    borderImg.color = Color.green;
                    break;
                case PairState.Offline:
                    borderImg.color = Color.white;
                    break;
                default:
                    break;
            }
            statusText.text = state.ToString();
        });
    }

    private void DongleAPI_OnTrackerStatus(TrackerDeviceInfo device)
    {
        if (device.CurrentIndex == currentIndex)
        {
            deviceInfo = device;
            UnityDispatcher.Invoke(() =>
            {
                centralBtn.interactable = true;
                roleIDDD.interactable = true;
                roleIDDD.SetValueWithoutNotify(deviceInfo.RoleID);
                var battr = (deviceInfo.Battery / 50f) * 6;
                batteryImg.fillAmount = battr;
            });
        }
    }

    private void DongleAPI_OnDisconnected(int trackerIndx)
    {
        if (currentIndex == trackerIndx)
        {
            UnityDispatcher.Invoke(() =>
            {
                currentIndex = transform.GetSiblingIndex();
                centralBtn.interactable = false;
                roleIDDD.interactable = false;
                batteryImg.fillAmount = 0;
                isConnected = false;
            });
        }
    }

    private void DongleAPI_OnConnected(int trackerIndx)
    {
        if (currentIndex == trackerIndx)
        {
            UnityDispatcher.Invoke(() =>
            {
                roleIDDD.interactable = deviceInfo != null;
                centralBtn.interactable = true;
                if (deviceInfo != null)
                    roleIDDD.SetValueWithoutNotify(deviceInfo.RoleID);
                isConnected = true;
            });
        }
    }

    private void DongleAPI_OnButtonClicked(int trackerIndx)
    {
        if (currentIndex == trackerIndx)
        {
            UnityDispatcher.Invoke(() =>
            {
                buttonHightLightImg.gameObject.SetActive(true);
            });
        }
    }

    private void DongleAPI_OnButtonDown(int trackerIndx)
    {
        if (currentIndex == trackerIndx)
        {
            UnityDispatcher.Invoke(() =>
            {
                buttonHightLightImg.gameObject.SetActive(true);
            });
        }
    }
}
