using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Deck : MonoBehaviour, IDropHandler
{
    // Start is called before the first frame update
    public void OnDrop(PointerEventData eventData) // Quand une carte est lâchée sur le terrain
    {
        UnityEngine.Debug.Log(eventData.pointerDrag == null);
        UnityEngine.Debug.Log(GameManager.Instance.JePeuxJouer(eventData.pointerDrag.GetComponent<Carte>().Player, "RetournerDeck") || eventData.pointerDrag.GetComponent<Carte>() == null);
        if (eventData.pointerDrag == null || GameManager.Instance.JePeuxJouer(eventData.pointerDrag.GetComponent<Carte>().Player, "RetournerDeck") || eventData.pointerDrag.GetComponent<Carte>() == null) { return; }

        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        UnityEngine.Debug.Log($"!carteDeplace.EstEnJeu {!carteDeplace.EstEnJeu} !carteDeplace.isOwned {!carteDeplace.isOwned} GameManager.Instance.JePeuxJouer(carteDeplace.Player, DeplacerDeck) {GameManager.Instance.JePeuxJouer(carteDeplace.Player, "DeplacerDeck")} ");
        if (!carteDeplace.isOwned || !carteDeplace.EstEnJeu || carteDeplace.EstStratege || !GameManager.Instance.JePeuxJouer(carteDeplace.Player, "DeplacerDeck")) { return; }
        carteDeplace.PlaceDeTerrain.CartePlacee = null;
        carteDeplace.PlaceDeTerrain = null;
        carteDeplace.TerrainIdVise = 0;
        carteDeplace.EstRemise = true;
    }
}
