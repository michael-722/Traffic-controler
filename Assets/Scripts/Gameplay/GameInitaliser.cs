using UnityEngine;

public class GameInitaliser : MonoBehaviour
{
    [SerializeField] private Relay relay = null;
    [SerializeField] private UIManager ui = null;

    private void Start()
    {
        
        SetUpListener();
    }

    private void SetUpListener()
    {   
        
    }

    //NOTE================
    /*
     need to look into making universal broadcast system 
    so stuff just listens rather then maually liking 

    is possible?
    pros vs cons?
     */
}