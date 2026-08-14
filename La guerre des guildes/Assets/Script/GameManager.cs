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
    [SyncVar] public int NombreCartesPioche;
    public List<CarteSettings> CartesSettings = new List<CarteSettings>(); //Ajoutees manuellement
    public EcranDeConfirmation EcranDeConfirmation;
    public static GameManager Instance;
    public Sprite ImageVisible;
    public Sprite ImagePasVisible;
    public GameObject ContourAllie;
    public GameObject ContourEnnemi;
    public PlayerManager JoueurEnCours;
    public bool JEnCoursAPioche;
    public GameObject Defausse;
    [SyncVar] public int DecalageTour;
    [SyncVar(hook = nameof(OnTourChanged))]
    public int Tour;
    public GameObject TourObject;
    public GrandeCarteMontree GrandeCarte;
    public Carte CarteMontree;
    [SyncVar] public string EtatDuJeu;
    public List<Carte> ToutesLesCartes = new List<Carte>();
    public List<Carte> TouteLaDefausse = new List<Carte>();
    public List<PlaceTerrain> TousLesTerrains = new List<PlaceTerrain>();
    //public List<PlayerManager> TousLesJoueurs = new List<PlayerManager>();
    public readonly SyncList<PlayerManager> TousLesJoueurs = new SyncList<PlayerManager>();

    // Fonction de Start / Stop
    public void Start()
    {
        if (CartesSettings.Count == 0) { ImporterCartes(); }
        EtatDuJeu = "EnAttente";
    }
    public void Awake() { Instance = this; }
    public override void OnStopServer()
    {
        base.OnStopServer();
        TousLesJoueurs.Clear();
        TouteLaDefausse.Clear();
        ReinitialiserPartie();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        CarteMontree = null;
    }

    //On Chose Changed
    public void OnTourChanged(int ancienneValeur, int nouvelleValeur)
    {
        if (TousLesJoueurs.Count <= 0) return;

        if (TousLesJoueurs.Count >= 2)
        {
            JoueurEnCours = TousLesJoueurs[(Tour + DecalageTour) % 2];
            PlayerManager.LocalPlayer.BoutonTourSuivant.GetComponent<Button>().interactable = JoueurEnCours == PlayerManager.LocalPlayer;
        }
        else if (TousLesJoueurs.Count == 1)
        {
            JoueurEnCours = TousLesJoueurs[0];
            TousLesJoueurs[0].BoutonTourSuivant.GetComponent<Button>().interactable = true;
        }
        JEnCoursAPioche = false;
        TourObject.GetComponent<TMP_Text>().text = nouvelleValeur.ToString();
    }

    // Fonction de début
    public void ImporterCartes()
    {
        CartesSettings.Clear(); //On enlève tout
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
        if (carteChoisie.PlaceDeTerrain == null || carteDeplacee.PlaceDeTerrain == null) { return; } //Echange entre carte de deck
        if (!DeplacementAutorise(carteChoisie.PlaceDeTerrain.Id, carteDeplacee.PlaceDeTerrain.Id)) { return; }
        EcranDeConfirmation.GameObject().SetActive(true);
        string texteAffiche = "";
        if (choix == "Echanger" && !carteDeplacee.EstStratege && !carteChoisie.EstStratege)
        { texteAffiche = $"Voulez-vous échanger {carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom} ?"; }
        else if (choix == "Echanger" && (carteDeplacee.EstStratege || carteChoisie.EstStratege))
        { texteAffiche = $"Voulez-vous changer de stratège et mettre {carteDeplacee.Stats.Prenom} à la place ?"; }
        else if (choix == "Attaquer")
        {
            texteAffiche = $"Voulez-vous attaquer ";
            if (carteChoisie.EstVisible)
            { texteAffiche += $"{carteChoisie.Stats.Prenom} "; }
            texteAffiche += $"avec {carteDeplacee.Stats.Prenom}, en infligeant {carteDeplacee.PAVar.ToString()} dégâts ?";
            if (carteChoisie.isOwned) { texteAffiche += "\nAttention c'est votre carte."; }
        }
        else if (choix == "Lien")
        {
            texteAffiche = $"{carteDeplacee.Stats.Prenom} apperçoit {carteChoisie.Stats.Prenom} et refuse de se battre \nLien";
            EcranDeConfirmation.BoutonConfirmer.gameObject.SetActive(false);
        }
        GrandeCarte.CacherCarte();
        //Visuels
        EcranDeConfirmation.Texte.text = texteAffiche;
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
        GrandeCarte.CacherCarte();
        string texteAffiche = "";
        switch (choix)
        {
            case "Gagner":
                texteAffiche = "Vous avez gagné :)";
                break;
            case "Perdre":
                texteAffiche = "Vous avez perdu :(";
                break;
            case "Cout":
                texteAffiche = "Vous n'avez pas assez de Point de Mouvement";
                break;
            case "StrategeProtege":
                texteAffiche = "Ce Stratege est protégé. Eliminez les cartes alentours pour pouvoir l'attaquer";
                break;
            case "DeckPlein":
                texteAffiche = "Il n'y a plus de place dans votre deck";
                break;
            case "PiochePreparation":
                texteAffiche = "Vous ne pouvez pas piocher pendant la phase de préparation. \nAppuyez sur Prêt.";
                break;
            case "PiocheVide":
                texteAffiche = "La pioche est vide.";
                break;
            case "AttendreStratege":
                texteAffiche = "Vous devez attendre que l'autre joueur choisisse un stratège.";
                break;
            case "DejaPioche":
                texteAffiche = "Vous avez déjà pioché !";
                break;
        }

        EcranDeConfirmation.Texte.text = texteAffiche;
        EcranDeConfirmation.ChoixTemp = choix;
        EcranDeConfirmation.BoutonConfirmer.gameObject.SetActive(true);
    }

    // Grande Carte
    public void MontrerGrandeCarte()
    {
        if (CarteMontree == null) { return; }
        GrandeCarte.MontrerCarte(CarteMontree);
    }
    public void CacherGrandeCarte()
    {
        GrandeCarte.CacherCarte();
    }

    // Vérification autorise
    public bool AttaqueAutorisee(Carte attaquante)
    {
        if (EtatDuJeu == "Jouer" && JoueurEnCours == attaquante.Player) { return true; }
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
    public bool JePeuxJouer(PlayerManager joueur, string action, Carte carte)
    {
        if (joueur == null || action == null) { UnityEngine.Debug.LogError("Joueur ou Action est null"); return false; }
        if (joueur.DoisAttendreStratege)
        {
            Proposition("AttendreStratege");
            UnityEngine.Debug.Log("Attend le stratege");
            return false;
        }
        else if (joueur.DoisChoisirStratege)// Si je dois choisir un nouveau stratege
        {
            if (action == "Deplacer" && ((carte.Player.Id == 0 && carte.TerrainIdVise == 1) || (carte.Player.Id == 1 && carte.TerrainIdVise == 4)))
            { return true; }
            else { return false; }
        }
        if (carte != null)
        {
            //UnityEngine.Debug.Log($"{EtatDuJeu} avec joueur {joueur.Id} alors que je suis joueur en cours ? {joueur == JoueurEnCours} faisant {action}");
            if ((action == "Deplacer" || action == "Echanger" || action == "RetournerDeck") &&
            ((carte.Player.Id == 0 && carte.TerrainIdVise == 4) || (carte.Player.Id == 1 && carte.TerrainIdVise == 1)))
            { return false; } // Si on essaie d'envahir son terrain
            if (EtatDuJeu == "Jouer" && joueur == JoueurEnCours) // Non si on n'est pas le joueur actif
            { return true; }
            if (EtatDuJeu == "Preparation" && (action == "Deplacer" || action == "Echanger" || action == "RetournerDeck")) //On peut déplacer et échanger
            {
                if ((carte.Player.Id == 0 && carte.TerrainIdVise < 4)
                || (carte.Player.Id == 1 && (carte.TerrainIdVise >= 4 || carte.TerrainIdVise == 0)))
                { return true; }
            }
        }
        else if (carte == null && action == "Piocher" && EtatDuJeu == "Jouer" && joueur == JoueurEnCours)
        {
            if (JEnCoursAPioche)
            {
                Proposition("DejaPioche");
                return false;
            }
            else { return true; }
        }
        else if (carte == null && action == "Piocher" && EtatDuJeu == "Preparation")
        {
            Proposition("PiochePreparation");
            JEnCoursAPioche = false;
            return false;
        }
        return false;
    }


    //Server
    [Server]
    public void ReinitialiserPartie()
    {
        foreach (Carte carte in ToutesLesCartes)
        { NetworkServer.Destroy(carte.gameObject); }
        foreach (PlaceTerrain terrain in TousLesTerrains)
        { NetworkServer.Destroy(terrain.gameObject); }

        ToutesLesCartes.Clear();
        TousLesTerrains.Clear();
        TouteLaDefausse.Clear();

        Tour = 0;
        EtatDuJeu = "EnAttente";
        CreerDeck(); // Relance une nouvelle pioche mélangée
    }
    [Server]
    public void CreerDeck()
    {
        Pioche.Clear();
        for (int i = 0; i < CartesSettings.Count; i++) // JAI CHANGE ICI POUR LES AMBIVALENTS
        {
            if (CartesSettings[i].Id < 200)
            { Pioche.Add(CartesSettings[i].Id); }
            if (CartesSettings[i].Type == "Ambivalent")
            { i++; }
        }
        Melanger(Pioche);
        EtatDuJeu = "Preparation";
        NombreCartesPioche = Pioche.Count;
    }
    [Server]
    public void ViderPioche()
    {
        Pioche.Clear();
        NombreCartesPioche = Pioche.Count;
        UnityEngine.Debug.Log("Je vide la pioche :)");
    }
    [Server]
    public void Piocher(NetworkConnectionToClient conn)
    {
        PlayerManager joueur = conn.identity.GetComponent<PlayerManager>();
        if (Pioche.Count == 0)
        {
            PlayerManager joueurVide = conn.identity.GetComponent<PlayerManager>();
            joueurVide.TargetTransfertProposition(conn, "PiocheVide"); // à créer, voir plus bas
            UnityEngine.Debug.LogError("La pioche est vide :/");
            return;
        }

        int dataId = Pioche[0];
        Pioche.RemoveAt(0);
        NombreCartesPioche = Pioche.Count;
        GameObject cardObj = Instantiate(joueur.PrefabCarte);
        Carte carte = cardObj.GetComponent<Carte>();
        carte.Id = dataId;
        carte.Player = joueur;

        NetworkServer.Spawn(cardObj, conn);
        joueur.PiocherCarte(cardObj);
        JEnCoursAPioche = true;
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
        if (JoueurEnCours.Stratege != null)
        { JoueurEnCours.ValeurPM(JoueurEnCours.Stratege.PMVar); }

    }
    [Server]
    public void CommencerLeJeu()
    {
        DecalageTour = 1;
        if (TousLesJoueurs[1].Stratege.PMVar < TousLesJoueurs[0].Stratege.PMVar)
        { DecalageTour = 0; }
        if (TousLesJoueurs[1].Stratege.PMVar == TousLesJoueurs[0].Stratege.PMVar && UnityEngine.Random.Range(0, 2) == 1)
        { DecalageTour = 0; }

        Tour = 1;
        EtatDuJeu = "Jouer";
        if (JoueurEnCours.Stratege != null)
        { JoueurEnCours.ValeurPM(JoueurEnCours.Stratege.PMVar); }
        RpcMettreAJourTousLesBoutons();
    }

    // Client RPCs
    [ClientRpc]
    void RpcMettreAJourTousLesBoutons()
    {
        // ce code s'exécute UNE fois par client, localement
        if (PlayerManager.LocalPlayer != null)
        {
            PlayerManager.LocalPlayer.BoutonTourSuivant.GetComponent<Button>().GetComponentInChildren<TMP_Text>().text = "Tour Suivant";
        }
    }
}