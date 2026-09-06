using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

public class PlayerManager : NetworkBehaviour
{
    [SyncVar]
    public int Id; //1 ou 2
    public GameObject PrefabCarte;
    public GameObject PrefabTerrain;
    public GameObject DeckJoueur;
    public GameObject DeckAdversaire;
    public GameObject TerrainsJoueur;
    public GameObject TerrainsAdverse;
    public GameObject DossierCarte;
    public GameObject PMObjet;
    public GameObject BoutonTourSuivant;
    public GameObject CibleChoisie;
    [SyncVar(hook = nameof(OnStrategeChanged))]
    public Carte Stratege;
    public static PlayerManager LocalPlayer;
    [SyncVar(hook = nameof(OnPMChanged))]
    public float PMEnCours;
    [SyncVar]
    public bool EstPret;
    [SyncVar(hook = nameof(OnDoisChoisirChanged))]
    public bool DoisChoisirStratege;
    [SyncVar(hook = nameof(OnDoisAttendreChanged))]
    public bool DoisAttendreStratege;

    // Fonctions Start
    public override void OnStartClient()
    {
        base.OnStartClient();
        DeckJoueur = GameObject.Find("DeckJoueur");
        DeckAdversaire = GameObject.Find("DeckAdversaire");
        TerrainsJoueur = GameObject.Find("TerrainsJoueur");
        TerrainsAdverse = GameObject.Find("TerrainsAdverse");
        DossierCarte = GameObject.Find("DossierCarte");
        PMObjet = GameObject.Find("PMEnCoursObjet");
        BoutonTourSuivant = GameObject.Find("Bouton - Tour suivant");
        Initialiser();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        GameManager.Instance.TousLesJoueurs.Add(this);
        Id = GameManager.Instance.TousLesJoueurs.IndexOf(this);
    }
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        GameManager.Instance.TousLesJoueurs.Remove(this);
    }

    //On Chose Changed
    public void OnPMChanged(float ancienneValeur, float nouvelleValeur)
    {
        if (!isLocalPlayer) return;
        if (PMObjet == null) return;
        string pmChecker = nouvelleValeur.ToString();
        if (Stratege != null) { pmChecker += " / " + Stratege.PMVar; }
        PMObjet.GetComponent<TMP_Text>().text = pmChecker;
    }
    public void OnStrategeChanged(Carte ancienneCarte, Carte nouvelleCarte)
    {
        if (!isLocalPlayer) return;
        if (DoisChoisirStratege) { BoutonTourSuivant.GetComponent<Button>().interactable = false; }
        else { BoutonTourSuivant.GetComponent<Button>().interactable = nouvelleCarte != null; }
    }
    public void OnDoisChoisirChanged(bool ancienneValeur, bool nouvelleValeur)
    {
        if (!isLocalPlayer) return;
        if (nouvelleValeur)
        {
            //UnityEngine.Debug.LogError("Il dois choisir son stratège, zou");
            // afficher l'UI de choix de stratège
        }
    }
    public void OnDoisAttendreChanged(bool ancienneValeur, bool nouvelleValeur)
    {
        if (!isLocalPlayer) return;
        if (nouvelleValeur && this == GameManager.Instance.JoueurEnCours)
        {
            // afficher l'UI "attends que l'adversaire choisisse"
            BoutonTourSuivant.GetComponent<Button>().interactable = false;
        }
        else
        { BoutonTourSuivant.GetComponent<Button>().interactable = true; }
    }


    //Fonction classiques
    private void Initialiser()
    {
        DoisChoisirStratege = false;
        DoisAttendreStratege = false;
    }

    //Actions
    public void PiocherCarte(GameObject carte)
    {
        carte.GetComponent<Carte>().Initialiser();
        RpcMontrerCarte(carte);
    }
    public void PiocherTerrain(GameObject terrain)
    {
        RpcMontrerTerrain(terrain);
    }
    public void JouerCarte(Carte carte, int terrainId)
    { CmdJouerCarte(carte.netId, terrainId); }
    public void RangerCarte(Carte carte)
    { CmdRangerCarte(carte.netId); }
    private void EchangerCarte(uint carteDeplaceeId, uint carteChoisieId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteDeplaceeId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;

        UnityEngine.Debug.Log("Tentative d'échange");
        Carte carteDeplacee = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();
        ServerEchangerCarte(carteDeplaceeId, carteChoisie.TerrainId, carteChoisieId, carteDeplacee.TerrainId);
    }
    private void DuoterCarte(Carte carteDeplacee, Carte carteChoisie)
    {
        //UnityEngine.Debug.LogError($"DuoterCarte");
        ServerDuoterCarte(carteDeplacee, carteChoisie);
    }
    private void AttaquerCarte(uint carteAttaquanteId, uint carteChoisieId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteAttaquanteId, out NetworkIdentity identity1)) { return; }
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2)) { return; }
        Carte carteAttaquante = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        if (carteAttaquante.liensVar.Contains(carteChoisie.Id))
        {
            carteAttaquante.EstVisible = true;
            carteChoisie.EstVisible = true;
            foreach (PlayerManager joueur in GameManager.Instance.TousLesJoueurs)
            {
                TargetTransfertProposition(joueur.connectionToClient, carteAttaquante, carteChoisie, "Lien");
            }
            return;
        }

        int degats = carteAttaquante.PAVar;
        int degatsDef = carteChoisie.PAVar;
        if (carteChoisie.Stats.Type == "Robot") { degats = 1; }
        if (carteAttaquante.Stats.Type == "Robot") { degatsDef = 1; }
        ServeurAttaquerCarte(carteAttaquanteId, carteChoisieId, degats, degatsDef);
    }
    public void UtiliserPouvoirCarte(Carte carte)
    {
        if (!isLocalPlayer) return;

        /*
            int nbCibles = PouvoirRegistry.NombreDeCibles(carte.IdPouvoirVar);

            if (nbCibles == 0)
            { CmdActiverPouvoir(CarteMontree.netId); }
            else
            { StartCoroutine(AttendreCibles(nbCibles)); }
        */
    }




    // Vérification du gagnant
    private bool VerifierGagnant()
    {
        List<Carte> CartesJoueur1 = new List<Carte>();
        List<Carte> CartesJoueur2 = new List<Carte>();
        foreach (Carte carte in GameManager.Instance.ToutesLesCartes)
        {
            if (carte.isOwned) { CartesJoueur1.Add(carte); }
            else { CartesJoueur2.Add(carte); }
        }
        if (CartesJoueur1.Count == 0 || CartesJoueur2.Count == 0)
        { return true; }
        else if (LienBlocked(CartesJoueur1, CartesJoueur2))
        { UnityEngine.Debug.LogError($"égalité les gars, tout le monde la même, mais en lien"); }
        return false;
    }
    private void ConclusionGagnant()
    {
        List<Carte> CartesJoueur1 = new List<Carte>();
        List<Carte> CartesJoueur2 = new List<Carte>();
        foreach (Carte carte in GameManager.Instance.ToutesLesCartes)
        {
            if (carte.isOwned) { CartesJoueur1.Add(carte); }
            else { CartesJoueur2.Add(carte); }
        }
        //UnityEngine.Debug.LogError($"Joueur 0 a {CartesJoueur1.Count} carte , joueur 1 a {CartesJoueur2.Count} carte");
        if (CartesJoueur1.Count != CartesJoueur2.Count)
        {
            int idPerdant = (CartesJoueur1.Count == 0) ? 0 : 1;
            int idGagnant = (idPerdant == 0) ? 1 : 0;
            PlayerManager joueurPerdant = GameManager.Instance.TousLesJoueurs.Find(j => j.Id == idPerdant);
            PlayerManager joueurGagnant = GameManager.Instance.TousLesJoueurs.Find(j => j.Id == idGagnant);

            if (joueurPerdant != null) { joueurPerdant.TargetAfficherProposition(joueurPerdant.connectionToClient, "Perdre"); }// false = perdu
            if (joueurGagnant != null) { joueurGagnant.TargetAfficherProposition(joueurGagnant.connectionToClient, "Gagner"); } // true = gagné
        }
        else { UnityEngine.Debug.LogError($"égalité les gars, tout le monde la même"); }
    }
    private bool LienBlocked(List<Carte> CartesJoueur1, List<Carte> CartesJoueur2)
    {
        foreach (Carte carte in CartesJoueur1)
        {
            foreach (Carte carteLien in CartesJoueur2)
            {
                if (!carteLien.liensVar.Contains(carte.Id))
                { return false; }
            }
        }
        foreach (Carte carte in CartesJoueur2)
        {
            foreach (Carte carteLien in CartesJoueur1)
            {
                if (!carteLien.liensVar.Contains(carte.Id))
                { return false; }
            }
        }
        return true;
    }


    // Transfert de proposition
    public void TransfertProposition(string choix)
    { GameManager.Instance.Proposition(choix); }
    public void TransfertProposition(Carte carteAttaquante, Carte carteChoisie, string choix)
    { GameManager.Instance.Proposition(carteAttaquante, carteChoisie, choix); }

    //Commands
    [Command]
    public void CmdJouerCarte(uint carteNetId, int terrainId)
    {
        if (!NetworkServer.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();

        carte.TerrainIdVise = terrainId; // on aligne TerrainIdVise sur la destination reçue, avant tout check
        if (!GameManager.Instance.JePeuxJouer(this, "Deplacer", carte)) { return; }

        ServeurJouerCarte(carteNetId, terrainId);
        CoutAction(1);
    }

    [Command]
    private void CmdRangerCarte(uint carteNetId)
    {
        if (!NetworkServer.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();

        if (!GameManager.Instance.JePeuxJouer(this, "RetournerDeck", carte)) { return; }

        ServeurRangerCarte(carteNetId);
        CoutAction(1);
    }

    [Command]
    public void CmdConfirmerAction(uint carteDeplaceeId, uint carteChoisieId, string choix)
    {
        if (!NetworkServer.spawned.TryGetValue(carteDeplaceeId, out NetworkIdentity identity1))
        { return; }
        if (!NetworkServer.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
        { return; }
        Carte carteDeplacee = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        if (carteDeplacee.Player != this) { return; }

        if (choix == "Echanger" && !carteDeplacee.EstStratege && !carteChoisie.EstStratege)
        {
            UnityEngine.Debug.Log("On Echange deux cartes");
            if (carteChoisie.Player != this) { return; } // Un échange se fait entre deux cartes du même joueur
            EchangerCarte(carteDeplaceeId, carteChoisieId);
            CoutAction(2);
        }
        else if (choix == "Attaquer")
        {
            AttaquerCarte(carteDeplaceeId, carteChoisieId);
            CoutAction(1);
        }
        else if (choix == "Duo")
        {
            DuoterCarte(carteDeplacee, carteChoisie);
            CoutAction(1);
        }
    }

    [Command]
    public void CmdPiocher()
    {
        if (!GameManager.Instance.JePeuxJouer(this, "Piocher", null)) { return; }
        GameManager.Instance.Piocher(connectionToClient);
        CoutAction(1);
    }

    [Command]
    public void CmdJeSuisPret()
    {
        EstPret = true;
        if (GameManager.Instance.TousLesJoueurs.All(j => j.EstPret))
        { GameManager.Instance.CommencerLeJeu(); }
    }
    [Command]
    public void CmdFinTour()
    { GameManager.Instance.PasserAuTourSuivant(); }


    //Servers
    [Server]
    public bool CoutAction(float cout)
    {
        if (PMEnCours - cout < 0)
        {
            TargetAfficherProposition(connectionToClient, "Cout");
            return false;
        }
        PMEnCours = PMEnCours - cout;
        return true;
    }

    [Server]
    public void ValeurPM(float nouvelleValeur)
    {
        PMEnCours = nouvelleValeur;
    }

    [Server]
    private void ServeurAttaquerCarte(uint carteAttaquanteId, uint carteChoisieId, int degatsAtt, int degatDef)
    {
        if (!NetworkServer.spawned.TryGetValue(carteAttaquanteId, out NetworkIdentity identity1))
            return;
        if (!NetworkServer.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteAttaquante = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        carteAttaquante.EstVisible = true;
        carteChoisie.EstVisible = true;

        carteChoisie.PVar = carteChoisie.PVar - degatsAtt; //Deja bon pour robots et piège (bon je dois le faire)
        if (carteChoisie.PVar > 0) // Si elle survit, elle réplique
        {
            carteAttaquante.PVar = carteAttaquante.PVar - degatDef;
            if (carteAttaquante.PVar <= 0) { carteAttaquante.Mourir(); }
        }
        else { carteChoisie.Mourir(); }
    }

    [Server]
    private void ServeurJouerCarte(uint carteNetId, int terrainId) //Les déplacements
    {

        if (!NetworkServer.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        carte.TerrainId = terrainId; //Declanche tout
        ServerRendreStratege(carte, terrainId); // Rendre Stratège si besoin est
    }

    [Server]
    private void ServeurRangerCarte(uint carteNetId)
    {
        if (!NetworkServer.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        carte.TerrainId = 0;
    }

    [Server]
    internal void ServeurMourirCarte(GameObject carte)
    {
        Carte carteMourante = carte.GetComponent<Carte>();
        bool cEtaitLeStratege = carteMourante.EstStratege;
        PlayerManager joueurConcerne = carteMourante.Player;

        if (cEtaitLeStratege)
        {
            joueurConcerne.Stratege = null;
            carteMourante.EstStratege = false;
        }

        if (carteMourante.PlaceDeTerrain != null) { carteMourante.TerrainId = -1; }

        // Retrait immédiat et synchrone côté serveur, pour que VerifierGagnant() voie l'état à jour tout de suite
        GameManager.Instance.ToutesLesCartes.Remove(carteMourante);
        // Propagation aux clients (y compris le client local du host, dès que le RPC est traité)
        RpcRetirerCarte(carteMourante.netId);

        carteMourante.EstEnJeu = false;

        if (VerifierGagnant())
        {
            ConclusionGagnant();
            return;
        }

        if (cEtaitLeStratege)
        {
            joueurConcerne.DoisChoisirStratege = true;
            PlayerManager adversaire = GameManager.Instance.TousLesJoueurs.Find(j => j.Id != joueurConcerne.Id);
            if (adversaire != null) { adversaire.DoisAttendreStratege = true; }
        }
    }

    [Server]
    void ServerEchangerCarte(uint carteNetId, int terrainId, uint carteNetId2, int terrainId2)
    {
        if (!NetworkServer.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        carte.TerrainId = terrainId;
        if (!NetworkServer.spawned.TryGetValue(carteNetId2, out NetworkIdentity identity2)) { return; }
        Carte carte2 = identity2.GetComponent<Carte>();
        carte2.TerrainId = terrainId2;
    }
    [Server]
    void ServerDuoterCarte(Carte carteDeplacee, Carte carteChoisie)
    {
        //UnityEngine.Debug.LogError($"Duo entre {carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom}");
        if (carteDeplacee.Player != carteChoisie.Player) { return; }
        if (carteDeplacee.EstStratege || carteChoisie.EstStratege) { return; }
        if (!carteDeplacee.Stats.Duo || !carteDeplacee.Stats.Duo) { return; }

        string IDDuoString = carteDeplacee.Stats.Particularite;
        int IDDuo = IDDuoString != null && int.TryParse(IDDuoString, out int parsedID) ? parsedID : -1;
        //UnityEngine.Debug.LogError($"Je suis passée avec l'ID {IDDuo}");

        carteChoisie.Id = IDDuo;
        CarteSettings partenaire = GameManager.Instance.CartesSettings.Find(c => c.Id == IDDuo);
        carteChoisie.Stats = partenaire;
        carteChoisie.MontrerCarte();
        carteChoisie.InitialiserVar();
        carteDeplacee.Mourir();
    }
    [Server]
    private void ServerRendreStratege(Carte carte, int terrainId)
    {
        if ((Id == 0 && terrainId == 1) || (Id == 1 && terrainId == 4))
        {
            Stratege = carte;
            PMEnCours = Stratege.PMVar;
            carte.EstStratege = true;
            carte.EstVisible = true;

            ServeurStrategeChoisi(this); // ← ici, "this" car ServerRendreStratege est déjà une méthode de PlayerManager
        }
    }

    [Server]
    private void ServeurStrategeChoisi(PlayerManager joueurConcerne)
    {
        joueurConcerne.DoisChoisirStratege = false;

        PlayerManager adversaire = GameManager.Instance.TousLesJoueurs.Find(j => j.Id != joueurConcerne.Id);
        if (adversaire != null)
        {
            adversaire.DoisAttendreStratege = false;
        }
    }

    // Target RPCs
    [TargetRpc]
    public void TargetAfficherProposition(NetworkConnectionToClient target, string message)
    {
        GameManager.Instance.Proposition(message);
    }

    [TargetRpc]
    public void TargetTransfertProposition(NetworkConnectionToClient target, string type)
    {
        TransfertProposition(type); // réutilise ta fonction existante côté client
    }

    [TargetRpc]
    public void TargetTransfertProposition(NetworkConnectionToClient target, Carte carteAttaquante, Carte carteChoisie, string choix)
    {
        TransfertProposition(carteAttaquante, carteChoisie, choix); // réutilise ta fonction existante côté client
    }

    //Client RPC
    [ClientRpc]
    private void RpcMontrerCarte(GameObject carte)
    {
        if (carte == null) return;
        if (isLocalPlayer) // Si je suis le joueur
        {
            carte.transform.SetParent(DeckJoueur.transform, false); // Je la met dans mon deck
            carte.GetComponent<Carte>().MontrerCarte(); // Je la montre
        }
        else
        {
            carte.transform.SetParent(DeckAdversaire.transform, false); // Je la met dans le deck de l'autre joueur
            carte.GetComponent<Carte>().CacherCarte(); // Je la cache
        }
        GameManager.Instance.ToutesLesCartes.Add(carte.GetComponent<Carte>());
    }

    [ClientRpc]
    private void RpcRetirerCarte(uint carteNetId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        GameManager.Instance.ToutesLesCartes.Remove(carte);
        GameManager.Instance.TouteLaDefausse.Add(carte);
    }

    [ClientRpc]
    private void RpcMontrerTerrain(GameObject terrain)
    {
        PlaceTerrain terraing = terrain.GetComponent<PlaceTerrain>();
        if (terrain == null) return;
        if (isLocalPlayer) // Si je suis le joueur
        {
            //Je place d'un côté MAIS le coordonnées sont les mêmes donc je peux :D le faire dans le start !
            if (terraing.Id <= 3)
            {
                terraing.transform.SetParent(TerrainsJoueur.transform, false);
                terraing.EstAMoi = true;
                if (terraing.Id == 1) { terraing.EstTerrainStratege = true; }
            }
            else { terraing.transform.SetParent(TerrainsAdverse.transform, false); }
            terraing.Placement(1);
        }
        else
        {
            if (terraing.Id > 3)
            {
                terraing.transform.SetParent(TerrainsJoueur.transform, false);
                terraing.EstAMoi = true;
                if (terraing.Id == 4) { terraing.EstTerrainStratege = true; }
            }
            else { terraing.transform.SetParent(TerrainsAdverse.transform, false); }
            terraing.Placement(-1);
        }
        GameManager.Instance.TousLesTerrains.Add(terrain.GetComponent<PlaceTerrain>());
    }
}