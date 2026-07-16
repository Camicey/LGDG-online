using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoutonTourSuivant : MonoBehaviour
{
    public bool EstClique;
    public static BoutonTourSuivant Instance;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().interactable = false;
    }

    public void Awake() { Instance = this; }

    public void OnClick()
    {
        //Passer à l'autre joueur en cours
        //Lui remet des PMs
        UnityEngine.Debug.Log(GameManager.Instance.EtatDuJeu);
        if (GameManager.Instance.EtatDuJeu == "Jouer")
        {
            GameManager.Instance.JoueurEnCours = GameManager.Instance.TousLesJoueurs[GameManager.Instance.Tour % 2];
            GameManager.Instance.Tour++;
            UnityEngine.Debug.Log(GameManager.Instance.Tour);

        }
        else if (GameManager.Instance.EtatDuJeu == "Preparation")
        {
            //On donne la main au premier joueur
            GameManager.Instance.JoueurEnCours = GameManager.Instance.TousLesJoueurs[1];
            GameManager.Instance.Tour = 1;
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
