using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class PlaceTerrain : NetworkBehaviour, IDropHandler
{
    [SyncVar] public int Id;
    [SyncVar] public Carte CartePlacee;
    //public PlayerManager PlayerManager;
    public bool EstTerrainStratege;
    public bool EstAMoi;

    public void Start()
    {
        CartePlacee = null;
    }


    public void Placement(int i)
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (Id == 3 || Id == 5) { rt.anchoredPosition = new Vector3(280 * i, 0, 0); }
        else if (Id == 2 || Id == 6) { rt.anchoredPosition = new Vector3(-280 * i, 0, 0); }
        else if (Id == 1) { rt.anchoredPosition = new Vector3(0, -50 * i, 0); }
        else if (Id == 4) { rt.anchoredPosition = new Vector3(0, 50 * i, 0); }
    }
    public void OnDrop(PointerEventData eventData) // Quand une carte est lâchée sur le terrain
    {
        if (eventData.pointerDrag == null || CartePlacee != null) { return; }
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (!carteDeplace.isOwned || eventData.button == PointerEventData.InputButton.Right) { return; }
        if ((EstAMoi && carteDeplace.EstEnJeu == false) // Pose du deck au terrain
        || (carteDeplace.EstEnJeu == true && GameManager.Instance.DeplacementAutorise(carteDeplace.PlaceDeTerrain.Id, Id)
        && GameManager.Instance.JePeuxJouer(carteDeplace.PlayerManager, "Deplacer"))) // Pose de terrain en terrain 
        {
            carteDeplace.EstEnJeu = true; // Si je pose la carte sur la place, on pose un true
            if (carteDeplace.PlaceDeTerrain != null) { carteDeplace.PlaceDeTerrain.CartePlacee = null; }
            carteDeplace.TerrainIdVise = Id;
        }

    }

}
