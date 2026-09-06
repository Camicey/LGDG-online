using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class Pouvoir : MonoBehaviour
{

    // Soit je crée un excel de tous les pouvoirs avec un Id
    // Soit je fais un dictionnaire,
    // Soit je 

    public Carte CarteAssociee;

    public void Start()
    { }

    public void OnClick()
    {
        // Faire les vérifications
        ChoisirPouvoir();
    }

    public void ChoisirPouvoir()
    {
        switch (CarteAssociee.IdPouvoirVar)
        {
            case 0:
                //Rien ne se passe 
                break;
            case 1:
                //Ambivaler(carte.Complement);
                break;
        }
        //Cout du pouvoir
    }

    public void Ambivaler(int idAmbivalent)
    {
        CarteAssociee.Id = idAmbivalent;
        CarteSettings partenaire = GameManager.Instance.CartesSettings.Find(c => c.Id == idAmbivalent);
        CarteAssociee.Stats = partenaire;
        CarteAssociee.MontrerCarte();
        CarteAssociee.InitialiserVar();
    }


}