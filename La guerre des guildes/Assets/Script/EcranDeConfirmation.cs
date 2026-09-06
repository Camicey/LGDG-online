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
    public Button BoutonConfirmer2;
    public Button BoutonAnnuler;
    public Carte CarteDeplaceeTemp;
    public Carte CarteChoisieTemp;
    public string ChoixTemp;


    public void Confirmer()
    {
        if (ChoixTemp == "Duo/Echanger")
        { ChoixTemp = "Duo"; }
        if (CarteChoisieTemp != null && CarteDeplaceeTemp != null)
        {
            PlayerManager.LocalPlayer.CmdConfirmerAction(
            CarteDeplaceeTemp.netId,
            CarteChoisieTemp.netId,
            ChoixTemp
            );
        }
        RemiseAZero();
    }

    public void ConfirmerSecondeAction()
    {
        UnityEngine.Debug.Log("On arrive a la seconde action avec " + ChoixTemp);
        if (ChoixTemp == "Duo/Echanger") // Pour le choix entre Duo ou Echanger
        {
            UnityEngine.Debug.Log("On arrive a la seconde action a l'intérieur :" + BoutonConfirmer2.GetComponentInChildren<TMP_Text>().text);
            PlayerManager.LocalPlayer.CmdConfirmerAction(
            CarteDeplaceeTemp.netId,
            CarteChoisieTemp.netId,
            BoutonConfirmer2.GetComponentInChildren<TMP_Text>().text
            );
        }
        BoutonConfirmer2.gameObject.SetActive(false);
        RemiseAZero();
    }

    public void Annuler()
    {
        RemiseAZero();
    }

    public void ChoisirSonAction(string choix)
    {
        gameObject.SetActive(true);
        string texteAffiche = "";
        BoutonAnnuler.gameObject.SetActive(false);
        BoutonConfirmer.GetComponentInChildren<TMP_Text>().text = "Ok";
        ChoixTemp = choix;

        switch (choix)
        {
            case "Gagner":
                texteAffiche = "Vous avez gagné :)";
                break;
            case "Perdre":
                texteAffiche = "Vous avez perdu :(";
                break;
            case "Cout":
                texteAffiche = "Vous n'avez pas assez de Point de Mouvement";
                break;
            case "StrategeProtege":
                texteAffiche = "Ce Stratege est protégé. Eliminez les cartes alentours pour l'attaquer";
                break;
            case "DeckPlein":
                texteAffiche = "Il n'y a plus de place dans votre deck";
                break;
            case "PiochePreparation":
                texteAffiche = "Vous ne pouvez pas piocher pendant la phase de préparation. \nAppuyez sur Prêt.";
                break;
            case "PiocheVide":
                texteAffiche = "La pioche est vide.";
                break;
            case "AttendreStratege":
                texteAffiche = "Vous devez attendre que l'autre joueur choisisse un stratège.";
                break;
            case "DejaPioche":
                texteAffiche = "Vous avez déjà pioché !";
                break;
        }
        BoutonConfirmer2.gameObject.SetActive(false);
        Texte.text = texteAffiche;
    }

    public void ChoisirSonAction(Carte carteDeplacee, Carte carteChoisie, string choix)
    {
        //Visuels
        gameObject.SetActive(true);
        BoutonAnnuler.gameObject.SetActive(true);
        BoutonConfirmer.GetComponentInChildren<TMP_Text>().text = choix;
        BoutonAnnuler.GetComponentInChildren<TMP_Text>().text = "Annuler";
        CarteDeplaceeTemp = carteDeplacee;
        CarteChoisieTemp = carteChoisie;
        ChoixTemp = choix;

        string texteAffiche = "";
        switch (choix)
        {
            case "Lien":
                texteAffiche = $"{carteDeplacee.Stats.Prenom} apperçoit {carteChoisie.Stats.Prenom} et refuse de se battre";
                break;
            case "Duo":
                texteAffiche = $"{carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom} décident de se battre ensemble !";
                break;
            case "Duo/Echanger":
                texteAffiche = $"Voulez vous que {carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom} se battent ensemble ou les échanger ?";
                BoutonConfirmer2.gameObject.SetActive(true);
                BoutonConfirmer.GetComponentInChildren<TMP_Text>().text = "Duo";
                BoutonConfirmer2.GetComponentInChildren<TMP_Text>().text = "Echanger";
                CarteDeplaceeTemp = carteDeplacee;
                CarteChoisieTemp = carteChoisie;
                break;
            case "StrategeProtege":
                texteAffiche = $"Ce stratège est protégé par ses cartes alentours, vous ne pouvez pas l'attaquer.";
                break;
            case "Echanger":
                if (!carteDeplacee.EstStratege && !carteChoisie.EstStratege)
                { texteAffiche = $"Voulez-vous échanger {carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom} ?"; }
                else { texteAffiche = $"Voulez-vous changer de stratège et mettre {carteDeplacee.Stats.Prenom} à la place ?"; }
                break;
            case "Attaquer":
                texteAffiche = $"Voulez-vous attaquer ";
                if (carteChoisie.EstVisible)
                { texteAffiche += $"{carteChoisie.Stats.Prenom} "; }
                texteAffiche += $"avec {carteDeplacee.Stats.Prenom}, en infligeant {carteDeplacee.PAVar.ToString()} dégâts ?";
                if (carteChoisie.isOwned) { texteAffiche += "\nAttention c'est votre carte."; }
                break;
        }
        if (choix == "Duo/Echanger" || choix == "Echanger" || choix == "Attaquer")
        { BoutonAnnuler.gameObject.SetActive(true); }
        else { BoutonAnnuler.gameObject.SetActive(false); }
        if (choix == "Duo/Echanger") { BoutonConfirmer2.gameObject.SetActive(true); }
        else { BoutonConfirmer2.gameObject.SetActive(false); }

        Texte.text = texteAffiche.ToString();
    }

    private void RemiseAZero()
    {
        CarteDeplaceeTemp = null;
        CarteChoisieTemp = null;
        ChoixTemp = "";
        BoutonConfirmer2.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

}