using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Carte", menuName = "Carte")]
public class CarteSettings : ScriptableObject
{
    public int Id;
    public string Prenom;
    public Sprite Image;
    public float PM;
    public int PV;
    public int PA;
    public int IdPouvoir;
    public string Pouvoir;
    public string ComplementPouvoir;
    public float CoutPouvoir;
    public string Particularite;
    public string Famille;
    public Sprite FamilleImage;
    public string Type;
    public Sprite TypeImage;
    public List<int> liens = new List<int>();

}