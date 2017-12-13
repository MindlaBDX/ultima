using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SliderValueViewer : MonoBehaviour {

    public Text sliderValue;
    string you;
    string him;

    // Use this for initialization
    void Start() {

    }
	// Update is called once per frame
	void Update () {
        him = (10 - GetComponent<Slider>().value).ToString();
        you = GetComponent<Slider>().value.ToString();
        sliderValue.text = "               " + "Vous : " + you +
                "                          " + 
                "Autre joueur : " + him;
		
	}
}
