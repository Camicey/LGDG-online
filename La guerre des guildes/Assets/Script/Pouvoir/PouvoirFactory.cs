using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public enum PouvoirId
{
    Aucun = 0,
    Deplacer = 1,
    Soigner = 2,
    Retourner = 4,
    CouperLien = 5,
    CreerLien = 6,
    CouperPouvoir = 7,
    Cible = 8,
    RetournerDeck = 9,
    Retirer = 9,
    InvoquerDieu = 10,
    Echanger = 11,
    Ambivaler = 12,
    RevenirPioche = 13,
    CreerMur = 14,
    DetruireMur = 15,
    Piocher = 17,
    ChangerStats = 18,
}

public static class PouvoirFactory
{
    public static Pouvoir GetPouvoirById(PouvoirId idPouvoir, Carte carte)
    {
        switch (idPouvoir)
        {
            case PouvoirId.Aucun:
                break;
            case PouvoirId.Ambivaler:
                return new PwAmbivaler(carte.Id);
        }
        return null;
    }
}