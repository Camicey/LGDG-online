using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoutonTourSuivant : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().interactable = false;
    }

    public void OnClick()
    {
        PlayerManager joueur = PlayerManager.LocalPlayer;
        if (GameManager.Instance.EtatDuJeu == "Jouer")
        {
            joueur.CmdFinTour();
            UnityEngine.Debug.Log(GameManager.Instance.Tour);
        }
        else if (GameManager.Instance.EtatDuJeu == "Preparation") //Si c'est le tour de pose
        {
            joueur.CmdJeSuisPret();
            UnityEngine.Debug.Log("Le joueur est prêt ?" + joueur.EstPret);
            GetComponent<Button>().interactable = false;
        }

    }

}
