using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public Sprite ImageDosCarte;
    public List<int> Pioche = new List<int>();
    public List<Carte> Defausse = new List<Carte>();
    public List<CarteSettings> CartesSettings = new List<CarteSettings>(); //Toutes les cartes settings

    public override void OnStartServer()
    {
        CreateDeck();
    }

    [Server]
    private void CreateDeck()
    {
        Pioche.Clear();

        foreach (var card in CartesSettings)
        {
            Pioche.Add(card.Id);
        }
        Shuffle(Pioche);
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    [Server]
    public void Piocher(NetworkConnectionToClient conn)
    {
        if (Pioche.Count == 0) return;

        PlayerManager joueur = conn.identity.GetComponent<PlayerManager>();

        if (joueur.DeckCartes.Count > 5) return;

        int dataId = Pioche[0];
        Pioche.RemoveAt(0);

        GameObject cardObj = Instantiate(joueur.PrefabCarte);

        Carte carte = cardObj.GetComponent<Carte>();
        carte.Id = dataId;
        //CarteSettings data = CartesSettings.Find(c => c.Id == dataId);

        carte.Initialiser();
        carte.PlayerManager = joueur;

        NetworkServer.Spawn(cardObj, conn);
        joueur.PiocherCarte(cardObj);
    }

}