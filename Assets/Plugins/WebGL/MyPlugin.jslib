var MyPlugin = {
    $dependencies:{
        peerConnection: null,
        dataChannel: null,
        ws: null,
    },
    SendWS : function(message){
        ws.send(message);
    },
    Init : function(){
        const configuration = {'iceServers': [{'urls': 'stun:stun.l.google.com:19302'}]}
        peerConnection = new RTCPeerConnection(configuration);
        dataChannel = peerConnection.createDataChannel("channel");
        ws = new WebSocket("ws://112.168.15.96:9090");
        ws.addEventListener("open", function(event){
        });
        ws.addEventListener("message", function(event){
            MyGameInstance.SendMessage('SigClient', 'HandleMessage', event.data);
        });
        peerConnection.addEventListener('icecandidate', event => {
            if (event.candidate) {
                console.log(event.candidate);
                let candidate = {candidate : event.candidate.candidate, 
                    sdpMid : event.candidate.sdpMid, 
                    sdpMLineIndex : event.candidate.sdpMLineIndex,
                    usernameFragment : event.candidate.usernameFragment};
                MyGameInstance.SendMessage('SigClient', 'SendIceCandidate', JSON.stringify(candidate));
            }
        });

        peerConnection.addEventListener('connectionstatechange', event => {
            console.log(event);
            if (peerConnection.connectionState === 'connected') {
                //window.alert("Connection Successful!");
                MyGameInstance.SendMessage('SigClient', 'OnConnectionSuccess');
            }
        });

        peerConnection.addEventListener('datachannel', event => {
            dataChannel = event.channel;
        });

        dataChannel.addEventListener('open', event => {
            console.log("Data Channel open!");
            MyGameInstance.SendMessage('SigClient', 'OnDataChannelOpen');
        });
        
        dataChannel.addEventListener('message', event => {
            const message = event.data;
            //console.log(message);
            //window.alert("Data received: " + message);
            MyGameInstance.SendMessage('Network', 'HandleDataStream', message);
        });

    },
    makeCall : async function(){
        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);
        MyGameInstance.SendMessage('SigClient', 'SendOffer', offer.sdp);
    },
    OnReceiveOffer : async function(offer){
        let offerstr = UTF8ToString(offer);
        offerstr.replace('\r\n', ' ');
        //console.log({sdp:offerstr});
        peerConnection.setRemoteDescription(new RTCSessionDescription({type:"offer",sdp:offerstr}));
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        MyGameInstance.SendMessage('SigClient', 'SendAnswer', answer.sdp);
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

mergeInto(LibraryManager.library, MyPlugin);