using UnityEngine;
using Mirror;

public class BoutonCommencer : MonoBehaviour
{
    public GameManager JeuEnCours;

    void Start()
    {
        Invoke(nameof(VerifierServeur), 0.1f);
    }

    void VerifierServeur()
    {
        gameObject.SetActive(NetworkServer.active);
    }
    public void OnClickCommencer()
    {
        if (GameManager.Instance.TousLesJoueurs.Count == 2)
        {
            gameObject.SetActive(false);
            //Les choses qu'on devra commencer ici
            GameManager.Instance.Pioche.Clear();
            GameManager.Instance.CreerDeck();

            foreach (PlayerManager joueur in GameManager.Instance.TousLesJoueurs)
            {
                joueur.ValeurPM(10);
                for (int i = 0; i < 4; i++)
                {
                    GameManager.Instance.Piocher(joueur.connectionToClient);
                }
            }
            for (int i = 1; i < 7; i++)
            { GameManager.Instance.CreerTerrain(GameManager.Instance.TousLesJoueurs[0].connectionToClient, i); }
        }
        else //Pour mes tests, a enlever
        {
            gameObject.SetActive(false);
            GameManager.Instance.Pioche.Clear();
            GameManager.Instance.CreerDeck();
            foreach (PlayerManager joueur in GameManager.Instance.TousLesJoueurs)
            {
                joueur.ValeurPM(10);
                for (int i = 0; i < 4; i++)
                {
                    GameManager.Instance.Piocher(joueur.connectionToClient);
                }
            }
            for (int i = 1; i < 7; i++)
            { GameManager.Instance.CreerTerrain(GameManager.Instance.TousLesJoueurs[0].connectionToClient, i); }
        }
    }
}