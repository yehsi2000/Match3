using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NativeWebSocket;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using System;

public class SignalingClient : MonoBehaviour {
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField myIdInput;
    [SerializeField] private TMP_InputField targetIdInput;
    [SerializeField] private TMP_Text copyAlertText;
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button joinBtn;
    [SerializeField] private Button gameStartBtn;
    [SerializeField] private Button backBtn;

    private WebSocket websocket;

    private string targetID;
    private string myID;


    [DllImport("__Internal")]
    private static extern void Init();
    [DllImport("__Internal")]
    private static extern void makeCall();
    [DllImport("__Internal")]
    private static extern void OnReceiveOffer(string offer);
    [DllImport("__Internal")]
    private static extern void OnReceiveAnswer(string answer);
    [DllImport("__Internal")]
    private static extern void OnReceiveIceCandidate(string candidate, string sdpMid, int sdpMLineIndex, string usernamefrag);
    [DllImport("__Internal")]
    private static extern void SendData(string data);
    [DllImport("__Internal")]
    private static extern void ErrorAlert(string errorstring);
    [DllImport("__Internal")]
    private static extern void SendWS(string message);

    [HideInInspector]
    public bool isConnected;

    async void Awake() {
        Debug.Log("start sig client");
        DontDestroyOnLoad(gameObject);
        isConnected = false;
        Init();
        websocket = new WebSocket(ServerConfig.wsServer);
        await websocket.Connect();
        if (websocket.State == WebSocketState.Open) Debug.Log("WebSocket connected!");
        websocket.OnMessage += (bytes) => {
            // 수신한 메시지 처리
            HandleMessage(bytes);
        };
        websocket.OnClose += (e) => { Debug.Log("WebSocket closed : " + e); };
        websocket.OnOpen += () => { Debug.Log("WebSocket open"); };
        websocket.OnError += (e) => { Debug.Log("WebSocket error : " + e); };

        createRoomBtn.onClick.AddListener(() => {
            if (websocket.State == WebSocketState.Open) myIdInput.text = myID;
        });

        joinBtn.onClick.AddListener(() => {
            targetID = targetIdInput.text;
            makeCall();
        });

        
        gameStartBtn.onClick.AddListener(() => {
            //websocket.Close();
            PlayerPrefs.SetString("myid", nameInput.text);
            SceneManager.LoadScene("MultiGameScene");
        });
        gameStartBtn.gameObject.SetActive(false);
        

        backBtn.onClick.AddListener(() => {
            Destroy(gameObject);
            Destroy(this);
            SceneManager.LoadScene("ModeSelectionScene");
        });
    }

    private void Update() {
        //#if !UNITY_WEBGL || UNITY_EDITOR
        //websocket.DispatchMessageQueue();
        //#endif
    }

    async void SendWebSocketMessage(string message) {
        if (websocket.State == WebSocketState.Open) {
            await websocket.SendText(message);
            //SendWS(message);
            //Debug.Log("Message sent: " + message);
        } else {
            Debug.Log("cannot send as websocket is "+websocket.State);
        }
    }

    public void HandleMessage(string message) {
        //Debug.Log("Message received: " + message);
        if (message.StartsWith("$connect")) {
            string connectRes = message.Substring(message.IndexOf(":") + 1);
            myID = connectRes;
            return;
        }
        else if (message.StartsWith("$ready")) {
            string seed = message.Substring(message.IndexOf(":") + 1);
            if (Int32.TryParse(seed, out int res))
                PlayerPrefs.SetInt("rngseed", res);
        }
        else if (message.StartsWith("$error")) {
            string errorMsg = message.Substring(message.IndexOf(":") + 1);
            targetID = "";
            targetIdInput.text = "";
            ErrorAlert(errorMsg);
            return;
        }

        var json = JsonUtility.FromJson<SignalingMessage>(message);
        //Debug.Log($"targetid:{json.targetId}, type:{json.type}, sdp:{json.sdp}, candidate:{json.candidate}");

        if (json.type.Equals("offer")) {
            Debug.Log("targetID=" + targetID);
            if (targetID == null || targetID.Length == 0) targetID = json.targetId;
            Debug.Log("Received offer: " + json.sdp);
            OnReceiveOffer(json.sdp);
        }
        else if (json.type == "answer") {
            Debug.Log("Received Answer: " + json.sdp);
            OnReceiveAnswer(json.sdp);
        }
        else if (json.type == "candidate") {
            var candidateInit = JsonUtility.FromJson<SerializableRTCIceCandidateInit>(json.candidate);
            int sdpindex = candidateInit.sdpMLineIndex ?? 0;
            OnReceiveIceCandidate(candidateInit.candidate, candidateInit.sdpMid, sdpindex, candidateInit.usernameFragment);
        }
    }

