using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Deck : MonoBehaviour, IDropHandler
{
    // Start is called before the first frame update
    public void OnDrop(PointerEventData eventData) // Quand une carte est lâchée sur le terrain
    {

        UnityEngine.Debug.Log("ON ME DROP");
        if (eventData.pointerDrag == null) { return; }

        UnityEngine.Debug.Log("WOOOOO");
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (!carteDeplace.isOwned || !carteDeplace.EstEnJeu || carteDeplace.EstStratege) { return; }
        if (carteDeplace.PlaceDeTerrain != null) { carteDeplace.PlaceDeTerrain.CartePlacee = null; }
        carteDeplace.PlaceDeTerrain = null;
        UnityEngine.Debug.Log("LE DECK");
    }
}
