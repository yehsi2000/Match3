var MyPlugin = {
    $dependencies:{
        peerConnection: null,
        dataChannel: null,
        ws: null,
    },
    $impl : {
        WSAddress : function (){
        let host = window.location.hostname;
        let protocol = window.location.protocol === "https" ? "wss:" : "ws:";
        let wsUrl = protocol + "//" + host + "/matchsig";
        console.log("@websocket url : ", wsUrl);
        if(host == 'localhost') wsUrl = 'ws://zekiddo.iptime.org:9090';
        return wsUrl;
        }
    },

    SendWS : function(message){
        ws.send(message);
    },
    
    GetServerAddress : function(){
        let wsUrl = impl.WSAddress();
        let bufferSize = lengthBytesUTF8(wsUrl) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(wsUrl, buffer, bufferSize);
        return buffer;
    },
    Init : function(){
        const configuration = {'iceServers': [{'urls': 'stun:stun.l.google.com:19302'}]}
        peerConnection = new RTCPeerConnection(configuration);
        dataChannel = peerConnection.createDataChannel("channel");
        let wsUrl = impl.WSAddress();
        ws = new WebSocket(wsUrl);
        ws.addEventListener("open", function(event){
        });
        ws.addEventListener("message", function(event){
            SendMessage('SigClient', 'HandleMessage', event.data);
        });
        peerConnection.addEventListener('icecandidate', event => {
            if (event.candidate) {
                console.log(event.candidate);
                let candidate = {candidate : event.candidate.candidate, 
                    sdpMid : event.candidate.sdpMid, 
                    sdpMLineIndex : event.candidate.sdpMLineIndex,
                    usernameFragment : event.candidate.usernameFragment};
                SendMessage('SigClient', 'SendIceCandidate', JSON.stringify(candidate));
            }
        });

        peerConnection.addEventListener('connectionstatechange', event => {
            console.log(event);
            if (peerConnection.connectionState === 'connected') {
                //window.alert("Connection Successful!");
                SendMessage('SigClient', 'OnConnectionSuccess');
            }
        });

        peerConnection.addEventListener('datachannel', event => {
            dataChannel = event.channel;
        });

        dataChannel.addEventListener('open', event => {
            console.log("Data Channel open!");
            SendMessage('SigClient', 'OnDataChannelOpen');
        });
        
        dataChannel.addEventListener('message', event => {
            const message = event.data;
            //console.log(message);
            //window.alert("Data received: " + message);
            SendMessage('Network', 'HandleDataStream', message);
        });

    },
    makeCall : async function(){
        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);
        SendMessage('SigClient', 'SendOffer', offer.sdp);
    },
    OnReceiveOffer : async function(offer){
        let offerstr = UTF8ToString(offer);
        offerstr.replace('\r\n', ' ');
        //console.log({sdp:offerstr});
        peerConnection.setRemoteDescription(new RTCSessionDescription({type:"offer",sdp:offerstr}));
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        SendMessage('SigClient', 'SendAnswer', answer.sdp);
    },
    OnReceiveAnswer : async function(answer){
        peerConnection.setRemoteDescription(new RTCSessionDescription({type:"answer",sdp:UTF8ToString(answer)}));
    },
    OnReceiveIceCandidate : async function(candidate, sdpMid, sdpMLineIndex, usernamefrag){
        await peerConnection.addIceCandidate(new RTCIceCandidate({
            candidate: UTF8ToString(candidate),
            sdpMid: UTF8ToString(sdpMid),
            sdpMLineIndex: sdpMLineIndex,
            usernameFragment: UTF8ToString(usernamefrag)
        }));
    },
    SendData : function(message){
        if(dataChannel.readyState === 'open'){
            //console.log("sent : " + UTF8ToString(message));
            dataChannel.send(UTF8ToString(message));
        } else {
            console.log(dataChannel.readyState);
            window.alert("Data Channel not open!");
        }
    },
    ErrorAlert : function(errorstring){
        window.alert(UTF8ToString(errorstring));
    }
};

autoAddDeps(MyPlugin, '$impl');
mergeInto(LibraryManager.library, MyPlugin);