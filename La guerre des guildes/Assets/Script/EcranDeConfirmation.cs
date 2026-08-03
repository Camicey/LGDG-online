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

    public void Confirmer()
    {
        if (CarteChoisieTemp != null && CarteDeplaceeTemp != null)
        {
            PlayerManager.LocalPlayer.CmdConfirmerAction(
            CarteDeplaceeTemp.netId,
            CarteChoisieTemp.netId,
            ChoixTemp
            );
        }
        gameObject.SetActive(false);
    }

    public void Annuler()
    {
        UnityEngine.Debug.Log("J'annule, rien ne se passe");
        gameObject.SetActive(false);
    }
}