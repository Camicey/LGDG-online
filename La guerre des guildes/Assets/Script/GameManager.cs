using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public Sprite ImageDosCarte;
    public List<int> Pioche = new List<int>();
    public List<Carte> Defausse = new List<Carte>(); // Pour l'instant inutilisee, peut etre faire une liste de int a la place
    public List<CarteSettings> CartesSettings = new List<CarteSettings>(); //Ajoutees manuellement

    public override void OnStartServer()
    {
        ImporterCartes();
        CreateDeck();
    }

    public void ImporterCartes()
    {
        CartesSettings.Clear();
        // Lire CSV
        string[] export = File.ReadAllLines("Assets/Script/ExportCartes.csv");
        string ligne = export[0];
        for (int i = 1; i < export.Length; i++)
        {
            CarteSettings carte = ScriptableObject.CreateInstance<CarteSettings>();
            ligne = export[i];
            string[] colonnes = ligne.Split(',');
            carte.Id = int.Parse(colonnes[0]);
            carte.Prenom = colonnes[1];
            carte.PM = int.Parse(colonnes[2]);
            carte.PV = int.Parse(colonnes[3]);
            carte.PA = int.Parse(colonnes[4]);
            //Image
            string nomImage = colonnes[5];
            carte.Image = Resources.Load<Sprite>("Images/Personnage/" + nomImage);
            carte.Pouvoir = colonnes[6];
            carte.IdPouvoir = int.Parse(colonnes[7]);
            carte.ComplementPouvoir = colonnes[8];
            carte.CoutPouvoir = float.Parse(colonnes[9]);
            string[] lienTransfert = colonnes[10].Split('/');
            foreach (string lien in lienTransfert) { carte.liens.Add(int.Parse(lien)); }
            carte.Particularite = colonnes[12];
            carte.Famille = colonnes[13];
            carte.FamilleImage = Resources.Load<Sprite>("Images/Famille/" + carte.Famille);
            carte.Type = colonnes[14];
            carte.TypeImage = Resources.Load<Sprite>("Images/Type/" + carte.Type);

            //Id,Prenom,PM,PV,PA,Image,Pouvoir,IdPouvoir,Complement Pouvoir,Cout,LienID,Liens,Particularite,Famille,Role

            UnityEngine.Debug.Log($"{carte.Id} - {carte.Prenom}");

            CartesSettings.Add(carte);
        }

    }

    [Server]
    private void CreateDeck()
    {
        Pioche.Clear();

        foreach (var carteStats in CartesSettings)
        {
            Pioche.Add(carteStats.Id);
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
        carte.PlayerManager = joueur;

        NetworkServer.Spawn(cardObj, conn);
        joueur.PiocherCarte(cardObj);
    }

}