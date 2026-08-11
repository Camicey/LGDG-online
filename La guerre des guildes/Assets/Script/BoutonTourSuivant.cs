using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoutonTourSuivant : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().interactable = false;
        GetComponent<Button>().GetComponentInChildren<TMP_Text>().text = "Prêt";
    }

    public void OnClick()
    {
        PlayerManager joueur = PlayerManager.LocalPlayer;
        if (GameManager.Instance.EtatDuJeu == "Jouer") { joueur.CmdFinTour(); }
        else if (GameManager.Instance.EtatDuJeu == "Preparation") //Si c'est le tour de pose
        {
            joueur.CmdJeSuisPret();
            GetComponent<Button>().interactable = false;
        }

    }

}
