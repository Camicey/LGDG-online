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
        if ((carteDeplace.PlayerManager.TerrainsJoueurList.Contains(this) && carteDeplace.EstEnJeu == false) || (carteDeplace.EstEnJeu == true && DeplacementAutorise(carteDeplace.PlaceDeTerrain.Id, Id)))
        {
            carteDeplace.EstEnJeu = true; // Si je pose la carte sur la place, on pose un true
            if (carteDeplace.PlaceDeTerrain != null) { carteDeplace.PlaceDeTerrain.CartePlacee = null; }
            carteDeplace.PlaceDeTerrain = this;
            //CartePlacee = carteDeplace;
        }
    }

    public bool DeplacementAutorise(int IdOrigine, int IdVise)
    {
        if ((IdOrigine == 1 || IdOrigine == 4) && (IdOrigine + 1 == IdVise || IdOrigine + 2 == IdVise)) { return true; }
        else if ((IdOrigine == 2 || IdOrigine == 5) && (IdVise == 3 || IdVise == 6)) { return true; }
        else if ((IdOrigine == 3 || IdOrigine == 6) && (IdVise == 2 || IdVise == 5)) { return true; }
        if ((IdOrigine == 2 || IdOrigine == 3) && (IdVise == 1)) { return true; }
        else if ((IdOrigine == 5 || IdOrigine == 6) && (IdVise == 4)) { return true; }
        return false;
    }
}
