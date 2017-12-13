using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class GameController : MonoBehaviour {

	// -------- Public --------------- //

	// Push buttons
	public Button playButton;
	public Button submitButton;
	public Button yesButton;
	public Button noButton;
	public Button validateSuccessButton;
	public Button validateFailureButton;

	// Panels / Frames
	public GameObject panelPlay;
	public GameObject panelJoueur1;
	public GameObject panelJoueur2;
	public GameObject panelWaitToServer;
	public GameObject panelFailResult;
	public GameObject panelSuccessResult;

	// Texts elements
	public Text winingText;
	public Text amountForJ2Text;

	// Url
	public string url = "http://127.0.0.1:8000/";

	// Slider
	public Slider curseur;

	// ---------------- //

	string gameId;

	string role;

	// When received the corresponding response from server, 
	//      it will be turn to true
	bool gotHelloJ1OrJ2 =  false;
	bool gotJ1SendValueToJ2 = false;
	bool gotJ1AskJ2 = false;
	bool gotJ2GetPropositionJ1 = false;

	bool playerAnswer;
	bool exchangeAccepted;

	int amountPlayer;
	int amountOtherPlayer; 

	// For update
	bool occupied = false;

	// For game state
	int state;

	// States
	int waitingHelloJ1OrJ2 = 0;
	int waitingJ1SendValueToJ2 = 1;
	int waitingJ1AskJ2 = 2;
	int waitingJ2GetPropositionJ1 = 3;
	int endOfGame = 4;
	int waitingUser = 5;

	// Web pages called
	string pageHelloJ1OrJ2 = "";
	string pageJ1SendValueToJ2 = "propose/";
	string pageJ1AskJ2 = "accepted/";
	string pageJ2acceptOrRefuse = "acceptation/";
	string pageJ2AskPropostionFromJ1 = "what_has_been_proposed/"; 

	// Keys of POST requests
	string keyPropositionJ1 = "proposition";
	string keyAcceptJ2 = "choice";
	string keyGameId = "id";

	// Keys of panels dictionary
	int keyPlay = 0;
	int keyJoueur1 = 1;
	int keyJoueur2 = 2;
	int keyWaitToServer = 3;
	int keyFailResult = 4;
	int keySuccessResult = 5;

	Dictionary <int, GameObject> panels = new Dictionary <int, GameObject>();

	// Use this for initialization
	void Start () {

		// Create associations for push buttons
		playButton.onClick.AddListener(Play);
		submitButton.onClick.AddListener (Submit);
		yesButton.onClick.AddListener (Accept);
		noButton.onClick.AddListener (Refuse);
		validateSuccessButton.onClick.AddListener (Reload);
		validateFailureButton.onClick.AddListener (Reload);

		panels [keyPlay] = panelPlay;
		panels [keyFailResult] = panelFailResult;
		panels [keyJoueur1] = panelJoueur1;
		panels [keyJoueur2] = panelJoueur2;
		panels [keySuccessResult] = panelSuccessResult;
		panels [keyWaitToServer] = panelWaitToServer;

	}

	// Update is called once per frame
	void Update () {
		if (occupied) {
			return;
		}
		occupied = true;
		if (state == waitingHelloJ1OrJ2 && gotHelloJ1OrJ2) { //After Play Button.
			ManageRole ();

		} else if (state == waitingJ1SendValueToJ2 && gotJ1SendValueToJ2) { 
			// Server received data. 
			// Next state is to wait the reply from other player (refuse or accept)
			StartCoroutine (J1AskJ2Accept ());
			state = waitingJ1AskJ2;

		} else if (state == waitingJ1AskJ2 && gotJ1AskJ2) {
			// J1 wait reponse from J2: ACCEPT OR REFUSE?
			if (playerAnswer) { // Other player submitted his response
				EndOfGamePlayer1 ();
				state = endOfGame;
			} else {
				gotJ1AskJ2 = false;
				StartCoroutine (J1AskJ2Accept ());
			}
		
		} else if (state == waitingJ2GetPropositionJ1 && gotJ2GetPropositionJ1){	
			// J2 wait for J1 to give his proposition	
			if (playerAnswer) { // If player 1 submitted his proposition...
				amountForJ2Text.text = MessagePropositionToJ2 ();
				Display (keyWaitToServer, false);
				Display (keyJoueur2, true); // Proposition received, display amount and yes/no button.
				state = waitingUser;
			} else {
				gotJ2GetPropositionJ1 = false;
				StartCoroutine (J2GetPropositionJ1());
			}
		}
		occupied = false;
	}

	// --------------------- Push buttons ------------------------------------ //

	void Play () {
		Debug.Log ("Click on: Play!");
		StartCoroutine (HelloJ1OrJ2());

		Display (keyWaitToServer, true);
		Display(keyPlay, false);

		state = waitingHelloJ1OrJ2;
	}

	void Submit () {
		Debug.Log ("Click on: Submit");
		amountPlayer = Mathf.RoundToInt(curseur.value); // Amount J1 keep for himself
		amountOtherPlayer = Mathf.RoundToInt(curseur.maxValue) - amountPlayer; // Amount J1 propose to J2

		StartCoroutine (J1SendValueToJ2()); // Send value to server

		Display(keyWaitToServer, true); // Display waiting screen
		Display (keyJoueur1, false); // Hide screen with slider

		state = waitingJ1SendValueToJ2;
	}

	void Accept () {
		Debug.Log ("Click on: Accept");
		exchangeAccepted = true;
		StartCoroutine (J2AcceptingValueJ1 ());
		EndOfGamePlayer2 ();
	}

	void Refuse () {
		Debug.Log ("Click on: Refuse");
		exchangeAccepted = false;
		StartCoroutine (J2RefusingValueJ1 ());
		EndOfGamePlayer2 ();
	}

	void Reload () {
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	// ------------ Server request for getting role ----------------------- //

	IEnumerator HelloJ1OrJ2 () {
		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddField ("none", "none"); // The form can not be empty

		WWW w = new WWW(url + pageHelloJ1OrJ2, form);
		yield return w;

		if (!string.IsNullOrEmpty(w.error)) {
			Debug.Log(w.error);
			StartCoroutine (HelloJ1OrJ2 ());
		}
		else {
			Debug.Log("HelloJ1OrJ2");
			Debug.Log (w.text);

			string[] response = w.text.Split('/');

			gameId = response [0]; 
			Debug.Log("Game id is : " + gameId.ToString());
			role =  response[1]; // if role == 1 : J1 ; else J2
			gotHelloJ1OrJ2 = true;
		}
	}

	// -------- Server requests from J2 ----------------- //

	IEnumerator J1SendValueToJ2 () {
		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddField (keyGameId, gameId);
		form.AddField(keyPropositionJ1, amountOtherPlayer.ToString());

		WWW w = new WWW(url + pageJ1SendValueToJ2, form);
		yield return w;

		if (!string.IsNullOrEmpty(w.error)) {
			Debug.Log(w.error);
			StartCoroutine (J1SendValueToJ2 ());
		}
		else {
			Debug.Log("J1SendValueToJ2");
			Debug.Log (w.text);
			gotJ1SendValueToJ2 = true;
		}
	}

	IEnumerator J1AskJ2Accept () {
		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddField (keyGameId, gameId);

		WWW ww = new WWW(url+pageJ1AskJ2, form);
		yield return ww;
		if (!string.IsNullOrEmpty(ww.error)) {
			Debug.Log(ww.error);
			StartCoroutine (J1AskJ2Accept ());
		}
		else {
			Debug.Log("Website called for money");
			Debug.Log (ww.text);
			string[] serverResponse = ww.text.Split('/');
			playerAnswer = (serverResponse [0] == "1");
			exchangeAccepted = (serverResponse [1] == "1");
			gotJ1AskJ2 = true;
		}
	}

	// -------- Server requests from J2 ----------------- //

	IEnumerator J2GetPropositionJ1 () {
		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddField (keyGameId, gameId);

		WWW w = new WWW(url+pageJ2AskPropostionFromJ1, form);
		yield return w;
		if (!string.IsNullOrEmpty(w.error)) {
			Debug.Log(w.error);
			StartCoroutine (J2GetPropositionJ1 ());
		}
		else {
			Debug.Log("J2GetPropositionJ1: " + w.text);
			string[] response = w.text.Split('/');
			playerAnswer = response [0] == "1"; // Player 1 give his proposition to server
			amountPlayer =  int.Parse(response[1]); 
			gotJ2GetPropositionJ1 = true;
		}
	}

	IEnumerator J2AcceptingValueJ1 () {
		// Create a Web Form
		WWWForm form = new WWWForm ();
		form.AddField (keyGameId, gameId);
		form.AddField(keyAcceptJ2, "1"); // accept = 1

		WWW w = new WWW(url+pageJ2acceptOrRefuse, form);
		yield return w;

		if (!string.IsNullOrEmpty(w.error)) {
			Debug.Log(w.error);
			StartCoroutine (J2AcceptingValueJ1 ());
		}
		else {
			Debug.Log("You have accepted the offer!");
			exchangeAccepted = true;
		}
	}

	IEnumerator J2RefusingValueJ1 () {
		// Create a Web Form
		WWWForm form = new WWWForm ();
		form.AddField (keyGameId, gameId);
		form.AddField (keyAcceptJ2, "0"); // refuse = 0;

		WWW w = new WWW(url+pageJ2acceptOrRefuse, form);
		yield return w;
		if (!string.IsNullOrEmpty (w.error)) {
			Debug.Log(w.error);
			StartCoroutine (J2RefusingValueJ1 ());
		}
		else {
			Debug.Log("You have refused the offer!");
			Debug.Log (w.text);
			string serverResponse = w.text;

			exchangeAccepted = false;
		}
	}

	// ---------------------------------- //

	void Display (int key, bool value) {
		panels[key].SetActive (value);
	}

	void ManageRole () {

		if (role == "1") {
			// Player 1
			// Hide waiting message
			Display (keyWaitToServer, false);
			Display(keyJoueur1, true);
			state = waitingUser;
			// Wait for the user to click on the submit button (next request: J1askJ2)
		} else {
			// Player 2
			StartCoroutine (J2GetPropositionJ1 ()); // Wait for the proposition of J1.
			state = waitingJ2GetPropositionJ1; // Wait for the response of the server
		}
	}


	void EndOfGamePlayer2 () {
		if (exchangeAccepted) { // If exchange is accepted...
			winingText.text = MessageIfExchangeIsAccepted ();
			Display (keySuccessResult, true);
		} else { // If exchange is refused...
			Display (keyFailResult, true);
		}
		Display (keyJoueur2, false);
	}

	void EndOfGamePlayer1 () {
		if (exchangeAccepted) { // If exchange is accepted...
			winingText.text = MessageIfExchangeIsAccepted ();
			Display (keySuccessResult, true);
		} else { // If exchange is refused...
			Display (keyFailResult, true);
		}
		Display (keyWaitToServer, false);
	}

	// ------------------- Messages --------------------- //

	string MessageIfExchangeIsAccepted () {
		return "Le partage est accepté !\nVous avez gagné " + amountPlayer.ToString () + " !";
	}

	string MessagePropositionToJ2 () {
		return "L'autre joueur a décidé de garder "+ (curseur.maxValue - amountPlayer).ToString () +  
			"\n et vous propose "+ amountPlayer.ToString () + ".\nAcceptez-vous l'offre de l'autre joueur?";
	}
}