    void HandleMessage(byte[] bytes) {
        string message = System.Text.Encoding.UTF8.GetString(bytes).Trim();
        //Debug.Log("Message received: " + message);
        if (message.StartsWith("$connect")) {
            string connectRes = message.Substring(message.IndexOf(":") + 1);
            myID = connectRes;
            return;
        }
        else if (message.StartsWith("$ready")) {
            string seed = message.Substring(message.IndexOf(":") + 1);
            if (Int32.TryParse(seed, out int res))
                PlayerPrefs.SetInt("rngseed",res);
        }
        else if (message.StartsWith("$error")) {
            string errorMsg = message.Substring(message.IndexOf(":") + 1);
            targetID = "";
            targetIdInput.text = "";
            ErrorAlert(errorMsg);
            return;
        }
        
        var json = JsonUtility.FromJson<SignalingMessage>(message);
        //Debug.Log($"targetid:{json.targetId}, type:{json.type}, sdp:{json.sdp}, candidate:{json.candidate}");

        if (json.type.Equals("offer")) {
            Debug.Log("targetID="+targetID);
            if (targetID == null || targetID.Length == 0) targetID = json.targetId;
            Debug.Log("Received offer: " + json.sdp);
            OnReceiveOffer(json.sdp);
        }
        else if (json.type == "answer") {
            Debug.Log("Received Answer: " + json.sdp);
            OnReceiveAnswer(json.sdp);
        }
        else if (json.type == "candidate") {
            var candidateInit = JsonUtility.FromJson<SerializableRTCIceCandidateInit>(json.candidate);
            int sdpindex = candidateInit.sdpMLineIndex ?? 0;
            OnReceiveIceCandidate(candidateInit.candidate, candidateInit.sdpMid, sdpindex, candidateInit.usernameFragment);
        }
    }

    public void SendOffer(string sdp) {
        var message = new SignalingMessage { targetId = targetID, type = "offer", sdp = sdp };
        SendWebSocketMessage(JsonUtility.ToJson(message));
    }

    public void SendAnswer(string sdp) {
        var message = new SignalingMessage { targetId = targetID, type = "answer", sdp = sdp };
        SendWebSocketMessage(JsonUtility.ToJson(message));
    }

    public void SendIceCandidate(string iceCandidate) {
        var message = new SignalingMessage { targetId = targetID, type = "candidate", candidate = iceCandidate };
        SendWebSocketMessage(JsonUtility.ToJson(message));
    }

    public void SendMsg(string message) {
        SendData(message);
    }

    public void OnDataChannelOpen() {
        Debug.Log(gameStartBtn);
        gameStartBtn.gameObject.SetActive(true);
    }

    public void OnConnectionSuccess() {
        //TODO : 연결성공 메세지
        isConnected = true;
    }

    [System.Serializable]
    public struct SignalingMessage {
        public string targetId;
        public string type;
        public string sdp;
        public string candidate;
    }

    [System.Serializable]
    public struct SerializableRTCIceCandidateInit {
        public string candidate;
        public string sdpMid;
        public int? sdpMLineIndex;
        public string usernameFragment;
    }
}
