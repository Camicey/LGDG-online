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
            //Les choses qu'on devra commencer ici
            GameManager.Instance.Pioche.Clear();
            GameManager.Instance.CreerDeck();

            UnityEngine.Debug.Log("Test de créer un deck");
            foreach (PlayerManager joueur in GameManager.Instance.TousLesJoueurs)
            {
                joueur.ValeurPM(10);
                for (int i = 0; i < 4; i++)
                {
                    GameManager.Instance.Piocher(joueur.connectionToClient);
                }
            }
            gameObject.SetActive(false);
        }
    }
}