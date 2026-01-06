using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class PlaceTerrain : NetworkBehaviour, IDropHandler
{
    public int Id;
    public Carte CartePlacee;

    public void Start() { CartePlacee = null; }

    public void OnDrop(PointerEventData eventData) //Quand une carte est lâchée sur le terrain
    {
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (eventData.pointerDrag != null && CartePlacee == null && gameObject.name == "PlaceTerrainJoueur" && carteDeplace.isOwned)
        {
            carteDeplace.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // C'est pour remettre le pivot au centre
            carteDeplace.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            carteDeplace.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
            carteDeplace.EstEnJeu = true; //Si je pose la carte sur la place, on pose un true
            CartePlacee = carteDeplace;
            carteDeplace.PlaceDeTerrain = this;
        }
    }
}
