using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallSpear : MonoBehaviour
{
    SpearManager spearManager;
    DestinationPoint destinationPoint;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _moveSpeedMultiplier;
    private void Start()
    {
        spearManager = SpearManager.Instance;
        destinationPoint = DestinationPoint.Instance;
    }

    public void Spawn(Vector3 position)
    {
        gameObject.SetActive(true);
        transform.position = new Vector3(position.x, 7, 0);
    }
    public void Move(Transform target)
    {
        _moveSpeed = (10 + (7 - target.position.y) * 2) * _moveSpeedMultiplier;
        transform.position = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("DestinationPoint"))
        {
            gameObject.SetActive(false);

            Vector3 landing = collision.transform.position;
            if (destinationPoint.fishIn)
            {
                Transform caught = destinationPoint.CaughtFish;
                destinationPoint.DespawnFish();
                collision.gameObject.SetActive(false);
                spearManager.spearLanded = true;
                GameManager.Instance.Score++;
                FiskerEvents.RaiseFishCaught(landing, caught);
            }
            else
            {
                spearManager.spearLanded = true;
                collision.gameObject.SetActive(false);
                FiskerEvents.RaiseSpearMissed(landing);
            }
        }
    }
}
