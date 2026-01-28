using UnityEngine;

public class BlinkingDamage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private static readonly int OutlineThicknessId  = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorId      = Shader.PropertyToID("_OutlineColor");
    private static readonly int DisolveAmountId     = Shader.PropertyToID("_DisolveAmount");
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
