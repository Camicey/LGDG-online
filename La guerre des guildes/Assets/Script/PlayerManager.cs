using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    public List<GameObject> pioche = new List<GameObject>();
    public List<Carte> PiocheCarte = new List<Carte>();
    public GameObject PrefabCarte;
    public GameObject DeckJoueur;
    public GameObject DeckAdversaire;
    public GameObject PlaceTerrainJoueur;
    public GameObject PlaceTerrainAdversaire;
    public GameObject DossierCarte;
    public GameManager JeuEnCours;
    public List<Carte> DeckCartes = new List<Carte>();
    [SyncVar]
    public int Cartesjouees = 0; //Cette information n'est PAS partagée
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
        pioche.Add(PrefabCarte);
    }
    [Command]
    public void CmdInstancier()
    {
        foreach (CarteSettings carteSettings in JeuEnCours.CartesSettings) //On instancie les cartes
        {
            GameObject derniere = Instantiate(PrefabCarte, new Vector3(0, 0, 0), Quaternion.identity, DossierCarte.transform); //-500 -500
            NetworkServer.Spawn(derniere, connectionToClient);
            derniere.GetComponent<Carte>().Stats = carteSettings;
            //derniere.GetComponent<Carte>().Commencer();
        }
    }
    [Command]
    public void CmdPiocher()
    {
        if (DeckCartes.Count < 5) //Limite de pioche à 5 cartes
        {
            //Ce qu'il faudra changer 
            GameObject carte = Instantiate(pioche[Random.Range(0, pioche.Count)], new UnityEngine.Vector3(0, 0, 0), UnityEngine.Quaternion.identity);
            //Ce qu'il faudre changer
            carte.GetComponent<Carte>().Initialiser();
            DeckCartes.Add(carte.GetComponent<Carte>()); //On ajoute au Deck la carte
            NetworkServer.Spawn(carte, connectionToClient);
            RpcShowCard(carte, "Dealt");
        }
    }

    public void JouerCarte(GameObject carte)
    {
        CmdJouerCarte(carte);
        Cartesjouees++;
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
            if (isLocalPlayer) //Si je suis le joueur
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
        }
    }

}
