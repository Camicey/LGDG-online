using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using Mirror.BouncyCastle.Asn1.Misc;
using Unity.VisualScripting;

public class PlayerManager : NetworkBehaviour
{
    public GameObject PrefabCarte;
    public GameObject DeckJoueur;
    public GameObject DeckAdversaire;
    public GameObject PlaceTerrain;
    public GameObject TerrainsJoueur; // Inutilisé pour l'instant
    public GameObject Defausse; // Pour l'instant inutilisee, peut etre faire une liste de int a la place
    public List<PlaceTerrain> TerrainsJoueurList = new List<PlaceTerrain>();
    public GameObject TerrainsAdverse; // Inutilisé pour l'instant
    public List<PlaceTerrain> TerrainsAdverseList = new List<PlaceTerrain>();
    public GameObject DossierCarte;
    public static PlayerManager LocalPlayer;

    public override void OnStartClient()
    {
        base.OnStartClient();
        DeckJoueur = GameObject.Find("DeckJoueur");
        DeckAdversaire = GameObject.Find("DeckAdversaire");
        TerrainsJoueur = GameObject.Find("TerrainsJoueur");
        TerrainsAdverse = GameObject.Find("TerrainsAdverse");
        DossierCarte = GameObject.Find("DossierCarte");
        Defausse = GameObject.Find("Defausse");
        TerrainsJoueurList.Add(GameObject.Find("PlaceTerrain1").GetComponent<PlaceTerrain>());
        TerrainsJoueurList.Add(GameObject.Find("PlaceTerrain2").GetComponent<PlaceTerrain>());
        TerrainsJoueurList.Add(GameObject.Find("PlaceTerrain3").GetComponent<PlaceTerrain>());
        TerrainsAdverseList.Add(GameObject.Find("PlaceTerrain4").GetComponent<PlaceTerrain>());
        TerrainsAdverseList.Add(GameObject.Find("PlaceTerrain5").GetComponent<PlaceTerrain>());
        TerrainsAdverseList.Add(GameObject.Find("PlaceTerrain6").GetComponent<PlaceTerrain>());

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
        }
        if (choix == "Attaquer")
        {
            AttaquerCarte(carteDeplaceeId, carteChoisieId);
        }
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

        int terrainInitial = carteDeplacee.PlaceDeTerrain.Id;
        RpcJouerCarte(carteDeplaceeId, carteChoisie.PlaceDeTerrain.Id);
        RpcJouerCarte(carteChoisieId, terrainInitial);
        carteDeplacee.PlaceDeTerrain = carteChoisie.PlaceDeTerrain;
        if (terrainInitial >= 4)
        { carteChoisie.PlaceDeTerrain = TerrainsAdverseList.Find(t => t.Id == terrainInitial); }
        else { carteChoisie.PlaceDeTerrain = TerrainsJoueurList.Find(t => t.Id == terrainInitial); }

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
            PlaceTerrain terrain2 = TerrainsJoueurList.Find(t => t.Id == 2);
            PlaceTerrain terrain3 = TerrainsJoueurList.Find(t => t.Id == 3);
            PlaceTerrain terrain5 = TerrainsAdverseList.Find(t => t.Id == 5);
            PlaceTerrain terrain6 = TerrainsAdverseList.Find(t => t.Id == 6);
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
    void RpcJouerCarte(uint carteNetId, int terrainId)
    {

        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity)) { return; }
        //On récupère nos variables
        Carte carte = identity.GetComponent<Carte>();
        PlaceTerrain terrain = TerrainsJoueurList.Find(t => t.Id == terrainId);
        if (terrain == null) { terrain = TerrainsAdverseList.Find(t => t.Id == terrainId); }

        carte.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Remettre le pivot au centre
        carte.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        carte.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
        PlaceTerrain terrainAdversaire;
        int idTerrain = terrain.gameObject.GetComponent<PlaceTerrain>().Id + 3;
        if (isLocalPlayer)
        {
            carte.transform.SetParent(terrain.transform, false); // Je la place sur le Terrain Joueur
            terrain.CartePlacee = carte.GetComponent<Carte>();
            if (terrain.EstTerrainStratege && terrain.Id == 1)
            {
                carte.EstStratege = true;
                carte.EstVisible = true;
            }
        }
        else
        {
            if (idTerrain > 6) { idTerrain = idTerrain - 6; }
            if (idTerrain >= 4) { terrainAdversaire = TerrainsAdverseList.Find(t => t.Id == idTerrain); }
            else { terrainAdversaire = TerrainsJoueurList.Find(t => t.Id == idTerrain); }

            carte.transform.SetParent(terrainAdversaire.transform, false);
            terrainAdversaire.CartePlacee = carte.GetComponent<Carte>();
            carte.GetComponent<Carte>().PlaceDeTerrain = terrainAdversaire;
            if (terrainAdversaire.EstTerrainStratege && terrainAdversaire.Id == 4)
            {
                carte.EstStratege = true;
                carte.EstVisible = true;
                carte.GetComponent<Carte>().MontrerCarte();
            }
        }
        carte.EstEnJeu = true;
        ViderTerrain("Normal");
    }


    //Toutes petites fonctions
    private void ViderTerrain(string choix) // Pour enlever les cartes encore placées sur eux
    {
        switch (choix)
        {
            case "Tout":
                foreach (PlaceTerrain terrainTest in TerrainsAdverseList) { terrainTest.CartePlacee = null; }
                foreach (PlaceTerrain terrainTest in TerrainsJoueurList) { terrainTest.CartePlacee = null; }
                break;
            case "Normal":
                foreach (PlaceTerrain terrainTest in TerrainsAdverseList) { if (terrainTest.transform.childCount == 0) { terrainTest.CartePlacee = null; } }
                foreach (PlaceTerrain terrainTest in TerrainsJoueurList) { if (terrainTest.transform.childCount == 0) { terrainTest.CartePlacee = null; } }
                break;
        }
    }
}