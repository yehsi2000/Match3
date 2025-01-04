using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using UnityEditor.Sprites;


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
struct SwapPacket {
    public int x1;
    public int y1;
    public int x2;
    public int y2;
}

public class MultiGameController : Match3 {
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    private Queue<SwapPacket> packetQueue;

    protected override void Start() {
        base.Start();
        packetQueue = new Queue<SwapPacket>();
    }

    protected override void Awake() {
        base.Awake();
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequest));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    protected override void Update() {
        base.Update();
        if (packetQueue.Count > 0) {
            SwapPacket packet = packetQueue.Dequeue();
            ProcessFlip(packet);
        }
    }

    void ProcessFlip(SwapPacket packet) {
        Node selected = getNodeAtPoint(new Point(packet.x1, packet.y1));
        Node flipped = getNodeAtPoint(new Point(packet.x2, packet.y2));
        if (selected != null && flipped != null) {
            if (selected.GetPiece() != null && flipped.GetPiece() != null) {
                //selected.GetPiece().MovePositionTo(flipped.GetPiece().transform.position);
                //flipped.GetPiece().MovePositionTo(selected.GetPiece().transform.position);
                FlipPieces(new Point(packet.x1, packet.y1), new Point(packet.x2, packet.y2), true);
            }
        }
    }

    protected override void SendFlip(NodePiece selected, NodePiece flipped) {
        if (connectedTcpClient == null) {
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
            connectedTcpClient.GetStream().Write(data, 0, data.Length);
        }
    }

    private void Connect() {

    }

    private void ListenForIncommingRequest() {
        try {
            tcpListener = new TcpListener(IPAddress.Any, 5000);
            tcpListener.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[1024];
            while (true) {
                using (connectedTcpClient = tcpListener.AcceptTcpClient()) {
                    using (NetworkStream stream = connectedTcpClient.GetStream()) {
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
            }
        } catch (SocketException socketException) {
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
