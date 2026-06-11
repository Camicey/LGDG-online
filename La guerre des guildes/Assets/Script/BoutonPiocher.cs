using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void OnClickPiocher()
    {
        if (PlayerManager.LocalPlayer != null) { PlayerManager.LocalPlayer.CmdPiocher(); }
    }
}