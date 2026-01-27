using Input;
using UnityEngine;

public class PlayerController : MonoBehaviour, IActivity
{
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private CharacterController _characterController;
    
    // Параметри доріжок
    [SerializeField] private float _laneDistance = 3f; // Відстань між доріжками
    [SerializeField] private float _laneChangeSpeed = 10f; // Швидкість зміни доріжки
    private int _currentLane = 1; // 0 - ліва, 1 - центр, 2 - права
    private float _targetXPosition;
    
    // Параметри стрибка
    private float _verticalVelocity;
    private bool _isGrounded;
    
    // Параметри ковзання
    private bool _isSliding;
    private float _slideTimer;
    private float _slideDuration = 1f;
    private float _normalHeight;
    private float _slideHeight = 1f;
    
    private void Start()
    {
        _input.Initialize();
        _input.OnSwipeDown += Rift;
        _input.OnSwipeUp += Jump;
        _input.OnSwipeLeft += Left;
        _input.OnSwipeRight += Right;
        
        _targetXPosition = transform.position.x;
        _normalHeight = _characterController.height;
    }

    private void Update()
    {
        // Перевірка на землю
        _isGrounded = _characterController.isGrounded;
        
        // Рух вперед
        Vector3 moveVector = transform.forward * _playerData.Speed * Time.deltaTime;
        
        // Рух між доріжками (горизонтальний)
        float currentX = transform.position.x;
        float newX = Mathf.Lerp(currentX, _targetXPosition, Time.deltaTime * _laneChangeSpeed);
        float horizontalMove = newX - currentX;
        moveVector.x = horizontalMove;
        
        // Гравітація та стрибок
        if (_isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        
        moveVector.y = _verticalVelocity * Time.deltaTime;
        
        // Таймер ковзання
        if (_isSliding)
        {
            _slideTimer -= Time.deltaTime;
            if (_slideTimer <= 0)
            {
                StopSlide();
            }
        }
        
        // Застосування всього руху через CharacterController
        _characterController.Move(moveVector);
    }

    public void Jump()
    {
        if (_isGrounded && !_isSliding)
        {
            _verticalVelocity = _playerData.JumpForce;
        }
    }

    public void Rift()
    {
        if (_isGrounded && !_isSliding)
        {
            StartSlide();
        }
    }

    public void Left()
    {
        if (_currentLane > 0)
        {
            _currentLane--;
            UpdateTargetPosition();
        }
    }

    public void Right()
    {
        if (_currentLane < 2)
        {
            _currentLane++;
            UpdateTargetPosition();
        }
    }
    
    private void UpdateTargetPosition()
    {
        _targetXPosition = (_currentLane - 1) * _laneDistance;
    }
    
    private void StartSlide()
    {
        _isSliding = true;
        _slideTimer = _slideDuration;
        _characterController.height = _slideHeight;
        _characterController.center = new Vector3(0, _slideHeight / 2, 0);
    }
    
    private void StopSlide()
    {
        _isSliding = false;
        _characterController.height = _normalHeight;
        _characterController.center = new Vector3(0, _normalHeight / 2, 0);
    }
    
    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnSwipeDown -= Rift;
            _input.OnSwipeUp -= Jump;
            _input.OnSwipeLeft -= Left;
            _input.OnSwipeRight -= Right;
        }
    }
}

public interface IActivity
{
    void Jump();
    void Rift();
    void Left();
    void Right();
}

[System.Serializable]
public struct PlayerData
{
    [Tooltip("Швидкість руху вперед")]
    public float Speed;
    
    [Tooltip("Сила стрибка")]
    public float JumpForce;
}