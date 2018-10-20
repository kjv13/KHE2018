﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using GoogleARCore.CrossPlatform;

[System.Serializable]
public class RoomKey
{
    public string key;
}

public class User
{
    public string uid;
    public bool isHost;
    public string name;
    public string roomKey;
    public AnchorPayload achor;

    public User()
    {
        uid = System.Guid.NewGuid().ToString();
        isHost = false;
        name = "Unset";
        roomKey = "";
    }

    public string asJson()
    {
        return JsonUtility.ToJson(this);
    }

}

public class AnchorPayload {
	public XPAnchor physicalAnchor;
	public int roomNumber;
	public string ipAddress;

	public string asJson() {
		return JsonUtility.ToJson(this);
	}
}


// TODO: timeLeft and payload for each emit event.
// should update accordingly. the game sequence should always have a value.
public class GameSequence {
	public User user;
	public string joinedRoom;
	public Socket relay;	
	public int timeLeft = 60;
	public bool isHost = false;
    public InputField inputRoom;

	private readonly string wsEndpoint = "ws://party-play.herokuapp.com/";

	public GameSequence(User usr, InputField fld) {
		user = usr;
		inputRoom = fld;
		SetupClient();
	}

    public void SetupClient() {

        // relay = IO.Socket("ws://party-play.herokuapp.com/");
        relay = IO.Socket(this.wsEndpoint);

        relay.On(Socket.EVENT_CONNECT, () =>
        {
            Debug.Log("Connected to middleman");
        });
    }

    // host creates a room
    public void hostCreatesARoom() {
        isHost = true;
		relay.Emit("createRoom", user.asJson());
    }

    public void joinRoom()
    {
        user.roomKey = inputRoom.text;
        // Join specified room id
        relay.Emit("joinRoom", user.asJson());
    }

    public void drop()
    {
        relay.Emit("leaveRoom", user.asJson());
    }

    public void broadcastLocalization() {
    	relay.Emit("broadcastLocalizationData", user.achor.asJson());
    }


    public void startGame()
    {
        user.roomKey = inputRoom.text;
        relay.Emit("startGame", user.asJson());
    }

    public void broadcastData() {
    	relay.Emit("broadcastData", user.asJson());
    }

    public void stopGame() {
    	relay.Emit("stopGame", user.asJson());
    }

    public void setupListeners() {
		// When anyone joins the room output room Id
        relay.On("joinedRoom", (data) =>
        {
            string str = data.ToString();
            RoomKey id = JsonUtility.FromJson<RoomKey>(str);
            user.roomKey = id.key;
            // Debug.Log(id.key);
            // Debug.Log(user.uid);
        });

        // This is emmited from the channel of the room
        relay.On("newMemberJoined", (data) =>
         {
             User newUser = JsonUtility.FromJson<User>(data.ToString());
             if (user.uid == newUser.uid)
             {
                 Debug.Log("I Joined!");
             }
             else
             {
                 Debug.Log("New person joined!");
                 Debug.Log(newUser.uid);
             }
         });


        // This is emmited from the channel of the room
        relay.On("memberDropped", (data) =>
         {
             User deadUser = JsonUtility.FromJson<User>(data.ToString());
             Debug.Log("Someone left!");
             Debug.Log(deadUser.uid);

         });
    }


}

// potentially derivable UI abstraction.
//  need to use for main thread UI updates.
// NOTE: for UI updates, and an example on the socket client,
// see: https://github.com/floatinghotpot/socket.io-unity/blob/master/Demo/SocketIOScript.cs
public class GameUI
{
}

public class GameDataRelay : MonoBehaviour
{
	// private readonly string httpEndpoint = "https://party-play.herokuapp.com/";
	// private readonly string wsEndpoint = "ws://party-play.herokuapp.com/";
    public Text dataEntry;
    public Text txtRoom;
    public GameSequence seq;
    public InputField inputRoom;


    void Awake()
    {
        User user = new User();
        seq = new GameSequence(user, inputRoom);
    }


    void Start()
    {
    	seq.setupListeners();
    }

    // ui update loop
    void Update() {

    }

    public void createRoom() {
    	seq.hostCreatesARoom();
    }

    public void leaveRoom() {
    	seq.drop();
    }

    public void joinRoom() {
    	seq.joinRoom();
    }

    public void endGame() {
    	seq.stopGame();
    }

    public void sendLocalizationData() {

    }

}
