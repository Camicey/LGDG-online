using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;
using Mirror.Examples.Basic;
using Unity.VisualScripting;

public class Carte : NetworkBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IDropHandler  //Les suppléments sont les promesses de fonction
{

    [SerializeField] public Canvas canvas;
    public CanvasGroup canvasGroup;
    public PlayerManager Player; //Joueur a qui appartient la carte
    public RectTransform rectTransform;
    [SyncVar(hook = nameof(OnVisibleChanged))] public bool EstVisible;
    [SyncVar] public bool EstStratege;
    [SyncVar] public bool EstEnJeu;
    [SyncVar] public bool EstRemise;
    public bool EstMontree;
    private Vector2 offset;

    // Information importante carte
    [SyncVar] public int Id;
    [SyncVar(hook = nameof(OnTerrainIdChanged))] public int TerrainId; // Id du terrain sur lequel il est, 0 est deck, -1 est mort
    public int TerrainIdVise;
    [SyncVar] public bool EstEchange;
    public PlaceTerrain PlaceDeTerrain;

    public CarteSettings Stats;

    public Sprite ImageDosCarte;

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

    //Les paramètres qui changent
    [SyncVar] public float PMVar;
    [SyncVar(hook = nameof(OnPVarChanged))] public int PVar;
    [SyncVar] public int PAVar;
    [SyncVar] public int IdPouvoirVar;
    [SyncVar] public string PouvoirVar;
    [SyncVar] public float CoutPouvoirVar;
    public List<int> liensVar = new();

