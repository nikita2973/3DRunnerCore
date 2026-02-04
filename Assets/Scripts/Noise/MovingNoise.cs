using System;
using DG.Tweening;
using UnityEngine;

public class MovingNoise : MonoBehaviour
{
  [SerializeField]private float _duration;
  [SerializeField]private float _targetLane;

  private void OnEnable()
  {
    transform.DOMoveZ(transform.position.z-700, _duration);
  }
}
