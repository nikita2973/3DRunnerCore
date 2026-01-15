using Input;
using UnityEngine;

public class PlayerController : MonoBehaviour,IActivity
{
    [SerializeField]private PlayerInput input;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input.Initialize();
        input.OnSwipeDown += Rift;
        input.OnSwipeUp += Jump;
        input.OnSwipeLeft += Left;
        input.OnSwipeRight += Right;
    }


    public void Jump()
    {
        throw new System.NotImplementedException();
    }

    public void Rift()
    {
        throw new System.NotImplementedException();
    }

    public void Left()
    {
        throw new System.NotImplementedException();
    }

    public void Right()
    {
        throw new System.NotImplementedException();
    }
}

public interface IActivity
{
    void Jump();
    void Rift();
    void Left();
    void Right();
}