using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPlatformBreaker : MonoBehaviour
{
    [SerializeField] List<GameObject> _platformParts;
    [SerializeField] float _breakDelay = 0.1f;
    [SerializeField] bool _triggered;

    public void BreakAll()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(BreakRoutine());
    }

    IEnumerator BreakRoutine()
    {
        // If it is just a single Sprite+Collider object
        if (_platformParts == null || _platformParts.Count == 0)
        {
             if (transform.childCount > 0)
             {
                 _platformParts = new List<GameObject>();
                 foreach(Transform child in transform)
                 {
                     _platformParts.Add(child.gameObject);
                 }
                 _platformParts.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
             }
             else
             {
                 // Single object case:
                 // We can't break it piece by piece easily if it's one sprite.
                 // We can try to shake it then disable it?
                 // Or just disable it immediately.
                 // Since the user asked for "breaking", and it's a single object, 
                 // the most reasonable thing is to just remove it so the boss falls.
                 
                 // Maybe play a particle effect here? (Not requested, but good practice)
                 
                 var colSingle = GetComponent<Collider2D>();
                 if (colSingle) colSingle.enabled = false;
                 
                 var rend = GetComponent<Renderer>();
                 if (rend) rend.enabled = false;
                 
                 yield break;
             }
        }

        foreach (var part in _platformParts)
        {
            if (part != null)
            {
                // Disable or Destroy
                // Part might be just a sprite object
                part.SetActive(false); // or Destroy(part);
            }
            yield return new WaitForSeconds(_breakDelay);
        }
        
        // Disable main collider if it exists on root
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        
        // Destroy self eventually
        Destroy(gameObject, 1f);
    }
}
