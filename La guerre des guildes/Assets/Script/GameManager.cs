using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using System;
using TMPro;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{

    public List<int> Pioche = new List<int>();
    public List<CarteSettings> CartesSettings = new List<CarteSettings>(); //Ajoutees manuellement
    public EcranDeConfirmation EcranDeConfirmation;
    public static GameManager Instance;
    public Sprite ImageVisible;
    public Sprite ImagePasVisible;
    public GameObject ContourAllie;
    public GameObject ContourEnnemi;
    public PlayerManager JoueurEnCours;
    [SyncVar(hook = nameof(OnTourChanged))]
    public int Tour;
    public GameObject TourObject;
    [SyncVar] public string EtatDuJeu;
    public List<Carte> ToutesLesCartes = new List<Carte>();
    public List<PlaceTerrain> TousLesTerrains = new List<PlaceTerrain>();
    public List<PlayerManager> TousLesJoueurs = new List<PlayerManager>();

    public void Start()
    {
        Instance = this;
        if (CartesSettings.Count == 0) { ImporterCartes(); }
        EtatDuJeu = "EnAttente";
    }
    public void Awake()
    { Instance = this; }
    public override void OnStopServer()
    {
        base.OnStopServer();
        TousLesJoueurs.Clear();
        ToutesLesCartes.Clear();
        TousLesTerrains.Clear();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        TousLesJoueurs.Clear();
        ToutesLesCartes.Clear();
        TousLesTerrains.Clear();
    }


    public void OnTourChanged(int ancienneValeur, int nouvelleValeur)
    {
        UnityEngine.Debug.Log($"OnTourChanged - {isServer} {isClient} {isLocalPlayer}");

        if (TousLesJoueurs.Count >= 2)
        {
            JoueurEnCours = TousLesJoueurs[Tour % 2];
            PlayerManager.LocalPlayer.BoutonTourSuivant.GetComponent<Button>().interactable = JoueurEnCours == PlayerManager.LocalPlayer;
        }
        else
        {
            JoueurEnCours = TousLesJoueurs[0];
        }
        if (JoueurEnCours.Stratege != null) { JoueurEnCours.ValeurPM(JoueurEnCours.Stratege.PMVar); } // On lui remet ses PMs //MAIS PAS ICI

        TourObject.GetComponent<TMP_Text>().text = nouvelleValeur.ToString();
    }

    public void ImporterCartes()
    {
        CartesSettings.Clear(); //On enlève tout
        //string[] export = File.ReadAllLines("Assets/Resources/ExportCartes.csv"); // Lire CSV
        TextAsset csv = Resources.Load<TextAsset>("ExportCartes");

        if (csv == null)
        {
            UnityEngine.Debug.LogError("Impossible de charger ExportCartes.csv");
            return;
        }

        string[] export = csv.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None); //testons
        string ligne = export[0];
        for (int i = 1; i < export.Length; i++)
        {
            CarteSettings carte = ScriptableObject.CreateInstance<CarteSettings>();
            ligne = export[i];
            string[] colonnes = ligne.Split(',');
            carte.Id = int.Parse(colonnes[0]);
            carte.Prenom = colonnes[1].ToString();
            carte.PM = float.Parse(colonnes[2].Replace(".", ","));
            carte.PV = int.Parse(colonnes[3]);
            carte.PA = int.Parse(colonnes[4]);
            carte.Image = Resources.Load<Sprite>("Images/Personnage/" + colonnes[5]); //Image
            carte.Pouvoir = colonnes[6];
            carte.IdPouvoir = int.Parse(colonnes[7]);
            carte.ComplementPouvoir = colonnes[8];
            carte.CoutPouvoir = float.Parse(colonnes[9].Replace(".", ","));
            string[] lienTransfert = colonnes[10].Split('/');
            foreach (string lien in lienTransfert)
            { carte.liens.Add(int.Parse(lien)); }
            carte.Particularite = colonnes[12];
            carte.Famille = colonnes[13];
            carte.FamilleImage = Resources.Load<Sprite>("Images/Famille/" + carte.Famille);
            carte.Type = colonnes[14];
            carte.TypeImage = Resources.Load<Sprite>("Images/Type/" + carte.Type);
            //Id,Prenom,PM,PV,PA,Image,Pouvoir,IdPouvoir,Complement Pouvoir,Cout,LienID,Liens,Particularite,Famille,Role
            CartesSettings.Add(carte);
        }

    }

    //Server
    [Server]
    public void CreerDeck()
    {
        Pioche.Clear();
        foreach (var carteStats in CartesSettings)
        { Pioche.Add(carteStats.Id); }
        Melanger(Pioche);
        EtatDuJeu = "Preparation";
    }
    [Server]
    public void Piocher(NetworkConnectionToClient conn)
    {
        if (Pioche.Count == 0) return;
        PlayerManager joueur = conn.identity.GetComponent<PlayerManager>();
        if (joueur.DeckJoueur.transform.childCount >= 5) return;

        int dataId = Pioche[0];
        Pioche.RemoveAt(0);
        GameObject cardObj = Instantiate(joueur.PrefabCarte);
        Carte carte = cardObj.GetComponent<Carte>();
        carte.Id = dataId;
        carte.PlayerManager = joueur;

        NetworkServer.Spawn(cardObj, conn);
        joueur.PiocherCarte(cardObj);
    }
    [Server]
    public void CreerTerrain(NetworkConnectionToClient conn, int i)
    {
        //if terrain déjà instancié return;
        PlayerManager joueur = conn.identity.GetComponent<PlayerManager>();

        GameObject terrainObj = Instantiate(joueur.PrefabTerrain);
        PlaceTerrain terrain = terrainObj.GetComponent<PlaceTerrain>();
        terrain.Id = i;

        NetworkServer.Spawn(terrainObj, conn);
        joueur.PiocherTerrain(terrainObj);
    }
    [Server]
    public void PasserAuTourSuivant()
    {
        Tour++;
    }
    [Server]
    public void CommencerLeJeu()
    {
        Tour = 1;
        EtatDuJeu = "Jouer";
    }

    public void PlacerContour(RectTransform rect, bool estAMoi)
    {
        if (estAMoi)
        {
            ContourAllie.GetComponent<RectTransform>().anchoredPosition = rect.anchoredPosition;
        }
        else { ContourEnnemi.GetComponent<RectTransform>().anchoredPosition = rect.anchoredPosition; }
    }

    private void Melanger(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    //Propositions
    public void Proposition(Carte carteDeplacee, Carte carteChoisie, string choix)
    {
        if (carteChoisie.PlaceDeTerrain == null || carteDeplacee.PlaceDeTerrain == null) { UnityEngine.Debug.Log("On échange avec une place de deck"); return; }
        if (!DeplacementAutorise(carteChoisie.PlaceDeTerrain.Id, carteDeplacee.PlaceDeTerrain.Id)) { return; }
        EcranDeConfirmation.GameObject().SetActive(true);
        if (choix == "Echanger" && !carteDeplacee.EstStratege && !carteChoisie.EstStratege)
        { EcranDeConfirmation.Texte.text = $"Voulez-vous échanger {carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom} ?"; }
        else if (choix == "Echanger" && (carteDeplacee.EstStratege || carteChoisie.EstStratege))
        { EcranDeConfirmation.Texte.text = $"Voulez-vous changer de stratège et mettre {carteDeplacee.Stats.Prenom} à la place ?"; }
        else if (choix == "Attaquer")
        {
            EcranDeConfirmation.Texte.text = $"Voulez-vous attaquer ";
            if (carteChoisie.EstVisible)
            { EcranDeConfirmation.Texte.text += $"{carteChoisie.Stats.Prenom} "; }
            EcranDeConfirmation.Texte.text += $"avec {carteDeplacee.Stats.Prenom}, en infligeant {carteDeplacee.PAVar.ToString()} dégâts ?";
            if (carteChoisie.isOwned) { EcranDeConfirmation.Texte.text += "\nAttention c'est votre carte."; }
        }
        else if (choix == "Lien")
        {
            EcranDeConfirmation.Texte.text = $"{carteDeplacee.Stats.Prenom} apperçoit {carteChoisie.Stats.Prenom} et refuse de se battre \nLien";
            EcranDeConfirmation.BoutonConfirmer.gameObject.SetActive(false);
        }
        //Visuels
        EcranDeConfirmation.BoutonConfirmer.gameObject.SetActive(true);
        EcranDeConfirmation.BoutonConfirmer.GetComponentInChildren<TMP_Text>().text = choix;
        EcranDeConfirmation.CarteDeplaceeTemp = carteDeplacee;
        EcranDeConfirmation.CarteChoisieTemp = carteChoisie;
        EcranDeConfirmation.ChoixTemp = choix;
    }
    public void Proposition(string choix)
    {
        EcranDeConfirmation.GameObject().SetActive(true);
        EcranDeConfirmation.BoutonConfirmer.GetComponentInChildren<TMP_Text>().text = "Ok";
        if (choix == "Gagner")
        {
            EcranDeConfirmation.Texte.text = "Vous avez gagné :)";
        }
        else if (choix == "Perdre")
        {
            EcranDeConfirmation.Texte.text = "Vous avez perdu :(";
        }
        else if (choix == "Cout")
        {
            EcranDeConfirmation.Texte.text = "Vous n'avez pas assez de Point de Mouvement";
        }
        EcranDeConfirmation.ChoixTemp = choix;
        EcranDeConfirmation.BoutonConfirmer.gameObject.SetActive(true);
    }

    public bool AttaqueAutorisee(Carte attaquante)
    {
        if (EtatDuJeu == "Jouer" && JoueurEnCours == attaquante.PlayerManager) { return true; }
        return false;
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