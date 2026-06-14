using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class PlaceTerrain : NetworkBehaviour, IDropHandler
{
    [SyncVar] public int Id;
    public Carte CartePlacee;
    public PlayerManager PlayerManager;

    public void Start()
    {
        CartePlacee = null;
    }

    public void OnDrop(PointerEventData eventData) // Quand une carte est lâchée sur le terrain
    {
        if (eventData.pointerDrag == null || CartePlacee != null) { return; }
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (!carteDeplace.isOwned) { return; }
        if ((carteDeplace.PlayerManager.TerrainsJoueurList.Contains(this) && carteDeplace.EstEnJeu == false) || (carteDeplace.EstEnJeu == true && GameManager.Instance.DeplacementAutorise(carteDeplace.PlaceDeTerrain.Id, Id)))
        {
            carteDeplace.EstEnJeu = true; // Si je pose la carte sur la place, on pose un true
            if (carteDeplace.PlaceDeTerrain != null) { carteDeplace.PlaceDeTerrain.CartePlacee = null; }
            carteDeplace.PlaceDeTerrain = this;
        }
    }


}
