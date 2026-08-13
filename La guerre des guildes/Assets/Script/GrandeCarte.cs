using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;
using Mirror.Examples.Basic;
using Unity.VisualScripting;

public class GrandeCarteMontree : MonoBehaviour  //Les suppléments sont les promesses de fonction
{

    public Button BouttonPouvoir;

    //Tous les paramètres de chaque carte.
    public TMP_Text PrenomT;
    public Image ImageT;
    public Image VisibiliteT;

    public TMP_Text PMT;
    public TMP_Text PVT;
    public TMP_Text PAT;

    public TMP_Text PouvoirT;
    public TMP_Text CoutPouvoirT;
    public Image FamilleImageT;
    public Image TypeImageT;
    public TMP_Text LiensT;

    public void Start()
    {
        GetComponent<RectTransform>().anchoredPosition = new Vector2(1200, 0);
    }

    public void MontrerCarte(Carte carte)
    {
        PrenomT.text = carte.PrenomT.text;
        ImageT.sprite = carte.ImageT.sprite;
        ImageT.enabled = true;
        PMT.text = carte.PMVar.ToString();
        PVT.text = carte.PVar.ToString();
        PAT.text = carte.PAVar.ToString();
        VisibiliteT.enabled = true;
        VisibiliteT.sprite = carte.VisibiliteT.sprite;
        PouvoirT.text = carte.PouvoirVar;
        CoutPouvoirT.text = carte.CoutPouvoirVar.ToString() + "PM";
        FamilleImageT.sprite = carte.Stats.FamilleImage;
        TypeImageT.sprite = carte.Stats.TypeImage;
        TypeImageT.enabled = true;
        LiensT.text = carte.MontrerLiens();
        GetComponent<RectTransform>().anchoredPosition = new Vector2(-680, 0);
    }

    public void CacherCarte()
    {
        PrenomT.text = " ";
        ImageT.enabled = false;
        PMT.text = " ";
        PVT.text = " ";
        PAT.text = " ";
        PouvoirT.text = " ";
        CoutPouvoirT.text = " ";
        TypeImageT.enabled = false;
        VisibiliteT.enabled = false;
        LiensT.text = " ";
        GetComponent<RectTransform>().anchoredPosition = new Vector2(1200, 0);
    }

}