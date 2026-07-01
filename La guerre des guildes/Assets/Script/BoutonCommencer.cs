using UnityEngine;

public class BoutonCommencer : MonoBehaviour
{
    public GameManager JeuEnCours;

    public void OnClickCommencer()
    {

        //Les choses qu'on devra commencer ici
        GameManager.Instance.Pioche.Clear();
        GameManager.Instance.CreerDeck();
        UnityEngine.Debug.Log("Test de créer un deck");
        JeuEnCours.CreerDeck();
    }

}