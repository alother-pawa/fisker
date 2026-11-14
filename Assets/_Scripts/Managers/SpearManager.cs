using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpearManager : MonoBehaviour
{
    
    public static SpearManager Instance;
    [SerializeField] private BoxCollider2D _fishZone;
    [SerializeField] private BigSpear _bigSpear;
    [SerializeField] private DestinationPoint _destinationPoint;
    [SerializeField] private SmallSpear _smallSpear;
    private bool mouseDown = false;

    /// <summary>True when a new spear can be thrown (none is out).</summary>
    public bool CanThrow => !mouseDown;

    /// <summary>Whether a world point is inside the water a spear can be thrown at.</summary>
    public bool InThrowZone(Vector3 point) => _fishZone.bounds.Contains(new Vector3(point.x, point.y, _fishZone.bounds.center.z));

    /// <summary>The pointer in world space (z = 0), or null without a mouse.</summary>
    public static Vector3? PointerWorld()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return null;
        Vector3 point = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        point.z = 0;
        return point;
    }

    public bool spearLanded = false;
    private float timer = 0;
    private Vector3 mousePos = new Vector3(0, 0, 0);
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    private void Update()
    {
        var pointer = PointerWorld();
        if (pointer.HasValue && Mouse.current.leftButton.wasPressedThisFrame && !mouseDown)
        {
            if(GameManager.Instance.GameState != GameState.InGame) return;

            Vector3 mousePos = pointer.Value;
            if (!InThrowZone(mousePos))
            {
                return;
            }
        
            mouseDown = true;
            _bigSpear.gameObject.SetActive(true);
            _destinationPoint.SetPosition(mousePos);
            _smallSpear.Spawn(mousePos);
            FiskerEvents.RaiseSpearThrown(mousePos);
        }

        if (mouseDown)
        {
            _bigSpear.LauchSpear();
        }
        LaunchControl();
        if (spearLanded)
        {
            timer += Time.deltaTime;
            if (timer >= 0.25)
            {
                spearLanded = false;
                timer = 0;
                _bigSpear.ReLoad();
                mouseDown = false;
                FiskerEvents.RaiseSpearReloaded();
            }
        }
    }

    private void LaunchControl()
    {
        if (_bigSpear.OutOfCamera())
        {
            _bigSpear.gameObject.SetActive(false);
            _smallSpear.Move(_destinationPoint.transform);
        }
    }
}
