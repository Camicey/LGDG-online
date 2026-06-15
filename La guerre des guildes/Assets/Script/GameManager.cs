using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using System;
using TMPro;
using Unity.VisualScripting;

public class GameManager : NetworkBehaviour
{
    public Sprite ImageDosCarte;
    public List<int> Pioche = new List<int>();
    public List<Carte> Defausse = new List<Carte>(); // Pour l'instant inutilisee, peut etre faire une liste de int a la place
    public List<CarteSettings> CartesSettings = new List<CarteSettings>(); //Ajoutees manuellement
    public bool TerrainCree = false;
    public EcranDeConfirmation EcranDeConfirmation;
    public static GameManager Instance;

    public void Start()
    {
        Instance = this;
        ImporterCartes();
        if (NetworkServer.active)
        {
            CreerDeck();
        }
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
            carte.Image = Resources.Load<Sprite>("Images/Personnage/" + colonnes[5]); //Image
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
            CartesSettings.Add(carte);
        }

    }

    [Server]
    private void CreerDeck()
    {
        Pioche.Clear();

        foreach (var carteStats in CartesSettings)
        {
            Pioche.Add(carteStats.Id);
        }
        Melanger(Pioche);
    }

    private void Melanger(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
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

    public void Proposition(Carte carteDeplacee, Carte carteChoisie, string choix)
    {
        if (!DeplacementAutorise(carteChoisie.PlaceDeTerrain.Id, carteDeplacee.PlaceDeTerrain.Id)) { return; }
        EcranDeConfirmation.GameObject().SetActive(true);
        if (choix == "Echanger" && !carteDeplacee.EstStratege && !carteChoisie.EstStratege)
        { EcranDeConfirmation.Texte.text = $"Voulez-vous échanger {carteDeplacee.Stats.Prenom} et {carteChoisie.Stats.Prenom} ?"; }
        else if (choix == "Echanger" && (carteDeplacee.EstStratege || carteChoisie.EstStratege))
        { EcranDeConfirmation.Texte.text = $"Voulez-vous changer de stratège et mettre {carteDeplacee.Stats.Prenom} à la place ?"; }
        else if (choix == "Attaquer")
        {
            EcranDeConfirmation.Texte.text = $"Voulez-vous attaquer ";
            if (carteChoisie.EstVisible)
            { EcranDeConfirmation.Texte.text += $"{carteChoisie.Stats.Prenom} "; }
            EcranDeConfirmation.Texte.text += $"avec {carteDeplacee.Stats.Prenom}, en infligeant {carteDeplacee.PAVar.ToString()} dégâts ?";
            if (carteChoisie.isOwned) { EcranDeConfirmation.Texte.text += "\nAttention c'est votre carte."; }
        }
        //Visuels
        EcranDeConfirmation.BoutonConfirmer.GetComponentInChildren<TMP_Text>().text = choix;
        EcranDeConfirmation.CarteDeplaceeTemp = carteDeplacee;
        EcranDeConfirmation.CarteChoisieTemp = carteChoisie;
        EcranDeConfirmation.ChoixTemp = choix;
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
}