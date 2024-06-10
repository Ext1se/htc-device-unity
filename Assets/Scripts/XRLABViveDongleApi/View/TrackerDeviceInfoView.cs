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
            currentIndex = transform.GetSiblingIndex();
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
        UnityDispatcher.Invoke(() =>
        {
            if (device.CurrentIndex == currentIndex)
            {
                var battr = (device.Battery / 50f) * 6;
                batteryImg.fillAmount = battr;
            }
        });
    }

    private void DongleAPI_OnDisconnected(int trackerIndx)
    {
        UnityDispatcher.Invoke(() =>
        {
            currentIndex = transform.GetSiblingIndex();
            if (currentIndex == trackerIndx)
            {
                batteryImg.fillAmount = 0;
                isConnected = false;
            }
        });
    }

    private void DongleAPI_OnConnected(int trackerIndx)
    {
        UnityDispatcher.Invoke(() =>
        {
            currentIndex = transform.GetSiblingIndex();
            if (currentIndex == trackerIndx)
            {
                isConnected = true;
            }
        });
    }

    private void DongleAPI_OnButtonClicked(int trackerIndx)
    {
        UnityDispatcher.Invoke(() =>
        {
            currentIndex = transform.GetSiblingIndex();
            if (currentIndex == trackerIndx)
                buttonHightLightImg.gameObject.SetActive(false);
        });
    }

    private void DongleAPI_OnButtonDown(int trackerIndx)
    {
        UnityDispatcher.Invoke(() =>
        {
            currentIndex = transform.GetSiblingIndex();
            if (currentIndex == trackerIndx)
                buttonHightLightImg.gameObject.SetActive(true);
        });
    }
}
