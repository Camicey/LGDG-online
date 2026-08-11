using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Deck : MonoBehaviour, IDropHandler
{
    // Start is called before the first frame update
    public void OnDrop(PointerEventData eventData) // Quand une carte est lâchée sur le terrain
    {
        if (eventData.pointerDrag == null || eventData.pointerDrag.GetComponent<Carte>() == null) { return; }
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        bool jePeuxJouer = GameManager.Instance.JePeuxJouer(carteDeplace.Player, "RetournerDeck", carteDeplace);
        //UnityEngine.Debug.Log($"Elle est pas en jeu {!carteDeplace.EstEnJeu} Pas a moi {!carteDeplace.isOwned} Je peux pas la jouer {!jePeuxJouer} ");
        if (!carteDeplace.isOwned || !carteDeplace.EstEnJeu || carteDeplace.EstStratege || !jePeuxJouer) { return; }
        carteDeplace.EstRemise = true;
    }
}
