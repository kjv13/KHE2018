const Server = require('socket.io');
const randomKey = require('../util/randomKey.js');


const ON_EVENTS = {
    createRoom: 'createRoom',
    joinRoom: 'joinRoom',
    gatheringScores: 'gatheringScores',
    gameQuit: 'gameComplete',
}

const EMIT_EVENTS = {
    joinRoom: 'joinRoom',
    waitingRoomTimer: 'waitingRoomTimer',
    startGame: 'startGame',
    collectScores: 'collectScores',
    collectingScores: 'collectingScores',
    finalScores: 'finalScores',
    timeLeft: 'timeLeft',
    gameOver: 'gameOver'
}


function countDownInterval(seconds, body, end) {
    let i = seconds;
    const interval = setInterval(function () {
        body(i);
        if (--i === 0) {
            clearInterval(interval);
            end();
        }
    }, 1000);
    return interval;
}

function setUpGameRoom(io, key) {
    const room = io.sockets.in(key);

    const waitingRoomInterval = countDownInterval(3, (secondsLeft) => {
        room.emit(EMIT_EVENTS.waitingRoomTimer, secondsLeft);
    }, () => {
        room.emit(EMIT_EVENTS.startGame);

        countDownInterval(6, (secondsLeft) => {
            room.emit(EMIT_EVENTS.timeLeft, secondsLeft);
        }, () => {


            room.emit(EMIT_EVENTS.gameOver);

            const collectedScores = [];

            room.on(ON_EVENTS.gatheringScores, function (data) {
                const score = data.score;
                const userName = data.userName;
                const uid = data.uid;
                collectedScores.push({
                    score: score,
                    userName: userName,
                    uid: uid
                });
            })

            room.emit(EMIT_EVENTS.collectScores);

            countDownInterval(5, (timeLeft) => {
                room.emit(EMIT_EVENTS.collectingScores);
            }, () => {
                // room.off(ON_EVENTS.gatheringScores);
                room.emit(EMIT_EVENTS.finalScores, collectedScores);
            })

        })

    })




    room.on(ON_EVENTS.gameQuit, function (payload) {
        console.log('game bad');
        clearInterval(waitingRoomInterval);
    })
}

const setUpSocketIO = function (server) {


    let io = Server(server);

    io.on('connect_error', function (err) {
        console.log('Error connecting to server');
    });

    io.on('connection', function (socket) {

        socket.on(ON_EVENTS.createRoom, function () {
            const key = randomKey.get();
            console.log('Room being created with key: ', key);

            socket.join(key);
            socket.emit(ON_EVENTS.roomJoined, { key: key });
            setUpGameRoom(io, key);


        });
        

        socket.on(ON_EVENTS.joinRoom, function (key) {
            const trimedKey = key.trim();
            console.log('Joining room with key: ', trimedKey);
            socket.join(trimedKey);
            socket.emit(EMIT_EVENTS.roomJoined, { key: trimedKey });
        });

        console.log('user connected')
    });

    return io;

}
module.exports = {
    setup: setUpSocketIO
}