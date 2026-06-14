using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using Mirror.BouncyCastle.Asn1.Misc;

public class PlayerManager : NetworkBehaviour
{
    public GameObject PrefabCarte;
    public GameObject DeckJoueur;
    public GameObject DeckAdversaire;
    public GameObject PlaceTerrain;
    public GameObject TerrainsJoueur; // Inutilisé pour l'instant
    public List<PlaceTerrain> TerrainsJoueurList = new List<PlaceTerrain>();
    public GameObject TerrainsAdverse; // Inutilisé pour l'instant
    public List<PlaceTerrain> TerrainsAdverseList = new List<PlaceTerrain>();
    public GameObject DossierCarte;
    public GameManager JeuEnCours;
    public List<Carte> DeckCartes = new List<Carte>();
    public static PlayerManager LocalPlayer;

    public override void OnStartClient()
    {
        base.OnStartClient();
        JeuEnCours = GameObject.Find("GameManagerObject").GetComponent<GameManager>();
        DeckJoueur = GameObject.Find("DeckJoueur");
        DeckAdversaire = GameObject.Find("DeckAdversaire");
        TerrainsJoueur = GameObject.Find("TerrainsJoueur");
        TerrainsAdverse = GameObject.Find("TerrainsAdverse");
        DossierCarte = GameObject.Find("DossierCarte");
        TerrainsJoueurList.Add(GameObject.Find("PlaceTerrain1").GetComponent<PlaceTerrain>());
        TerrainsJoueurList.Add(GameObject.Find("PlaceTerrain2").GetComponent<PlaceTerrain>());
        TerrainsJoueurList.Add(GameObject.Find("PlaceTerrain3").GetComponent<PlaceTerrain>());
        TerrainsAdverseList.Add(GameObject.Find("PlaceTerrain4").GetComponent<PlaceTerrain>());
        TerrainsAdverseList.Add(GameObject.Find("PlaceTerrain5").GetComponent<PlaceTerrain>());
        TerrainsAdverseList.Add(GameObject.Find("PlaceTerrain6").GetComponent<PlaceTerrain>());
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
    }

    [Command]
    public void CmdConfirmerAction(uint carteDeplaceeId, uint carteChoisieId, string choix)
    {
        if (!NetworkServer.spawned.TryGetValue(carteDeplaceeId, out NetworkIdentity identity1))
            return;
        if (!NetworkServer.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteDeplacee = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        if (choix == "Echanger" && !carteDeplacee.EstStratege && !carteChoisie.EstStratege)
        {
            RpcEchangeCarte(carteDeplaceeId, carteChoisieId);
        }
    }

    [Command]
    public void CmdPiocher()
    {
        JeuEnCours.Piocher(connectionToClient);
    }

    public void JouerCarte(Carte carte, PlaceTerrain terrain)
    {
        CmdJouerCarte(carte.netId, terrain.Id);
    }

    public void PiocherCarte(GameObject carte)
    {
        carte.GetComponent<Carte>().Initialiser();
        RpcShowCard(carte, "Dealt");
    }

    [Command]
    public void CmdJouerCarte(uint carteNetId, int terrainId)
    {
        RpcJoueCarte(carteNetId, terrainId);
    }

    [ClientRpc]
    private void RpcEchangeCarte(uint carteDeplaceeId, uint carteChoisieId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteDeplaceeId, out NetworkIdentity identity1))
            return;
        if (!NetworkClient.spawned.TryGetValue(carteChoisieId, out NetworkIdentity identity2))
            return;
        Carte carteDeplacee = identity1.GetComponent<Carte>();
        Carte carteChoisie = identity2.GetComponent<Carte>();

        int terrainInitial = carteDeplacee.PlaceDeTerrain.Id;
        RpcJoueCarte(carteDeplaceeId, carteChoisie.PlaceDeTerrain.Id);
        RpcJoueCarte(carteChoisieId, terrainInitial);
        carteDeplacee.PlaceDeTerrain = carteChoisie.PlaceDeTerrain;
        if (terrainInitial >= 4)
        { carteChoisie.PlaceDeTerrain = TerrainsAdverseList.Find(t => t.Id == terrainInitial); }
        else { carteChoisie.PlaceDeTerrain = TerrainsJoueurList.Find(t => t.Id == terrainInitial); }

    }

    [ClientRpc]
    private void RpcShowCard(GameObject carte, string type)
    {
        if (carte == null) return;
        if (type == "Dealt") // Si elles viennent d'être piochées
        {
            if (isLocalPlayer) // Si je suis le joueur
            {
                carte.transform.SetParent(DeckJoueur.transform, false); // Je la met dans mon deck
                DeckCartes.Add(carte.GetComponent<Carte>());
                carte.GetComponent<Carte>().MontrerCarte(); // Je la montre
                UnityEngine.Debug.Log("C'est ma carte");
            }
            else
            {
                carte.transform.SetParent(DeckAdversaire.transform, false); // Je la met dans le deck de l'autre joueur
                carte.GetComponent<Carte>().CacherCarte(); // Je la cache
                UnityEngine.Debug.Log("Pas ma carte");
            }
        }
    }

    [ClientRpc]
    void RpcJoueCarte(uint carteNetId, int terrainId)
    {
        if (!NetworkClient.spawned.TryGetValue(carteNetId, out NetworkIdentity identity))
        {
            Debug.LogError("Carte introuvable côté client !");
            return;
        }
        Carte carte = identity.GetComponent<Carte>();
        PlaceTerrain terrain = TerrainsJoueurList.Find(t => t.Id == terrainId);
        if (terrain == null) { terrain = TerrainsAdverseList.Find(t => t.Id == terrainId); }
        int idTerrain = terrain.gameObject.GetComponent<PlaceTerrain>().Id + 3;

        carte.canvasGroup.alpha = 1f;
        carte.canvasGroup.blocksRaycasts = true;
        carte.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // C'est pour remettre le pivot au centre
        carte.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        carte.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);

        if (idTerrain > 6) { idTerrain = idTerrain - 6; }
        if (isLocalPlayer)
        {
            carte.transform.SetParent(terrain.transform, false); // Je la place sur le Terrain Joueur
            terrain.CartePlacee = carte.GetComponent<Carte>();
            if (idTerrain == 1) { carte.EstStratege = true; }
        }
        else
        {
            if (idTerrain == 4) { carte.EstStratege = true; } // N'est pas très au point pour l'instant
            PlaceTerrain terrainAdversaire;
            if (idTerrain >= 4)
            { terrainAdversaire = TerrainsAdverseList.Find(t => t.Id == idTerrain); }
            else { terrainAdversaire = TerrainsJoueurList.Find(t => t.Id == idTerrain); }
            carte.transform.SetParent(terrainAdversaire.transform, false);
            terrainAdversaire.CartePlacee = carte.GetComponent<Carte>();
            carte.GetComponent<Carte>().PlaceDeTerrain = terrainAdversaire;
        }
        carte.EstEnJeu = true;
        //Ce qui est en dessous c'est pour enlever les cartes encore placées sur eux
        foreach (PlaceTerrain terrainTest in TerrainsAdverseList) { if (terrainTest.transform.childCount == 0) { terrainTest.CartePlacee = null; } }
        foreach (PlaceTerrain terrainTest in TerrainsJoueurList) { if (terrainTest.transform.childCount == 0) { terrainTest.CartePlacee = null; } }
    }
}