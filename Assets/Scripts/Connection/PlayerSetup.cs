using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    public Behaviour[] componentsToDisable;
    public GameObject[] gameObjectsToDisable;
    public Image[] imagesToChangeColor;
    public GameObject canvas;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            for (int i = 0; i < componentsToDisable.Length; i++)
            {
                componentsToDisable[i].enabled = false;
            }
            for (int i = 0; i < gameObjectsToDisable.Length; i++)
            {
                gameObjectsToDisable[i].SetActive(false);
            }
            imagesToChangeColor[0].color = new Color32(225, 65, 66, 255);
            canvas.SetActive(false);
        }
        
    }
}
/*public override void OnStartLocalPlayer()
{
    // Ici tu gardes les composants actifs
    canvas.SetActive(true);
}

public override void OnStartClient()
{
    if (!isLocalPlayer)
    {
        foreach (var comp in componentsToDisable)
            comp.enabled = false;

        if (imagesToChangeColor.Length > 0)
            imagesToChangeColor[0].color = new Color32(225, 65, 66, 255);

        canvas.SetActive(false);
    }
}
*/