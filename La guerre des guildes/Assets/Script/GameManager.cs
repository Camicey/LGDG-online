using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public PlayerManager PlayerManager;
    public Sprite ImageDosCarte;
    public List<Carte> Pioche = new List<Carte>();
    public List<Carte> Defausse = new List<Carte>();
    public List<CarteSettings> CartesSettings = new List<CarteSettings>(); //Toutes les cartes settings

    public void Piocher()
    {
        NetworkIdentity networkIdentity = NetworkClient.connection.identity;
        PlayerManager = networkIdentity.GetComponent<PlayerManager>();
        if (PlayerManager.LocalPlayer == null)
        {
            Debug.LogWarning("Player local pas encore prêt");
            return;
        }
        PlayerManager.LocalPlayer.CmdPiocher();
    }

}
