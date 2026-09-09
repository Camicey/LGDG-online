using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class PwAmbivaler : Pouvoir
{

    public int IdAmbivalent;

    public PwAmbivaler(int idAmbivalent)
    {
        IdAmbivalent = 13;
    }

    public override void UtiliserPouvoir(Carte carte)
    {
        UnityEngine.Debug.Log($"Pouvoir Ambivaler utilisé sur la carte {carte.Stats.Prenom}  avec le partenaire {IdAmbivalent}");
        carte.Id = IdAmbivalent;
        CarteSettings partenaire = GameManager.Instance.CartesSettings.Find(c => c.Id == IdAmbivalent);
        carte.Stats = partenaire;
        carte.MontrerCarte();
        carte.InitialiserVar();
    }

}