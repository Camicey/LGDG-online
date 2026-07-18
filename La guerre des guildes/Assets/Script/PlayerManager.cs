using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using TMPro;
using Mirror.BouncyCastle.Asn1.Misc;
using Unity.VisualScripting;

public class PlayerManager : NetworkBehaviour
{
    public GameObject PrefabCarte;
    public GameObject PrefabTerrain;
    public GameObject DeckJoueur;
    public GameObject DeckAdversaire;
    public GameObject TerrainsJoueur;
    public GameObject Defausse;
    public GameObject TerrainsAdverse;
    public GameObject DossierCarte;
    public GameObject PMObjet;
    public GameObject BoutonTourSuivant;
    [SyncVar(hook = nameof(OnStrategeChanged))]
    public Carte Stratege;
    public static PlayerManager LocalPlayer;
    [SyncVar(hook = nameof(OnPMChanged))]
    public float PMEnCours;
    [SyncVar]
    public bool EstPret;


    //Fonction Network
    public override void OnStartClient()
    {
        base.OnStartClient();
        DeckJoueur = GameObject.Find("DeckJoueur");
        DeckAdversaire = GameObject.Find("DeckAdversaire");
        TerrainsJoueur = GameObject.Find("TerrainsJoueur");
        TerrainsAdverse = GameObject.Find("TerrainsAdverse");
        DossierCarte = GameObject.Find("DossierCarte");
        Defausse = GameObject.Find("Defausse");
        PMObjet = GameObject.Find("PMEnCoursObjet");
        BoutonTourSuivant = GameObject.Find("Bouton - Tour suivant");
        GameManager.Instance.TousLesJoueurs.Add(this);
    }
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
    }
    public override void OnStopClient()
    {
        base.OnStopClient();
        GameManager.Instance.TousLesJoueurs.Remove(this);
    }


    //Fonction classiques
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
        BoutonTourSuivant.GetComponent<Button>().interactable = (nouvelleCarte != null);
    }

    public void JouerCarte(Carte carte, int terrainId)
    {
        CmdJouerCarte(carte.netId, terrainId);
    }
    public void RangerCarte(Carte carte)
    {
        CmdRangerCarte(carte.netId);
    }
    public void PiocherCarte(GameObject carte)
    {
        carte.GetComponent<Carte>().Initialiser();
        RpcMontrerCarte(carte);
    }

    public void PiocherTerrain(GameObject terrain)
    {
        RpcMontrerTerrain(terrain);
    }

    private void EchangerCarte(uint carteDeplaceeId, uint carteChoisieId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteDeplaceeId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteDeplacee = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();
        ServerEchangerCarte(carteDeplaceeId, carteChoisie.TerrainId, carteChoisieId, carteDeplacee.TerrainId);
    }

    private void AttaquerCarte(uint carteAttaquanteId, uint carteChoisieId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteAttaquanteId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteAttaquante = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        if (carteAttaquante.liensVar.Contains(carteChoisie.Id))
        {
            carteAttaquante.EstVisible = true;
            carteChoisie.EstVisible = true;
            GameManager.Instance.Proposition(carteAttaquante, carteChoisie, "Lien");
            return;
        }

        if (carteChoisie.EstStratege)
        {
            PlaceTerrain terrain2 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 2);
            PlaceTerrain terrain3 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 3);
            PlaceTerrain terrain5 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 5);
            PlaceTerrain terrain6 = GameManager.Instance.TousLesTerrains.Find(t => t.Id == 6);
            // Si on attaque le stratège, on fait attention que les aversaires n'ont pas de carte a côté
            //Penser a rajouter le moment ou le stratège sera SEUL
            if (carteChoisie.PlaceDeTerrain.Id == 1 &&
            ((terrain2.CartePlacee != null && !terrain2.CartePlacee.isOwned) ||
            (terrain3.CartePlacee != null && !terrain3.CartePlacee.isOwned)))
            { return; }
            if (carteChoisie.PlaceDeTerrain.Id == 4 &&
            ((terrain5.CartePlacee != null && !terrain5.CartePlacee.isOwned) ||
            (terrain6.CartePlacee != null && !terrain6.CartePlacee.isOwned)))
            { return; }
        }
        int degats = carteAttaquante.PAVar;
        int degatsDef = carteChoisie.PAVar;
        if (carteChoisie.Stats.Type == "Robot") { degats = 1; }
        if (carteAttaquante.Stats.Type == "Robot") { degatsDef = 1; }
        ServeurAttaquerCarte(carteAttaquanteId, carteChoisieId, degats, degatsDef);
    }

    private void VerifierGagnant()
    {
        int joueurNbCarte = 0;
        int autreJoueurNbCarte = 0;
        foreach (Carte carte in GameManager.Instance.ToutesLesCartes)
        {
            if (carte.isOwned) { joueurNbCarte++; }
            else { autreJoueurNbCarte++; }
        }
        if (joueurNbCarte == 0 || autreJoueurNbCarte == 0)
        { GameManager.Instance.Proposition("Gagner"); }
    }




    //Commands
    [Command]
    public void CmdJouerCarte(uint carteNetId, int terrainId)
    {
        ServeurJouerCarte(carteNetId, terrainId);
        CoutAction(1);
    }
    [Command]
    private void CmdRangerCarte(uint carteNetId)
    {
        ServeurRangerCarte(carteNetId);
        CoutAction(1);
    }

    [Command]
    public void CmdMourir(GameObject carte)
    {
        ServeurMourirCarte(carte);
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
        if (choix == "Echanger" && !carteDeplacee.EstStratege && !carteChoisie.EstStratege)
        {
            EchangerCarte(carteDeplaceeId, carteChoisieId);
            CoutAction(2);
        }
        else if (choix == "Attaquer")
        {
            AttaquerCarte(carteDeplaceeId, carteChoisieId);
            CoutAction(1);
        }
    }

    [Command]
    public void CmdPiocher()
    {
        GameManager.Instance.Piocher(connectionToClient);
        CoutAction(1);
    }

    [Command]
    public void CmdJeSuisPret()
    {
        EstPret = true;
        if (GameManager.Instance.TousLesJoueurs.TrueForAll(j => j.EstPret))
        { GameManager.Instance.CommencerLeJeu(); }
    }
    [Command]
    public void CmdFinTour()
    {
        GameManager.Instance.PasserAuTourSuivant();
    }



    //Servers
    [Server]
    public bool CoutAction(float cout)
    {
        if (PMEnCours - cout < 0)
        {
            GameManager.Instance.Proposition("Cout");
            return false;
        }
        PMEnCours = PMEnCours - cout;
        return true;
    }

    [Server]
    public void ValeurPM(int nouvelleValeur)
    {
        PMEnCours = nouvelleValeur;
    }


    [Server]
    private void ServeurAttaquerCarte(uint carteAttaquanteId, uint carteChoisieId, int degatsAtt, int degatDef)
    {
        if (!NetworkClient.spawned.TryGetValue(carteAttaquanteId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteAttaquante = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        carteAttaquante.EstVisible = true;
        carteChoisie.EstVisible = true;

        carteChoisie.PVar = carteChoisie.PVar - degatsAtt; //Deja bon pour robots et piège (bon je dois le faire)
        if (carteChoisie.PVar > 0) // Si elle survit, elle réplique
        {
            carteAttaquante.PVar = carteAttaquante.PVar - carteChoisie.PAVar;
            if (carteAttaquante.PVar <= 0) { carteAttaquante.Mourir(); }
        }
        else { carteChoisie.Mourir(); }
    }

    [Server]
    void ServeurJouerCarte(uint carteNetId, int terrainId)
    {

        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        carte.TerrainId = terrainId; //Declanche tout
        carte.TerrainIdVise = 0;
        RpcRendreStratege(carte, terrainId);
        ViderTerrain("Normal");
    }

    [Server]
    private void ServeurRangerCarte(uint carteNetId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        carte.TerrainId = 0;
        ViderTerrain("Normal");
    }

    [Server]
    private void ServeurMourirCarte(GameObject carte)
    {
        Carte carteMourante = carte.GetComponent<Carte>();
        if (carteMourante.EstStratege) { carteMourante.PlayerManager.Stratege = null; }
        if (carteMourante.PlaceDeTerrain != null) { carteMourante.TerrainId = -1; }
        GameManager.Instance.ToutesLesCartes.Remove(carteMourante.GetComponent<Carte>());
        VerifierGagnant();
    }


    [Server]
    void ServerEchangerCarte(uint carteNetId, int terrainId, uint carteNetId2, int terrainId2)
    {
        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        Carte carte = identity.GetComponent<Carte>();
        carte.TerrainId = terrainId;
        if (!NetworkClient.spawned.TryGetValue(carteNetId2, out NetworkIdentity identity2)) { return; }
        Carte carte2 = identity2.GetComponent<Carte>();
        carte2.TerrainId = terrainId2;
        ViderTerrain("Normal");
    }


    //Client RPC

    [ClientRpc]
    private void RpcRendreStratege(Carte carte, int terrainId)
    {
        if ((isLocalPlayer && terrainId == 1) || (!isLocalPlayer && terrainId == 4))
        {
            Stratege = carte;
            PMEnCours = Stratege.PMVar;
            BoutonTourSuivant.GetComponent<Button>().interactable = true;
        }
    }

    [ClientRpc]
    private void RpcGagner()
    {
        //IF moi joueur
        GameManager.Instance.Proposition("Gagner");
        // If pas moi joueur
        GameManager.Instance.Proposition("Perdre");
    }

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


    //Toutes petites fonctions
    [Server]
    private void ViderTerrain(string choix) // Pour enlever les cartes encore placées sur eux
    {
        switch (choix)
        {
            case "Tout":
                foreach (PlaceTerrain terrainTest in GameManager.Instance.TousLesTerrains) { terrainTest.CartePlacee = null; }
                break;
            case "Normal":
                foreach (PlaceTerrain terrainTest in GameManager.Instance.TousLesTerrains) { if (terrainTest.transform.childCount == 0) { terrainTest.CartePlacee = null; } }
                break;
        }
    }

}