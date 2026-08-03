using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public void Start()
    {
        //GetComponent<Button>().interactable = false; //A mettre quand j'aurais vérifié qu'il y a deux joueurs
    }

    public void OnClickPiocher()
    {
        if (PlayerManager.LocalPlayer != null) { PlayerManager.LocalPlayer.CmdPiocher(); }
    }
}