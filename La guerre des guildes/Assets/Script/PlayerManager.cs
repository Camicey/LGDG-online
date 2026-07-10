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
    public GameObject TerrainsJoueur; // Inutilisé pour l'instant
    public GameObject Defausse; // Pour l'instant inutilisee, peut etre faire une liste de int a la place
    public List<PlaceTerrain> TerrainsJoueurList = new List<PlaceTerrain>();
    public GameObject TerrainsAdverse; // Inutilisé pour l'instant
    public List<PlaceTerrain> TerrainsAdverseList = new List<PlaceTerrain>();
    public GameObject DossierCarte;
    public GameObject PMObjet;
    public Carte Stratege;
    public static PlayerManager LocalPlayer;
    [SyncVar(hook = nameof(OnPMChanged))]
    public int PMEnCours;

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

    //Fonctions
    public void OnPMChanged(int ancienneValeur, int nouvelleValeur)
    {
        if (!isLocalPlayer) return;
        if (PMObjet == null) return;
        string pmChecker = nouvelleValeur.ToString();
        if (Stratege != null) { pmChecker += " / " + Stratege.PMVar; }
        PMObjet.GetComponent<TMP_Text>().text = pmChecker;
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

    [Server]
    public bool CoutAction(int cout)
    {
        if (PMEnCours - cout <= 0)
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

    [Command]
    public void CmdPiocher()
    {
        GameManager.Instance.Piocher(connectionToClient);
    }

    public void JouerCarte(Carte carte, PlaceTerrain terrain)
    {
        CmdJouerCarte(carte.netId, terrain.Id);
    }

    public void PiocherCarte(GameObject carte)
    {
        carte.GetComponent<Carte>().Initialiser();
        RpcShowCard(carte);
    }

    public void PiocherTerrain(GameObject terrain)
    {
        RpcShowTerrain(terrain);
    }

    [Command]
    public void CmdJouerCarte(uint carteNetId, int terrainId)
    {
        RpcJouerCarte(carteNetId, terrainId);
    }



    [Command]
    public void CmdMourir(GameObject carte)
    {
        RpcMourirCarte(carte);
    }

    [ClientRpc]
    private void RpcMourirCarte(GameObject carte)
    {
        Carte carteMourante = carte.GetComponent<Carte>();
        if (carteMourante.PlaceDeTerrain != null)
        {
            carteMourante.PlaceDeTerrain.CartePlacee = null;
        }
        carteMourante.transform.SetParent(Defausse.transform, false);
        carteMourante.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
        carteMourante.Initialiser();
        carteMourante.PVar = 0;
        carteMourante.PlayerManager = null;

        GameManager.Instance.ToutesLesCartes.Remove(carteMourante.GetComponent<Carte>());

        VerifierGagnant();
    }

    private void EchangerCarte(uint carteDeplaceeId, uint carteChoisieId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteDeplaceeId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteDeplacee = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        //int terrainInitial = carteDeplacee.PlaceDeTerrain.Id;
        //RpcJouerCarte(carteDeplaceeId, carteChoisie.PlaceDeTerrain.Id);
        //RpcJouerCarte(carteChoisieId, terrainInitial);
        RpcEchangerCarte(carteDeplaceeId, carteChoisie.PlaceDeTerrain.Id, carteChoisieId, carteDeplacee.PlaceDeTerrain.Id);

        /*carteDeplacee.PlaceDeTerrain = carteChoisie.PlaceDeTerrain;
        if (terrainInitial >= 4)
        { carteChoisie.PlaceDeTerrain = TerrainsAdverseList.Find(t => t.Id == terrainInitial); }
        else { carteChoisie.PlaceDeTerrain = TerrainsJoueurList.Find(t => t.Id == terrainInitial); }*/

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
            //Blabla je peux pas attaquer

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
        RpcAttaquerCarte(carteAttaquanteId, carteChoisieId, degats, degatsDef);
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

    [ClientRpc]
    private void RpcGagner()
    {
        //IF moi joueur
        GameManager.Instance.Proposition("Gagner");
        // If pas moi joueur
        GameManager.Instance.Proposition("Perdre");
    }

    [ClientRpc]
    private void RpcAttaquerCarte(uint carteAttaquanteId, uint carteChoisieId, int degatsAtt, int degatDef)
    {
        if (!NetworkClient.spawned.TryGetValue(carteAttaquanteId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteAttaquante = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        carteAttaquante.EstVisible = true;
        carteAttaquante.MontrerCarte();
        carteChoisie.EstVisible = true;
        carteChoisie.MontrerCarte();

        carteChoisie.PVar = carteChoisie.PVar - degatsAtt; //Deja bon pour robots et piège (bon je dois le faire)
        if (carteChoisie.PVar > 0) // Si elle survit, elle réplique
        {
            carteAttaquante.PVar = carteAttaquante.PVar - carteChoisie.PAVar;
            if (carteAttaquante.PVar <= 0) { carteAttaquante.Mourir(); }
        }
        else { carteChoisie.Mourir(); }
        carteChoisie.PVT.text = carteChoisie.PVar.ToString();
        carteAttaquante.PVT.text = carteAttaquante.PVar.ToString();

    }

    [ClientRpc]
    private void RpcShowCard(GameObject carte)
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
    private void RpcShowTerrain(GameObject terrain)
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
        }
        else
        {
            //Je place de l'autre
            if (terraing.Id > 3)
            {
                terraing.transform.SetParent(TerrainsJoueur.transform, false);
                terraing.EstAMoi = true;
            }
            else
            {
                terraing.transform.SetParent(TerrainsAdverse.transform, false);
                if (terraing.Id == 4) { terraing.EstTerrainStratege = true; }
            }
        }
        GameManager.Instance.TousLesTerrains.Add(terrain.GetComponent<PlaceTerrain>());
    }

    [ClientRpc]
    void RpcJouerCarte(uint carteNetId, int terrainId)
    {

        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        //On récupère nos variables
        Carte carte = identity.GetComponent<Carte>();
        PlaceTerrain terrain = GameManager.Instance.TousLesTerrains.Find(t => t.Id == terrainId);
        if (terrain == null) { return; }
        carte.transform.SetParent(terrain.transform, false); // Je la place sur le Terrain Joueur
        PivotCentre(carte);
        terrain.CartePlacee = carte.GetComponent<Carte>();
        if (isLocalPlayer)
        {
            if (terrain.EstTerrainStratege && terrain.Id == 1)
            {
                carte.EstStratege = true;
                Stratege = carte;
                carte.EstVisible = true;
            }
        }
        else
        {
            if (terrain.EstTerrainStratege && terrain.Id == 4)
            {
                carte.EstStratege = true;
                Stratege = carte;
                carte.EstVisible = true;
            }
        }
        carte.EstEnJeu = true;
        ViderTerrain("Normal");
    }

    [ClientRpc]
    void RpcEchangerCarte(uint carteNetId, int terrainId, uint carteNetId2, int terrainId2)
    {
        List<uint> cartesId = new List<uint>();
        cartesId.Add(carteNetId);
        cartesId.Add(carteNetId2);
        List<int> terrainsId = new List<int>();
        terrainsId.Add(terrainId);
        terrainsId.Add(terrainId2);

        for (int i = 0; i < 2; i++)
        {
            if (!NetworkClient.spawned.TryGetValue(cartesId[i], out NetworkIdentity identity)) { return; }
            //On récupère nos variables
            Carte carte = identity.GetComponent<Carte>();
            PlaceTerrain terrain = TerrainsJoueurList.Find(t => t.Id == terrainsId[i]);
            if (terrain == null) { terrain = TerrainsAdverseList.Find(t => t.Id == terrainsId[i]); }
            if (isLocalPlayer)
            {
                carte.transform.SetParent(terrain.transform, false); // Je la place sur le Terrain Joueur
                terrain.CartePlacee = carte.GetComponent<Carte>();
                if (terrain.EstTerrainStratege)
                {
                    carte.EstStratege = true;
                    Stratege = carte;
                    carte.EstVisible = true;
                }
                if (terrainsId[i] >= 4)
                { carte.PlaceDeTerrain = TerrainsAdverseList.Find(t => t.Id == terrainsId[i]); }
                else { carte.PlaceDeTerrain = TerrainsJoueurList.Find(t => t.Id == terrainsId[i]); }
            }
            else
            {
                PlaceTerrain terrainAdversaire;
                int idTerrain = terrain.gameObject.GetComponent<PlaceTerrain>().Id + 3;
                if (idTerrain > 6) { idTerrain = idTerrain - 6; }
                if (idTerrain >= 4) { terrainAdversaire = TerrainsAdverseList.Find(t => t.Id == idTerrain); }
                else { terrainAdversaire = TerrainsJoueurList.Find(t => t.Id == idTerrain); }

                carte.transform.SetParent(terrainAdversaire.transform, false);
                terrainAdversaire.CartePlacee = carte.GetComponent<Carte>();
                carte.GetComponent<Carte>().PlaceDeTerrain = terrainAdversaire;
                if (terrainAdversaire.EstTerrainStratege)
                {
                    carte.EstStratege = true;
                    carte.EstVisible = true;
                    carte.GetComponent<Carte>().MontrerCarte();
                }
            }

            carte.EstEnJeu = true;
            PivotCentre(carte);
        }
        ViderTerrain("Normal");
    }


    //Toutes petites fonctions
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
    private void PivotCentre(Carte carte) // Pour enlever les cartes encore placées sur eux
    {
        carte.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Remettre le pivot au centre
        carte.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        carte.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
    }
}