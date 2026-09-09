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
    public RectTransform rectTransform;
    [SyncVar(hook = nameof(OnVisibleChanged))] public bool EstVisible;
    [SyncVar] public bool EstStratege;
    [SyncVar] public bool EstEnJeu;
    public bool EstRemise;
    public bool EstMontree;
    public bool EstEchange;
    public bool AUtilisePouvoir;
    private Vector2 offset;

    // Information importante carte
    [SyncVar] public int Id;
    [SyncVar(hook = nameof(OnTerrainIdChanged))] public int TerrainId; // Id du terrain sur lequel il est, 0 est deck, -1 est mort
    public int TerrainIdVise;
    [SyncVar]
    public PlayerManager Player; //Joueur a qui appartient la carte

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
    public GameObject Icones;
    public List<Icone> ListeIcones;

    //Les paramètres qui changent
    [SyncVar] public float PMVar;
    [SyncVar(hook = nameof(OnPVarChanged))] public int PVar;
    [SyncVar] public int PAVar;
    [SyncVar] public int IdPouvoirVar;
    public Pouvoir FonctionPouvoirVar;
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
        AUtilisePouvoir = false;
        VisibiliteT.sprite = GameManager.Instance.ImagePasVisible; //Oeil fermé 
        ImageDosCarte = Resources.Load<Sprite>("Images/" + "DosAdversaires");
        InitialiserVar();
    }
    public void InitialiserVar()
    {
        PVar = Stats.PV;
        PAVar = Stats.PA;
        PMVar = Stats.PM;
        IdPouvoirVar = Stats.IdPouvoir;
        FonctionPouvoirVar = PouvoirFactory.GetPouvoirById((PouvoirId)IdPouvoirVar, this);
        PouvoirVar = Stats.Pouvoir;
        CoutPouvoirVar = Stats.CoutPouvoir;

        liensVar.Clear();
        foreach (int lien in Stats.liens)
        {
            liensVar.Add(lien);
        }

        if (ListeIcones.Count > 0)
        {
            foreach (Icone icone in Icones.GetComponentsInChildren<Icone>())
            {
                icone.transform.SetParent(GameManager.Instance.Defausse.transform, false);
            }
            ListeIcones.Clear();
        }
        if (Stats.Duo)
        {
            GameObject iconeCree = Instantiate(GameManager.Instance.PrefabIcone, Vector3.zero, Quaternion.identity);
            iconeCree.GetComponent<Icone>().transform.SetParent(Icones.transform, false);
            iconeCree.GetComponent<Icone>().CreerIcone("Duo", "IcoDuos", "/", Stats.IDPartenaire);
            ListeIcones.Add(iconeCree.GetComponent<Icone>());
        }
    }

    // On chose changed
    public void OnVisibleChanged(bool ancienneValeur, bool nouvelleValeur)
    {
        if (nouvelleValeur)
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
        PlaceTerrain vieuxTerrain = GameManager.Instance.TousLesTerrains.Find(t => t.Id == ancienTerrain);
        PlaceTerrain terrain = GameManager.Instance.TousLesTerrains.Find(t => t.Id == nouveauTerrain);
        if (vieuxTerrain != null && ancienTerrain != nouveauTerrain) { vieuxTerrain.CartePlacee = null; }
        if (nouveauTerrain > 0) //Si je vais vers un nouveau terrain
        {
            if (terrain == null) { return; }
            PlaceDeTerrain = terrain;
            terrain.CartePlacee = this;
            EstEnJeu = true;
            transform.SetParent(terrain.transform, false);
            PivotCentre(); // On le remet bien
        }
        else if (nouveauTerrain == -1 || nouveauTerrain == 0) //Si je veux aller dans le deck ou mourir
        {
            PlaceDeTerrain = null;
            if (nouveauTerrain == -1) //Si elle meurt
            {
                transform.SetParent(GameManager.Instance.Defausse.transform, false);
                GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
                PVar = 0;
                //Player = null; 
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

    //Cacher et montrer carte
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
        Icones.SetActive(true);
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
        Icones.SetActive(false);
    }
    public string MontrerLiens() //Afficher les liens sur la carte
    {
        string description = " ";
        foreach (int lien in liensVar)
        {
            if (lien == 0) { description += "?" + "\n"; }
            else { description += GameManager.Instance.CartesSettings.Find(c => c.Id == lien).Prenom + "\n"; }
        }
        if (description == " ") { description = "Personne"; }
        return description;
    }

    // Contour time
    public void DeplacerContour()
    {
        if (!EstMontree && PlaceDeTerrain != null) { MontrerContour(); } // Je met le contour
        else if (EstMontree) { CacherContour(); } // Je le renvoie loin
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

    // Fonctions classiques
    public bool StrategeSeul()
    {
        int decompte = 0;
        UnityEngine.Debug.Log($"La carte seule est {Player.Id}");
        foreach (Carte carte in GameManager.Instance.ToutesLesCartes)
        {
            if (carte.Player != null && (carte.Player.Id == Player.Id)) { decompte++; }
        }
        if (decompte == 1 && GameManager.Instance.NombreCartesPioche == 0) { return true; }
        return false;
    }
    public void Mourir()
    {
        UnityEngine.Debug.Log($"{Stats.Prenom} est mort.e.");
        Player.ServeurMourirCarte(this.GameObject());
    }
    public void PivotCentre() // Pour enlever les cartes encore placées sur eux
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Remettre le pivot au centre
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
    }

    // Tout en dessous c'est pour déplacer la carte
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null) { canvas = GetComponentInParent<Canvas>(); }
        if (!isOwned) { return; }//Si la carte n'est pas à moi, YEET
        if (eventData.button == PointerEventData.InputButton.Right) { FamilleImageT.color = Color.red; } //C'est l'attaque
        if (EstStratege && !StrategeSeul()) { return; }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            canvasGroup.alpha = .7f; // Opacité de la carte quand je clique dessus
            canvasGroup.blocksRaycasts = false;
            PivotCentre();
            transform.SetParent(canvas.transform, false);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out offset);
        }
        if (EstMontree) { Invoke(nameof(CacherContour), 0.6f); }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isOwned || (EstStratege && !StrategeSeul())) { return; }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);
            rectTransform.anchoredPosition = localPoint;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isOwned || eventData.button == PointerEventData.InputButton.Right || (EstStratege && !StrategeSeul()))
        {
            FamilleImageT.color = Color.white;
            return;
        }
        PivotCentre();
        canvasGroup.alpha = 1f; // Opacité de la carte quand je clique dessus
        canvasGroup.blocksRaycasts = true;
        if (eventData.button == PointerEventData.InputButton.Left) { Cheminer(); } //Si j'initie un déplacement
    }
    public void Cheminer()
    {
        PlaceTerrain t = GameManager.Instance.TousLesTerrains.Find(t => t.Id == TerrainIdVise);
        string Depart = "Deck";
        string Arrivee = "Deck";
        if (EstEnJeu) { Depart = "Terrain"; }
        if (TerrainIdVise != 0) { Arrivee = "Terrain"; }
        if (EstEchange) { Arrivee = "Carte"; }
        //UnityEngine.Debug.Log($" {Depart} => {Arrivee} ");
        if (Arrivee == "Terrain")
        {
            if (GameManager.Instance.JePeuxJouer(Player, "Deplacer", this)) { Player.JouerCarte(this, TerrainIdVise); }
            else { RetourEnPlace(Depart); }
        }
        if (Arrivee == "Deck")
        {
            if (Depart == "Terrain" && EstRemise && GameManager.Instance.JePeuxJouer(Player, "RetournerDeck", this))
            {
                Player.RangerCarte(this);
                LayoutRebuilder.MarkLayoutForRebuild(Player.DeckJoueur.GetComponent<RectTransform>());
            }
            else { RetourEnPlace(Depart); }
            EstRemise = false;
        }
        if (Arrivee == "Carte") { RetourEnPlace(Depart); EstEchange = false; }
    }
    public void RetourEnPlace(string place)
    {
        if (place == "Terrain") { transform.SetParent(PlaceDeTerrain.transform, false); }
        else
        {
            transform.SetParent(Player.DeckJoueur.transform, false);
            LayoutRebuilder.MarkLayoutForRebuild(Player.DeckJoueur.GetComponent<RectTransform>());
        }
        TerrainIdVise = 0;
    }

    public void UtiliserPouvoir()
    {
        FonctionPouvoirVar.UtiliserPouvoir(this);
    }

    // Cliquer sur la carte et se laisser drop dessus
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && Player != null && PlaceDeTerrain != null) { DeplacerContour(); }
        //Cheat Code
