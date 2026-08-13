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
        if (!GameManager.Instance.JePeuxJouer(PlayerManager.LocalPlayer, "Piocher", null))
        {
            UnityEngine.Debug.LogError("Je suis returné");
            return;
        }
        if (PlayerManager.LocalPlayer != null)
        {

            UnityEngine.Debug.LogError("Je veux piocher");
            if (PlayerManager.LocalPlayer.DeckJoueur.transform.childCount >= 5)
            {
                PlayerManager.LocalPlayer.TransfertProposition("DeckPlein");
                UnityEngine.Debug.LogError("Deck ?");
                return;
            }
            PlayerManager.LocalPlayer.CmdPiocher(); // le serveur tranchera pour la pioche vide
        }
    }
}