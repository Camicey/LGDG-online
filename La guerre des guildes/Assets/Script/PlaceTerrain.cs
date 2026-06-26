using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class PlaceTerrain : NetworkBehaviour, IDropHandler
{
    [SyncVar] public int Id;
    public Carte CartePlacee;
    //public PlayerManager PlayerManager;
    public bool EstTerrainStratege;

    public void Start()
    {
        CartePlacee = null;
        if (Id == 1 || Id == 4) { EstTerrainStratege = true; }
        else { EstTerrainStratege = false; }
    }

    public void OnDrop(PointerEventData eventData) // Quand une carte est lâchée sur le terrain
    {
        if (eventData.pointerDrag == null || CartePlacee != null) { return; }
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (!carteDeplace.isOwned) { return; }
        if ((carteDeplace.PlayerManager.TerrainsJoueurList.Contains(this) && carteDeplace.EstEnJeu == false) // Pose du deck au terrain
        || (carteDeplace.EstEnJeu == true && GameManager.Instance.DeplacementAutorise(carteDeplace.PlaceDeTerrain.Id, Id))) // Pose de terrain en terrain
        {
            UnityEngine.Debug.Log("Place de terrain originel");
            carteDeplace.EstEnJeu = true; // Si je pose la carte sur la place, on pose un true
            if (carteDeplace.PlaceDeTerrain != null) { carteDeplace.PlaceDeTerrain.CartePlacee = null; }
            carteDeplace.PlaceDeTerrain = this;
            CartePlacee = carteDeplace;
        }
    }


}