#if DEVELOPMENT_BUILD
        if (Input.GetKey(KeyCode.D) && eventData.button == PointerEventData.InputButton.Left) { Player.ServeurMourirCarte(this.GameObject()); }
        if (Input.GetKey(KeyCode.V) && eventData.button == PointerEventData.InputButton.Left) { GameManager.Instance.ViderPioche(); }
#endif
        if (eventData.button == PointerEventData.InputButton.Middle && !EstVisible && EstEnJeu) { MontrerCarte(); } // A retirer
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) { return; }
        GameManager gm = GameManager.Instance;
        Carte carteDeplace = eventData.pointerDrag.GetComponent<Carte>();
        if (EstEnJeu && carteDeplace.isOwned && isOwned && eventData.button == PointerEventData.InputButton.Left
        && carteDeplace.Stats.Duo && Stats.Duo && Stats.IDPartenaire == carteDeplace.Id)
        {
            UnityEngine.Debug.Log($"On duote entre {Stats.Prenom} et {carteDeplace.Stats.Prenom}");
            UnityEngine.Debug.Log(" Savoir si je peux échanger " + gm.JePeuxJouer(Player, "Echanger", this));
            if (gm.JePeuxJouer(Player, "Echanger", this)) { gm.Proposition(carteDeplace, this, "Duo/Echanger"); }
            else { gm.Proposition(carteDeplace, this, "Duo"); }
        }
        else if (EstEnJeu && carteDeplace.isOwned && isOwned && eventData.button == PointerEventData.InputButton.Left && gm.JePeuxJouer(Player, "Echanger", this))
        {
            //UnityEngine.Debug.Log($"On échange entre {Stats.Prenom} et {carteDeplace.Stats.Prenom}");
            carteDeplace.EstEchange = true;
            gm.Proposition(carteDeplace, this, "Echanger");
        }
        if (EstEnJeu && carteDeplace.EstEnJeu && carteDeplace.isOwned && eventData.button == PointerEventData.InputButton.Right && gm.JePeuxJouer(carteDeplace.Player, "Attaquer", carteDeplace))
        {
            //UnityEngine.Debug.Log($"{carteDeplace.Stats.Prenom} attaque {Stats.Prenom} qui Stratege {EstStratege} et Stratege Seul ? {StrategeSeul()}");
            if (EstStratege && !StrategeSeul())
            {
                // Si on attaque le stratège, on fait attention que les aversaires n'ont pas de carte a côté
                PlaceTerrain t2 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 2);
                PlaceTerrain t3 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 3);
                PlaceTerrain t5 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 5);
                PlaceTerrain t6 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 6);

                bool conclusion = false;
                if (TerrainId == 1 && ((t2.CartePlacee != null && !t2.CartePlacee.isOwned)
                || (t3.CartePlacee != null && !t3.CartePlacee.isOwned))) { conclusion = true; }
                else if (TerrainId == 4 && ((t5.CartePlacee != null && !t5.CartePlacee.isOwned)
                || (t6.CartePlacee != null && !t6.CartePlacee.isOwned))) { conclusion = true; }
                if (conclusion)
                {
                    gm.Proposition("StrategeProtege");
                    return;
                }
            }
            gm.Proposition(carteDeplace, this, "Attaquer");
        }
    }
}