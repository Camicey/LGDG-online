using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.VisualScripting;

public class EcranDeConfirmation : MonoBehaviour
{
    public TMP_Text Texte;
    public Button BoutonConfirmer;
    public Button BoutonAnnuler;
    public Carte CarteDeplaceeTemp;
    public Carte CarteChoisieTemp;
    public string ChoixTemp;

    //Proposition(Carte carteDeplacee, Carte carteChoisie, string choix)
    public void Confirmer()
    {
        PlayerManager.LocalPlayer.CmdConfirmerAction(
            CarteDeplaceeTemp.netId,
            CarteChoisieTemp.netId,
            ChoixTemp
        );

        gameObject.SetActive(false);
    }
    /*
        public void Confirmer()
        {
            UnityEngine.Debug.Log($"Je Confirme l'action de {ChoixTemp}");
            GameManager.Instance.ConfirmerAction(CarteDeplaceeTemp, CarteChoisieTemp, ChoixTemp);
            this.GameObject().SetActive(false);
        }*/

    public void Annuler()
    {
        UnityEngine.Debug.Log("J'annule, rien ne se passe");
        this.GameObject().SetActive(false);
    }
}