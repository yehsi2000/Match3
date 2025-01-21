using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct SwapPacket {
    public int x1;
    public int y1;
    public int x2;
    public int y2;
}

public class Network : MonoBehaviour{
    TcpListener tcpListener;
    Thread tcpListenerThread;
    TcpClient serverSocket;
    TcpClient connectedTcpClient;
    public Queue<SwapPacket> packetQueue;
    public static Network instance;
    public bool isServer = true;

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
}
