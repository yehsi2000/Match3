const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 9090 });
const clients = new Map();
const connections = new Map();

wss.on('connection', (ws) => {
    const id = generateUniqueId();
    clients.set(id, ws);
    ws.id = id;
    console.log(`Client connected with id: ${id}`);
    ws.send("$connect:"+id);

    ws.on('message', (message) => {
        const parsedMessage = JSON.parse(message);
        const targetId = parsedMessage.targetId;
        console.log("targetid = ",targetId);
        if(targetId=="") return;
        const targetClient = clients.get(targetId);

        if (targetClient && targetClient.readyState === WebSocket.OPEN) {
            if (connections.has(targetId) && connections.get(targetId) !== id) {
                console.log(`Client ${id} is trying to connect to ${targetId}, but ${targetId} is already connected to ${connections.get(targetId)}`);
                ws.send("$error:Target client is already connected to another client");
                return;
            }

            console.log(`Message sent from ${id} to ${targetId}:`, parsedMessage);
            parsedMessage.targetId = id;
            targetClient.send(JSON.stringify(parsedMessage));

            if (parsedMessage.type === 'offer' || parsedMessage.type === 'answer') {
                connections.set(id, targetId);
                connections.set(targetId, id);
            }
        }
    });

    ws.on('close', () => {
        clients.delete(id);
        console.log(`Client disconnected with id: ${id}`);
    });
});

function generateUniqueId() {
    return Math.random().toString(10).substring(2,6) + '-' + (Date.now()/1000).toString(10).substring(2,6);
}

console.log('WebSocket signaling server is running on ws://localhost:9090');
