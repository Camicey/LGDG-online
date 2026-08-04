using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class Menu : MonoBehaviour
{

    public string adresseIP = "localhost";

    public void OnClickQuitterJeu()
    {
        Application.Quit();
    }


    public void OnClickRejoindrePartie() //(string adresseIP) pour le futur
    {
        NetworkManager.singleton.networkAddress = "localhost";
        NetworkManager.singleton.StartClient();
    }

    public void OnClickCreerPartie()
    {
        NetworkManager.singleton.StartHost();
    }

    public void OnClickPartieSolo()
    {
        // j'ai pas encore
    }

}