using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Icone : MonoBehaviour
{

    public Sprite Image;
    public string Type;
    public bool EstMontre;
    public string Infos;
    public int IDPartenaire;

    // Start is called before the first frame update
    void Start()
    {
        CanvasGroup groupeIcone = GetComponent<CanvasGroup>();
        groupeIcone.blocksRaycasts = true;
        groupeIcone.interactable = true;
    }

    public void OnClick()
    {
        UnityEngine.Debug.Log("Infos de l'icône : " + Infos);
    }

    public void CreerIcone(string type, string nomImage, string infos, int iD)
    {
        Type = type;
        Image = Resources.Load<Sprite>("Images/Icones/" + nomImage);
        Infos = infos;
        if (type == "Duo")
        {
            CarteSettings partenaire = GameManager.Instance.CartesSettings.Find(c => c.Id == iD);
            Infos = "Duo avec la carte " + partenaire.Prenom;
        }
        IDPartenaire = iD;

        // Mettre à jour l'image de l'icône
        GetComponent<UnityEngine.UI.Image>().sprite = Image;
    }

    // Update is called once per frame
    void Update()
    {

    }

}
