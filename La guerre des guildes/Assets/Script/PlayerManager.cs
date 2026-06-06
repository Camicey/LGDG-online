using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    public GameObject PrefabCarte;
    public GameObject DeckJoueur;
    public GameObject DeckAdversaire;
    public GameObject PlaceTerrainJoueur;
    public GameObject PlaceTerrainAdversaire;
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
        PlaceTerrainJoueur = GameObject.Find("PlaceTerrainJoueur");
        PlaceTerrainAdversaire = GameObject.Find("PlaceTerrainAdversaire");
        DossierCarte = GameObject.Find("DossierCarte");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    [Command]
    public void CmdPiocher()
    {
        JeuEnCours.Piocher(connectionToClient);
    }

    public void JouerCarte(GameObject carte)
    {
        CmdJouerCarte(carte);
    }
    public void PiocherCarte(GameObject carte)
    {
        carte.GetComponent<Carte>().Initialiser();
        RpcShowCard(carte, "Dealt");
    }

    [Command]
    private void CmdJouerCarte(GameObject carte)
    {
        RpcShowCard(carte, "Played");
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
                carte.GetComponent<Carte>().MontrerCarte(); // Je la montre
            }
            else
            {
                carte.transform.SetParent(DeckAdversaire.transform, false); // Je la met dans le deck de l'autre joueur
                carte.GetComponent<Carte>().CacherCarte(); // Je la cache
            }
        }
        else if (type == "Played") // Si elle est placée sur le terrain
        {
            if (isLocalPlayer)
            {
                carte.transform.SetParent(PlaceTerrainJoueur.transform, false); // Je la place sur le Terrain Joueur
            }
            else
            {
                carte.transform.SetParent(PlaceTerrainAdversaire.transform, false); // Je la place sur le Terrain Adverse
            }
            carte.GetComponent<Carte>().EstEnJeu = true;
        }
    }
}
