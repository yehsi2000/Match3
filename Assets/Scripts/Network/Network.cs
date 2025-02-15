using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;



public class Network : MonoBehaviour{
//     TcpListener tcpListener;
//     Thread tcpListenerThread;
//     TcpClient serverSocket;
//     TcpClient connectedTcpClient;
    public static Network instance;

    [SerializeField]
    MultiGameController multiGameController;


    [DllImport("__Internal")]
    private static extern void SendData(string data);

    [DllImport("__Internal")]
    private static extern void ErrorAlert(string errorstring);

    private Coroutine messageRoutine;
    private Coroutine gameStartCheck;

    private int myseed;

    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else if (instance != this) {
            Destroy(gameObject);
        }
    }

    private void Start() {
        multiGameController.isReady = true;
        messageRoutine = StartCoroutine(SendMyInfo());
        gameStartCheck = StartCoroutine(CheckBothReady());
    }

    private void OnDestroy() {
        multiGameController.isReady = false;
        StopCoroutine(messageRoutine);
        StopCoroutine(gameStartCheck);
    }

    IEnumerator SendMyInfo() {
        myseed = getRandomSeed();
        multiGameController.RandomSeed = myseed;
        while (true) {
            if (multiGameController.isOpponentReady)
                yield break;
            SendData("name=" + PlayerPrefs.GetString("myid", "Player2"));
            SendData("seed=" + myseed);
            yield return new WaitForSeconds(0.5f);
        }
        
    }

    IEnumerator CheckBothReady() {
        while (true) {
            if (multiGameController.isOpponentReady && multiGameController.isReady) {
                multiGameController.StartGame();
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void HandleDataStream(string data) {
        //Debug.Log("data received = " + data);
        if (data.StartsWith("flip")) {
            string[] flipdat = data.Substring(data.IndexOf("=") + 1).Split(',');
            string[] p1 = flipdat[0].Split(":");
            string[] p2 = flipdat[1].Split(":");
            MultiGameController.SwapPacket packet = new MultiGameController.SwapPacket();
            try {
                packet.x1 = Int32.Parse(p1[0]);
                packet.y1 = Int32.Parse(p1[1]);
                packet.x2 = Int32.Parse(p2[0]);
                packet.y2 = Int32.Parse(p2[1]);
            }
            catch (FormatException e) {
                ErrorAlert("Error while receiving piece packet : " + e);
            }
            //Debug.Log("queue count:" + multiGameController.packetQueue.Count);
            multiGameController.packetQueue.Enqueue(packet);
        }
        else if (data.StartsWith("seed")) {
            if (Int32.TryParse(data.Substring(data.IndexOf("=") + 1), out int res)) {
                multiGameController.RandomSeed = res;
                SendData("ready");
            }
            else {
                ErrorAlert("Error : Wrong random seed received!");
            }
        }
        else if (data.StartsWith("name")) {
            multiGameController.OpponentId = data.Substring(data.IndexOf("=") + 1);
        }
        else if (data.StartsWith("score")) {
            if (Int32.TryParse(data.Substring(data.IndexOf("=") + 1), out int res)) {
                ScoreManager.instance.SetOpScore(res);
            }
        }
        else if (data.StartsWith("ready")) {
            multiGameController.isOpponentReady = true;
        }
        else if (data.StartsWith("special")) {
            string[] pressdat = data.Substring(data.IndexOf("=") + 1).Split(':');
            Point pnt = new Point(0, 0);
            int type = 0;
            try {
                pnt.x = Int32.Parse(pressdat[0]);
                pnt.y = Int32.Parse(pressdat[1]);
                type = Int32.Parse(pressdat[2]);

            }
            catch (FormatException e) {
                ErrorAlert("Error while receiving piece packet : " + e);
            }
            SpecialType val = new SpecialType((SpecialType.ESpecialType)type);
            multiGameController.ReceiveSpecialPress(pnt, val);
        }
        else if (data.StartsWith("gameover")) {
            if (Int32.TryParse(data.Substring(data.IndexOf("=") + 1), out int res)) {
                ScoreManager.instance.SetOpScore(res);
                multiGameController.FinalScoreTextUpdate(false);
            }
        }

    }

    public void SendFlip(NodePiece selected, NodePiece flipped) {
        if (selected != null && flipped != null) {
            Debug.Log($"Send flip {selected.index.x} {selected.index.y} with {flipped.index.x} {flipped.index.y}");
            SendData($"flip={selected.index.x}:{selected.index.y},{flipped.index.x}:{flipped.index.y}");
        }
    }

    public void SendScore(int score) {
        SendData($"score={score}");
    }

    public int getRandomSeed() {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789!@#$%^&*()";
        for (int i = 0; i < 20; i++)
            seed += acceptableChars[UnityEngine.Random.Range(0, acceptableChars.Length)];
        if (seed.GetHashCode() == 0) return getRandomSeed();
        else return seed.GetHashCode();
    }

    public void SendSpecialPressed(Point pnt, SpecialType val) {
        SendData($"special={pnt.x}:{pnt.y}:{(int)val.TypeVal}");
    }

    public void SendGameOver(int score) {
        SendData($"gameover={score}");
    }

    /*
     * Socket connection remains
    public bool isConnected {
        get { return connectedTcpClient != null || serverSocket !=null; }
    }

    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else if (instance != this) {
            Destroy(gameObject);
        }
        packetQueue = new Queue<SwapPacket>();
        if (isServer) {
            tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequest));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
        } else {
            serverSocket = new TcpClient();
            Connect();
            tcpListenerThread = new Thread(new ThreadStart(GetMessage));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
            
        }
        
    }

    public void SendFlip(NodePiece selected, NodePiece flipped) {
        TcpClient sendSocket = isServer ? connectedTcpClient : serverSocket;
        if (sendSocket == null) {
            Debug.Log("No client connected");
            return;
        }
        if (selected != null && flipped != null) {
            Debug.Log($"Flip {selected.index.x} {selected.index.y} with {flipped.index.x} {flipped.index.y}");
            SwapPacket packet = new SwapPacket();
            packet.x1 = selected.index.x;
            packet.y1 = selected.index.y;
            packet.x2 = flipped.index.x;
            packet.y2 = flipped.index.y;
            byte[] data = StructureToByteArray(packet);
            
            sendSocket.GetStream().Write(data, 0, data.Length);
        }
    }

    private void Connect() {
        serverSocket.Connect("192.168.0.6", 5000);
    }


    private void GetMessage() {
        while (serverSocket != null && serverSocket.Connected) {
            try {
                Byte[] bytes = new Byte[1024];
                using (NetworkStream stream = serverSocket.GetStream()) {
                    int length;
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        SwapPacket packet = new SwapPacket();
                        packet = ByteArrayToStructure<SwapPacket>(incommingData);
                        Debug.Log("Received: " + packet.x1 + " " + packet.y1 + " " + packet.x2 + " " + packet.y2);
                        packetQueue.Enqueue(packet);
                    }
                }
            }
            catch (SocketException socketException) {
                Debug.Log("SocketException " + socketException.ToString());
            }
        }
    }

    private void ListenForIncommingRequest() {
        try {
            tcpListener = new TcpListener(IPAddress.Any, 5000);
            tcpListener.Start();
            Debug.Log("Server is listening on 5000");
            Byte[] bytes = new Byte[1024];
            while (true) {
                using (connectedTcpClient = tcpListener.AcceptTcpClient()) {
                    using (NetworkStream stream = connectedTcpClient.GetStream()) {
                        int length;
                        if (connectedTcpClient.Connected) {
                            Debug.Log("Connected");
                        }
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            SwapPacket packet = new SwapPacket();
                            packet = ByteArrayToStructure<SwapPacket>(incommingData);
                            Debug.Log("Received: " + packet.x1 + " " + packet.y1 + " " + packet.x2 + " " + packet.y2);
                            packetQueue.Enqueue(packet);
                        }
                    }
                }
            }
        }
        catch (SocketException socketException) {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        return stuff;
    }

    public static byte[] StructureToByteArray<T>(T structure) where T : struct {
        int size = Marshal.SizeOf(structure);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    */
}
