using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;
using System.Diagnostics;

public class Carte : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler  //Les suppléments sont les promesses de fonction
{

    [SerializeField] public Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 PositionBase;
    public PlayerManager PlayerManager; //Joueur a qui appartient la carte
    public RectTransform rectTransform;

    //GameObject Terrain //Ca devrait être supprimé vu que ça n'a pas l'air d'être important
    public GameObject PlaceTerrainJoueur;
    public GameObject PlaceTerrainAdversaire;

    // Information importante carte
    [SyncVar]
    public int Id;
    public PlaceTerrain PlaceDeTerrain;
    public bool EstEnJeu = false;

    public CarteSettings Stats;

    //Tous les paramètres de chaque carte.
    public TMP_Text PrenomT;

    public Image ImageT;

    public TMP_Text PMT;
    public TMP_Text PVT;
    public TMP_Text PAT;

    public TMP_Text PouvoirT;
    public TMP_Text CoutPouvoirT;
    public Image FamilleImageT;
    public Image TypeImageT;
    public TMP_Text LiensT;

    //Les paramètres qui changent
    public int PVar;
    public int PAVar;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        PositionBase = rectTransform.anchoredPosition;
        canvas = GetComponentInParent<Canvas>();

        //Ca devrait être supprimé vu que ça n'a pas l'air d'être important
        PlaceTerrainAdversaire = GameObject.Find("PlaceTerrainAdversaire");
        PlaceTerrainJoueur = GameObject.Find("PlaceTerrainJoueur");
    }

    public override void OnStartClient() // De la carte
    {
        base.OnStartClient();

        //Ce qui permet au client de récupérer la carte setting à partir de l'Id
        Stats = FindObjectOfType<GameManager>().CartesSettings.Find(c => c.Id == Id);

        Initialiser();
        MontrerCarte();
    }

    public void Initialiser()
    {
        PlaceDeTerrain = null;
        EstEnJeu = false;
        PVar = Stats.PV;
        PAVar = Stats.PA;
    }

    public void CacherCarte()
    {
        //Retirer tout ce qui est visible
        PrenomT.text = " ";
        ImageT.enabled = false;
        PMT.text = " ";
        PVT.text = " ";
        PAT.text = " ";
        PouvoirT.text = " ";
        CoutPouvoirT.text = " ";
        NetworkIdentity networkIdentity = NetworkClient.connection.identity;
        PlayerManager = networkIdentity.GetComponent<PlayerManager>();
        FamilleImageT.sprite = PlayerManager.JeuEnCours.ImageDosCarte;
        TypeImageT.enabled = false;
        LiensT.text = " ";
    }

    public void MontrerCarte()
    {
        //gameObject.GetComponent<Image>().sprite = CarteDevant;
        PrenomT.text = Stats.Prenom;
        ImageT.sprite = Stats.Image;
        ImageT.enabled = true;
        PMT.text = Stats.PM.ToString();
        PVT.text = PVar.ToString();
        PAT.text = PAVar.ToString();
        PouvoirT.text = Stats.PouvoirVar;
        CoutPouvoirT.text = Stats.CoutPouvoirVar.ToString() + "PM";
        FamilleImageT.sprite = Stats.FamilleImage;
        TypeImageT.sprite = Stats.TypeImage;
        TypeImageT.enabled = true;
        LiensT.text = MontrerLiens();
    }
    public string MontrerLiens() //Afficher les liens sur la carte
    {
        string description = " ";
        foreach (CarteSettings lien in Stats.liensVar)
        { description += lien.Prenom + "\n"; }
        if (description == " ") { description = "Personne"; }
        return description;
    }

    //Tout en dessous c'est pour déplacer la carte
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isOwned) { return; } //Si la carte n'est pas à moi, YEET
        PositionBase = rectTransform.anchoredPosition;
        canvasGroup.alpha = .7f; // Opacité de la carte quand je clique dessus
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isOwned) { return; }
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isOwned) { return; }
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        if (GetComponent<Carte>().EstEnJeu == false)
        {
            rectTransform.anchoredPosition = PositionBase; //On revient dans le deck
        }
        else if (GetComponent<Carte>().EstEnJeu == true)
        {
            rectTransform.SetParent(PlaceDeTerrain.transform, false); // On va dans le terrain
            PositionBase = new Vector3(0, 0, 0);
            rectTransform.anchoredPosition = PositionBase;

            NetworkIdentity networkIdentity = NetworkClient.connection.identity;
            PlayerManager = networkIdentity.GetComponent<PlayerManager>();
            PlayerManager.JouerCarte(gameObject);
        }
    }

    /* //C'est pour vérifier l'appartenance (mais tout va bien)
        public void OnPointerDown(PointerEventData eventData)
        {
            if (isOwned) { UnityEngine.Debug.Log("Cette carte m'appartient !"); }
            if (PrenomT.text == " ") { MontrerCarte(); }
            else { CacherCarte(); }
        }
    */
}