    //Fonctions de Unity
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        PivotCentre();
    }
    public override void OnStartClient() // De la carte
    {
        base.OnStartClient();

        //Ce qui permet au client de récupérer la carte setting à partir de l'Id
        Stats = GameManager.Instance.CartesSettings.Find(c => c.Id == Id);
        if (Stats == null)
        {
            Debug.LogError("Stats pas trouvé pour un Id de " + Id);
            return;
        }
        Initialiser();
        MontrerCarte();
    }
    public void Initialiser()
    {
        PlaceDeTerrain = null;
        TerrainId = 0;
        TerrainIdVise = 0;
        EstEnJeu = false;
        EstVisible = false;
        EstStratege = false;
        EstRemise = false;
        VisibiliteT.sprite = GameManager.Instance.ImagePasVisible; //Oeil fermé 
        PVar = Stats.PV;
        PAVar = Stats.PA;
        PMVar = Stats.PM;
        IdPouvoirVar = Stats.IdPouvoir;
        PouvoirVar = Stats.Pouvoir;
        NetworkIdentity networkIdentity = NetworkClient.connection.identity;
        Player = networkIdentity.GetComponent<PlayerManager>();
        ImageDosCarte = Resources.Load<Sprite>("Images/" + "DosAdversaires");
        CoutPouvoirVar = Stats.CoutPouvoir;
        liensVar.Clear();
        foreach (int lien in Stats.liens)
        {
            liensVar.Add(lien);
        }
    }

    public void OnVisibleChanged(bool ancienneValeur, bool nouvelleValeur)
    {
        if (nouvelleValeur == true)
        {
            MontrerCarte();
            if (isOwned)
            {
                VisibiliteT.sprite = GameManager.Instance.ImageVisible; //Oeil ouvert
                VisibiliteT.enabled = true;
            }
            else { VisibiliteT.enabled = false; }
            if (EstStratege) { MontrerCarte(); }
        }
        else
        {
            VisibiliteT.sprite = GameManager.Instance.ImagePasVisible; //Oeil fermé 
            if (PrenomT.text == " ") { VisibiliteT.enabled = false; }
        }
    }

    public void OnPVarChanged(int ancienneValeur, int nouvelleValeur)
    {
        PVT.text = nouvelleValeur.ToString();
    }

    public void OnTerrainIdChanged(int ancienTerrain, int nouveauTerrain)
    {
        //UnityEngine.Debug.Log($"Nous allons de {ancienTerrain} à {nouveauTerrain}"); //Quand j'ai des bugs
        if (nouveauTerrain > 0) //Si je vais vers un nouveau terrain
        {
            PlaceTerrain terrain = GameManager.Instance.TousLesTerrains.Find(t => t.Id == nouveauTerrain);
            if (terrain == null) { return; }
            PlaceDeTerrain = terrain;
            terrain.CartePlacee = this;
            EstEnJeu = true;
            transform.SetParent(terrain.transform, false);
            PivotCentre(); // On le remet bien
        }
        else if (nouveauTerrain == -1 || nouveauTerrain == 0) //Si je veux aller dans le deck ou mourir
        {
            PlaceTerrain vieuxTerrain = GameManager.Instance.TousLesTerrains.Find(t => t.Id == ancienTerrain);
            if (vieuxTerrain != null) { vieuxTerrain.CartePlacee = null; } //Enlever la carte dessus
            PlaceDeTerrain = null;
            if (nouveauTerrain == -1) //Si elle meurt
            {
                transform.SetParent(Player.Defausse.transform, false);
                GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
                PVar = 0;
                Player = null;
            }
            else if (nouveauTerrain == 0) //Si elle retourne juste dans le deck
            {
                if (isOwned) { transform.SetParent(Player.DeckJoueur.transform, false); } //On choisit le bon deck
                else { transform.SetParent(Player.DeckAdversaire.transform, false); }
                EstEnJeu = false;
            }
        }
        if (EstMontree) { CacherContour(); }
        TerrainIdVise = 0;
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
        FamilleImageT.sprite = ImageDosCarte;
        TypeImageT.enabled = false;
        VisibiliteT.enabled = false;
        LiensT.text = " ";
        EstVisible = false;
    }
    public void MontrerCarte()
    {
        PrenomT.text = Stats.Prenom;
        ImageT.sprite = Stats.Image;
        ImageT.enabled = true;
        PMT.text = PMVar.ToString();
        PVT.text = PVar.ToString();
        PAT.text = PAVar.ToString();
        PouvoirT.text = PouvoirVar;
        CoutPouvoirT.text = CoutPouvoirVar.ToString() + "PM";
        FamilleImageT.sprite = Stats.FamilleImage;
        TypeImageT.sprite = Stats.TypeImage;
        TypeImageT.enabled = true;
        LiensT.text = MontrerLiens();
    }
    public string MontrerLiens() //Afficher les liens sur la carte
    {
        string description = " ";
        foreach (int lien in liensVar)
        {
            if (lien == 0)
            {
                description += "?" + "\n";
            }
            else
            {
                description += GameManager.Instance.CartesSettings.Find(c => c.Id == lien).Prenom + "\n";
            }
        }
        if (description == " ") { description = "Personne"; }
        return description;
    }
    public void Mourir()
    {
        UnityEngine.Debug.Log($"{Stats.Prenom} est mort.e.");
        Player.CmdMourir(this.GameObject());
    }
    public void PivotCentre() // Pour enlever les cartes encore placées sur eux
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Remettre le pivot au centre
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
    }
    public void DeplacerContour()
    {
        if (!EstMontree && PlaceDeTerrain != null)
        { MontrerContour(); } // Je met le contour
        else if (EstMontree)
        { CacherContour(); } // Je le renvoie loin
    }

    public void MontrerContour()
    {
        GameManager JeuEnCours = GameManager.Instance;
        RectTransform rt = JeuEnCours.ContourEnnemi.GetComponent<RectTransform>();
        if (isOwned) { rt = JeuEnCours.ContourAllie.GetComponent<RectTransform>(); }
        Vector2 addition = Player.TerrainsAdverse.GetComponent<RectTransform>().anchoredPosition;
        if (PlaceDeTerrain.EstAMoi) { addition = Player.TerrainsJoueur.GetComponent<RectTransform>().anchoredPosition; }
        EstMontree = true;
        rt.anchoredPosition = PlaceDeTerrain.GetComponent<RectTransform>().anchoredPosition + addition;
        if (JeuEnCours.CarteMontree != null)
        {
            JeuEnCours.CarteMontree.EstMontree = false;
            if (JeuEnCours.CarteMontree.isOwned && !isOwned)
            { JeuEnCours.ContourAllie.GetComponent<RectTransform>().anchoredPosition = new Vector2(1200, 0); }
            else if (!JeuEnCours.CarteMontree.isOwned && isOwned)
            { JeuEnCours.ContourEnnemi.GetComponent<RectTransform>().anchoredPosition = new Vector2(1200, 0); }
        }
        JeuEnCours.CarteMontree = this;
        if (EstVisible || isOwned) { GameManager.Instance.MontrerGrandeCarte(); }
    }

    public void CacherContour()
    {
        RectTransform rt = GameManager.Instance.ContourEnnemi.GetComponent<RectTransform>();
        if (isOwned) { rt = GameManager.Instance.ContourAllie.GetComponent<RectTransform>(); }
        rt.anchoredPosition = new Vector2(1200, 0);
        EstMontree = false;
        GameManager.Instance.CarteMontree = null;
        if (GameManager.Instance.CarteMontree == null || (!EstVisible && !isOwned)) { GameManager.Instance.CacherGrandeCarte(); }
    }

    //Tout en dessous c'est pour déplacer la carte
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null) { canvas = GetComponentInParent<Canvas>(); }
        if (!isOwned) { return; }//Si la carte n'est pas à moi, YEET
        if (eventData.button == PointerEventData.InputButton.Right) { FamilleImageT.color = Color.red; } //C'est l'attaque
        if (EstStratege) { return; }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            canvasGroup.alpha = .7f; // Opacité de la carte quand je clique dessus
            canvasGroup.blocksRaycasts = false;

            if (PlaceDeTerrain != null)
            {
                transform.SetParent(canvas.transform, false);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out offset);
            }
        }
        if (EstMontree) { CacherContour(); }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isOwned || EstStratege) { return; }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (PlaceDeTerrain != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint
                    );
                rectTransform.anchoredPosition = localPoint;
            }
            else { rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor; }
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isOwned) { return; }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            FamilleImageT.color = Color.white;
            return;
        }
        if (EstStratege) { return; }
        canvasGroup.alpha = 1f; // Opacité de la carte quand je clique dessus
        canvasGroup.blocksRaycasts = true;
        GameManager gm = GameManager.Instance;
        bool envahiAdversaire = (Player.Id == 0 && TerrainIdVise == 4) || (Player.Id == 1 && TerrainIdVise == 1);
        UnityEngine.Debug.Log($"J'essaie de jouer {gm.JePeuxJouer(Player, "Deplacer")}");
        PivotCentre();
        if (eventData.button == PointerEventData.InputButton.Left && gm.JePeuxJouer(Player, "Deplacer")) //Si j'initie un déplacement
        {
            if (!EstEnJeu) // Deck =>
            {
                if (TerrainIdVise != 0) // => Terrain
                { Player.JouerCarte(this, TerrainIdVise); }
                else if (TerrainIdVise == 0) // => Terrain Fail ou => Deck
                {
                    LayoutRebuilder.MarkLayoutForRebuild(Player.DeckJoueur.GetComponent<RectTransform>());
                    EstRemise = false; // Pour le => Deck
                }
            }
            else if (EstEnJeu && TerrainId != TerrainIdVise) // Terrain =>
            {
                if (TerrainIdVise != 0 && !envahiAdversaire) // => Terrain
                { Player.JouerCarte(this, TerrainIdVise); }
                else // => Deck
                {
                    UnityEngine.Debug.Log(EstRemise);
                    if (EstRemise) // Succès
                    {
                        Player.RangerCarte(this);
                        LayoutRebuilder.MarkLayoutForRebuild(Player.DeckJoueur.GetComponent<RectTransform>());
                        EstRemise = false;
                    }
                    else if (!EstRemise) // Fail
                    { transform.SetParent(PlaceDeTerrain.transform, false); }
                }
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle && !EstVisible && EstEnJeu) { MontrerCarte(); } // A retirer
        else if (eventData.button == PointerEventData.InputButton.Left && Player != null && PlaceDeTerrain != null) { DeplacerContour(); }
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) { return; }
        GameManager gm = GameManager.Instance;
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (EstEnJeu && carteDeplace.isOwned && isOwned && eventData.button == PointerEventData.InputButton.Left && gm.JePeuxJouer(Player, "Echange"))
        {
            UnityEngine.Debug.Log($"On échange entre {Stats.Prenom} et {carteDeplace.Stats.Prenom}");
            gm.Proposition(carteDeplace, this, "Echanger");
        }
        if (EstEnJeu && carteDeplace.EstEnJeu && carteDeplace.isOwned && eventData.button == PointerEventData.InputButton.Right && gm.JePeuxJouer(Player, "Attaquer"))
        {
            UnityEngine.Debug.Log($"{carteDeplace.Stats.Prenom} attaque {Stats.Prenom}");
            gm.Proposition(carteDeplace, this, "Attaquer");
        }
    }
}