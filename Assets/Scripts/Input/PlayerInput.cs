
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    [System.Serializable]
    public class PlayerInput : IInput,IDisposable
    {
        private InputData _inputData;
        private Vector2 startPosition;
        private float startTime;

        [Header("Налаштування свайпу")] [SerializeField]
        private float minSwipeDistance = 100f;

        [SerializeField] private float maxSwipeTime = 1f;
        [SerializeField] private float directionThreshold = 0.9f;

        public System.Action OnSwipeLeft;
        public System.Action OnSwipeRight;
        public System.Action OnSwipeUp;
        public System.Action OnSwipeDown;

        private InputSystemActions _inputSystem;

        public void SetDataGetter(InputData inputData) => this._inputData = inputData;

        public void Initialize()
        {
            _inputSystem = new InputSystemActions();
            _inputSystem.Enable();

            // Підписка на події
            _inputSystem.PlayerTouch.TouchPress.started += OnTouchStarted;
            _inputSystem.PlayerTouch.TouchPress.canceled += OnTouchEnded;

            Debug.Log("✓ PlayerInput ініціалізовано");
            Debug.Log($"TouchPress enabled: {_inputSystem.PlayerTouch.TouchPress.enabled}");
            Debug.Log($"TouchPosition enabled: {_inputSystem.PlayerTouch.TouchPosition.enabled}");
        }

        private void OnTouchStarted(InputAction.CallbackContext context)
        {
            startPosition = _inputSystem.PlayerTouch.TouchPosition.ReadValue<Vector2>();
            startTime = Time.time;
            Debug.Log($"🟢 Touch Started at: {startPosition}");
        }

        private void OnTouchEnded(InputAction.CallbackContext context)
        {
            Vector2 endPosition = _inputSystem.PlayerTouch.TouchPosition.ReadValue<Vector2>();
            float swipeTime = Time.time - startTime;

            Debug.Log($"🔴 Touch Ended at: {endPosition}");
            Debug.Log($"⏱ Swipe Time: {swipeTime}s, Distance: {Vector2.Distance(startPosition, endPosition)}");

            if (swipeTime > maxSwipeTime)
            {
                Debug.Log("❌ Свайп занадто повільний");
                return;
            }

            DetectSwipe(startPosition, endPosition);
        }

        private void DetectSwipe(Vector2 start, Vector2 end)
        {
            Vector2 swipeVector = end - start;
            float distance = swipeVector.magnitude;

            Debug.Log($"📏 Swipe distance: {distance}, Min required: {minSwipeDistance}");

            if (distance < minSwipeDistance)
            {
                Debug.Log("❌ Свайп занадто короткий");
                return;
            }

            Vector2 direction = swipeVector.normalized;
            Debug.Log($"➡️ Swipe direction: {direction}");

            // Визначення напрямку
            if (Vector2.Dot(direction, Vector2.right) > directionThreshold)
            {
                Debug.Log("✅ Свайп вправо");
                OnSwipeRight?.Invoke();
            }
            else if (Vector2.Dot(direction, Vector2.left) > directionThreshold)
            {
                Debug.Log("✅ Свайп вліво");
                OnSwipeLeft?.Invoke();
            }
            else if (Vector2.Dot(direction, Vector2.up) > directionThreshold)
            {
                Debug.Log("✅ Свайп вгору");
                OnSwipeUp?.Invoke();
            }
            else if (Vector2.Dot(direction, Vector2.down) > directionThreshold)
            {
                Debug.Log("✅ Свайп вниз");
                OnSwipeDown?.Invoke();
            }
            else
            {
                Debug.Log($"❓ Діагональний свайп (threshold: {directionThreshold})");
            }
        }

        // Не забудь відписатися!
        public void Dispose()
        {
            if (_inputSystem != null)
            {
                _inputSystem.PlayerTouch.TouchPress.started -= OnTouchStarted;
                _inputSystem.PlayerTouch.TouchPress.canceled -= OnTouchEnded;
                _inputSystem.Disable();
                Debug.Log("PlayerInput disposed");
            }
        }
    }
}