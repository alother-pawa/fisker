using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DestinationPoint : MonoBehaviour
{
    public static DestinationPoint Instance;
    // Every fish overlapping the point: one fish swimming out must not hide another still inside.
    private readonly List<Collider2D> fishInside = new List<Collider2D>();

    /// <summary>True while at least one fish is inside the point.</summary>
    public bool fishIn => Fish != null;

    /// <summary>The fish currently inside the point, or null.</summary>
    public Transform CaughtFish => Fish != null ? Fish.transform : null;

    private Collider2D Fish
    {
        get
        {
            fishInside.RemoveAll(f => f == null || !f.gameObject.activeInHierarchy);
            return fishInside.Count > 0 ? fishInside[fishInside.Count - 1] : null;
        }
    }
    
    // Start is called before the first frame update
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    private void Update()
    {

    }
    public void SetPosition(Vector3 position)
    {
        fishInside.Clear();
        gameObject.SetActive(true);
        transform.position = position;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Fish") && !fishInside.Contains(collision))
        {
            fishInside.Add(collision);
        }
    }
    //on trigger exit
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Fish"))
        {
            fishInside.Remove(collision);
        }
    }
    public void DespawnFish()
    {
        var fish = Fish;
        if (fish == null) return;
        fishInside.Remove(fish);
        fish.gameObject.SetActive(false);
    }
}
