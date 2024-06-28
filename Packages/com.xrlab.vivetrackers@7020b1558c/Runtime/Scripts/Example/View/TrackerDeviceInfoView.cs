using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] TextMeshProUGUI trackerStatusText;
    [SerializeField] TextMeshProUGUI dongleStatusText;
    [SerializeField] TextMeshProUGUI ATM_ModeText;
    [SerializeField] TMP_Dropdown roleIDDD;
    [SerializeField] Button centralBtn;
    [SerializeField] Button unpairBtn;
    [SerializeField] Button powerBtn;
    [SerializeField] Button remapBtn;

    static System.Type dropDownRoles;

    OculusActivityPlugin plugin;
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
            dongleAPI.OnDongleStatus += DongleAPI_OnDongleStatus;
            dongleAPI.OnTrack += DongleAPI_OnTrack;
        }
    }

    Color bgFromColor;
    Color lightFromColor;
    bool isConnected = false;
    bool isHost = false;
    Vector3 startPoint;
    Vector3 endPoint;
    Transform tracker;

    [Header("Debug")]
    [SerializeField] float distance;
    [SerializeField] TrackData data;
    [SerializeField] CanvasGroup ack_canvasGroup;
    [SerializeField] TMP_InputField ack_command;
    [SerializeField] Button sendAckBtn;

    public static void AddRoleEnum(System.Type roles)
    {
        dropDownRoles = roles;
    }

    private void Start()
    {
        UnityDispatcher.Create();
        plugin = FindAnyObjectByType<OculusActivityPlugin>();
        currentIndex = transform.GetSiblingIndex();
        bgFromColor = bgImg.color;
        lightFromColor = bgLight.color;

        if (dropDownRoles != null)
        {
            List<string> list = new List<string>(System.Enum.GetNames(dropDownRoles.GetType()));
            roleIDDD.ClearOptions();
            roleIDDD.AddOptions(list); 
        }

        roleIDDD?.onValueChanged.AddListener(OnRoleChange);
        centralBtn?.onClick.AddListener(OnCentralBtnClick);
        unpairBtn?.onClick.AddListener(OnUnpairBtnClick);
        powerBtn?.onClick.AddListener(OnPowerBtnClick);
        sendAckBtn?.onClick.AddListener(SendACK);
        remapBtn.onClick.AddListener(OnRemap);
    }

    private void OnRemap()
    {
        currentIndex = transform.GetSiblingIndex();
        dongleAPI.ReMap(currentIndex);
    }

    private void OnPowerBtnClick()
    {
        currentIndex = transform.GetSiblingIndex();
        dongleAPI.PowerOff(currentIndex);
    }

    private void OnUnpairBtnClick()
    {
        currentIndex = transform.GetSiblingIndex();
        dongleAPI.Unpair(currentIndex);
    }

    private void OnCentralBtnClick()
    {
        currentIndex = transform.GetSiblingIndex();
        Blink();
        tracker = plugin.GetTracker(currentIndex);
        if (tracker != null)
            startPoint = tracker.position;
    }

    void Blink()
    {
        currentIndex = transform.GetSiblingIndex();
        dongleAPI.Blink(currentIndex);
        StartCoroutine(_OnBlink());
    }

    IEnumerator _OnBlink()
    {
        for (int i = 0; i < 4; i++)
        {
            bgLight.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            bgLight.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
        }
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
            bgImg.color = Color.Lerp(bgImg.color, isHost ? Color.blue : bgFromColor, Time.deltaTime);
            bgLight.color = Color.Lerp(bgLight.color, lightFromColor, Time.deltaTime);
        }

        var c = buttonHightLightImg.color;
        c.a = Mathf.Lerp(c.a, 0, Time.deltaTime);
        buttonHightLightImg.color = c;

        if (tracker != null)
            distance = Vector3.Distance(startPoint, tracker.position);
    }
    
    private void DongleAPI_OnDongleStatus(PairState[] states)
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
                case PairState.PowerOff:
                    borderImg.color = Color.green;
                    break;
                case PairState.Empty:
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
            //if (!isConnected)
            //    statusText.text = state.ToString();
            //else statusText.text = data.status.ToString();
            dongleStatusText.text = state.ToString();
        });
    }

    private void DongleAPI_OnTrackerStatus(TrackerDeviceInfo device)
    {
        if (device.CurrentIndex == currentIndex)
        {
            deviceInfo = device;
            UnityDispatcher.Invoke(() =>
            {
                centralBtn.interactable = deviceInfo != null;
                if (ack_canvasGroup != null) ack_canvasGroup.interactable = deviceInfo != null;
                roleIDDD.interactable = deviceInfo != null;
                roleIDDD.SetValueWithoutNotify(deviceInfo.RoleID);
                var battr = (deviceInfo.Battery / 50f);// * 6;
                batteryImg.fillAmount = battr;
                batteryImg.color = Color.Lerp(Color.red, Color.green, battr);
                isConnected = true;
                isHost = device.IsHost;
                trackerStatusText.text = deviceInfo.status.ToString();
                ATM_ModeText.text = deviceInfo.Mode.ToString();
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
                if (ack_canvasGroup != null) ack_canvasGroup.interactable = false;
                batteryImg.fillAmount = 0;
                isConnected = false;
                trackerStatusText.text = "wait connection...";
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
                if (ack_canvasGroup != null) ack_canvasGroup.interactable = deviceInfo != null;
                centralBtn.interactable = deviceInfo != null;
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
                var c = buttonHightLightImg.color;
                c.a = 1;
                buttonHightLightImg.color = c;
                isConnected = true;
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
                var c = buttonHightLightImg.color;
                c.a = 1;
                buttonHightLightImg.color = c;
                isConnected = true;
            });
        }
    }

    private void DongleAPI_OnTrack(int trackerIndx, TrackData trackData, long time_delta)
    {
        if (currentIndex == trackerIndx)
        {
            data = trackData;
            isConnected = true;
        }
    }

    void SendACK()
    {
        if(!string.IsNullOrEmpty(ack_command.text))
        {
            dongleAPI.SendAck(currentIndex, ack_command.text);
        }
    }
}
