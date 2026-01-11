using Input;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]private PlayerInput input;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input.Initialize();
    }

  
}
